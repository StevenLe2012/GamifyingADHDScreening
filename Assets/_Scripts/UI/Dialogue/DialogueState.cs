using System.Collections.Generic;
using UnityEngine;

/*
 * Holds a dictionary of per-NPC dialogue states in this scene.
 */
namespace Dialogue
{
    public class DialogueState : MonoBehaviour
    {
        // Dict(npcName, dialogueTreeState)
        public Dictionary<string, string> stateDict;  // gets the NPC we are talking to and what state we are in with that NPC

        private void Awake()
        {
            if (stateDict == null)
            {
                stateDict = new Dictionary<string, string>();
                Debug.Log("[DialogueState] Initialized stateDict in Awake().");
            }
        }

        // Optional: call this if you're ever unsure the dict exists.
        public void EnsureInitialized()
        {
            if (stateDict == null)
            {
                stateDict = new Dictionary<string, string>();
                Debug.Log("[DialogueState] EnsureInitialized() created new dictionary.");
            }
        }
    }
}
