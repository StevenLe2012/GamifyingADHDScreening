using System;
using Helpers;
using System.Collections.Generic;
//using Interactables;
using UIElements;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/*
 * This code handles how the dialogue Tree and UI interact with eachother as well as how callback functions work together with everything.
 */

namespace Dialogue
{
    public class DialogueHandler : MonoBehaviour
    {
        [SerializeField] private DialogueTreeObjects dialogueTree;
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private UnityEvent onDialogueEnd;  // callback for when dialogue ends
        [SerializeField] private ScriptableEvent[] scriptableEvents;
        // DialogueUI provider


        //Hami: Handoff to resume narrative after explore

        // --- NEW: hard gate for re-talk ---
        [SerializeField] private Interactor npcInteractor;   // drag the NPC’s Interactor here
        [SerializeField] private float inputDebounceSeconds = 0.25f;


        // === Add near other serialized fields ===
        [SerializeField] private float exploreDurationSeconds = 120f; // default 2 min
        [SerializeField] private UnityEngine.Events.UnityEvent onExploreBegin;    // show "Explore..." hint
        [SerializeField] private UnityEngine.Events.UnityEvent onExploreUnlocked; // show "Return to NPC..." hint

        // Fallback config (keep names in sync with DialogueUnits)
        [SerializeField] private string exploreHandoffSignal = "GoExplore"; // must match onEnterEventName on your last Intro node
        [SerializeField] private string exploreNextStateKey = "AfterExplore";
        [SerializeField] private float exploreSecondsFallback = 60f;

        // Internal flags
        private bool _pendingExploreHandoff;
        private float _blockInputUntilTime;
        private DialogueUnit _lastShownUnit; // <-- track what was actually displayed

        private bool _nextDialogueUnlocked = true;   // allow the first conversation
        private Coroutine _exploreCo;

        // Optional getter if NPC prompts want to check this gate
        public bool IsNextDialogueUnlocked => _nextDialogueUnlocked;

        // === Add this method ===
        public void HandoffToExploreAndSetState(string nextStateKey, float exploreSeconds = -1f)
        {
            // 1) checkpoint next state for this NPC
            dialogueTree.GoToState(nextStateKey);

            // 2) close the UI and enter Explore
            dialogueUI.EndDialogue();
            GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);

            // --- NEW: hard-disable re-interaction + debounce ---
            _blockInputUntilTime = Time.time + inputDebounceSeconds;
            // 3) gate re-talk until Explore is done
            _nextDialogueUnlocked = false;
            if (npcInteractor) npcInteractor.enabled = false;



            if (_exploreCo != null) StopCoroutine(_exploreCo);
            float dur = (exploreSeconds > 0f) ? exploreSeconds : exploreDurationSeconds;
            _exploreCo = StartCoroutine(CoExplorePhase(dur));
        }
        
        private IEnumerator CoExplorePhase(float seconds)
        {
            onExploreBegin?.Invoke();
            float t = seconds;
            while (t > 0f) { t -= Time.deltaTime; yield return null; }
            _nextDialogueUnlocked = true;
            Debug.Log("[Handler] Explore unlocked — re-talk allowed.");
            onExploreUnlocked?.Invoke();

            // --- NEW: re-enable interactor when explore ends ---
            if (npcInteractor) npcInteractor.enabled = true;
        }




        //Hami: Add State

        private DialogueState _lastDialogueState;



        //Hami: End change

        // initializes the dialogueTree and the scriptableEvents
        void Start()
        {
            dialogueTree.ResetCallbacks();
            foreach (var scriptableEvent in scriptableEvents)
            {
                dialogueTree.RegisterScriptableCallback(
                    scriptableEvent.eventName,
                    () => scriptableEvent.unityEvent.Invoke());
            }
            dialogueTree.SetUpDialogueUnitsDict();

            //Hami: Add a delay
            // Delay continue by one frame to avoid chaining through multiple auto-advance nodes
            dialogueTree.continueCallback += () => StartCoroutine(CoContinueNextFrame());



            

            //dialogueTree.continueCallback += dialogueUI.ContinueDialogue;
            //dialogueTree.continueCallback += ContinueDialogue;


            dialogueTree.endDialogueCallback += dialogueUI.EndDialogue;
            dialogueTree.endDialogueCallback += EndDialogue;
        }

        // // used when interacting with the NPC to get the dialogueState
        // public void OnInteract(Interactor interactor)
        // {
        //     Debug.Log("dialogue begin");
        //     var dialogueState = interactor.GetComponent<DialogueState>();
        //     if (dialogueState == null) return;

        //     dialogueTree.SetUpDialogueState(dialogueState);
            
        //     // my code for updating GameManager to show that you are in narrative mode
        //     GameManager.Instance.UpdateGameState(GameManager.GameState.Narrative);
            
        //     ContinueDialogue();
        // }
        //Hami: Update interact code
        public void OnInteract(Interactor interactor)
        {

            if (Time.time < _blockInputUntilTime) return; // prevent same-frame re-open

            if (GameManager.Instance.State == GameManager.GameState.Explore && !_nextDialogueUnlocked)
                return;


            if (GameManager.Instance.State == GameManager.GameState.Explore && !_nextDialogueUnlocked)
                return;

            var dialogueState = interactor.GetComponent<DialogueState>();
            if (dialogueState == null) return;

            dialogueTree.SetUpDialogueState(dialogueState);
            GameManager.Instance.UpdateGameState(GameManager.GameState.Narrative);

            // allow this conversation; the next handoff will close & gate again
            _nextDialogueUnlocked = true;

            // Start from the state we checkpointed:
            HandleDialogue(dialogueTree.GetNextDialogueUnit());
        }



        //Hami: End change





        

                // ----Hami- NEW: delayed continue ----
        private System.Collections.IEnumerator CoContinueNextFrame()
        {
            // wait 1 frame so GoToState() has applied and UI can render current node
            yield return null; // or: yield return new WaitForEndOfFrame();
            ContinueDialogue();
        }




        // gets the next dialogue
        private void ContinueDialogue()
        {
            HandleDialogue(dialogueTree.GetNextDialogueUnit());
        }

        // performs scriptable event when dialogue ends
        // private void EndDialogue()
        // {
        //     // my code for updating GameManager to show that you are in now in Explore mode
        //     // TODO: make it so that you can change it to any state after ending dialogue rather than just explore
        //     GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);
            
        //     onDialogueEnd.Invoke();
        // }

        //Hami: Updater End Dialogue
        private void EndDialogue()
        {
            // If a unit marked "GoExplore" was shown, force the handoff here.
            if (_pendingExploreHandoff ||
                (_lastShownUnit != null && _lastShownUnit.onEnterEventName == exploreHandoffSignal))
            {
                _pendingExploreHandoff = false;

                var nextKey = string.IsNullOrEmpty(exploreNextStateKey) ? "AfterExplore" : exploreNextStateKey;
                var secs    = (exploreSecondsFallback > 0f) ? exploreSecondsFallback : -1f;

                Debug.Log($"[Handler] Performing Explore handoff → next='{nextKey}' secs={secs}");
                HandoffToExploreAndSetState(nextKey, secs);

                _blockInputUntilTime = Time.time + 0.25f; // debounce same-frame re-open
                return; // prevent second Explore flip below
            }

            // Normal (non-handoff) close
            GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);
            _blockInputUntilTime = Time.time + 0.25f; // debounce anyway
            onDialogueEnd.Invoke();
        }
        //Hami: Change End




        // this function handles the dialogue through interacting with the dialogueUI to set up the UI and show the dialogue on screen.
        //private void HandleDialogue(DialogueUnit dialogueUnit)
        //{
        //    // Get the UI From the UI provider
        //    // Populate the dialogue UI
        //    dialogueUI.SetAudioObjects(dialogueUnit.audioObjects);  // my attempt to add in subtitles and voice
        //    dialogueUI.SetNextEvent(dialogueUnit.nextEventWithoutButton); // my attempt to add merging branches and ending dialogue w/o button
        //    dialogueUI.SetDialogueOptions(dialogueUnit.options);
        //    dialogueUI.ContinueDialogue();
        //}

        //Hami: Attempt to change line color

        private void HandleDialogue(DialogueUnit dialogueUnit)
        {
            if (dialogueUnit == null)
            {
                Debug.LogWarning("No DialogueUnit for current state.");
                dialogueTree.EndDialogue(); // optional: close cleanly
                return;
            }

            //Hami: Try to make another Character enter
            if (!string.IsNullOrWhiteSpace(dialogueUnit.onEnterEventName))
            {
                dialogueTree.CallScriptableAction(dialogueUnit.onEnterEventName);
            }

            // Track last unit actually shown
            _lastShownUnit = dialogueUnit;

            // If this unit signals "GoExplore", mark the handoff intent.
            // (Works even if UnityEvent not wired.)
            if (!string.IsNullOrWhiteSpace(dialogueUnit.onEnterEventName) &&
                dialogueUnit.onEnterEventName == exploreHandoffSignal)
            {
                _pendingExploreHandoff = true;
                Debug.Log($"[Handler] Marked pending explore handoff from unit '{dialogueUnit.requiredStateKey}'.");
            }


            if (!string.IsNullOrWhiteSpace(dialogueUnit.onEnterEventName) &&
                dialogueUnit.onEnterEventName == exploreHandoffSignal)
            {
                // Even if the UnityEvent path fails to invoke, we’ll complete the handoff in EndDialogue().
                _pendingExploreHandoff = true;
            }


            dialogueUI.BindDialogueUnit(dialogueUnit);  // <-- sets _currentCharacter, audio, options, next event
            dialogueUI.ContinueDialogue();
        }

    }


}