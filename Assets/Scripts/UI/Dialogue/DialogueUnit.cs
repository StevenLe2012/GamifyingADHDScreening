using System;
using UnityEngine;

/*
 * This code is to show a single unit of dialogue.
 */ 

namespace Dialogue
{
    [Serializable]

    public class DialogueUnit
    {
        public string requiredStateKey;  // where we are in the dialogue (ex: so we move past initial welcome message)
        [TextArea(2, 5)]
        public string[] sentences;  // sentences to display in the textbox
        public AudioObjects[] audioObjects;
        public DialogueOption[] options;  // button options
    }
}
