using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
#endif

namespace MoxoCPT
{
    public static class LoggingReport
    {
        private const string CSVSeperator = ",";

        // Stable for the whole browser run (set once via EnsureSessionId). It is
        // intentionally NOT reset per CPT run — narrative dialogue and the play-time
        // summary share it. Re-runs of the same island are separated by CurrentAttempt.
        public static string CurrentSessionId { get; private set; } = "";

        // Resolved once per browser run and reused everywhere. This is the single
        // source of truth for participant_id across ALL collections, so the value
        // (and its embedded date) cannot drift mid-session — even across midnight.
        public static string CurrentParticipantId { get; private set; } = "";

        /// <summary>Creates a session id once per browser run if not already set.</summary>
        public static void EnsureSessionId()
        {
            if (!string.IsNullOrWhiteSpace(CurrentSessionId)) return;
            CurrentSessionId = "S_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        /// <summary>
        /// Resolves the participant id ONCE (from GameManager, falling back to a
        /// timestamp) and caches it for the rest of the run. All loggers must read
        /// CurrentParticipantId rather than re-querying GameManager, otherwise the
        /// value can change between calls (e.g. the timestamp fallback, or a
        /// midnight date rollover) and break joins across collections.
        /// </summary>
        public static void EnsureParticipantId()
        {
            if (!string.IsNullOrWhiteSpace(CurrentParticipantId)) return;

            var gm = GameManager.Instance;
            var pid = (gm != null) ? gm.ParticipantId : null;

            if (string.IsNullOrWhiteSpace(pid))
                pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            CurrentParticipantId = pid;
        }

        /// <summary>UTC wall-clock timestamp (ISO-8601) for the current moment.</summary>
        public static string NowUtcIso() => DateTime.UtcNow.ToString("o");

        // ---------- Attempt tracking ----------
        // The same island's CPT can be re-run within ONE browser session (the
        // "Let's try that again" safeguard / Replay Training). session_id stays
        // stable across the whole run (so it still joins to the narrative dialogue
        // and the play-time summary), and this per-island attempt counter keeps
        // each re-run cleanly separated instead of overwriting the previous one.
        private static readonly Dictionary<string, int> _attemptByIsland =
            new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>1-based attempt number for the CPT run currently in progress.</summary>
        public static int CurrentAttempt { get; private set; } = 1;

        /// <summary>
        /// Call once at the start of every real CPT run (OnGameBeginReal). Increments
        /// the attempt counter for that island and updates CurrentAttempt so all
        /// trial/distractor rows logged during the run are tagged with the right attempt.
        /// </summary>
        public static void BeginCptAttempt(string islandId)
        {
            var key = string.IsNullOrWhiteSpace(islandId) ? "UNKNOWN" : islandId.Trim();
            _attemptByIsland.TryGetValue(key, out var n);
            n += 1;
            _attemptByIsland[key] = n;
            CurrentAttempt = n;
        }

        // ---------- PUBLIC API ----------
        public static void CreateReportCSV()
        {
            EnsureSessionId();

#if UNITY_EDITOR
            var path = GetCSVPath();
            EnsureDirectory(path);

            if (File.Exists(path))
                return;

            using (var sw = File.CreateText(path))
                sw.WriteLine(string.Join(CSVSeperator, CSVHeaders));
#endif
        }

        public static void AppendToReportCSV(Report report)
        {
            var inv = CultureInfo.InvariantCulture;

            string sessionId = !string.IsNullOrWhiteSpace(report.SessionId)
                ? report.SessionId
                : CurrentSessionId;

            // Absolute wall-clock timestamp for this trial (shared by CSV + Firebase).
            string clientUtc = NowUtcIso();

            // Attempt number for the CPT run this trial belongs to.
            int attempt = CurrentAttempt;

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

            string isiMs = (report.InterStimulusIntervalMs >= 0)
                ? report.InterStimulusIntervalMs.ToString(inv)
                : "";
            string isiBaseIdx = (report.IsiBaseIndex >= 0) ? report.IsiBaseIndex.ToString(inv) : "";

            // phase_trial_index: blank if unset (< 0)
            string phaseTrial = (report.PhaseTrialIndex >= 0)
                ? report.PhaseTrialIndex.ToString(inv)
                : "";

            // trial_index: blank if unset (< 0)
            string trialIndex = (report.TrialIndex >= 0)
                ? report.TrialIndex.ToString(inv)
                : "";

#if UNITY_EDITOR
            var path = GetCSVPath();
            EnsureDirectory(path);

            if (!File.Exists(path))
            {
                using (var swHead = File.CreateText(path))
                    swHead.WriteLine(string.Join(CSVSeperator, CSVHeaders));
            }

            var row = string.Join(CSVSeperator, new[]
            {
                Escape(report.ParticipantId),
                Escape(sessionId),
                Escape(clientUtc),
                attempt.ToString(inv),

                trialIndex,

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

                rt,
                rtNonTarget,

                report.Correct.ToString(),
                Escape(report.Outcome),

                report.Attentiveness.ToString(),
                report.Timeliness.ToString(),
                report.HyperReactiveness.ToString(),
                report.Impulsiveness.ToString(),
                report.HyperReactiveCount.ToString(inv),

                isiMs,
                isiBaseIdx,
                report.InterStimulusScheduleSeed.ToString(inv),
                report.TrialPlanSeed.ToString(inv),
                report.IsiPermutationSeed.ToString(inv),
            });

            using (var sw = File.AppendText(path))
                sw.WriteLine(row);
#endif

            UploadToFirebase(report, sessionId, clientUtc, attempt);
        }

        // ---------- INTERNALS ----------
        private static readonly string[] CSVHeaders = new[]
        {
            "participant_id",
            "session_id",
            "client_utc",
            "attempt",

            "trial_index",

            "phase",
            "phase_trial_index",

            "island_id",
            "island_name",

            "stimulus_type",
            "stimulus_name",
            "stimulus_duration_ms",
            "stimulus_onset_ms",
            "stimulus_offset_ms",
            "stimulus_actual_duration_ms",

            "response_made",

            "reaction_time_ms",
            "reaction_time_ms_non_target",

            "correct",
            "outcome",

            "attentiveness",
            "timeliness",
            "hyperreactiveness",
            "impulsiveness",
            "hyperreactive_count",

            "inter_stimulus_interval_ms",
            "isi_base_index",
            "isi_study_seed",
            "trial_plan_seed",
            "isi_permutation_seed"
        };

#if UNITY_EDITOR
        private static string GetCSVPath()
        {
            var pid = GetCurrentParticipantId();
            var safePid = Sanitize(pid);

            var dir = System.IO.Path.Combine(Environment.CurrentDirectory, "Assets", "Resources", "ParticipantData", "ReportData");
            return System.IO.Path.Combine(dir, $"P_{safePid}_MoxoCPT.csv");
        }

        private static void EnsureDirectory(string filePath)
        {
            var dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Unknown";
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
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
#endif

        private static string GetCurrentParticipantId()
        {
            EnsureParticipantId();
            return CurrentParticipantId;
        }

        // ---------- Firebase upload ----------

        private static void UploadToFirebase(Report r, string sessionId, string clientUtc, int attempt)
        {
            var svc = FirebaseService.Instance;
            if (svc == null) return;

            var pid    = !string.IsNullOrWhiteSpace(r.ParticipantId) ? r.ParticipantId : GetCurrentParticipantId();
            var safePid = FirebaseService.SanitizeKey(pid);
            var safeSid = FirebaseService.SanitizeKey(sessionId);
            var safeIsland = FirebaseService.SanitizeKey(
                string.IsNullOrWhiteSpace(r.IslandId) ? "UNKNOWN" : r.IslandId);
            var key     = $"t{(r.TrialIndex >= 0 ? r.TrialIndex.ToString() : "x")}";

            // Attempt segment keeps re-runs of the SAME island from overwriting each
            // other (this path uses PUT, so without it a replay would clobber attempt 1).
            var path = $"umaki/cpt_trials/{safePid}/{safeSid}/{safeIsland}/a{attempt}/{key}";
            var json = BuildCPTTrialJson(r, sessionId, clientUtc, attempt);

            svc.PutJson(path, json);
        }

        private static string BuildCPTTrialJson(Report r, string sessionId, string clientUtc, int attempt)
        {
            var sb = new StringBuilder(512);
            sb.Append(FirebaseService.JS("participant_id",              r.ParticipantId));
            sb.Append(FirebaseService.JS("session_id",                  sessionId));
            sb.Append(FirebaseService.JS("client_utc",                  clientUtc));
            sb.Append(FirebaseService.JN("attempt",                     attempt));
            sb.Append(FirebaseService.JN("trial_index",                 r.TrialIndex));
            sb.Append(FirebaseService.JS("phase",                       r.Phase));
            sb.Append(FirebaseService.JN("phase_trial_index",           r.PhaseTrialIndex));
            sb.Append(FirebaseService.JS("island_id",                   r.IslandId));
            sb.Append(FirebaseService.JS("island_name",                 r.IslandName));
            sb.Append(FirebaseService.JS("stimulus_type",               r.StimulusType));
            sb.Append(FirebaseService.JS("stimulus_name",               r.StimulusName));
            sb.Append(FirebaseService.JN("stimulus_duration_ms",        r.StimulusDurationMs));
            sb.Append(FirebaseService.JN("stimulus_onset_ms",           r.StimulusOnsetMs));
            sb.Append(FirebaseService.JN("stimulus_offset_ms",          r.StimulusOffsetMs));
            sb.Append(FirebaseService.JN("stimulus_actual_duration_ms", r.StimulusActualDurationMs));
            sb.Append(FirebaseService.JB("response_made",               r.ResponseMade));
            sb.Append(FirebaseService.JN("reaction_time_ms",            r.ReactionTimeMs));
            sb.Append(FirebaseService.JN("reaction_time_ms_non_target", r.ReactionTimeNonTargetMs));
            sb.Append(FirebaseService.JB("correct",                     r.Correct));
            sb.Append(FirebaseService.JS("outcome",                     r.Outcome));
            sb.Append(FirebaseService.JB("attentiveness",               r.Attentiveness));
            sb.Append(FirebaseService.JB("timeliness",                  r.Timeliness));
            sb.Append(FirebaseService.JB("hyperreactiveness",           r.HyperReactiveness));
            sb.Append(FirebaseService.JB("impulsiveness",               r.Impulsiveness));
            sb.Append(FirebaseService.JN("hyperreactive_count",         r.HyperReactiveCount));
            sb.Append(FirebaseService.JN("inter_stimulus_interval_ms",  r.InterStimulusIntervalMs));
            sb.Append(FirebaseService.JN("isi_base_index",              r.IsiBaseIndex));
            sb.Append(FirebaseService.JN("isi_study_seed",              r.InterStimulusScheduleSeed));
            sb.Append(FirebaseService.JN("trial_plan_seed",             r.TrialPlanSeed));
            sb.Append(FirebaseService.JN("isi_permutation_seed",        r.IsiPermutationSeed));
            return FirebaseService.WrapJson(sb.ToString());
        }
    }
}
