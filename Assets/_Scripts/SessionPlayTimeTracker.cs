using System;
using System.Text;
using MoxoCPT;
using UnityEngine;

/// <summary>
/// Tracks wall-clock play time from intro video start until the ending cutscene finishes.
/// Keeps counting while the browser tab is hidden so breaks / tab switches are measurable.
/// Uploads one summary row to Firebase RTDB at session end.
/// </summary>
public class SessionPlayTimeTracker : MonoBehaviour
{
    static SessionPlayTimeTracker _instance;

    static bool _running;
    static bool _finished;
    static DateTime _startUtc;
    static DateTime? _endUtc;
    static DateTime? _hiddenSinceUtc;
    static long _tabHiddenMs;
    static int _tabHiddenCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _instance = null;
        _running = false;
        _finished = false;
        _startUtc = default;
        _endUtc = null;
        _hiddenSinceUtc = null;
        _tabHiddenMs = 0;
        _tabHiddenCount = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("[SessionPlayTimeTracker]");
        _instance = go.AddComponent<SessionPlayTimeTracker>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Begin timing when the intro video starts (IntroBoot). No-op if already started.</summary>
    public static void StartSession()
    {
        if (_running || _finished) return;

        _running = true;
        _startUtc = DateTime.UtcNow;
        _endUtc = null;
        _hiddenSinceUtc = null;
        _tabHiddenMs = 0;
        _tabHiddenCount = 0;

        LoggingReport.EnsureSessionId();

        Debug.Log($"[SessionPlayTimeTracker] Started at {_startUtc:O} session={LoggingReport.CurrentSessionId}");
    }

    /// <summary>Stop timing and upload summary (called when the game ends).</summary>
    public static void FinishSession()
    {
        if (_finished) return;
        if (!_running)
        {
            Debug.LogWarning("[SessionPlayTimeTracker] FinishSession called but session never started.");
            return;
        }

        _finished = true;
        _running = false;
        _endUtc = DateTime.UtcNow;

        if (_hiddenSinceUtc.HasValue)
        {
            _tabHiddenMs += (long)(_endUtc.Value - _hiddenSinceUtc.Value).TotalMilliseconds;
            _hiddenSinceUtc = null;
        }

        var totalMs = GetTotalDurationMs();
        var hiddenMs = _tabHiddenMs;
        var visibleMs = Math.Max(0, totalMs - hiddenMs);

        Debug.Log(
            $"[SessionPlayTimeTracker] Finished. total={totalMs}ms visible={visibleMs}ms " +
            $"tab_hidden={hiddenMs}ms tab_switches={_tabHiddenCount} session={LoggingReport.CurrentSessionId}");

        UploadSummary(totalMs, visibleMs, hiddenMs);
    }

    public static long GetTotalDurationMs()
    {
        if (!_running && !_finished) return 0;
        var end = _endUtc ?? DateTime.UtcNow;
        return (long)(end - _startUtc).TotalMilliseconds;
    }

    public static long GetTabHiddenDurationMs()
    {
        var hidden = _tabHiddenMs;
        if (_running && _hiddenSinceUtc.HasValue)
            hidden += (long)(DateTime.UtcNow - _hiddenSinceUtc.Value).TotalMilliseconds;
        return hidden;
    }

    public static int TabHiddenCount => _tabHiddenCount;

    void OnEnable()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLPageVisibility.Register(this);
#endif
    }

    public void OnBrowserVisibilityChanged(int hidden)
    {
        HandleVisibility(hidden == 0);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        HandleVisibility(hasFocus);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        HandleVisibility(!pauseStatus);
    }

    void HandleVisibility(bool isVisible)
    {
        if (!_running || _finished) return;

        var now = DateTime.UtcNow;
        if (!isVisible)
        {
            if (!_hiddenSinceUtc.HasValue)
            {
                _hiddenSinceUtc = now;
                _tabHiddenCount++;
            }
        }
        else if (_hiddenSinceUtc.HasValue)
        {
            _tabHiddenMs += (long)(now - _hiddenSinceUtc.Value).TotalMilliseconds;
            _hiddenSinceUtc = null;
        }
    }

    static void UploadSummary(long totalMs, long visibleMs, long hiddenMs)
    {
        var svc = FirebaseService.Instance;
        if (svc == null) return;

        var pid = ResolveParticipantId();
        var sessionId = LoggingReport.CurrentSessionId;
        if (string.IsNullOrWhiteSpace(sessionId))
            sessionId = "S_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        var safePid = FirebaseService.SanitizeKey(pid);
        var safeSid = FirebaseService.SanitizeKey(sessionId);
        var path = $"umaki/session_summary/{safePid}/{safeSid}";

        var endUtc = _endUtc ?? DateTime.UtcNow;
        var sb = new StringBuilder(384);
        sb.Append(FirebaseService.JS("participant_id", pid));
        sb.Append(FirebaseService.JS("session_id", sessionId));
        sb.Append(FirebaseService.JS("started_at_utc", _startUtc.ToString("o")));
        sb.Append(FirebaseService.JS("finished_at_utc", endUtc.ToString("o")));
        sb.Append(FirebaseService.JN("total_play_duration_ms", totalMs));
        sb.Append(FirebaseService.JN("tab_visible_duration_ms", visibleMs));
        sb.Append(FirebaseService.JN("tab_hidden_duration_ms", hiddenMs));
        sb.Append(FirebaseService.JN("tab_hidden_count", _tabHiddenCount));

        // Frame-cadence quality during the CPT. Frame rate caps RT precision in WebGL,
        // so these let the analysis flag/exclude sessions whose timing is untrustworthy.
        // Values are -1 / 0 when no CPT frames were sampled (treated as blank in CSV).
        sb.Append(FirebaseService.JN("cpt_frame_samples", CptPerformanceMonitor.FrameSamples));
        sb.Append(FirebaseService.JN("cpt_median_fps", Round1(CptPerformanceMonitor.MedianFps)));
        sb.Append(FirebaseService.JN("cpt_p05_fps", Round1(CptPerformanceMonitor.P05Fps)));
        sb.Append(FirebaseService.JN("cpt_frame_ms_median", Round2(CptPerformanceMonitor.FrameMsMedian)));
        sb.Append(FirebaseService.JN("cpt_frame_ms_iqr", Round2(CptPerformanceMonitor.FrameMsIqr)));
        sb.Append(FirebaseService.JN("cpt_long_frame_count", CptPerformanceMonitor.LongFrameCount));

        svc.PutJson(path, FirebaseService.WrapJson(sb.ToString()));
    }

    // Round to a fixed number of decimals and narrow to float for FirebaseService.JN.
    // Sentinels (< 0) pass through unchanged so the CSV converter can blank them.
    static float Round1(double v) => v < 0 ? -1f : (float)Math.Round(v, 1);
    static float Round2(double v) => v < 0 ? -1f : (float)Math.Round(v, 2);

    static string ResolveParticipantId()
    {
        // Use the cached single source of truth so the summary's participant_id
        // matches every other collection (see LoggingReport.EnsureParticipantId).
        LoggingReport.EnsureParticipantId();
        if (!string.IsNullOrWhiteSpace(LoggingReport.CurrentParticipantId))
            return LoggingReport.CurrentParticipantId;

        if (!string.IsNullOrWhiteSpace(ParticipantSession.Number))
            return $"{ParticipantSession.Number}_{DateTime.Today:yyyy-MM-dd}";

        return "unknown_" + DateTime.Today.ToString("yyyy-MM-dd");
    }
}
