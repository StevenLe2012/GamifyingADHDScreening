// DialogueStateData.cs
using System;
using System.Collections.Generic;

namespace Dialogue
{
    /// <summary>Serializable, non-Unity data container for dialogue progression.</summary>
    [Serializable]
    public class DialogueStateData
    {
        // Dict(npcName, stateKey)
        public Dictionary<string, string> stateDict = new Dictionary<string, string>();

        public void SetState(string npcName, string stateKey)
        {
            if (string.IsNullOrWhiteSpace(npcName)) return;
            stateDict[npcName] = stateKey ?? string.Empty;
        }

        public string GetState(string npcName, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(npcName)) return fallback;
            return stateDict != null && stateDict.TryGetValue(npcName, out var v) ? v : fallback;
        }
    }
}
