using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoxoCPT
{
    [Serializable]
    
    public class Report
    {
        public List<bool> Attentiveness;
        public List<bool> Timelineess;
        public List<bool> HyperReactive;
        public List<bool> Impulsive;
        
        public List<int> ReactionSpeed;
    }
}

