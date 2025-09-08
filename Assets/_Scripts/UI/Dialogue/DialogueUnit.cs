using System;
using UnityEngine;
using UnityEngine.Events;

/*
 * This code is to show a single unit of dialogue.
 */ 

namespace Dialogue
{
    [Serializable]

    public class DialogueUnit
    {
        public string requiredStateKey;  // where we are in the dialogue (ex: so we move past initial welcome message)
        public string speakerName;            // who is speaking ("Player", "Ekonn", etc.)
        [TextArea(2, 5)] public string dialogueText; // this is the line of text
        public AudioObjects[] audioObjects;
        public DialogueOption[] options;  // button options
        [Header("If options == 0, then this will activate")]
        public UnityEvent nextEventWithoutButton;
    }
}
