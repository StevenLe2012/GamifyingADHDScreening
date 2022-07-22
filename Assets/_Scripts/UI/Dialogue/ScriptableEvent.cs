using System;
using UnityEngine.Events;

/*
 * This code is to be able make UnityEvents work and pull up an event such as a UI to sell items or whatever else we can think of.
 */

namespace Helpers
{
    [Serializable]

    public class ScriptableEvent
    {
        public string eventName;
        public UnityEvent unityEvent;
    }
}