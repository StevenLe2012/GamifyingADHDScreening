using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MoxoCPT
{
    [Serializable]
    
    public class Report
    {
        public List<bool> Attentiveness;
        public List<bool> Timelineess;
        public List<bool> HyperReactiveness;
        public List<bool> Impulsiveness;
        public List<float> ReactionTime;

        public int HyperReactiveCount = 0;
    }
}

