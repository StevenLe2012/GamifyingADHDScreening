using System;
using Helpers;
using System.Collections.Generic;
//using Interactables;
using UIElements;
using UnityEngine;
using UnityEngine.Events;

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
            dialogueTree.continueCallback += dialogueUI.ContinueDialogue;
            dialogueTree.continueCallback += ContinueDialogue;
            dialogueTree.endDialogueCallback += dialogueUI.EndDialogue;
            dialogueTree.endDialogueCallback += EndDialogue;
        }

        // used when interacting with the NPC to get the dialogueState
        public void OnInteract(Interactor interactor)
        {
            var dialogueState = interactor.GetComponent<DialogueState>();
            if (dialogueState == null) return;

            dialogueTree.SetUpDialogueState(dialogueState);
            ContinueDialogue();
        }

        // gets the next dialogue
        private void ContinueDialogue()
        {
            HandleDialogue(dialogueTree.GetNextDialogueUnit());
        }

        // performs scriptable event when dialogue ends
        private void EndDialogue()
        {
            onDialogueEnd.Invoke();
        }

        // this function handles the dialogue through interacting with the dialogueUI to set up the UI and show the dialogue on screen.
        private void HandleDialogue(DialogueUnit dialogueUnit)
        {
            // Get the UI From the UI provider
            // Populate the dialogue UI
            dialogueUI.SetAudioObjects(dialogueUnit.audioObjects);  // my attempt to add in subtitles and voice
            
            
            //my attempt
            if (!string.IsNullOrEmpty(dialogueUnit.goToState))
            {
                dialogueUI.SetNextState(dialogueUnit.goToState);
                dialogueUI.SetNextEvent(dialogueUnit.nextEventWithoutButton);
                //dialogueTree.ResetState(dialogueUnit.goToState);
                dialogueUI.SetDialogueOptions(dialogueUnit.options, dialogueTree.defaultOption);
                dialogueUI.ContinueDialogue();
            }
            else if (!string.IsNullOrEmpty(dialogueUnit.endDialogueAndSetState))
            {
                dialogueUI.SetNextState(dialogueUnit.endDialogueAndSetState);
                dialogueUI.SetNextEvent(dialogueUnit.nextEventWithoutButton);
                //dialogueTree.ResetState(dialogueUnit.endDialogueAndSetState);
                dialogueUI.SetDialogueOptions(dialogueUnit.options, dialogueTree.defaultOption);
                dialogueUI.ContinueDialogue();
                //dialogueUI.gameObject.SetActive(true);
                //Invoke("EndDialogue", dialogueUI._curAudioObject.clip.length); //SOMETIMES SKIPS (BUGGY?)
                //dialogueTree.EndDialogue();
                /*
                 * TLDR: I can't do anything here. i need ot change dialogue UI to change what happens bruh.
                 */
            }
            else
            {
                dialogueUI.SetDialogueOptions(dialogueUnit.options, dialogueTree.defaultOption);
                dialogueUI.ContinueDialogue();
            }
            // set in inspector a unity event for "do automatically next" or something and we can call it in dialogue UI to then do that next automatically after it is called.
            

            //dialogueUI.SetDialogueOptions(dialogueUnit.options, dialogueTree.defaultOption);
            //dialogueUI.SetNextState(dialogueUnit.goToState);
            //dialogueUI.ContinueDialogue();
        }

    }


}