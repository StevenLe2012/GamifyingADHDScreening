using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UIElements;
using Helpers;

namespace Dialogue
{
    public class TestHandler : MonoBehaviour
    {
        public static TestHandler I { get; private set; }

        // Talking means: started and not ending/locked
        public bool IsTalking => _talking && !_ending && !locked;

        [Header("References")]
        [SerializeField] private DialogueTreeObjects dialogueTree;
        [SerializeField] private DialogueUI dialogueUI;

        [Header("Dialogue UI Roots")]
        [Tooltip("Drag your DialogueOptions root here.")]
        [SerializeField] private GameObject dialogueOptionsRoot;

        [Tooltip("Optional: drag SubtitleText or the Dialogue panel root here.")]
        [SerializeField] private GameObject dialogueSubtitleRoot;

        [Header("Scriptable Events")]
        [SerializeField] private ScriptableEvent[] scriptableEvents;

        [Header("Keys")]
        [SerializeField] private string introStartKey = "Intro";
        [SerializeField] private string afterExploreKey = "AfterExplore";
        [SerializeField] private string startMoxoKey = "StartMOXO";
        [SerializeField] private string showIslandPickerSignal = "ShowIslandPicker";

        [Header("Signals (DialogueUnit.onEnterEventName)")]
        [SerializeField] private string exploreSignal = "GoExplore";
        [SerializeField] private string startMoxoSignal = "GoStartMOXO";

        [Header("Explore Phase")]
        [SerializeField] private float exploreSeconds = 60f;
        [SerializeField] private float inputDebounceSeconds = 0.25f;

        [Header("UI Hint")]
        [SerializeField] private ExploreHintUI hintUI;
        [SerializeField] private string exploreStartText = "Explore the cabin on your left";
        [SerializeField] private string exploreEndText = "Time’s up — return to the wizard to learn more about this magical world!";
        [SerializeField] private string startMoxoText = "Turn right and press the button to travel to the next island.";
        [SerializeField] private string dialogueOptionsHintText = "Use the arrow keys to choose an option, then press Space to confirm.";

        [Header("After Game")]
        [SerializeField] private string afterGameKey = "AfterGame";
        [SerializeField] private string afterGameText = "Press A to talk to the wizard.";

        // ---- internal ----
        private bool locked;
        private Coroutine exploreCo;

        private float debounceUntilUnscaled;

        private bool _talking;
        private bool _ending;

        private int _lastContinueFrame = -999;
        private int _lastOptionConfirmFrame = -999;

        private Coroutine _confirmEndCo;

        private DialogueUnit _currentUnit;

        private const float kSuppressSeconds = 0.25f;

        // ---- runtime ContinueFromOption support ----
        private MethodInfo _continueFromOption;
        private bool _optionAdvanceInFlight = false;
        private int _lastSelectedOptionIndex = -1;
        private DialogueOption _lastSelectedOptionObj = null;

        private bool CurrentUnitHasOptions()
        {
            return _currentUnit != null && _currentUnit.options != null && _currentUnit.options.Length > 0;
        }

        private void Awake()
        {
            I = this;
            if (dialogueTree != null) dialogueTree = Instantiate(dialogueTree);
        }

        private void OnDisable()
        {
            UIInputFocus.Pop(this);
        }

        private void OnDestroy()
        {
            UIInputFocus.Pop(this);
            if (I == this) I = null;
        }

        private void Start()
        {
            EkonnAnimBus.EnsureStandingIdle();

            dialogueTree.ResetCallbacks();
            dialogueTree.SetUpDialogueUnitsDict();

            _continueFromOption = dialogueTree.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => string.Equals(m.Name, "ContinueFromOption", StringComparison.OrdinalIgnoreCase));

            if (_continueFromOption != null)
                Debug.Log($"[TestHandler] Found ContinueFromOption signature: ({string.Join(", ", _continueFromOption.GetParameters().Select(p => p.ParameterType.Name))})");
            else
                Debug.LogWarning("[TestHandler] ContinueFromOption NOT found on DialogueTreeObjects. Options may repeat.");

            if (scriptableEvents != null)
            {
                foreach (var se in scriptableEvents)
                {
                    if (se == null || string.IsNullOrWhiteSpace(se.eventName)) continue;
                    dialogueTree.RegisterScriptableCallback(se.eventName, () => se.unityEvent?.Invoke());
                }
            }

            dialogueTree.continueCallback += RequestContinueNextFrame;

            dialogueTree.endDialogueCallback += dialogueUI.EndDialogue;
            dialogueTree.endDialogueCallback += HandleEndDialogue;
        }

        public void StartIntroDialogue(DialogueState dialogueState)
        {
            locked = false;
            debounceUntilUnscaled = 0f;

            _talking = false;
            _ending = false;

            _lastContinueFrame = -999;
            _lastOptionConfirmFrame = -999;

            _currentUnit = null;
            _optionAdvanceInFlight = false;

            // ✅ stop any leftover invokes from previous run
            dialogueUI?.StopAutoAdvance();

            TryStartConversationFromState(dialogueState);
        }

        // ---------- Continue guard ----------
        private void RequestContinueNextFrame()
        {
            if (_ending) return;
            if (!_talking) return;
            if (locked) return;
            if (_optionAdvanceInFlight) return;

            // ✅ if current unit has options, NEVER continue (even if root hidden)
            if (CurrentUnitHasOptions()) return;

            if (Time.unscaledTime < debounceUntilUnscaled) return;

            if (Time.frameCount == _lastContinueFrame) return;
            _lastContinueFrame = Time.frameCount;

            StartCoroutine(CoContinueNextFrame_Guarded());
        }

        private IEnumerator CoContinueNextFrame_Guarded()
        {
            // ✅ still block if options exist
            if (CurrentUnitHasOptions())
                yield break;

            yield return null;

            if (_ending || locked) yield break;
            if (_optionAdvanceInFlight) yield break;

            if (dialogueOptionsRoot != null && dialogueOptionsRoot.activeInHierarchy)
                yield break;

            ContinueInternal();
        }

        // ============ START CONVERSATION ============
        public void TryStartConversationFromState(DialogueState dialogueState)
        {
            if (locked) return;
            if (Time.unscaledTime < debounceUntilUnscaled) return;
            if (dialogueState == null) return;

            _ending = false;
            _talking = true;

            _lastContinueFrame = -999;
            _lastOptionConfirmFrame = -999;

            _currentUnit = null;
            _optionAdvanceInFlight = false;

            // ✅ clear any pending dialogue UI invokes
            dialogueUI?.StopAutoAdvance();

            UIInputFocus.Push(this);
            UIInputFocus.SuppressForSeconds(kSuppressSeconds);

            dialogueTree.SetUpDialogueState(dialogueState);

            if (!dialogueTree.dialogueState.stateDict.ContainsKey(dialogueTree.npcName) ||
                string.IsNullOrEmpty(dialogueTree.dialogueState.stateDict[dialogueTree.npcName]))
            {
                dialogueTree.GoToState(introStartKey);
            }

            GameManager.Instance.UpdateGameState(GameManager.GameState.Narrative);

            string islandId = IslandTravelManager.I && IslandTravelManager.I.CurrentIsland != null ? IslandTravelManager.I.CurrentIsland.islandId : "";
            MoxoCPT.LoggingDialogueChoices.BeginConversation(dialogueTree.npcName, islandId, "Narrative");


            var first = dialogueTree.GetNextDialogueUnit();
            Handle(first);
        }

        // ============ OPTION SELECTION ============
        public void SelectOption(int optionIndex)
        {
            if (_ending) return;
            if (!_talking) return;
            if (locked) return;
            if (_optionAdvanceInFlight) return;

            // same-frame guard
            if (Time.frameCount == _lastOptionConfirmFrame) return;
            _lastOptionConfirmFrame = Time.frameCount;

            // also block continue from same press
            _lastContinueFrame = Time.frameCount;

            if (_currentUnit == null)
            {
                Debug.LogWarning("[TestHandler] SelectOption: _currentUnit is null (no unit bound yet).");
                return;
            }

            var opts = _currentUnit.options;
            if (opts == null || opts.Length == 0)
            {
                Debug.LogWarning("[TestHandler] SelectOption: current unit has no options.");
                return;
            }

            if (optionIndex < 0 || optionIndex >= opts.Length)
            {
                Debug.LogWarning($"[TestHandler] SelectOption: index {optionIndex} out of range (0..{opts.Length - 1}).");
                return;
            }

            Debug.Log($"[TestHandler] SelectOption({optionIndex}) -> invoke UnityEvent then ContinueFromOption.");

            hintUI?.HideDialogueOptionsHint();

            _optionAdvanceInFlight = true;

            _lastSelectedOptionIndex = optionIndex;
            _lastSelectedOptionObj = opts[optionIndex];

            // If this option specifies a next state key, jump there immediately.
            if (_lastSelectedOptionObj != null &&
                !string.IsNullOrWhiteSpace(_lastSelectedOptionObj.nextStateKey))
            {
                dialogueTree.GoToState(_lastSelectedOptionObj.nextStateKey);
            }

            // ✅ cancel any “end confirm” that might fire next frame
            if (_confirmEndCo != null) { StopCoroutine(_confirmEndCo); _confirmEndCo = null; }

            // ✅ HARD STOP any dialogueUI Invoke chain BEFORE we advance
            dialogueUI?.StopAutoAdvance();

            // Hide options immediately (prevents double-confirm)
            if (dialogueOptionsRoot != null && dialogueOptionsRoot.activeSelf)
                dialogueOptionsRoot.SetActive(false);

            UIInputFocus.SuppressForSeconds(0.12f);

            try
            {
                opts[optionIndex].actionToTrigger?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestHandler] Option UnityEvent threw exception: {e}");
            }

            StartCoroutine(CoAdvanceAfterOption());
        }

        // private IEnumerator CoAdvanceAfterOption()
        // {
        //     yield return null; // let state changes apply

        //     if (_ending || locked)
        //     {
        //         _optionAdvanceInFlight = false;
        //         yield break;
        //     }

        //     TryInvokeContinueFromOption();

        //     yield return null; // some trees apply next unit one frame later

        //     if (_ending || locked)
        //     {
        //         _optionAdvanceInFlight = false;
        //         yield break;
        //     }

        //     var next = dialogueTree.GetNextDialogueUnit();
        //     _optionAdvanceInFlight = false;

        //     Handle(next);
        // }

        private IEnumerator CoAdvanceAfterOption()
        {
            // Let any state changes from the option's UnityEvent/nextStateKey apply.
            yield return null;

            if (_ending || locked)
            {
                _optionAdvanceInFlight = false;
                yield break;
            }

            // Nudge the tree via ContinueFromOption (if it exists), but it won't auto-advance while _optionAdvanceInFlight is true.
            TryInvokeContinueFromOption();

            // Some trees may apply state changes / callbacks one frame later.
            yield return null;

            if (_ending || locked)
            {
                _optionAdvanceInFlight = false;
                yield break;
            }

            var next = dialogueTree.GetNextDialogueUnit();
            _optionAdvanceInFlight = false;

            Handle(next);
        }


        private void TryInvokeContinueFromOption()
        {
            if (_continueFromOption == null) return;

            try
            {
                var ps = _continueFromOption.GetParameters();

                if (ps.Length == 0)
                {
                    _continueFromOption.Invoke(dialogueTree, null);
                    return;
                }

                if (ps.Length == 1 && ps[0].ParameterType == typeof(int))
                {
                    _continueFromOption.Invoke(dialogueTree, new object[] { _lastSelectedOptionIndex });
                    return;
                }

                if (ps.Length == 1)
                {
                    object arg = null;

                    if (_lastSelectedOptionObj != null && ps[0].ParameterType.IsInstanceOfType(_lastSelectedOptionObj))
                        arg = _lastSelectedOptionObj;
                    else if (ps[0].ParameterType == typeof(object))
                        arg = _lastSelectedOptionObj;

                    if (arg != null)
                    {
                        _continueFromOption.Invoke(dialogueTree, new object[] { arg });
                        return;
                    }
                }

                Debug.LogWarning($"[TestHandler] ContinueFromOption unsupported signature: ({string.Join(", ", ps.Select(p => p.ParameterType.Name))})");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TestHandler] Exception invoking ContinueFromOption: {e.Message}");
            }
        }

        // ============ CORE FLOW ============
        private void Handle(DialogueUnit unit)
        {
            if (_ending || locked) return;

            // ✅ stop any pending invokes whenever we bind a new unit
            dialogueUI?.StopAutoAdvance();

            _currentUnit = unit;

            string islandId = IslandTravelManager.I && IslandTravelManager.I.CurrentIsland != null ? IslandTravelManager.I.CurrentIsland.islandId : "";

            string requiredKey = unit != null ? (unit.requiredStateKey ?? "") : "";
            string onEnter = unit != null ? (unit.onEnterEventName ?? "") : "";

            MoxoCPT.LoggingDialogueChoices.SetDialogueContext(dialogueTree.npcName, requiredKey, onEnter, islandId, "Narrative");


            if (unit == null)
            {
                BeginConfirmEnd();
                return;
            }

            if (!string.IsNullOrWhiteSpace(unit.onEnterEventName))
                dialogueTree.CallScriptableAction(unit.onEnterEventName);

            if (!string.IsNullOrEmpty(unit.onEnterEventName))
            {
                if (unit.onEnterEventName == exploreSignal)
                {
                    HandoffToExplore(afterExploreKey, exploreSeconds);
                    return;
                }
                if (unit.onEnterEventName == startMoxoSignal)
                {
                    HandoffToStartMOXO(startMoxoKey, startMoxoText);
                    return;
                }
                if (unit.onEnterEventName == showIslandPickerSignal)
                {
                    EkonnAnimBus.SitDown();
                    dialogueUI.EndDialogue();

                    if (dialogueOptionsRoot != null && dialogueOptionsRoot.activeSelf)
                        dialogueOptionsRoot.SetActive(false);

                    UIInputFocus.Pop(this);
                    UIInputFocus.SuppressForSeconds(kSuppressSeconds);

                    GameManager.Instance.ForceUpdateGameState(GameManager.GameState.Explore);

                    var picker = IslandSelectionUI.I ?? UnityEngine.Object.FindObjectOfType<IslandSelectionUI>(true);
                    if (picker != null) picker.ShowRemaining();

                    debounceUntilUnscaled = Time.unscaledTime + inputDebounceSeconds;
                    _talking = false;
                    return;
                }
            }

            dialogueUI.BindDialogueUnit(unit);
            dialogueUI.ContinueDialogue();

            if (unit.options != null && unit.options.Length > 0)
                hintUI?.ShowDialogueOptionsHint(dialogueOptionsHintText);
            else
                hintUI?.HideDialogueOptionsHint();
        }

        private void BeginConfirmEnd()
        {
            if (_confirmEndCo != null) return;
            _confirmEndCo = StartCoroutine(CoConfirmEndNextFrame());
        }

        private IEnumerator CoConfirmEndNextFrame()
        {
            yield return null;

            _confirmEndCo = null;
            if (_ending || locked) yield break;
            if (_optionAdvanceInFlight) yield break;

            // if options exist, don't auto-end
            if (CurrentUnitHasOptions())
                yield break;

            if (dialogueOptionsRoot != null && dialogueOptionsRoot.activeInHierarchy)
                yield break;

            if (dialogueSubtitleRoot != null && dialogueSubtitleRoot.activeInHierarchy)
                yield break;

            var next = dialogueTree.GetNextDialogueUnit();
            if (next != null)
            {
                Handle(next);
                yield break;
            }

            End();
        }

        private void ContinueInternal()
        {
            if (_ending) return;
            Handle(dialogueTree.GetNextDialogueUnit());
        }

        private void HandleEndDialogue() => End();

        private void End()
        {
            if (_ending) return;
            _ending = true;
            _talking = false;

            if (_confirmEndCo != null) { StopCoroutine(_confirmEndCo); _confirmEndCo = null; }

            // ✅ stop invokes so nothing "restarts" a line after end
            dialogueUI?.StopAutoAdvance();

            hintUI?.HideDialogueOptionsHint();

            if (dialogueOptionsRoot != null && dialogueOptionsRoot.activeSelf)
                dialogueOptionsRoot.SetActive(false);

            UIInputFocus.Pop(this);
            UIInputFocus.SuppressForSeconds(kSuppressSeconds);

            EkonnAnimBus.SitDown();
            GameManager.Instance.ForceUpdateGameState(GameManager.GameState.Explore);

            debounceUntilUnscaled = Time.unscaledTime + inputDebounceSeconds;
        }

        // ============ EXPLORE PHASE ============
        private void HandoffToExplore(string nextKey, float seconds)
        {
            dialogueTree.GoToState(nextKey);

            EkonnAnimBus.SitDown();
            dialogueUI.EndDialogue();
            dialogueUI?.StopAutoAdvance();

            if (dialogueOptionsRoot != null && dialogueOptionsRoot.activeSelf)
                dialogueOptionsRoot.SetActive(false);

            UIInputFocus.Pop(this);
            UIInputFocus.SuppressForSeconds(kSuppressSeconds);

            GameManager.Instance.ForceUpdateGameState(GameManager.GameState.Explore);

            locked = true;
            debounceUntilUnscaled = Time.unscaledTime + inputDebounceSeconds;

            if (exploreCo != null) StopCoroutine(exploreCo);
            exploreCo = StartCoroutine(CoExplore(seconds));

            if (hintUI) hintUI.ShowExploreHint(seconds, exploreStartText, exploreEndText);
        }

        private IEnumerator CoExplore(float seconds)
        {
            float t = seconds;
            while (t > 0f) { t -= Time.deltaTime; yield return null; }
            locked = false;
        }

        private void HandoffToStartMOXO(string nextKey, string instructionText)
        {
            dialogueTree.GoToState(nextKey);

            EkonnAnimBus.SitDown();
            dialogueUI.EndDialogue();
            dialogueUI?.StopAutoAdvance();

            if (dialogueOptionsRoot != null && dialogueOptionsRoot.activeSelf)
                dialogueOptionsRoot.SetActive(false);

            UIInputFocus.Pop(this);
            UIInputFocus.SuppressForSeconds(kSuppressSeconds);

            GameManager.Instance.ForceUpdateGameState(GameManager.GameState.Explore);

            debounceUntilUnscaled = Time.unscaledTime + inputDebounceSeconds;

            if (hintUI != null)
                hintUI.ShowExploreHint(0f, "", instructionText);
        }

        public void HandoffToAfterGame(string nextKey, string instructionText)
        {
            dialogueTree.GoToState(nextKey);

            dialogueUI.EndDialogue();
            dialogueUI?.StopAutoAdvance();

            if (dialogueOptionsRoot != null && dialogueOptionsRoot.activeSelf)
                dialogueOptionsRoot.SetActive(false);

            UIInputFocus.Pop(this);
            UIInputFocus.SuppressForSeconds(kSuppressSeconds);

            GameManager.Instance.ForceUpdateGameState(GameManager.GameState.Explore);

            debounceUntilUnscaled = Time.unscaledTime + inputDebounceSeconds;

            if (hintUI != null)
                hintUI.ShowExploreHint(0f, "", instructionText);
        }
    }
}
