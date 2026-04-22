using System.Collections;
using UnityEngine;

namespace MoxoCPT
{
    public class Interact : MonoBehaviour
    {
    // Monotonically increasing press counter.
    // ButtonPress increments it; StartReport snapshots it at trial start and
    // detects presses as any increment since the snapshot.
    // ChangeShapes never needs to clear it → no race condition.
    public static int _pressCount = 0;

    /// <summary>
    /// Fired on the exact frame a correct hit is registered (target trial + first press
    /// while the stimulus is still on screen). Subscribe to drive koala happy animation.
    /// </summary>
    public static event System.Action OnCorrectHit;


        // Keep the old boolean as a forwarding alias so any external code that still
        // writes Interact._buttonPressed = true continues to work.
        public static bool _buttonPressed
        {
            get => false;  // reading is meaningless now — use the counter
            set { if (value) _pressCount++; }
        }

        // --------------------------------------------------------------------
        // Backward-compatible overload (matches the 6-arg call from ChangeShapes)
        // It will use report.Phase / report.PhaseTrialIndex if ChangeShapes set them.
        // --------------------------------------------------------------------
        public static IEnumerator StartReport(
            Report report,
            float totalWindowSeconds,
            int trialIndex,
            float stimulusDurationSeconds,
            long stimulusOnsetMs,
            bool isTargetTrial
        )
        {
            // If ChangeShapes didn’t set these, fall back to safe defaults
            string phase = string.IsNullOrWhiteSpace(report.Phase) ? "" : report.Phase;
            int phaseTrialIndex = report.PhaseTrialIndex;

            // Stimulus is ON at the start of the window in your design
            return StartReport(
                report,
                totalWindowSeconds,
                trialIndex,
                stimulusDurationSeconds,
                stimulusOnsetMs,
                isTargetTrial,
                phase,
                phaseTrialIndex,
                true
            );
        }

        /// <summary>
        /// Phase-aware trial logger.
        /// isTargetTrial is SOURCE OF TRUTH (passed from ChangeShapes).
        ///
        /// stimulusDurationSeconds: how long stimulus is ON
        /// totalWindowSeconds: response window (ON + ISI)
        /// isStimulusCurrentlyOnAtStart: should be true in your current design
        /// </summary>
        public static IEnumerator StartReport(
            Report report,
            float totalWindowSeconds,
            int trialIndex,
            float stimulusDurationSeconds,
            long stimulusOnsetMs,
            bool isTargetTrial,
            string phase,
            int phaseTrialIndex,
            bool isStimulusCurrentlyOnAtStart
        )
        {
            float timePassed = 0f;

            // Snapshot the counter at trial start — any increment is a new press.
            int pressSeenCount = _pressCount;

            bool hadPress = false;
            float firstPressTime = -1f;

            // ---- metadata ----
            report.TrialIndex = trialIndex;

            // Phase fields (ChangeShapes should already set them, but we overwrite safely)
            report.Phase = phase ?? "";
            report.PhaseTrialIndex = phaseTrialIndex;

            report.StimulusDurationMs = Mathf.RoundToInt(stimulusDurationSeconds * 1000f);
            report.StimulusOnsetMs = stimulusOnsetMs;

            // Participant/session best-effort
            report.ParticipantId = (GameManager.Instance != null) ? GameManager.Instance.ParticipantId : "";
            report.SessionId = LoggingReport.CurrentSessionId;

            // Island best-effort
            var travel = IslandTravelManager.I;
            report.IslandId = (travel != null && travel.CurrentIsland != null) ? (travel.CurrentIsland.islandId ?? "") : "";
            report.IslandName = (travel != null && travel.CurrentIsland != null) ? (travel.CurrentIsland.displayName ?? "") : "";

            // stimulus type
            report.StimulusType = isTargetTrial ? "target" : "non_target";

            // ---- listen for response ----
            while (timePassed <= totalWindowSeconds)
            {
                // Process every new press since the trial started (counter never decrements).
                while (_pressCount > pressSeenCount)
                {
                    pressSeenCount++;   // consume one press

                    if (!hadPress)
                    {
                        hadPress = true;
                        firstPressTime = timePassed;

                        bool stimulusIsOnNow =
                            isStimulusCurrentlyOnAtStart && (timePassed <= stimulusDurationSeconds);

                        if (isTargetTrial)
                        {
                            if (stimulusIsOnNow)
                            {
                                report.Timeliness = true;
                                // Notify subscribers (e.g. koala happy animation) immediately.
                                OnCorrectHit?.Invoke();
                            }

                            report.Attentiveness = true;
                        }
                        else
                        {
                            report.Impulsiveness = true;
                        }
                    }
                    else
                    {
                        // Subsequent presses within the same trial window
                        report.HyperReactiveness = true;
                        report.HyperReactiveCount++;
                    }
                }

                timePassed += Time.deltaTime;
                yield return null;
            }

            // ---- response + RT ----
            report.ResponseMade = hadPress;

            if (hadPress)
            {
                float rtMs = firstPressTime * 1000f;
                float? rt = (rtMs <= 0f) ? (float?)null : rtMs; // rule: never store 0

                // ✅ Recommended split:
                // reaction_time_ms          = target RT only (hit); null on non-target trials
                // reaction_time_ms_non_target = non-target RT only (false alarm); null on target trials
                report.ReactionTimeMs = isTargetTrial ? rt : (float?)null;
                report.ReactionTimeNonTargetMs = !isTargetTrial ? rt : (float?)null;
            }
            else
            {
                report.ReactionTimeMs = null;
                report.ReactionTimeNonTargetMs = null;
            }

            // ---- outcome + correct ----
            if (isTargetTrial && hadPress)
            {
                report.Outcome = "hit";
                report.Correct = true;
            }
            else if (isTargetTrial && !hadPress)
            {
                report.Outcome = "miss";
                report.Correct = false;
            }
            else if (!isTargetTrial && hadPress)
            {
                report.Outcome = "false_alarm";
                report.Correct = false;
            }
            else
            {
                report.Outcome = "correct_reject";
                report.Correct = true;
            }

            // runtime score tracker (optional)
            CPTScoreRuntime.I?.RegisterTrial(isTargetTrial, report.Attentiveness, report.Impulsiveness);

            // write trial
            LoggingReport.AppendToReportCSV(report);
        }
    }
}
