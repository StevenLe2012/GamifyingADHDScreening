using System;
using System.Globalization;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
#endif

namespace MoxoCPT
{
    /// <summary>
    /// Tight dialogue choice CSV logger (one row per option selection).
    /// Timebase: Time.realtimeSinceStartup * 1000 (ms).
    /// </summary>
    public static class LoggingDialogueChoices
    {
        private const string CSV = ",";

        // ---- session-scoped runtime context ----
        private static string _conversationId = "";
        private static int _conversationStep = 0;

        private static string _npc = "";
        private static string _requiredStateKey = "";
        private static string _onEnterEventName = "";

        private static string _phase = "";
        private static string _islandId = "";

        private static long _optionsShownMs = -1;
        private static string _optionsPresentedText = "";
        private static int _optionsCount = 0;
        private static int _selectionChangedCount = 0;

        // Public access if you need it
        public static string CurrentConversationId => _conversationId;
        public static int CurrentConversationStep => _conversationStep;

        // ---------- Public API ----------

        /// <summary>
        /// Call when a conversation begins (e.g., StartFromKey / TryStartConversationFromState).
        /// Creates a new conversation_id and resets step counters.
        /// </summary>
        public static void BeginConversation(string npc, string islandId, string phase)
        {
            _conversationId = "C_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            _conversationStep = 0;

            _npc = npc ?? "";
            _islandId = islandId ?? "";
            _phase = phase ?? "";

            _requiredStateKey = "";
            _onEnterEventName = "";

            _optionsShownMs = -1;
            _optionsPresentedText = "";
            _optionsCount = 0;
            _selectionChangedCount = 0;

#if UNITY_EDITOR
            CreateCSVIfMissing();
#endif
        }

        /// <summary>
        /// Call once per dialogue unit displayed (before showing options).
        /// Increments conversation_step.
        /// </summary>
        public static void SetDialogueContext(
            string npc,
            string requiredStateKey,
            string onEnterEventName,
            string islandId,
            string phase
        )
        {
            if (string.IsNullOrWhiteSpace(_conversationId))
            {
                // Safety: if caller forgot BeginConversation, start one implicitly.
                BeginConversation(npc, islandId, phase);
            }

            _npc = npc ?? _npc ?? "";
            _requiredStateKey = requiredStateKey ?? "";
            _onEnterEventName = onEnterEventName ?? "";

            _islandId = islandId ?? _islandId ?? "";
            _phase = phase ?? _phase ?? "";

            _conversationStep++;
        }

        /// <summary>
        /// Call exactly when options become visible (after text is populated).
        /// Store snapshot + options_shown_ms.
        /// </summary>
        public static void OptionsShown(long optionsShownMs, string optionsPresentedText, int optionsCount, int selectionChangedCountAtOpen = 0)
        {
            _optionsShownMs = optionsShownMs;
            _optionsPresentedText = optionsPresentedText ?? "";
            _optionsCount = Mathf.Max(0, optionsCount);
            _selectionChangedCount = Mathf.Max(0, selectionChangedCountAtOpen);
        }

        /// <summary>
        /// Update selection_changed_count during navigation.
        /// (Call from StickOptionSelector each time selection moves.)
        /// </summary>
        public static void AddSelectionChange()
        {
            _selectionChangedCount++;
        }

        /// <summary>
        /// Log one row at the moment an option is chosen.
        /// </summary>
        public static void LogChoice(int selectedOptionIndex, string selectedOptionText, long choiceTimeMs)
        {
            // Compute RT only if we have a valid options_shown_ms
            long rt = (_optionsShownMs > 0) ? (choiceTimeMs - _optionsShownMs) : -1;

            // Simple leak heuristic: super fast is usually accidental input carryover
            bool leak = (rt >= 0 && rt < 150);

            var pid       = GetCurrentParticipantId();
            var sessionId = MoxoCPT.LoggingReport.CurrentSessionId ?? "";
            var inv       = CultureInfo.InvariantCulture;

#if UNITY_EDITOR
            CreateCSVIfMissing();

            var row = string.Join(CSV, new[]
            {
                // identifiers
                Escape(pid),
                Escape(sessionId),
                Escape(_islandId),
                Escape(_phase),

                // context
                Escape(_npc),
                Escape(_requiredStateKey),
                Escape(_onEnterEventName),
                Escape(_conversationId),

                // timing
                _optionsShownMs > 0 ? _optionsShownMs.ToString(inv) : "",
                choiceTimeMs > 0 ? choiceTimeMs.ToString(inv) : "",
                rt >= 0 ? rt.ToString(inv) : "",

                // choice
                selectedOptionIndex.ToString(inv),
                Escape(selectedOptionText),

                // snapshot + behavior
                Escape(_optionsPresentedText),
                _selectionChangedCount.ToString(inv),
                _optionsCount.ToString(inv),
                leak.ToString(),

                // step
                _conversationStep.ToString(inv),
            });

            using (var sw = File.AppendText(GetCSVPath()))
                sw.WriteLine(row);
#endif

            UploadToFirebase(pid, sessionId, selectedOptionIndex, selectedOptionText,
                             choiceTimeMs, rt, leak, inv);
        }

        // ---------- CSV headers ----------
        private static readonly string[] Headers = new[]
        {
            "participant_id",
            "session_id",
            "island_id",
            "phase",

            "npc",
            "required_state_key",
            "on_enter_event_name",
            "conversation_id",

            "options_shown_ms",
            "choice_time_ms",
            "reaction_time_ms",

            "selected_option_index",
            "selected_option_text",

            "options_presented_text",
            "selection_changed_count",
            "options_count",
            "was_input_leak_suspected",

            "conversation_step"
        };

        // ---------- File helpers ----------
#if UNITY_EDITOR
        private static void CreateCSVIfMissing()
        {
            var path = GetCSVPath();
            EnsureDirectory(path);

            if (!File.Exists(path))
            {
                using (var sw = File.CreateText(path))
                    sw.WriteLine(string.Join(CSV, Headers));
            }
        }

        private static string GetCSVPath()
        {
            var pid = GetCurrentParticipantId();
            var safePid = Sanitize(pid);

            var dir = System.IO.Path.Combine(Environment.CurrentDirectory, "Assets", "Resources", "ParticipantData", "ReportData");
            return System.IO.Path.Combine(dir, $"P_{safePid}_DialogueChoice.csv");
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
            var gm = GameManager.Instance;
            var pid = (gm != null ? gm.ParticipantId : null);

            if (string.IsNullOrWhiteSpace(pid))
                pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            return pid;
        }

        // ---------- Firebase upload ----------

        private static void UploadToFirebase(
            string pid, string sessionId,
            int selectedOptionIndex, string selectedOptionText,
            long choiceTimeMs, long rt, bool leak,
            IFormatProvider inv)
        {
            var svc = FirebaseService.Instance;
            if (svc == null) return;

            var safePid = FirebaseService.SanitizeKey(pid);
            var safeSid = FirebaseService.SanitizeKey(sessionId);
            var path    = $"umaki/dialogue_choices/{safePid}/{safeSid}";

            var sb = new StringBuilder(512);
            sb.Append(FirebaseService.JS("participant_id",          pid));
            sb.Append(FirebaseService.JS("session_id",              sessionId));
            sb.Append(FirebaseService.JS("island_id",               _islandId));
            sb.Append(FirebaseService.JS("phase",                   _phase));
            sb.Append(FirebaseService.JS("npc",                     _npc));
            sb.Append(FirebaseService.JS("required_state_key",      _requiredStateKey));
            sb.Append(FirebaseService.JS("on_enter_event_name",     _onEnterEventName));
            sb.Append(FirebaseService.JS("conversation_id",         _conversationId));
            sb.Append(FirebaseService.JN("options_shown_ms",        _optionsShownMs));
            sb.Append(FirebaseService.JN("choice_time_ms",          choiceTimeMs));
            sb.Append(FirebaseService.JN("reaction_time_ms",        rt));
            sb.Append(FirebaseService.JN("selected_option_index",   selectedOptionIndex));
            sb.Append(FirebaseService.JS("selected_option_text",    selectedOptionText));
            sb.Append(FirebaseService.JS("options_presented_text",  _optionsPresentedText));
            sb.Append(FirebaseService.JN("selection_changed_count", _selectionChangedCount));
            sb.Append(FirebaseService.JN("options_count",           _optionsCount));
            sb.Append(FirebaseService.JB("was_input_leak_suspected",leak));
            sb.Append(FirebaseService.JN("conversation_step",       _conversationStep));

            svc.PostJson(path, FirebaseService.WrapJson(sb.ToString()));
        }
    }
}
