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
        [SerializeField] private string introStartKey = "Intro";
        [SerializeField] private string afterExploreKey = "AfterExplore";
        [SerializeField] private string exploreSignal = "GoExplore";

        [Header("Explore Phase")]
        [SerializeField] private float exploreSeconds = 60f;
        [SerializeField] private float inputDebounceSeconds = 0.25f;

        [Header("UI Hint")]
        [SerializeField] private ExploreHintUI hintUI;
        [SerializeField] private string exploreStartText = "Explore the cabin on your left";
        [SerializeField] private string exploreEndText = "Time’s up — return to the wizard to learn more about this magical world!";

        // ---- NEW: 1-frame delay control ----
        [Header("Dialogue Pacing")]
        [SerializeField] private int continueDelayFrames = 1; // Wait N frames before advancing
        private bool _advanceQueued;
        private Coroutine _advanceCo;

        // ---- internal ----
        private bool locked;
        private float debounceUntil;
        private Coroutine exploreCo;

        private void Awake()
        {
            if (dialogueTree != null) dialogueTree = Instantiate(dialogueTree);
        }

        private void Start()
        {
            dialogueTree.ResetCallbacks();
            dialogueTree.SetUpDialogueUnitsDict();

            // Register scriptable events (APPEAR_KOALA, etc.)
            if (scriptableEvents != null)
            {
                foreach (var se in scriptableEvents)
                {
                    if (se == null || string.IsNullOrWhiteSpace(se.eventName)) continue;
                    dialogueTree.RegisterScriptableCallback(se.eventName, () => se.unityEvent?.Invoke());
                }
            }

            // === NEW: Queue advance for next frame instead of instant chain ===
            dialogueTree.continueCallback += QueueAdvanceNextFrame;
            dialogueTree.endDialogueCallback += HandleEndDialogue;
        }

        private void OnDestroy()
        {
            if (dialogueTree != null)
            {
                dialogueTree.continueCallback -= QueueAdvanceNextFrame;
                dialogueTree.endDialogueCallback -= HandleEndDialogue;
            }
        }

        // === NEW: Queue / coalesced advance ===
        private void QueueAdvanceNextFrame()
        {
            if (_advanceQueued) return;
            _advanceQueued = true;

            if (_advanceCo != null) StopCoroutine(_advanceCo);
            _advanceCo = StartCoroutine(CoAdvanceAfterFrames(continueDelayFrames));
        }

        private IEnumerator CoAdvanceAfterFrames(int frames)
        {
            for (int i = 0; i < Mathf.Max(1, frames); i++)
                yield return null;

            _advanceQueued = false;
            _advanceCo = null;
            Continue();
        }

        // ==== PUBLIC ENTRY ====
        public void TryStartConversationFromState(DialogueState dialogueState)
        {
            if (locked) return;
            if (Time.time < debounceUntil) return;
            if (dialogueState == null) return;

            dialogueTree.SetUpDialogueState(dialogueState);

            // Ensure default intro state
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

            // Fire on-enter event (e.g. APPEAR_KOALA)
            if (!string.IsNullOrWhiteSpace(unit.onEnterEventName))
            {
                dialogueTree.CallScriptableAction(unit.onEnterEventName);
            }

            // Explore trigger
            if (!string.IsNullOrEmpty(unit.onEnterEventName) &&
                unit.onEnterEventName == exploreSignal)
            {
                HandoffToExplore(afterExploreKey, exploreSeconds);
                return;
            }

            // Normal line
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
            while (t > 0f)
            {
                t -= Time.deltaTime;
                yield return null;
            }
            locked = false; // allow re-talk
        }
    }
}
