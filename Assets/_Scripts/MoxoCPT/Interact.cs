using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoxoCPT
{
    public class Interact : MonoBehaviour
    {
        public static bool _buttonPressed = false;
        private static bool _alreadyPressed;
        
        // TODO: Make it so they can't access ButtonPressed until after the game starts b/c that would cause error.
        public static IEnumerator StartReport(Report report, float duration)
        {
            var timePassed = 0f;
            _alreadyPressed = false;
            //Hami:
            bool hadPress = false;

            while (timePassed <= duration)
            {
                if (_buttonPressed)
                {
                    _buttonPressed = false;
                    //Hami:
                    hadPress = true;
                    Debug.Log($"[Interact] Press seen at t={timePassed:0.000}s (isTarget={IsTargetCard()})");
                    
                    
                    // Hyper Reactiveness
                    if (_alreadyPressed)
                    {
                        Debug.Log("Pressed Again!");
                        report.HyperReactiveness = true;
                        report.HyperReactiveCount++;
                    }
                    
                    // Storing Target/Distractor, Card Duration, and ReactionTime
                    else
                    {
                        report.IsTarget = IsTargetCard();
                        report.TimeShown = duration / 2;
                        report.ReactionTime = timePassed;
                    }

                    // Attentiveness and Timeliness
                    if (IsTargetCard() && !_alreadyPressed)
                    {
                        if (Cards.Instance.isActive) report.Timeliness = true;
                        report.Attentiveness = true;
                        _alreadyPressed = true;
                    }
            
                    // Impulsiveness
                    if (!IsTargetCard() && !_alreadyPressed)
                    {
                        report.Impulsiveness = true;
                        _alreadyPressed = true;
                    }
                }
                
                timePassed += Time.deltaTime;

                yield return null;
            }
            
            if (!hadPress)
                Debug.Log("[Interact] No press during this trial.");

            CPTScoreRuntime.I?.RegisterTrial(report.IsTarget, report.Attentiveness);
            LoggingReport.AppendToReportCSV(report);
        }

        private static bool IsTargetCard()
        {
            return Cards.Instance.curCard.gameObject.CompareTag("Target");
        }

    }
}

