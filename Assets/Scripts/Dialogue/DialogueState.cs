using System.Collections.Generic;
using UnityEngine;

/*
 * This code is to make a dictionary with the NPC name and state.
 */

namespace Dialogue
{
    public class DialogueState : MonoBehaviour
    {
        // Dict(npcName, dialogueTreeState)
        public Dictionary<string, string> stateDict;  // gets the NPC we are talking to and what state we are in with that NPC

        // maybe TODO: Add save/load methods (serilazation), so player doesn't have to go through dialogue tree each time they start

        private void Start()
        {
            stateDict = new Dictionary<string, string>();
        }
    }

}
