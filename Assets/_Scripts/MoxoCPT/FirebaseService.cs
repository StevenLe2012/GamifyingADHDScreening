using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    ///   umaki/cpt_trials/{participantId}/{sessionId}/{islandId}/a{attempt}/t{trialIndex}
    ///   umaki/distractor_events/{participantId}/{sessionId}/{clientKey}
    ///   umaki/dialogue_choices/{participantId}/{sessionId}/{clientKey}
    ///   umaki/session_summary/{participantId}/{sessionId}  (play time at end)
    ///
    /// RELIABILITY
    /// -----------
    /// All writes are idempotent PUTs queued through a retry worker (exponential
    /// backoff). PostJson generates a unique client key so a retry never creates a
    /// duplicate. On WebGL the queue is mirrored to localStorage, so a reload
    /// resumes any unsent writes, and a pagehide/visibility-hidden handler flushes
    /// pending writes with fetch(keepalive) so the final trials + summary still land
    /// even if the participant closes the tab (see Plugins/WebGL/FirebaseWebGL.jslib).
    ///
    /// SECURITY
    /// --------
    /// Set the project's Web API key in the inspector (or DefaultWebApiKey below) to
    /// enable Anonymous Authentication. When set, every write is signed with a
    /// short-lived ID token (?auth=...), so you can deploy locked-down rules that
    /// require `auth != null` (see Umaki_WebGL/database.rules.json).
    ///
    /// When the Web API key is empty, the service falls back to unauthenticated
    /// writes (legacy behaviour) so existing open rules keep working. To harden:
    ///   1. Fill in the Web API key.
    ///   2. Deploy the validated rules in database.rules.json.
    /// </summary>
    public class FirebaseService : MonoBehaviour
    {
        // ── Change this if your RTDB URL differs ──────────────────────────────
        // europe-west1 databases use .europe-west1.firebasedatabase.app, NOT .firebaseio.com
        private const string DefaultRtdbUrl =
            "https://umaki-f44d9-default-rtdb.europe-west1.firebasedatabase.app";

        // Firebase Web API key (Project settings → General → Web API Key).
        // Leave empty to disable auth (unauthenticated writes). It is safe to ship
        // this key in a client build; it only identifies the project — security is
        // enforced by the RTDB rules (auth != null) + Anonymous Authentication.
        private const string DefaultWebApiKey = "AIzaSyCOMhWzQtcbh5o4LH2zNu4qJohzQUSh1lI";
        // ─────────────────────────────────────────────────────────────────────

        [Tooltip("Firebase Realtime Database base URL (no trailing slash).")]
        [SerializeField] private string _rtdbUrl = DefaultRtdbUrl;

        [Tooltip("Firebase Web API key. When set, writes are signed with an Anonymous Auth token so locked-down rules (auth != null) can be used. Leave empty for legacy unauthenticated writes.")]
        [SerializeField] private string _webApiKey = DefaultWebApiKey;

        // Anonymous Auth token state (shared across the singleton's lifetime).
        private string _idToken = "";
        private double _tokenExpiresAtRealtime = -1.0;  // Time.realtimeSinceStartupAsDouble when the token expires
        private bool _authInFlight;

        // ── Retry queue ─────────────────────────────────────────────────────────
        private const string QueueStorageKey = "umaki_fb_queue_v1";
        private const float InitialBackoffSeconds = 1f;
        private const float MaxBackoffSeconds = 30f;
        private const int RotateAfterFailures = 5;  // move a stubborn item to the back so it can't starve others

        private readonly List<QueuedWrite> _queue = new List<QueuedWrite>();
        private bool _workerRunning;
        private float _backoffSeconds = InitialBackoffSeconds;
        private static long _eventSeq;

        [Serializable]
        private class QueuedWrite
        {
            public string method;
            public string path;
            public string body;
            [NonSerialized] public int attempts;
        }

        [Serializable]
        private class QueueWrapper
        {
            public List<QueuedWrite> items = new List<QueuedWrite>();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void FB_StorageSet(string key, string val);
        [DllImport("__Internal")] private static extern string FB_StorageGet(string key);
        [DllImport("__Internal")] private static extern void FB_StorageRemove(string key);
        [DllImport("__Internal")] private static extern void FB_SetFlushContext(string baseUrl, string token);
        [DllImport("__Internal")] private static extern void FB_RegisterUnloadFlush(string key);
#endif

        public static FirebaseService Instance { get; private set; }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only opt-in. When false (default), Editor Play sessions DO NOT write
        /// to the production Realtime Database. Set to true from a menu command or
        /// inline in code if you specifically want to test the upload pipeline.
        /// Has no effect in real builds — those always upload.
        /// </summary>
        public static bool AllowUploadInEditor = false;
#endif

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

#if UNITY_WEBGL && !UNITY_EDITOR
            // Last-chance flush on tab close, and resume any writes left over from a
            // previous page load that closed before the queue drained.
            FB_RegisterUnloadFlush(QueueStorageKey);
            UpdateFlushContext();  // prime base URL immediately (token filled in once signed in)
#endif
            LoadQueue();
            if (_queue.Count > 0)
                EnsureWorker();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Writes JSON to a deterministic path (idempotent PUT), queued with retry.</summary>
        public void PutJson(string path, string json)
            => EnqueueWrite(path, json);

        /// <summary>
        /// Writes JSON under the path with a unique client-generated key. Uses an
        /// idempotent PUT (not POST) so a retry — or a resend after a page reload —
        /// never creates a duplicate record.
        /// </summary>
        public void PostJson(string path, string json)
            => EnqueueWrite($"{path}/{NewEventKey()}", json);

        private static string NewEventKey()
        {
            // ticks (100ns) + per-run sequence + random → unique even across reloads.
            _eventSeq++;
            return $"e{DateTime.UtcNow.Ticks}_{_eventSeq}_{UnityEngine.Random.Range(0, 1000000)}";
        }

        // ── Queue + retry worker ────────────────────────────────────────────────

        private void EnqueueWrite(string path, string json)
        {
#if UNITY_EDITOR
            if (!AllowUploadInEditor)
            {
                Debug.Log($"[FirebaseService] Editor: skipping PUT → {path} (set FirebaseService.AllowUploadInEditor = true to enable).");
                return;
            }
#endif
            _queue.Add(new QueuedWrite { method = "PUT", path = path, body = json });
            PersistQueue();
            EnsureWorker();
        }

        private void EnsureWorker()
        {
            if (_workerRunning) return;
            _workerRunning = true;
            StartCoroutine(Worker());
        }

        private IEnumerator Worker()
        {
            while (_queue.Count > 0)
            {
                if (!string.IsNullOrEmpty(_webApiKey))
                    yield return EnsureAuth();

                // Keep the JS unload-flush handler primed with a fresh token.
                UpdateFlushContext();

                var item = _queue[0];
                bool ok = false;
                yield return SendOnce(item, success => ok = success);

                if (ok)
                {
                    _queue.RemoveAt(0);
                    PersistQueue();
                    _backoffSeconds = InitialBackoffSeconds;
                }
                else
                {
                    item.attempts++;

                    // Don't let one stubborn item starve the rest of the queue.
                    if (item.attempts >= RotateAfterFailures && _queue.Count > 1)
                    {
                        _queue.RemoveAt(0);
                        item.attempts = 0;
                        _queue.Add(item);
                        PersistQueue();
                    }

                    yield return new WaitForSeconds(_backoffSeconds);
                    _backoffSeconds = Mathf.Min(_backoffSeconds * 2f, MaxBackoffSeconds);
                }
            }

            _workerRunning = false;
        }

        private IEnumerator SendOnce(QueuedWrite item, Action<bool> done)
        {
            var query = !string.IsNullOrEmpty(_idToken)
                ? $"?auth={UnityWebRequest.EscapeURL(_idToken)}"
                : "";

            var url       = $"{_rtdbUrl}/{item.path}.json{query}";
            var bodyBytes = Encoding.UTF8.GetBytes(item.body);

            using (var req = new UnityWebRequest(url, item.method))
            {
                req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                bool success = req.result == UnityWebRequest.Result.Success;
                if (success)
                {
                    Debug.Log($"[FirebaseService] PUT OK → {item.path}");
                }
                else
                {
                    // 401/403 usually means the auth token expired/was rejected —
                    // drop it so the next loop re-signs in before retrying.
                    if (req.responseCode == 401 || req.responseCode == 403)
                    {
                        _idToken = "";
                        _tokenExpiresAtRealtime = -1.0;
                    }
                    Debug.LogWarning($"[FirebaseService] PUT retry ({item.path}): {req.error} | HTTP {req.responseCode}");
                }

                done(success);
            }
        }

        // ── Queue persistence (WebGL localStorage) ───────────────────────────────

        private void PersistQueue()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_queue.Count == 0)
            {
                FB_StorageRemove(QueueStorageKey);
                return;
            }
            var wrapper = new QueueWrapper { items = _queue };
            FB_StorageSet(QueueStorageKey, JsonUtility.ToJson(wrapper));
#endif
        }

        private void LoadQueue()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var raw = FB_StorageGet(QueueStorageKey);
            if (string.IsNullOrEmpty(raw)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<QueueWrapper>(raw);
                if (wrapper != null && wrapper.items != null && wrapper.items.Count > 0)
                {
                    _queue.AddRange(wrapper.items);
                    Debug.Log($"[FirebaseService] Resumed {wrapper.items.Count} pending write(s) from a previous session.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FirebaseService] Could not parse persisted queue: {e.Message}");
            }
#endif
        }

        private void UpdateFlushContext()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FB_SetFlushContext(_rtdbUrl, _idToken ?? "");
#endif
        }

        // ── Anonymous Authentication ────────────────────────────────────────────

        /// <summary>
        /// Ensures a valid Anonymous Auth ID token is available, signing in (or
        /// re-signing in) via the Identity Toolkit REST API when needed. Safe to
        /// call from multiple coroutines: concurrent callers wait for the in-flight
        /// sign-in to finish.
        /// </summary>
        private IEnumerator EnsureAuth()
        {
            // Token still valid (with a small safety margin)? Nothing to do.
            if (!string.IsNullOrEmpty(_idToken) &&
                Time.realtimeSinceStartupAsDouble < _tokenExpiresAtRealtime)
                yield break;

            // Another coroutine is already signing in — wait for it.
            if (_authInFlight)
            {
                while (_authInFlight)
                    yield return null;
                yield break;
            }

            _authInFlight = true;

            var signInUrl =
                $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_webApiKey}";
            var bodyBytes = Encoding.UTF8.GetBytes("{\"returnSecureToken\":true}");

            using (var req = new UnityWebRequest(signInUrl, "POST"))
            {
                req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    ParseAuthResponse(req.downloadHandler.text);
                    Debug.Log("[FirebaseService] Anonymous Auth OK.");
                }
                else
                {
                    Debug.LogWarning(
                        $"[FirebaseService] Anonymous Auth FAILED: {req.error} | HTTP {req.responseCode} | {req.downloadHandler.text}");
                }
            }

            _authInFlight = false;
        }

        /// <summary>
        /// Minimal extraction of idToken / expiresIn from the Identity Toolkit JSON
        /// response (avoids pulling in a full JSON parser for two fields).
        /// </summary>
        private void ParseAuthResponse(string responseJson)
        {
            _idToken = ExtractJsonString(responseJson, "idToken");

            var expiresInStr = ExtractJsonString(responseJson, "expiresIn");
            if (!double.TryParse(expiresInStr, System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture, out var expiresInSec))
                expiresInSec = 3600.0;

            // Refresh a minute early to avoid using a token that expires mid-request.
            _tokenExpiresAtRealtime =
                Time.realtimeSinceStartupAsDouble + Math.Max(0.0, expiresInSec - 60.0);
        }

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return "";

            var needle = $"\"{key}\"";
            var ki = json.IndexOf(needle, StringComparison.Ordinal);
            if (ki < 0) return "";

            var colon = json.IndexOf(':', ki + needle.Length);
            if (colon < 0) return "";

            int i = colon + 1;
            while (i < json.Length && (json[i] == ' ' || json[i] == '\t')) i++;
            if (i >= json.Length) return "";

            // Quoted string value
            if (json[i] == '"')
            {
                int start = i + 1;
                var sb = new StringBuilder();
                for (int j = start; j < json.Length; j++)
                {
                    char c = json[j];
                    if (c == '\\' && j + 1 < json.Length) { sb.Append(json[j + 1]); j++; continue; }
                    if (c == '"') break;
                    sb.Append(c);
                }
                return sb.ToString();
            }

            // Bare value (number) — read until , } or whitespace
            int s = i;
            while (i < json.Length && json[i] != ',' && json[i] != '}' &&
                   json[i] != ' ' && json[i] != '\n' && json[i] != '\r' && json[i] != '\t')
                i++;
            return json.Substring(s, i - s);
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
