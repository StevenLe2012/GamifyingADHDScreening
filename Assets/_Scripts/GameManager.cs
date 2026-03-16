using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [HideInInspector] public GameState State;
    public static event Action<GameState> OnGameStateChanged;

    // --- Participant metadata ---
    [Header("Experiment – Participant ID (structured)")]
    [SerializeField] private string participantNumber = "";
    [SerializeField] private string participantLastName = "";
    [SerializeField] private string sessionDate = ""; // yyyy-MM-dd; blank → today
    [SerializeField] private string participantId = ""; // legacy free-form

    public string ParticipantNumber => participantNumber;
    public string ParticipantLastName => participantLastName;
    public string SessionDateISO
    {
        get
        {
            if (string.IsNullOrWhiteSpace(sessionDate))
                sessionDate = DateTime.Today.ToString("yyyy-MM-dd");
            return sessionDate;
        }
    }

    private static string PadNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        if (int.TryParse(raw, out var n)) return n.ToString("D3");
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

    public string CompositeParticipantId
    {
        get
        {
            var num  = Sanitize(PadNumber(participantNumber));
            var last = Sanitize(participantLastName);
            var date = Sanitize(SessionDateISO);

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(num))  parts.Add(num);
            if (!string.IsNullOrEmpty(last)) parts.Add(last);
            if (!string.IsNullOrEmpty(date)) parts.Add(date);

            return parts.Count > 0 ? string.Join("_", parts) : "";
        }
    }

    public string ParticipantId
    {
        get
        {
            var composite = CompositeParticipantId;
            if (!string.IsNullOrWhiteSpace(composite)) return composite;
            if (!string.IsNullOrWhiteSpace(participantId)) return Sanitize(participantId);
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
        }
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(participantId))
            Debug.LogWarning("[GameManager] ParticipantId is empty. Set it in the Inspector before starting recordings.");

        if (IntroScreen.Instance != null && IntroScreen.Instance.IsVisible)
            UpdateGameState(GameState.Narrative);
        else
            UpdateGameState(GameState.Explore);
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
}
