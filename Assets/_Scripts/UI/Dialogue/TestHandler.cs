using System;
using System.Collections;
using UnityEngine;
using UIElements;
using Helpers;   // <-- add this at the top with your other using lines

namespace Dialogue
{
    public class TestHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueTreeObjects dialogueTree;
        [SerializeField] private DialogueUI dialogueUI;

        // NEW: wire ScriptableObject events (APPEAR_KOALA, etc.)
        [Header("Scriptable Events")]
        [SerializeField] private ScriptableEvent[] scriptableEvents; 
        // ScriptableEvent = { string eventName; UnityEvent unityEvent; } (same type you already use)

        [Header("Keys")]
        [SerializeField] private string introStartKey   = "Intro";
        [SerializeField] private string afterExploreKey = "AfterExplore";
        [SerializeField] private string exploreSignal   = "GoExplore"; // node that triggers the Explore phase

        [Header("Explore Phase")]
        [SerializeField] private float exploreSeconds = 60f;
        [SerializeField] private float inputDebounceSeconds = 0.25f;

        [Header("UI Hint")]
        [SerializeField] private ExploreHintUI hintUI;
        [SerializeField] private string exploreStartText = "Explore the cabin on your left";
        [SerializeField] private string exploreEndText = "Time’s up — return to the wizard to learn more about this magical world!";


        // ---- internal ----
        private bool locked;
        private float debounceUntil;
        private Coroutine exploreCo;
        private Action _continueThunk;

        private void Awake()
        {
            // Isolate callbacks so multiple NPCs don’t share the same SO callback list
            if (dialogueTree != null) dialogueTree = Instantiate(dialogueTree);
        }

        private void Start()
        {
            // fresh callback slate
            dialogueTree.ResetCallbacks();
            dialogueTree.SetUpDialogueUnitsDict();

            // REGISTER your ScriptableEvents (APPEAR_KOALA, etc.)
            if (scriptableEvents != null)
            {
                foreach (var se in scriptableEvents)
                {
                    if (se == null || string.IsNullOrWhiteSpace(se.eventName)) continue;
                    dialogueTree.RegisterScriptableCallback(se.eventName, () => se.unityEvent?.Invoke());
                }
            }

            _continueThunk = () => StartCoroutine(CoNextFrame());
            dialogueTree.continueCallback    += _continueThunk;
            dialogueTree.endDialogueCallback += HandleEndDialogue;
        }

        private void OnDestroy()
        {
            if (dialogueTree != null)
            {
                if (_continueThunk != null) dialogueTree.continueCallback -= _continueThunk;
                dialogueTree.endDialogueCallback -= HandleEndDialogue;
            }
        }

        private IEnumerator CoNextFrame() { yield return null; Continue(); }

        // ==== PUBLIC ENTRY (from your Player's Interactor) ====
        public void TryStartConversationFromState(DialogueState dialogueState)
        {
            if (locked) return;
            if (Time.time < debounceUntil) return;
            if (dialogueState == null) return;

            dialogueTree.SetUpDialogueState(dialogueState);

            // ensure first-time default
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

            // 1) Fire on-enter scriptable event (e.g., APPEAR_KOALA)
            if (!string.IsNullOrWhiteSpace(unit.onEnterEventName))
            {
                // Safe invocation: only fires if registered
                dialogueTree.CallScriptableAction(unit.onEnterEventName);
            }

            // 2) If this is the Explore trigger node, hand off immediately
            if (!string.IsNullOrEmpty(unit.onEnterEventName) &&
                unit.onEnterEventName == exploreSignal)
            {
                HandoffToExplore(afterExploreKey, exploreSeconds);
                return; // do NOT render this node; we just closed the UI
            }

            // 3) Normal node
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
            // checkpoint for the next talk
            dialogueTree.GoToState(nextKey);

            // close UI -> Explore
            dialogueUI.EndDialogue();
            GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);

            // hard lock + debounce
            locked = true;
            debounceUntil = Time.time + inputDebounceSeconds;

            // timer
            if (exploreCo != null) StopCoroutine(exploreCo);
            exploreCo = StartCoroutine(CoExplore(seconds));

            //UIHint
            if (hintUI) hintUI.ShowExploreHint(seconds, exploreStartText, exploreEndText);


        }

        private IEnumerator CoExplore(float seconds)
        {
            float t = seconds;
            while (t > 0f) { t -= Time.deltaTime; yield return null; }
            locked = false; // allow re-talk
        }
    }
}
