using System;
using System.Collections;
using System.Collections.Generic;
using Companion;
using MoxoCPT;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [HideInInspector] public GameState State;
    public static event Action<GameState> OnGameStateChanged;

    // --- Hami: experiment metadata (set this in Inspector) ---
    [Header("Experiment – Participant ID (structured)")]
    [SerializeField] private string participantNumber = "";   // e.g. "1" or "001"
    [SerializeField] private string participantLastName = "";  // e.g. "Smith"
    [SerializeField] private string sessionDate = "";          // yyyy-MM-dd; leave blank to auto-fill today

    // Back-compat single string (optional)
    [SerializeField] private string participantId = "";        // legacy free-form

    public string ParticipantNumber => participantNumber;
    public string ParticipantLastName => participantLastName;
    public string SessionDateISO
    {
        get
        {
            // If not specified, use today (ISO)
            if (string.IsNullOrWhiteSpace(sessionDate))
                sessionDate = DateTime.Today.ToString("yyyy-MM-dd");
            return sessionDate;
        }
    }

    // Zero-pad number to 3 digits (001, 012, 123)
    private static string PadNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        if (int.TryParse(raw, out var n)) return n.ToString("D3");
        return raw.Trim(); // if user typed non-numeric (e.g. "A01"), keep as-is
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        // allow letters, digits, dash, underscore
        var chars = s.Trim().ToUpperInvariant();
        System.Text.StringBuilder sb = new System.Text.StringBuilder(chars.Length);
        foreach (var c in chars)
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
        return sb.ToString();
    }

    public string CompositeParticipantId
    {
        get
        {
            var num = Sanitize(PadNumber(participantNumber));
            var last = Sanitize(participantLastName);
            var date = Sanitize(SessionDateISO); // already ISO

            // Build only from parts that exist
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(num))  parts.Add(num);
            if (!string.IsNullOrEmpty(last)) parts.Add(last);
            if (!string.IsNullOrEmpty(date)) parts.Add(date);

            return parts.Count > 0 ? string.Join("_", parts) : "";
        }
    }

    // Public accessor used by other systems
    public string ParticipantId
    {
        get
        {
            // Prefer structured composite; fall back to legacy string; as last resort make a timestamp
            var composite = CompositeParticipantId;
            if (!string.IsNullOrWhiteSpace(composite)) return composite;

            if (!string.IsNullOrWhiteSpace(participantId)) return Sanitize(participantId);

            return "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }
    }
    // --------------------------------------------------------

    
    public enum GameState {
        Narrative,
        Explore,
        PrepareCPT,
        CPT,
        Teleport,
        
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Hami: warn if you forgot to set an ID
        if (string.IsNullOrWhiteSpace(participantId))
        {
            Debug.LogWarning("[GameManager] ParticipantId is empty. " +
                             "Set it in the Inspector before starting recordings.");
        }

        UpdateGameState(GameState.Explore);
    }

    public void UpdateGameState(GameState newState)
    {
        if (newState == State) return;
        
        State = newState;
        
        switch (newState)
        {
            case GameState.Narrative:
                HandleNarrative();
                break;
            case GameState.Explore:
                HandleExplore();
                break;
            case GameState.PrepareCPT:
                HandlePrepareCPT();
                StartCoroutine(VarjoGazeGuard.I.EnsureGazeReady(25f)); 
                break;
            case GameState.CPT:
                HandleCPT();
                break;
            case GameState.Teleport:
                HandleTeleport();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnGameStateChanged?.Invoke(newState);


    }

    private void HandleNarrative()
    {
        print("GM: Narrative"); 
    }

    private void HandleExplore()
    {
       print("GM: Explore"); 
    }
    private void HandlePrepareCPT()
    {
        print("GM: PrepareCPT");
        
        var companion = GameObject.FindGameObjectWithTag("Companion");
        
        if (companion == null)
        {
            print("Could not find a Companion with tag: 'Companion'");
            return;
        }

        var companionFollowScript = companion.GetComponent<NPCFollow>();
        companionFollowScript.enabled = false;
                
        var companionDestinationScript = companion.GetComponent<GoToDestination>();
        companionDestinationScript.enabled = true;

        var companionAnimator = companion.GetComponent<Animator>();
        companionAnimator.enabled = true;


    }
    private void HandleCPT()
    {
        print("GM: CPT"); 
    }
    
    private void HandleTeleport()
    {
        print("GM: Teleport");
        
        var fadeScreen = GameObject.FindGameObjectWithTag("Fader");
        
        if (fadeScreen == null)
        {
            print("Could not find a FadeScreen with tag: 'Fader'");
            return;
        }

        var fader = fadeScreen.GetComponent<FadeScreen>();
        fader.TeleportFade();
        

    }
    
}


