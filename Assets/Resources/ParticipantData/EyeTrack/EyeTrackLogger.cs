using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.IO;
using Varjo.XR; // Varjo SDK

/// Add this to a single GameObject (same scene as MOXO).
public class EyeTrackLogger : MonoBehaviour
{
    public static EyeTrackLogger I { get; private set; }

    [Header("Mode gating")]
    [Tooltip("If OFF, no Varjo calls and no CSV logging happen (desktop mode, etc).")]
    [SerializeField] private bool eyeTrackingEnabled = true;

    [Tooltip("If true, logger follows PlayerModeManager: on in VR, off on Desktop.")]
    [SerializeField] private bool autoFollowPlayerMode = true;

    [Tooltip("Optional: assign your PlayerModeManager in the scene.")]
    [SerializeField] private PlayerModeManager playerModeManager;

    public bool EyeTrackingEnabled
    {
        get => eyeTrackingEnabled;
        set
        {
            eyeTrackingEnabled = value;
            if (!eyeTrackingEnabled) EndSession(); // stop cleanly if turned off
        }
    }

    [Header("HMD / Raycasting")]
    [SerializeField] private Transform hmd;                 // XR Origin's Camera (HMD)
    [SerializeField] private LayerMask gazeHitMask;         // Layer(s) of cards/distractors
    [SerializeField] private float maxRayDistance = 20f;

    [Header("CSV options")]
    [SerializeField] private string participantId = "P__";   // you can set this at runtime
    [SerializeField] private string filePrefix = "MoxoCPT";
    [SerializeField] private bool alsoWriteEditorCopy = true; // duplicate to Assets/Resources during play-in-Editor

    // --- session state ---
    private bool _logging;
    private float _taskTime;             // time since MOXO began
    private float _trialTime;            // time since current trial began
    private int   _trialIndex = -1;
    private int   _pressCount = 0;
    private string _lastKey = "N";       // U/D/L/R/A/Space/etc.
    private int   _newTrialFlag = 0;     // 1 on SOA, reset to 0 by writer

    // CSV writers
    private StreamWriter _writer;
    private string _livePath;
    private string _editorMirrorPath;

    // Header (keep columns stable)
    private static readonly string[] Header = new[]{
        "Frame","CaptureTimeNs","LogTimeMs","TrialTimeMs","GazeStatus",
        "GazeForward_X","GazeForward_Y","GazeForward_Z",
        "GazeOrigin_X","GazeOrigin_Y","GazeOrigin_Z",
        "InterPupillaryDistanceInMM",
        "LeftEyeStatus","LeftEyeForward_X","LeftEyeForward_Y","LeftEyeForward_Z","LeftEyeOrigin_X","LeftEyeOrigin_Y","LeftEyeOrigin_Z","LeftPupilIrisDiameterRatio","LeftPupilDiameterInMM","LeftIrisDiameterInMM",
        "RightEyeStatus","RightEyeForward_X","RightEyeForward_Y","RightEyeForward_Z","RightEyeOrigin_X","RightEyeOrigin_Y","RightEyeOrigin_Z","RightPupilIrisDiameterRatio","RightPupilDiameterInMM","RightIrisDiameterInMM",
        "FocusDistance","FocusStability",
        "NewTrial","Trial","KeyPressed","PressCount",
        "HitName","HitTag","HitX","HitY","HitZ","IsTarget"
    };

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (!hmd)
        {
            var cam = Camera.main;
            if (cam) hmd = cam.transform;
        }
    }

    private void Start()
    {
        // Optional: auto-follow PlayerModeManager if assigned
        if (autoFollowPlayerMode && playerModeManager != null)
        {
            EyeTrackingEnabled = (playerModeManager.CurrentMode == PlayerMode.VR);
            playerModeManager.OnModeChanged -= HandleModeChanged;
            playerModeManager.OnModeChanged += HandleModeChanged;
        }
    }

    private void OnDestroy()
    {
        if (playerModeManager != null)
            playerModeManager.OnModeChanged -= HandleModeChanged;
    }

    private void HandleModeChanged(PlayerMode mode)
    {
        EyeTrackingEnabled = (mode == PlayerMode.VR);
    }

    private float _diagTimer = 0f;

    private void Update()
    {
        // If disabled or not logging, do nothing (prevents any Varjo calls)
        if (!eyeTrackingEnabled || !_logging) return;

        _taskTime  += Time.deltaTime;
        _trialTime += Time.deltaTime;

        // --- DIAG: heartbeat once per second ---
        _diagTimer += Time.deltaTime;
        if (_diagTimer >= 1f)
        {
            _diagTimer = 0f;
            Debug.Log($"[EyeTrack] isAllowed={VarjoEyeTracking.IsGazeAllowed()} " +
                      $"isAvailable={VarjoEyeTracking.IsGazeAvailable()} " +
                      $"isCalibrated={VarjoEyeTracking.IsGazeCalibrated()} " +
                      $"logging={_logging} goActive={gameObject.activeInHierarchy}");
        }

        // Pull latest gaze buffer
        List<VarjoEyeTracking.GazeData> g;
        List<VarjoEyeTracking.EyeMeasurements> m;
        int count = VarjoEyeTracking.GetGazeList(out g, out m);

        if (count == 0) return;

        for (int i = 0; i < count; i++)
            LogOne(g[i], m[i]);
    }

    // -------- Public API (call from your game) --------

    // Prepare to record:
    private (string num, string last, string date, string id) GetMetaFromGM()
    {
        var gm = GameManager.Instance;
        string num  = gm != null ? gm.ParticipantNumber : "";
        string last = gm != null ? gm.ParticipantLastName : "";
        string date = gm != null ? gm.SessionDateISO : DateTime.Today.ToString("yyyy-MM-dd");
        string id   = gm != null ? gm.ParticipantId : $"P_{DateTime.Now:yyyyMMdd_HHmmss}";
        return (Sanitize(num), Sanitize(last), Sanitize(date), Sanitize(id));
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Trim();
    }

    private string BuildDataPathFromGM()
    {
        var (num, last, date, id) = GetMetaFromGM();
        var fileName = $"{filePrefix}_{id}.csv";
        var folder   = Path.Combine(Environment.CurrentDirectory, "Logs", "EyeTrack");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, fileName);
    }

    private string[] BuildHeaderWithMeta(string[] originalHeader)
    {
        var metaHeader = new[] { "ParticipantNumber", "LastName", "SessionDate" };
        return metaHeader.Concat(originalHeader).ToArray();
    }

    private string[] PrependMeta(string[] row)
    {
        var (num, last, date, _) = GetMetaFromGM();
        return new[] { num, last, date }.Concat(row).ToArray();
    }

    /// Call when MOXO begins (from MoxoCPTManager.OnGameBegin).
    public void BeginSession(string participantIdOverride = null)
    {
        // Respect the toggle: when OFF, don't start or touch Varjo
        if (!eyeTrackingEnabled)
        {
            Debug.Log("[EyeTrack] BeginSession skipped (eye tracking disabled).");
            return;
        }

        if (_logging) EndSession(); // safety

        // Varjo stream config
        VarjoEyeTracking.SetGazeOutputFilterType(VarjoEyeTracking.GazeOutputFilterType.Standard);
        VarjoEyeTracking.SetGazeOutputFrequency(VarjoEyeTracking.GazeOutputFrequency.MaximumSupported);

        // Reset counters
        _taskTime = 0f;
        _trialTime = 0f;
        _trialIndex = -1;
        _pressCount = 0;
        _lastKey = "N";
        _newTrialFlag = 0;

        // Paths
        string manualSuffix = string.IsNullOrWhiteSpace(participantIdOverride) ? "" : $"_{Sanitize(participantIdOverride)}";
        var mainPath = BuildDataPathFromGM();
        if (!string.IsNullOrEmpty(manualSuffix))
            mainPath = mainPath.Replace(".csv", manualSuffix + ".csv");

        _livePath = mainPath;
        Directory.CreateDirectory(Path.GetDirectoryName(_livePath));
        _writer = new StreamWriter(_livePath);

        // Write header (now with meta cols in front)
        WriteCsvRow(BuildHeaderWithMeta(Header));

#if UNITY_EDITOR
        if (alsoWriteEditorCopy)
        {
            var (num, last, date, id) = GetMetaFromGM();
            _editorMirrorPath = Path.Combine(
                Application.dataPath, "Resources", "ParticipantData", "EyeTrack",
                $"{filePrefix}_{id}.csv"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(_editorMirrorPath));
            File.WriteAllLines(_editorMirrorPath, new[] { string.Join(",", BuildHeaderWithMeta(Header)) });
        }
#endif

        _logging = true;
        Debug.Log($"[EyeTrack] Session started → {_livePath}");
    }

    /// Call when MOXO ends (from MoxoCPTManager.OnGameEnd).
    public void EndSession()
    {
        if (!_logging) return;
        _logging = false;
        _writer?.Flush();
        _writer?.Close();
        _writer = null;
        Debug.Log("[EyeTrack] Session ended.");
    }

    // public read-only flag so other scripts can check if logging is active
    public bool IsLogging => _logging;

    // convenience overload so callers can pass a char or string
    public void RecordPress(char key) => RegisterPress(key.ToString());
    public void RecordPress(string key) => RegisterPress(key);

    /// Call at the start of each trial (e.g., when you present a new card).
    public void MarkNewTrial(int trialIndex)
    {
        _trialIndex = trialIndex;
        _trialTime = 0f;
        _newTrialFlag = 1; // will be reset after the next row is written
    }

    /// Call from your ButtonPress when the response is made (during CPT only).
    public void RegisterPress(string key)
    {
        _pressCount++;
        _lastKey = key; // will reset to "N" after logging one row
    }

    /// Optional: set participant id anytime (e.g., from a UI form).
    public void SetParticipant(string id) => participantId = id;

    // -------------- internals --------------

    private string BuildDataPath()
    {
        var dir = Path.Combine(Application.persistentDataPath, "ParticipantData", "EyeTrack");
        var file = $"{filePrefix}_{participantId}_{DateTime.Now:yyyyMMdd-HHmmss}.csv";
        return Path.Combine(dir, file);
    }

    private void LogOne(VarjoEyeTracking.GazeData d, VarjoEyeTracking.EyeMeasurements meas)
    {
        // Combined gaze validity
        bool invalid = d.status == VarjoEyeTracking.GazeStatus.Invalid;

        // Transform local-to-HMD → world, for hit testing (Varjo is HMD space)
        Vector3 worldOrigin  = hmd ? hmd.TransformPoint(d.gaze.origin)      : d.gaze.origin;
        Vector3 worldForward = hmd ? hmd.TransformDirection(d.gaze.forward) : d.gaze.forward;

        // Raycast to scene
        string hitName = "";
        string hitTag  = "";
        Vector3 hitPoint = Vector3.zero;
        string isTarget = "";

        if (!invalid)
        {
            if (Physics.Raycast(worldOrigin, worldForward, out var hit, maxRayDistance, gazeHitMask, QueryTriggerInteraction.Collide))
            {
                hitName = hit.collider.name;
                hitTag  = hit.collider.tag;
                hitPoint = hit.point;
                isTarget = (hitTag == "Target") ? "1" : "0";
            }
        }

        // Build row
        var row = new List<string>(Header.Length);

        // 0..3 frame/capture/log/trial times
        row.Add(d.frameNumber.ToString());
        row.Add(d.captureTime.ToString());
        row.Add((_taskTime * 1000f).ToString("F3"));
        row.Add((_trialTime * 1000f).ToString("F3"));

        // Combined gaze status + vectors (HMD space)
        row.Add(invalid ? "INVALID" : "VALID");
        row.Add(invalid ? "" : d.gaze.forward.x.ToString("F5"));
        row.Add(invalid ? "" : d.gaze.forward.y.ToString("F5"));
        row.Add(invalid ? "" : d.gaze.forward.z.ToString("F5"));
        row.Add(invalid ? "" : d.gaze.origin.x.ToString("F5"));
        row.Add(invalid ? "" : d.gaze.origin.y.ToString("F5"));
        row.Add(invalid ? "" : d.gaze.origin.z.ToString("F5"));

        // IPD
        row.Add(invalid ? "" : meas.interPupillaryDistanceInMM.ToString("F3"));

        // Left eye
        bool leftInvalid = d.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        row.Add(leftInvalid ? "INVALID" : "VALID");
        row.Add(leftInvalid ? "" : d.left.forward.x.ToString("F5"));
        row.Add(leftInvalid ? "" : d.left.forward.y.ToString("F5"));
        row.Add(leftInvalid ? "" : d.left.forward.z.ToString("F5"));
        row.Add(leftInvalid ? "" : d.left.origin.x.ToString("F5"));
        row.Add(leftInvalid ? "" : d.left.origin.y.ToString("F5"));
        row.Add(leftInvalid ? "" : d.left.origin.z.ToString("F5"));
        row.Add(leftInvalid ? "" : meas.leftPupilIrisDiameterRatio.ToString("F3"));
        row.Add(leftInvalid ? "" : meas.leftPupilDiameterInMM.ToString("F3"));
        row.Add(leftInvalid ? "" : meas.leftIrisDiameterInMM.ToString("F3"));

        // Right eye
        bool rightInvalid = d.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        row.Add(rightInvalid ? "INVALID" : "VALID");
        row.Add(rightInvalid ? "" : d.right.forward.x.ToString("F5"));
        row.Add(rightInvalid ? "" : d.right.forward.y.ToString("F5"));
        row.Add(rightInvalid ? "" : d.right.forward.z.ToString("F5"));
        row.Add(rightInvalid ? "" : d.right.origin.x.ToString("F5"));
        row.Add(rightInvalid ? "" : d.right.origin.y.ToString("F5"));
        row.Add(rightInvalid ? "" : d.right.origin.z.ToString("F5"));
        row.Add(rightInvalid ? "" : meas.rightPupilIrisDiameterRatio.ToString("F3"));
        row.Add(rightInvalid ? "" : meas.rightPupilDiameterInMM.ToString("F3"));
        row.Add(rightInvalid ? "" : meas.rightIrisDiameterInMM.ToString("F3"));

        // Focus
        row.Add(invalid ? "" : d.focusDistance.ToString("F5"));
        row.Add(invalid ? "" : d.focusStability.ToString("F5"));

        // Trial flags & response
        row.Add(_newTrialFlag.ToString());   // NewTrial
        row.Add(_trialIndex.ToString());     // Trial
        row.Add(_lastKey);                   // KeyPressed
        row.Add(_pressCount.ToString());     // PressCount

        // Hit info
        row.Add(hitName);
        row.Add(hitTag);
        row.Add(hitPoint.x.ToString("F5"));
        row.Add(hitPoint.y.ToString("F5"));
        row.Add(hitPoint.z.ToString("F5"));
        row.Add(isTarget);

        // write
        WriteCsvRow(PrependMeta(row.ToArray()));

        // reset one-shot flags
        _newTrialFlag = 0;
        if (_lastKey != "N") _lastKey = "N";
    }

    private void WriteCsvRow(IReadOnlyList<string> values)
    {
        _writer.WriteLine(string.Join(",", values));
#if UNITY_EDITOR
        if (alsoWriteEditorCopy && !string.IsNullOrEmpty(_editorMirrorPath))
            File.AppendAllText(_editorMirrorPath, string.Join(",", values) + "\n");
#endif
    }

    private void OnApplicationQuit() => EndSession();
}
