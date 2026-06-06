using System;
using System.Collections;
using System.Collections.Generic;
using MoxoCPT;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [HideInInspector] public GameState State;
    public static event Action<GameState> OnGameStateChanged;

    // --- Participant metadata ---
    [Header("Experiment – Participant ID")]
    [Tooltip("5-digit participant code (e.g. 10042). Set via login screen, URL ?num=, or here.")]
    [SerializeField] private string participantNumber = "";
    [SerializeField] private string participantId = ""; // legacy free-form fallback

    public string ParticipantNumber => participantNumber;

    private static string PadCode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        // If it's a plain integer, zero-pad to 5 digits.
        if (int.TryParse(raw.Trim(), out var n)) return n.ToString("D5");
        return raw.Trim();
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var chars = s.Trim().ToUpperInvariant();
        var sb = new System.Text.StringBuilder(chars.Length);
        foreach (var c in chars)
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
        return sb.ToString();
    }

    public string ParticipantId
    {
        get
        {
            var code = PadCode(participantNumber);
            var date = DateTime.Today.ToString("yyyy-MM-dd");

            if (!string.IsNullOrWhiteSpace(code)) return $"{code}_{date}";
            if (!string.IsNullOrWhiteSpace(participantId)) return $"{Sanitize(participantId)}_{date}";
            return "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }
    }

    public enum GameState
    {
        Narrative,
        Explore,
        PrepareCPT,
        CPT,
        Teleport,
    }

    [Header("Debug")]
    [SerializeField] private bool logStateChangeCallStack = false;

    [Header("Guards")]
    [Tooltip("Blocks leaving Narrative if a UI owns input (UIInputFocus.IsBlocked).")]
    [SerializeField] private bool respectUIFocusGuard = true;

    [Tooltip("When leaving Narrative legitimately, suppress Space/Submit for this many seconds to avoid fall-through.")]
    [SerializeField] private float suppressAfterNarrativeSeconds = 0.25f;

    private static readonly object s_NarrativeInputOwner = new object();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Priority order:
        //   1. URL query parameters (researcher override — highest priority)
        //   2. ParticipantLoginScreen input (player-entered data from IntroBoot)
        //   3. Auto-generated timestamp fallback (see ParticipantId getter)
        //
        // Supported URL parameters:
        //   ?num=001            → participantNumber  (padded to 3 digits)

        //   ?date=2026-03-17    → sessionDate (ISO yyyy-MM-dd)
        //   ?pid=MY_ID          → participantId (legacy free-form)
        //
        // Example URL: https://your-game.web.app/?num=001&last=SMITH&date=2026-03-17
        ReadParticipantIdFromUrl();

        // If URL gave us nothing, pull from the in-game login screen.
        // Login screen ALWAYS wins over Inspector values — Inspector fields are
        // dev defaults only and must not silently override real participant data.
        if (ParticipantSession.WasSubmitted)
        {
            if (!string.IsNullOrEmpty(ParticipantSession.Number))
                participantNumber = ParticipantSession.Number;
        }

        Debug.Log($"[GameManager] Resolved ParticipantId = '{ParticipantId}'");
#endif
    }

    private void Start()
    {
        // Fresh session total for end-game display (four base islands only; see CPTScoreRuntime).
        CPTScoreRuntime.ResetFourIslandSessionTotal();

        // Fallback when intro video is skipped (?fast=1 / prepare failed).
        SessionPlayTimeTracker.StartSession();

        GameplayCameraClip.Apply();

        if (string.IsNullOrWhiteSpace(participantId) && string.IsNullOrWhiteSpace(participantNumber))
            Debug.LogWarning("[GameManager] ParticipantId is empty. Pass ?num=001&last=NAME in the URL or set it in the Inspector.");

        // IntroScreen.IsVisible requires canvasGroup to be non-null; if it isn't assigned in
        // the Inspector it returns false even though the screen is showing.  On WebGL we always
        // start in Narrative (the welcome screen is always the first thing the player sees).
#if UNITY_WEBGL && !UNITY_EDITOR
        UpdateGameState(GameState.Narrative);
#else
        if (IntroScreen.Instance != null && IntroScreen.Instance.IsVisible)
            UpdateGameState(GameState.Narrative);
        else
            UpdateGameState(GameState.Explore);
#endif

        StartCoroutine(CoMarkMainVisibleForLoadingOverlay());
    }

    IEnumerator CoMarkMainVisibleForLoadingOverlay()
    {
        yield return null;
        yield return null;
        MainSceneLoadBridge.MarkReady();
    }

    /// <summary>
    /// Normal state changes. Prevents leaving Narrative while UIInputFocus owns input.
    /// </summary>
    public void UpdateGameState(GameState newState)
    {
        // Hard guard: do not leave Narrative if a UI owns focus.
        if (respectUIFocusGuard &&
            State == GameState.Narrative &&
            newState != GameState.Narrative &&
            UIInputFocus.IsBlocked)
        {
            if (logStateChangeCallStack)
                Debug.Log($"[GameManager] Blocked {State}->{newState} (UIInputFocus.IsBlocked).");
            return;
        }

        ApplyState(newState, suppressOnExitNarrative:true);
    }

    /// <summary>
    /// Emergency override that ignores the UI focus guard (use sparingly).
    /// </summary>
    public void ForceUpdateGameState(GameState newState)
    {
        ApplyState(newState, suppressOnExitNarrative:true);
    }

    private void ApplyState(GameState newState, bool suppressOnExitNarrative)
    {
        if (newState == State) return;

#if UNITY_EDITOR
        if (logStateChangeCallStack)
            Debug.Log($"[GameManager] State change {State} -> {newState}\nCaller:\n{Environment.StackTrace}");
#endif

        // Manage the global input owner + optional suppression when leaving Narrative.
        if (State == GameState.Narrative && newState != GameState.Narrative)
        {
            UIInputFocus.Pop(s_NarrativeInputOwner);
            if (suppressOnExitNarrative && suppressAfterNarrativeSeconds > 0f)
                UIInputFocus.SuppressForSeconds(suppressAfterNarrativeSeconds);
        }
        else if (newState == GameState.Narrative)
        {
            UIInputFocus.Push(s_NarrativeInputOwner);
        }

        State = newState;

        switch (newState)
        {
            case GameState.Narrative: HandleNarrative(); break;
            case GameState.Explore:   HandleExplore();   break;
            case GameState.PrepareCPT:
                HandlePrepareCPT();
                StartCoroutine(VarjoGazeGuard.I.EnsureGazeReady(25f));
                break;
            case GameState.CPT:      HandleCPT();       break;
            case GameState.Teleport: HandleTeleport();  break;
            default: throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnGameStateChanged?.Invoke(newState);
    }

    private void OnDisable()
    {
        // Safety: release if object goes away while in Narrative
        UIInputFocus.Pop(s_NarrativeInputOwner);
    }

    private void HandleNarrative() => Debug.Log("GM: Narrative");
    private void HandleExplore()   => Debug.Log("GM: Explore");
    private void HandleCPT()       => Debug.Log("GM: CPT");

    private void HandlePrepareCPT()
    {
        Debug.Log("GM: PrepareCPT");

        var companion = GameObject.FindGameObjectWithTag("Companion");
        if (companion == null)
        {
            Debug.Log("Could not find a Companion with tag: 'Companion'");
            return;
        }

        SetBehaviourEnabledByTypeName(companion, "NPCFollow",       false);
        SetBehaviourEnabledByTypeName(companion, "GoToDestination", true);

        var animator = companion.GetComponent<Animator>();
        if (animator) animator.enabled = true;
    }

    private void HandleTeleport()
    {
        Debug.Log("GM: Teleport");

        var fadeScreen = GameObject.FindGameObjectWithTag("Fader");
        if (fadeScreen == null)
        {
            Debug.Log("Could not find a FadeScreen with tag: 'Fader'");
            return;
        }

        var fader = fadeScreen.GetComponent<FadeScreen>();
        fader.TeleportFade();
    }

    // --- helpers: enable/disable MonoBehaviours by type name without compile-time reference ---
    private static void SetBehaviourEnabledByTypeName(GameObject go, string typeName, bool enabled)
    {
        var t = FindTypeAnywhere(typeName);
        if (t == null)
        {
            Debug.Log($"[GameManager] Type '{typeName}' not found. Skipping.");
            return;
        }

        var comp = go.GetComponent(t) as Behaviour;
        if (comp != null) comp.enabled = enabled;
        else Debug.Log($"[GameManager] '{typeName}' component not present on '{go.name}'.");
    }

    private static Type FindTypeAnywhere(string shortName)
    {
        if (string.IsNullOrWhiteSpace(shortName)) return null;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(shortName, false);
            if (t != null) return t;

            try
            {
                t = Array.Find(asm.GetTypes(), tp => tp.Name == shortName);
                if (t != null) return t;
            }
            catch { /* some dynamic assemblies throw */ }
        }
        return null;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    // ── URL parameter helpers ─────────────────────────────────────────────────

    private void ReadParticipantIdFromUrl()
    {
        try
        {
            string url = Application.absoluteURL;

            // ?num=12345  → 5-digit participant code
            // ?pid=MY_ID  → legacy free-form fallback
            string num = GetQueryParam(url, "num");
            string pid = GetQueryParam(url, "pid");

            if (!string.IsNullOrEmpty(num)) participantNumber = num;
            if (!string.IsNullOrEmpty(pid)) participantId     = pid;

            Debug.Log($"[GameManager] URL → ParticipantId = '{ParticipantId}'");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameManager] URL param parse failed: {e.Message}");
        }
    }

    /// <summary>Returns the decoded value of a query parameter, or null if absent.</summary>
    private static string GetQueryParam(string url, string key)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key)) return null;

        // Match ?key= or &key=
        int idx = url.IndexOf("?" + key + "=", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) idx = url.IndexOf("&" + key + "=", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        int start = idx + key.Length + 2; // skip delimiter + key + "="
        int end   = url.IndexOf('&', start);
        if (end < 0) end = url.IndexOf('#', start);
        if (end < 0) end = url.Length;

        string raw = url.Substring(start, end - start);
        return Uri.UnescapeDataString(raw); // decode %20, + etc.
    }
#endif
}
