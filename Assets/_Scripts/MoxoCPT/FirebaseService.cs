using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MoxoCPT
{
    /// <summary>
    /// Sends data to Firebase Realtime Database via REST API (no Firebase SDK required).
    /// Auto-initialises itself before any scene loads.
    ///
    /// Other scripts call:
    ///   FirebaseService.Instance?.PutJson(path, json)   — deterministic key (CPT trials)
    ///   FirebaseService.Instance?.PostJson(path, json)  — Firebase push-key (events/choices)
    ///
    /// Data is stored under:
    ///   umaki/cpt_trials/{participantId}/{sessionId}/t{trialIndex}
    ///   umaki/distractor_events/{participantId}/{sessionId}  (push)
    ///   umaki/dialogue_choices/{participantId}/{sessionId}   (push)
    ///
    /// Firebase Realtime Database security rules (set in console):
    ///   { "rules": { "umaki": { ".read": false, ".write": true } } }
    /// </summary>
    public class FirebaseService : MonoBehaviour
    {
        // ── Change this if your RTDB URL differs ──────────────────────────────
        // europe-west1 databases use .europe-west1.firebasedatabase.app, NOT .firebaseio.com
        private const string DefaultRtdbUrl =
            "https://umaki-f44d9-default-rtdb.europe-west1.firebasedatabase.app";
        // ─────────────────────────────────────────────────────────────────────

        [Tooltip("Firebase Realtime Database base URL (no trailing slash).")]
        [SerializeField] private string _rtdbUrl = DefaultRtdbUrl;

        public static FirebaseService Instance { get; private set; }

        // Auto-create before any scene loads so logging classes can always find it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null) return;
            var go = new GameObject("[FirebaseService]");
            go.AddComponent<FirebaseService>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>PUT: writes to a deterministic path (idempotent).</summary>
        public void PutJson(string path, string json)
            => StartCoroutine(Send("PUT", path, json));

        /// <summary>POST: Firebase generates a unique push-key under the path.</summary>
        public void PostJson(string path, string json)
            => StartCoroutine(Send("POST", path, json));

        // ── Internal ──────────────────────────────────────────────────────────

        private IEnumerator Send(string method, string path, string json)
        {
            var url       = $"{_rtdbUrl}/{path}.json";
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            using (var req = new UnityWebRequest(url, method))
            {
                req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning($"[FirebaseService] {method} FAILED ({path}): {req.error} | HTTP {req.responseCode}");
                else
                    Debug.Log($"[FirebaseService] {method} OK → {path}");
            }
        }

        // ── Path helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Sanitise a string for use as a Firebase RTDB path segment.
        /// RTDB keys cannot contain . # $ [ ] /
        /// </summary>
        public static string SanitizeKey(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            return s.Replace('.', '-')
                    .Replace('#', '-')
                    .Replace('$', '-')
                    .Replace('[', '-')
                    .Replace(']', '-')
                    .Replace('/', '-')
                    .Replace(' ', '_');
        }

        // ── JSON field helpers (used by logging classes) ───────────────────────

        public static string JS(string k, string v)
        {
            if (v == null) return $"\"{k}\":null,";
            v = v.Replace("\\", "\\\\").Replace("\"", "\\\"")
                 .Replace("\n", "\\n").Replace("\r", "\\r");
            return $"\"{k}\":\"{v}\",";
        }

        public static string JN(string k, int v)
            => $"\"{k}\":{v},";

        public static string JN(string k, long v)
            => $"\"{k}\":{v},";

        public static string JN(string k, float v)
            => $"\"{k}\":{v.ToString(System.Globalization.CultureInfo.InvariantCulture)},";

        public static string JN(string k, float? v)
            => v.HasValue
               ? $"\"{k}\":{v.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},"
               : $"\"{k}\":null,";

        public static string JB(string k, bool v)
            => $"\"{k}\":{(v ? "true" : "false")},";

        /// <summary>
        /// Wraps a comma-separated sequence of field strings into a JSON object.
        /// Handles the trailing comma left by the JX helpers automatically.
        /// </summary>
        public static string WrapJson(string fields)
        {
            if (string.IsNullOrEmpty(fields)) return "{}";
            if (fields[fields.Length - 1] == ',')
                fields = fields.Substring(0, fields.Length - 1);
            return "{" + fields + "}";
        }
    }
}
