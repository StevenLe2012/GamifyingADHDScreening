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
        // global 0..69 across the whole island run
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
