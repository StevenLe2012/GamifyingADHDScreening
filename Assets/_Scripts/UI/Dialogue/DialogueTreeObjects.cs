using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UIElements;

/*
 * This file has all the helper functions to create all the specific dialogue trees possible with all the various user options.
 */

namespace Dialogue
{
    [CreateAssetMenu(fileName = "DialogueTree", menuName = "ScriptableObjects/Dialogue Tree")]
    public class DialogueTreeObjects : ScriptableObject
    {
        // Editor class members
        public string npcName;
        public string defaultState;  // when we interact with NPC for the first time
        public DialogueOption defaultOption;
        public string[] scriptableCallbackNames;
        public DialogueUnit[] dialogueUnits;  // array of dialogue that they NPC will say

        // Non-editor class members
        public DialogueState dialogueState;  // figure out where we are in the dialogue tree w/ particular NPC
        public Action continueCallback;
        public Action endDialogueCallback;
        public Dictionary<string, DialogueUnit> dialogueUnitsDict;  // will hold all the dialogue needed based on the current dialogueState
        public Dictionary<string, Action> scriptableCallbacks = new Dictionary<string, Action>();


        // this function adds the state to the specific NPC's stateDictionary
        public void AddToState(string stateToAdd)
        {
            dialogueState.stateDict[npcName] += stateToAdd;
        }

        // this function gets the dialoguge state for the specific NPC and removes the latest dialogue tree option.
        public void RemoveState(int length = 1)
        {
            // can't remove if there is no option yet.
            if (dialogueState.stateDict[npcName].Length < length)
            {
                return;
            }
            // removes latest dialogue tree option
            dialogueState.stateDict[npcName] = dialogueState.stateDict[npcName].Remove(
                dialogueState.stateDict[npcName].Length - length);
        }

        // this function will set the state of a particular NPC to whatever the newState is
        public void ResetState(string newState)
        {
            dialogueState.stateDict[npcName] = newState;
        }

        // this function calls the scriptable action to happen
        public void CallScriptableAction(string actionName)
        {
            scriptableCallbacks[actionName]();
        }

        // this function continues dialogue
        public void Continue()
        {
            continueCallback();
        }

        // this function ends dialogue
        public void EndDialogue()
        {
            Debug.Log("SHOULD END");
            endDialogueCallback();
        }

        // this function goes from the scriptable callback array to the actual dictionary for scriptableCallbacks
        public void RegisterScriptableCallback(string callbackName, Action action)
        {
            scriptableCallbacks[callbackName] = action;
        }
        
        // this function sets up the dialogue units dictionary so that the dictionary contains
        // each dialogue unit as a value and the requiredStateKey as the key.
        public void SetUpDialogueUnitsDict()
        {
            dialogueUnitsDict = new Dictionary<string, DialogueUnit>();
            foreach (var dialogueUnit in dialogueUnits)
            {
                dialogueUnitsDict[dialogueUnit.requiredStateKey] = dialogueUnit;
            }
        }

        // this function sets up the dialogue state so that when the player interacts with an NPC,
        // the dialogue state of the NPC is remembered.
        public void SetUpDialogueState(DialogueState state)
        {
            dialogueState = state;
            // if the NPC is not in the dictionary already, add NPC and set it to the default state.
            if (!dialogueState.stateDict.ContainsKey(npcName))
            {
                dialogueState.stateDict[npcName] = defaultState;
            }
        }

        // clears out previous callbacks when using scriptable actions
        public void ResetCallbacks()
        {
            continueCallback = () => { };
            endDialogueCallback = () => { };
            scriptableCallbacks.Clear();
        }

        // this function gets the next dialogue unit based on the dialogueState
        public DialogueUnit GetNextDialogueUnit()
        {
            // the dialogueState.stateDict[npcName] gets what state we are in with that particular NPC
            return dialogueUnitsDict.TryGetValue(dialogueState.stateDict[npcName], out var value) ? value : null;
        }
    }
}
