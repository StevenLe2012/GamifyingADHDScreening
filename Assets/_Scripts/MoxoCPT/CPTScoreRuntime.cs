using UnityEngine;

namespace MoxoCPT
{
    public class CPTScoreRuntime : MonoBehaviour
    {
        public static CPTScoreRuntime I { get; private set; }

        /// <summary>
        /// Sum of <see cref="CorrectTargetsHit"/> for completed MOXO runs on CARDS, BREAD, POISON, SKULL only.
        /// Used for end-game total display. Reset via <see cref="ResetFourIslandSessionTotal"/>.
        /// </summary>
        private static int s_cumulativeCorrectHitsFourBaseIslands;

        public static int CumulativeCorrectHitsFourBaseIslands => s_cumulativeCorrectHitsFourBaseIslands;

        public static void ResetFourIslandSessionTotal()
        {
            s_cumulativeCorrectHitsFourBaseIslands = 0;
        }

        /// <summary>
        /// After a successful CPT run, add this run's correct target hits if the island is one of the four base islands.
        /// </summary>
        public static void TryCommitRunToFourIslandTotal(int correctHits, string islandId)
        {
            var id = (islandId ?? "").Trim().ToUpperInvariant();
            if (id != "CARDS" && id != "BREAD" && id != "POISON" && id != "SKULL")
                return;

            s_cumulativeCorrectHitsFourBaseIslands += Mathf.Max(0, correctHits);
        }

        // Target counters
        private int _countedTargets;     // targets seen via RegisterTrial
        private int _countedHits;        // correct hits on target trials

        // Distractor counters (NEW)
        private int _countedDistractors;     // non-targets seen via RegisterTrial
        private int _countedFalseAlarms;     // presses on non-target trials (impulsiveness)

        // Optional planned totals
        private int _plannedTotalTargets = 0;
        private int _plannedTotalDistractors = 0; // NEW

        public int CorrectTargetsHit => _countedHits;
        public int FalseAlarms => _countedFalseAlarms; // NEW

        public int TotalTargets =>
            (_plannedTotalTargets > 0) ? _plannedTotalTargets : _countedTargets;

        public int TotalDistractors => // NEW
            (_plannedTotalDistractors > 0) ? _plannedTotalDistractors : _countedDistractors;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetScore()
        {
            _countedTargets = 0;
            _countedHits = 0;

            _countedDistractors = 0;     // NEW
            _countedFalseAlarms = 0;     // NEW

            _plannedTotalTargets = 0;
            _plannedTotalDistractors = 0; // NEW
        }

        public void SetTotalTargets(int total)
        {
            _plannedTotalTargets = Mathf.Max(0, total);
        }

        // Optional: call if you know total distractors too (NEW)
        public void SetTotalDistractors(int total)
        {
            _plannedTotalDistractors = Mathf.Max(0, total);
        }

        /// <summary>
        /// Call once per trial at the end of the trial.
        /// isTarget: whether that trial showed a target
        /// correctTargetHit: true only when a target was pressed correctly
        /// falseAlarm: true only when a non-target was pressed (impulsiveness)
        /// </summary>
        public void RegisterTrial(bool isTarget, bool correctTargetHit, bool falseAlarm)
        {
            if (isTarget)
            {
                if (_plannedTotalTargets <= 0)
                    _countedTargets++;

                if (correctTargetHit)
                    _countedHits++;
            }
            else
            {
                if (_plannedTotalDistractors <= 0)
                    _countedDistractors++;

                if (falseAlarm)
                    _countedFalseAlarms++;
            }
        }

        public string ScoreString() =>
            $"Targets: {CorrectTargetsHit}/{TotalTargets} | Non-target hits: {FalseAlarms}/{TotalDistractors}";
    }
}
