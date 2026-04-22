namespace MoxoCPT
{
    public class Report
    {
        // ---- Phase tracking ----
        // "NDP" (Non-Distractor Phase) or "DP" (Distractor Phase)
        public string Phase;
        // 0..34 inside the phase
        public int PhaseTrialIndex;

        // ---- Participant / session ----
        public string ParticipantId;
        public string SessionId;

        // ---- Trial identity ----
        // global 0..(totalTrialsPerIsland-1) across the whole island run (e.g. 0..83 for 84 cards)
        public int TrialIndex;

        // ---- Island ----
        public string IslandId;
        public string IslandName;

        // ---- Stimulus ----
        // "target" / "non_target"
        public string StimulusType;
        // exact prefab/object name shown
        public string StimulusName;

        // stimulus ON time (ms)
        public int StimulusDurationMs;

        // planned ISI after stimulus offset until next trial (ms); -1 if unknown
        public int InterStimulusIntervalMs;
        // index into the 42 seeded base ISI values; -1 if unknown
        public int IsiBaseIndex;

        // reproducibility metadata
        // seed used to build this phase's trial plan (target/non-target+duration order and card-pick stream)
        public int TrialPlanSeed;
        // study seed used to generate the 42 base ISI values
        public int InterStimulusScheduleSeed;
        // seed used for this phase's ISI permutation
        public int IsiPermutationSeed;

        // ms since app start when stimulus became visible and invisible
        public long StimulusOnsetMs;
        public long StimulusOffsetMs;
        public int StimulusActualDurationMs;

        // ---- Response ----
        public bool ResponseMade;

        // RT in ms ONLY for target trials (blank otherwise)
        public float? ReactionTimeMs;

        // RT in ms ONLY for non-target trials (blank otherwise)
        public float? ReactionTimeNonTargetMs;

        // ---- Accuracy / outcome ----
        public bool Correct;
        // "hit", "miss", "false_alarm", "correct_reject"
        public string Outcome;

        // ---- Original fields ----
        public bool Attentiveness;
        public bool Timeliness;
        public bool HyperReactiveness;
        public bool Impulsiveness;
        public int HyperReactiveCount;

        // Optional convenience helpers
        public bool IsTarget => StimulusType == "target";
        public bool IsNonTarget => StimulusType == "non_target";

        

        public Report()
        {
            ResetReport();
        }

        public void ResetReport()
        {
            // Phase
            Phase = "";
            PhaseTrialIndex = -1;

            // Participant / session
            ParticipantId = "";
            SessionId = "";

            // Trial identity
            TrialIndex = -1;

            // Island
            IslandId = "";
            IslandName = "";

            // Stimulus
            StimulusType = "";
            StimulusName = "";
            StimulusDurationMs = -1;
            InterStimulusIntervalMs = -1;
            IsiBaseIndex = -1;
            TrialPlanSeed = 0;
            InterStimulusScheduleSeed = 0;
            IsiPermutationSeed = 0;
            StimulusOnsetMs = -1;
            StimulusOffsetMs = -1;
            StimulusActualDurationMs = -1;

            // Response
            ResponseMade = false;
            ReactionTimeMs = null;
            ReactionTimeNonTargetMs = null;

            // Outcome
            Correct = false;
            Outcome = "";

            // Originals
            Attentiveness = false;
            Timeliness = false;
            HyperReactiveness = false;
            Impulsiveness = false;
            HyperReactiveCount = 0;
        }
    }
}
