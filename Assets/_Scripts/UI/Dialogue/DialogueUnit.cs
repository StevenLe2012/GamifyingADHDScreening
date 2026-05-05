using System;
using UnityEngine;
using UnityEngine.Events;

namespace Dialogue
{
    [Serializable]
    public class DialogueUnit
    {
        public string requiredStateKey;
        public string character; // Hami: Add name "Ekonn", "You", etc.
        public AudioObjects[] audioObjects;
        public DialogueOption[] options;

        [Header("If options == 0, then this will activate")]
        public UnityEvent nextEventWithoutButton;

        [Header("Named scene event to fire when this node is entered")]
        public string onEnterEventName; // e.g., "APPEAR_Koala"
    }

  
}
