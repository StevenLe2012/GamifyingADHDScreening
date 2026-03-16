using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace MoxoCPT
{
    public static class LoggingReport
    {
        private const string CSVSeperator = ",";

        // Set fresh each OnGameBeginReal() (CreateReportCSV is called there)
        public static string CurrentSessionId { get; private set; } = "";

        // ---------- PUBLIC API ----------
        public static void CreateReportCSV()
        {
            // Always start a NEW session id per run
            CurrentSessionId = "S_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var path = GetCSVPath();
            EnsureDirectory(path);

            if (File.Exists(path))
                return;

            using (var sw = File.CreateText(path))
                sw.WriteLine(string.Join(CSVSeperator, CSVHeaders));
        }

        public static void AppendToReportCSV(Report report)
        {
            var path = GetCSVPath();
            EnsureDirectory(path);

            if (!File.Exists(path))
            {
                using (var swHead = File.CreateText(path))
                    swHead.WriteLine(string.Join(CSVSeperator, CSVHeaders));
            }

            var inv = CultureInfo.InvariantCulture;

            string sessionId = !string.IsNullOrWhiteSpace(report.SessionId)
                ? report.SessionId
                : CurrentSessionId;

            // reaction_time_ms: blank if null
            string rt = report.ReactionTimeMs.HasValue
                ? report.ReactionTimeMs.Value.ToString(inv)
                : "";

            // reaction_time_ms_non_target: blank if null
            string rtNonTarget = report.ReactionTimeNonTargetMs.HasValue
                ? report.ReactionTimeNonTargetMs.Value.ToString(inv)
                : "";

            // stimulus_onset_ms: blank if unset (<= 0)
            string onset = (report.StimulusOnsetMs > 0)
                ? report.StimulusOnsetMs.ToString(inv)
                : "";

            // stimulus_duration_ms: blank if unset (< 0)
            string stimDur = (report.StimulusDurationMs >= 0)
                ? report.StimulusDurationMs.ToString(inv)
                : "";

            // stimulus_offset_ms: blank if unset (< 0)
            string offset = (report.StimulusOffsetMs > 0)
            ? report.StimulusOffsetMs.ToString(inv)
            : "";
            
            // stimulus_actual_ms: blank if unset (< 0)
            string stimActual = (report.StimulusActualDurationMs >= 0)
            ? report.StimulusActualDurationMs.ToString(inv)
            : "";

            // phase_trial_index: blank if unset (< 0)
            string phaseTrial = (report.PhaseTrialIndex >= 0)
                ? report.PhaseTrialIndex.ToString(inv)
                : "";

            // trial_index: blank if unset (< 0)
            string trialIndex = (report.TrialIndex >= 0)
                ? report.TrialIndex.ToString(inv)
                : "";

            var row = string.Join(CSVSeperator, new[]
            {
                Escape(report.ParticipantId),
                Escape(sessionId),

                trialIndex,

                // ---- new ----
                Escape(report.Phase),
                phaseTrial,

                Escape(report.IslandId),
                Escape(report.IslandName),

                Escape(report.StimulusType),
                Escape(report.StimulusName),
                stimDur,
                onset,
                offset,
                stimActual,


                report.ResponseMade.ToString(),

                // ✅ split RT columns (target vs non-target)
                rt,
                rtNonTarget,

                report.Correct.ToString(),
                Escape(report.Outcome),

                // ---- keep originals ----
                report.Attentiveness.ToString(),
                report.Timeliness.ToString(),
                report.HyperReactiveness.ToString(),
                report.Impulsiveness.ToString(),
                report.HyperReactiveCount.ToString(inv),
            });

            using (var sw = File.AppendText(path))
                sw.WriteLine(row);
        }

        // ---------- INTERNALS ----------
        private static readonly string[] CSVHeaders = new[]
        {
            "participant_id",
            "session_id",

            "trial_index",

            // ---- new ----
            "phase",
            "phase_trial_index",

            "island_id",
            "island_name",


            "stimulus_type",
            "stimulus_name",
            "stimulus_duration_ms",        // planned
            "stimulus_onset_ms",
            "stimulus_offset_ms",
            "stimulus_actual_duration_ms", // measured

            "response_made",

            // ✅ split RT columns (target vs non-target)
            "reaction_time_ms",
            "reaction_time_ms_non_target",

            "correct",
            "outcome",

            // keep originals
            "attentiveness",
            "timeliness",
            "hyperreactiveness",
            "impulsiveness",
            "hyperreactive_count"
        };

        private static string GetCSVPath()
        {
            var pid = GetCurrentParticipantId();
            var safePid = Sanitize(pid);

            var dir = Path.Combine(Environment.CurrentDirectory, "Assets", "Resources", "ParticipantData", "ReportData");
            return Path.Combine(dir, $"P_{safePid}_MoxoCPT.csv");
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

        // If you ever have commas in island names etc, quote them for CSV safety
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
