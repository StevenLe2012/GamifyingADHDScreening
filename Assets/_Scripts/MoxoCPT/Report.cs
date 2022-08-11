using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MoxoCPT
{
    
    
    public class Report
    {
        public bool IsTarget;
        public float TimeShown;
        public bool Attentiveness;
        public bool Timelineess;
        public bool HyperReactiveness;
        public bool Impulsiveness;
        public float ReactionTime;  // only for first time pressing button on a stimuli
        public int HyperReactiveCount;

        public void ResetReport()
        {
            Attentiveness = false;
            Timelineess = false;
            HyperReactiveness = false;
            Impulsiveness = false;
            ReactionTime = -1f;
            HyperReactiveCount = 0;
        }
    }
}

