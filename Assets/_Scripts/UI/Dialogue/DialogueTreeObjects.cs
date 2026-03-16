using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dialogue
{
    [CreateAssetMenu(fileName = "DialogueTree", menuName = "ScriptableObjects/Dialogue Tree")]
    public class DialogueTreeObjects : ScriptableObject
    {
        // Editor class members
        public string npcName;
        public string defaultState;  // when we interact with NPC for the first time
        public string[] scriptableCallbackNames;
        public DialogueUnit[] dialogueUnits;  // array of dialogue that the NPC will say

        // Non-editor class members
        [HideInInspector] public DialogueState dialogueState;
        public Action continueCallback;
        public Action endDialogueCallback;
        public Dictionary<string, DialogueUnit> dialogueUnitsDict;
        public Dictionary<string, Action> scriptableCallbacks = new Dictionary<string, Action>();

        [Header("Input Policy")]
        [Tooltip("If ON, DialogueTreeObjects.Continue() does NOTHING. This blocks Space/Submit from advancing dialogue units.")]
        [SerializeField] private bool disableManualContinue = true;

        [Tooltip("Optional: if ON, logs whenever a blocked manual Continue() happens so you can find who is calling it.")]
        [SerializeField] private bool logBlockedManualContinue = true;

        [Header("Safety / Debug")]
        [Tooltip("If true, when a state key is missing we will NOT return null; we return the last valid unit instead (prevents accidental End()).")]
        [SerializeField] private bool returnLastValidUnitWhenMissingKey = true;

        [Tooltip("If true, logs missing requiredStateKey clearly (highly recommended while debugging).")]
        [SerializeField] private bool logMissingStateKeys = true;

        private DialogueUnit _lastValidUnit;
        private string _lastValidKey;
        private string _lastMissingKey;

        public string CurrentStateKey
        {
            get
            {
                if (dialogueState == null || dialogueState.stateDict == null) return "";
                if (!dialogueState.stateDict.TryGetValue(npcName, out var key)) return "";
                return key ?? "";
            }
        }

        public bool LastLookupWasMissingKey => !string.IsNullOrEmpty(_lastMissingKey);

        public void AddToState(string stateToAdd)
        {
            if (dialogueState == null || dialogueState.stateDict == null) return;
            if (!dialogueState.stateDict.ContainsKey(npcName))
                dialogueState.stateDict[npcName] = defaultState;

            dialogueState.stateDict[npcName] += stateToAdd;
        }

        public void RemoveState(int length = 1)
        {
            if (dialogueState == null || dialogueState.stateDict == null) return;
            if (!dialogueState.stateDict.ContainsKey(npcName)) return;

            var s = dialogueState.stateDict[npcName] ?? "";
            if (s.Length < length) return;

            dialogueState.stateDict[npcName] = s.Remove(s.Length - length);
        }

        public void GoToState(string newState)
        {
            if (dialogueState == null || dialogueState.stateDict == null) return;
            dialogueState.stateDict[npcName] = newState;
        }

        public void CallScriptableAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName)) return;

            if (scriptableCallbacks.TryGetValue(actionName, out var action))
            {
                Debug.Log($"[DialogueTree] Calling scriptable action: {actionName}");
                action?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[DialogueTree] No scriptable action registered for '{actionName}'. " +
                                 $"Did you add it to DialogueHandler.scriptableEvents and spell it exactly?");
            }
        }

        /// <summary>
        /// MANUAL Continue (Space/Submit/etc.) — intentionally blocked.
        /// </summary>
        public void Continue()
        {
            if (disableManualContinue)
            {
                if (logBlockedManualContinue)
                {
                    Debug.LogWarning("[DialogueTree] Manual Continue() was BLOCKED (disableManualContinue=true).\nCaller:\n" +
                                     Environment.StackTrace);
                }
                return;
            }

            continueCallback?.Invoke();
        }

        /// <summary>
        /// Use THIS in Dialogue Option UnityEvents.
        /// This bypasses disableManualContinue so Space can't advance lines,
        /// but choosing an option CAN advance.
        /// </summary>
        public void ContinueFromOption()
        {
            continueCallback?.Invoke();
        }

        /// <summary>
        /// AUTO Continue — use this for automatic dialogue progression.
        /// Your dialogue system should call this, not Continue().
        /// </summary>
        public void AutoContinue()
        {
            continueCallback?.Invoke();
        }

        public void EndDialogue()
        {
            Debug.Log("[DialogueTree] EndDialogue() called.\nCaller:\n" + Environment.StackTrace);
            endDialogueCallback?.Invoke();
        }

        public void RegisterScriptableCallback(string callbackName, Action action)
        {
            scriptableCallbacks[callbackName] = action;
        }

        public void SetUpDialogueUnitsDict()
        {
            dialogueUnitsDict = new Dictionary<string, DialogueUnit>();

            if (dialogueUnits == null) return;

            foreach (var dialogueUnit in dialogueUnits)
            {
                if (dialogueUnit == null) continue;

                if (dialogueUnitsDict.ContainsKey(dialogueUnit.requiredStateKey))
                {
                    Debug.LogWarning($"[DialogueTree] Duplicate requiredStateKey '{dialogueUnit.requiredStateKey}' in {name}. Later entry overwrote earlier.");
                }

                dialogueUnitsDict[dialogueUnit.requiredStateKey] = dialogueUnit;
            }
        }

        public void SetUpDialogueState(DialogueState state)
        {
            dialogueState = state;

            if (dialogueState == null || dialogueState.stateDict == null)
                return;

            if (!dialogueState.stateDict.ContainsKey(npcName))
            {
                dialogueState.stateDict[npcName] = defaultState;
            }
        }

        public void ResetCallbacks()
        {
            continueCallback = () => { };
            endDialogueCallback = () => { };
            scriptableCallbacks.Clear();

            _lastValidUnit = null;
            _lastValidKey = null;
            _lastMissingKey = null;
        }

        public DialogueUnit GetNextDialogueUnit()
        {
            _lastMissingKey = null;

            if (dialogueUnitsDict == null || dialogueUnitsDict.Count == 0)
            {
                if (logMissingStateKeys)
                    Debug.LogWarning($"[DialogueTree] dialogueUnitsDict is empty for '{name}'. Did you call SetUpDialogueUnitsDict()?");
                return null;
            }

            var key = CurrentStateKey;

            if (dialogueUnitsDict.TryGetValue(key, out var value) && value != null)
            {
                _lastValidUnit = value;
                _lastValidKey = key;
                return value;
            }

            _lastMissingKey = key;

            if (logMissingStateKeys)
            {
                Debug.LogWarning(
                    $"[DialogueTree] Missing requiredStateKey for NPC '{npcName}'.\n" +
                    $"Current key: '{key}'\n" +
                    $"Last valid key: '{_lastValidKey}'\n" +
                    $"Tree asset: '{name}'"
                );
            }

            if (returnLastValidUnitWhenMissingKey && _lastValidUnit != null)
            {
                return _lastValidUnit;
            }

            return null;
        }
    }
}
