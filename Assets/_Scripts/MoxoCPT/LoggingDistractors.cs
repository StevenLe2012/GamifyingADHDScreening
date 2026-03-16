using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace MoxoCPT
{
    /// <summary>
    /// Separate CSV logger for distractor activity events (DP only).
    /// One row per distractor activation ("ON event") with onset + offset.
    /// </summary>
    public static class LoggingDistractors
    {
        private const string CSVSeperator = ",";

        // Call this once per run (same place you call LoggingReport.CreateReportCSV()).
        public static void CreateDistractorCSV()
        {
            var path = GetCSVPath();
            EnsureDirectory(path);

            if (File.Exists(path))
                return;

            using (var sw = File.CreateText(path))
                sw.WriteLine(string.Join(CSVSeperator, CSVHeaders));
        }

        /// <summary>
        /// Append one distractor activation event.
        /// </summary>
        public static void AppendDistractorEvent(DistractorEvent e)
        {
            var path = GetCSVPath();
            EnsureDirectory(path);

            if (!File.Exists(path))
            {
                using (var swHead = File.CreateText(path))
                    swHead.WriteLine(string.Join(CSVSeperator, CSVHeaders));
            }

            var inv = CultureInfo.InvariantCulture;

            // Keep session id consistent with your main report
            string sessionId = !string.IsNullOrWhiteSpace(e.SessionId)
                ? e.SessionId
                : LoggingReport.CurrentSessionId;

            // Optional blanks
            string trialIndex = (e.TrialIndex >= 0) ? e.TrialIndex.ToString(inv) : "";
            string phaseTrialIndex = (e.PhaseTrialIndex >= 0) ? e.PhaseTrialIndex.ToString(inv) : "";

            string onset = (e.OnsetMs > 0) ? e.OnsetMs.ToString(inv) : "";
            string offset = (e.OffsetMs > 0) ? e.OffsetMs.ToString(inv) : "";

            string planned = (e.PlannedDurationMs >= 0) ? e.PlannedDurationMs.ToString(inv) : "";
            string actual = (e.ActualDurationMs >= 0) ? e.ActualDurationMs.ToString(inv) : "";

            string dpOn = (e.DpElapsedOnsetMs >= 0) ? e.DpElapsedOnsetMs.ToString(inv) : "";
            string dpOff = (e.DpElapsedOffsetMs >= 0) ? e.DpElapsedOffsetMs.ToString(inv) : "";

            string weight = e.WeightTarget.HasValue ? e.WeightTarget.Value.ToString(inv) : "";

            var row = string.Join(CSVSeperator, new[]
            {
                Escape(e.ParticipantId),
                Escape(sessionId),

                Escape(e.IslandId),
                Escape(e.Phase), // should be "DP"

                trialIndex,
                phaseTrialIndex,

                Escape(e.DistractorId),   // "D1".."D6"
                Escape(e.DistractorName), // GameObject.name

                weight,

                onset,
                offset,
                planned,
                actual,
                dpOn,
                dpOff,

                e.ActiveCountAtOnset.ToString(inv),
                Escape(e.ActiveSetAtOnset) // e.g. "D2|D5" or ""
            });

            using (var sw = File.AppendText(path))
                sw.WriteLine(row);
        }

        // ---------------- Data container ----------------
        [Serializable]
        public struct DistractorEvent
        {
            public string ParticipantId;
            public string SessionId;

            public string IslandId;
            public string Phase; // "DP"

            public int TrialIndex;       // join key to trial CSV (optional but recommended)
            public int PhaseTrialIndex;  // join key (optional but recommended)

            public string DistractorId;   // "D1".."D6"
            public string DistractorName; // object name

            public float? WeightTarget; // time-share target weight

            public long OnsetMs;  // absolute ms (realtimeSinceStartup*1000)
            public long OffsetMs; // absolute ms

            public int PlannedDurationMs;
            public int ActualDurationMs;

            public int DpElapsedOnsetMs;  // ms since DP start
            public int DpElapsedOffsetMs; // ms since DP start

            public int ActiveCountAtOnset; // how many distractors were active AFTER turning this ON
            public string ActiveSetAtOnset; // like "D2|D5" (optional)
        }

        // ---------------- Internals ----------------
        private static readonly string[] CSVHeaders = new[]
        {
            "participant_id",
            "session_id",

            "island_id",
            "phase",

            "trial_index",
            "phase_trial_index",

            "distractor_id",
            "distractor_name",

            "weight_target",

            "onset_ms",
            "offset_ms",
            "planned_duration_ms",
            "actual_duration_ms",
            "dp_elapsed_onset_ms",
            "dp_elapsed_offset_ms",

            "active_count_at_onset",
            "active_set_at_onset"
        };

        private static string GetCSVPath()
        {
            var pid = GetCurrentParticipantId();
            var safePid = Sanitize(pid);

            var dir = Path.Combine(Environment.CurrentDirectory, "Assets", "Resources", "ParticipantData", "ReportData");
            return Path.Combine(dir, $"P_{safePid}_MoxoCPT_Distractors.csv");
        }

        private static void EnsureDirectory(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static string GetCurrentParticipantId()
        {
            var gm = GameManager.Instance;
            var pid = (gm != null ? gm.ParticipantId : null);

            if (string.IsNullOrWhiteSpace(pid))
                pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            return pid;
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Unknown";
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim();
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
            {
                s = s.Replace("\"", "\"\"");
                return $"\"{s}\"";
            }
            return s;
        }
    }
}
