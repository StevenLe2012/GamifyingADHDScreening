using System;
using UnityEngine;
using UnityEngine.Events;

namespace Dialogue
{
    [Serializable]
    public class DialogueOption
    {
        public string buttonText;          // what text to show
        public UnityEvent actionToTrigger; // what choosing the option triggers

        [Header("Optional Jump (leave blank to NOT jump)")]
        [Tooltip("If set, choosing this option will GoToState(nextStateKey) before invoking actionToTrigger.")]
        public string nextStateKey;
    }
}
