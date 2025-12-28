using System;
using System.Collections;
using UnityEngine;
using UIElements;
using Helpers;

namespace Dialogue
{
    public class TestHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueTreeObjects dialogueTree;
        [SerializeField] private DialogueUI dialogueUI;

        [Header("Scriptable Events")]
        [SerializeField] private ScriptableEvent[] scriptableEvents;

        [Header("Keys")]
        [SerializeField] private string introStartKey    = "Intro";
        [SerializeField] private string afterExploreKey  = "AfterExplore";
        [SerializeField] private string startMoxoKey     = "StartMOXO";
        // Add Picker
        [SerializeField] private string showIslandPickerSignal = "ShowIslandPicker";      

        [Header("Signals (DialogueUnit.onEnterEventName)")]
        [SerializeField] private string exploreSignal    = "GoExplore";
        [SerializeField] private string startMoxoSignal  = "GoStartMOXO";    

        [Header("Explore Phase")]
        [SerializeField] private float exploreSeconds = 60f;
        [SerializeField] private float inputDebounceSeconds = 0.25f;

        [Header("UI Hint")]
        [SerializeField] private ExploreHintUI hintUI;
        [SerializeField] private string exploreStartText = "Explore the cabin on your left";
        [SerializeField] private string exploreEndText   = "Time’s up — return to the wizard to learn more about this magical world!";
        [SerializeField] private string startMoxoText    = "Turn right and press the button to travel to the next island."; 

        [Header("After Game")]
        [SerializeField] private string afterGameKey  = "AfterGame";                
        [SerializeField] private string afterGameText = "Press A to talk to the wizard."; 

        // ---- internal ----
        private bool locked;
        private float debounceUntil;
        private Coroutine exploreCo;

        private void Awake()
        {
            // make sure each NPC has its own dialogueTree instance
            if (dialogueTree != null) dialogueTree = Instantiate(dialogueTree);
        }

        private void Start()
        {
            dialogueTree.ResetCallbacks();
            dialogueTree.SetUpDialogueUnitsDict();

            // register scriptable events
            if (scriptableEvents != null)
            {
                foreach (var se in scriptableEvents)
                {
                    if (se == null || string.IsNullOrWhiteSpace(se.eventName)) continue;
                    dialogueTree.RegisterScriptableCallback(se.eventName, () => se.unityEvent?.Invoke());
                }
            }

            // delay continue by one frame (prevents lines overlapping)
            dialogueTree.continueCallback += () => StartCoroutine(CoContinueNextFrame());

            // end dialogue bindings
            dialogueTree.endDialogueCallback += dialogueUI.EndDialogue;
            dialogueTree.endDialogueCallback += HandleEndDialogue;
        }

        private void OnDestroy()
        {
            if (dialogueTree != null)
            {
                dialogueTree.continueCallback    -= () => StartCoroutine(CoContinueNextFrame());
                dialogueTree.endDialogueCallback -= dialogueUI.EndDialogue;
                dialogueTree.endDialogueCallback -= HandleEndDialogue;
            }
        }

        private IEnumerator CoContinueNextFrame()
        {
            yield return null;  // wait one frame so UI can update cleanly
            Continue();
        }

        // ==== PUBLIC ENTRY ====
        public void TryStartConversationFromState(DialogueState dialogueState)
        {
            if (locked) return;
            if (Time.time < debounceUntil) return;
            if (dialogueState == null) return;

            dialogueTree.SetUpDialogueState(dialogueState);

            // ensure default start
            if (!dialogueTree.dialogueState.stateDict.ContainsKey(dialogueTree.npcName) ||
                string.IsNullOrEmpty(dialogueTree.dialogueState.stateDict[dialogueTree.npcName]))
            {
                dialogueTree.GoToState(introStartKey);
            }

            GameManager.Instance.UpdateGameState(GameManager.GameState.Narrative);
            Handle(dialogueTree.GetNextDialogueUnit());
        }

        // ==== CORE FLOW ====
        private void Handle(DialogueUnit unit)
        {
            if (locked) return;
            if (unit == null) { End(); return; }

            // fire on-enter scriptable event if any
            if (!string.IsNullOrWhiteSpace(unit.onEnterEventName))
            {
                dialogueTree.CallScriptableAction(unit.onEnterEventName);
            }

            // Signal: hand off to Explore
            if (!string.IsNullOrEmpty(unit.onEnterEventName) && unit.onEnterEventName == exploreSignal)
            {
                HandoffToExplore(afterExploreKey, exploreSeconds);
                return; // stop showing this node
            }

            // Signal: show StartMOXO instruction (no timer; show end text + button)
            if (!string.IsNullOrEmpty(unit.onEnterEventName) && unit.onEnterEventName == startMoxoSignal)
            {
                HandoffToStartMOXO(startMoxoKey, startMoxoText);
                return; // stop showing this node
            }

            //Dec 26: Add Picker:
            if (!string.IsNullOrEmpty(unit.onEnterEventName) &&
                unit.onEnterEventName == showIslandPickerSignal)
            {
                dialogueUI.EndDialogue();
                GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);

                // Show picker
                var picker = IslandSelectionUI.I ?? FindObjectOfType<IslandSelectionUI>(true);
                if (picker != null) picker.ShowRemaining();

                // Small debounce so the NPC can’t be re-triggered immediately
                debounceUntil = Time.time + inputDebounceSeconds;
                return;
            }
            // END CHANGE

            // display dialogue normally
            dialogueUI.BindDialogueUnit(unit);
            dialogueUI.ContinueDialogue();
        }

        private void Continue() => Handle(dialogueTree.GetNextDialogueUnit());

        private void HandleEndDialogue() => End();

        private void End()
        {
            GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);
            debounceUntil = Time.time + inputDebounceSeconds;
        }

        // ==== EXPLORE PHASE ====
        private void HandoffToExplore(string nextKey, float seconds)
        {
            dialogueTree.GoToState(nextKey);

            dialogueUI.EndDialogue();
            GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);

            locked = true;
            debounceUntil = Time.time + inputDebounceSeconds;

            if (exploreCo != null) StopCoroutine(exploreCo);
            exploreCo = StartCoroutine(CoExplore(seconds));

            if (hintUI) hintUI.ShowExploreHint(seconds, exploreStartText, exploreEndText);
        }

        private IEnumerator CoExplore(float seconds)
        {
            float t = seconds;
            while (t > 0f) { t -= Time.deltaTime; yield return null; }
            locked = false; // re-enable talking
        }

        // ==== START MOXO HANDOFF (no timer) ====
        private void HandoffToStartMOXO(string nextKey, string instructionText)
        {
            // Move state forward so next time we talk we continue from StartMOXO
            dialogueTree.GoToState(nextKey);

            // Close dialogue UI; keep world interactive
            dialogueUI.EndDialogue();
            GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);

            // Debounce NPC re-trigger for a short moment
            debounceUntil = Time.time + inputDebounceSeconds;

            // Show instruction as an immediate "end" message with Start button (A/Space)
            if (hintUI != null)
            {
                // duration=0 → your ExploreHintUI immediately shows end text + Start button and waits for A / Space
                hintUI.ShowExploreHint(0f, "", instructionText);
            }
        }
        
        public void HandoffToAfterGame(string nextKey, string instructionText)
        {
            // move dialogue state forward so next talk starts at AfterGame
            dialogueTree.GoToState(nextKey);

            // close dialogue UI (safe even if not active) and keep world interactive
            dialogueUI.EndDialogue();
            GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);

            // small debounce so NPC isn't re-triggered instantly
            debounceUntil = Time.time + inputDebounceSeconds;

            // show the instruction immediately, with Start button (A/Space)
            if (hintUI != null)
            {
                // duration = 0 → shows end text + Start button and waits for A / Space
                hintUI.ShowExploreHint(0f, "", instructionText);
            }
        }


    }
}
