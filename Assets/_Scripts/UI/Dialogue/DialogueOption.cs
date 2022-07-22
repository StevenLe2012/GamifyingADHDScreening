using System;
using UnityEngine;
using UnityEngine.Events;

/*
 * This code is for the different button options the player can choose and what event can be triggered from it.
 */

namespace Dialogue
{
    [Serializable]

    public class DialogueOption
    {
        public string buttonText;  // what text to show
        public UnityEvent actionToTrigger;  // what action will pushing the buttons trigger
    }
}
