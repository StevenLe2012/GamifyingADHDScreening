// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.UI;
// using TMPro;

// public class IntroScreen : MonoBehaviour
// {
//     [Header("Wiring")]
//     [SerializeField] private CanvasGroup canvasGroup;     // assign the CanvasGroup on IntroPanel
//     [SerializeField] private Button startButton;          // assign StartButton
//     [SerializeField] private Dialogue.DialogueHandler dialogueHandler; // drag your DialogueHandler here

//     [Header("Behavior")]
//     [SerializeField] private bool allowPressAtoStart = true;
//     [SerializeField] private float fadeSpeed = 10f;

//     private bool _isVisible = true;
//     private bool _starting = false;

//     private void Awake()
//     {
//         if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
//         if (startButton) startButton.onClick.AddListener(OnStartClicked);
//         // ensure visible at boot
//         ShowInstant(true);
//         // Optional: lock player movement/explore until intro dismissed
//         if (GameManager.Instance != null)
//             GameManager.Instance.UpdateGameState(GameManager.GameState.Narrative); // or a custom "Intro" state
//     }

//     private void Update()
//     {
//         if (!_isVisible || _starting || !allowPressAtoStart) return;

//         bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
//         bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

//         if (pressedA || pressedSpace)
//         {
//             OnStartClicked();
//         }
//     }

//     public void OnStartClicked()
//     {
//         if (_starting) return;
//         _starting = true;
//         StartCoroutine(CoFadeOutAndBegin());
//     }

//     private System.Collections.IEnumerator CoFadeOutAndBegin()
//     {
//         // fade out
//         while (canvasGroup.alpha > 0f)
//         {
//             canvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
//             yield return null;
//         }
//         canvasGroup.alpha = 0f;
//         _isVisible = false;
//         gameObject.SetActive(false);

//         // Kick off first dialogue
//         StartInitialDialogue();
//     }

//     private void ShowInstant(bool visible)
//     {
//         _isVisible = visible;
//         gameObject.SetActive(true);
//         canvasGroup.alpha = visible ? 1f : 0f;
//     }

//     private void StartInitialDialogue()
//     {
//         // Easiest: reuse your existing OnInteract flow so all state setup remains consistent.
//         // Find the player's Interactor in scene and call OnInteract.
//         var interactor = FindObjectOfType<Interactor>();
//         if (dialogueHandler != null && interactor != null)
//         {
//             dialogueHandler.OnInteract(interactor);
//         }
//         else
//         {
//             Debug.LogWarning("IntroScreen: Could not start dialogue (missing DialogueHandler or Interactor).");
//         }
//     }
// }



// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.UI;
// using TMPro;
// using UnityEngine.Events;

// public class IntroScreen : MonoBehaviour
// {
//     [Header("Wiring")]
//     [SerializeField] private CanvasGroup canvasGroup;           // WelcomePanel (CanvasGroup)
//     [SerializeField] private Button startButton;                // StartButton
//     [SerializeField] private Dialogue.DialogueHandler dialogueHandler; // (only used for Welcome)

//     [Header("Optional Text (assign these)")]
//     [SerializeField] private TextMeshProUGUI headerText;        // HeaderText child
//     [SerializeField] private TextMeshProUGUI bodyText;          // BodyText child

//     [Header("Behavior")]
//     [SerializeField] private bool showWelcomeOnAwake = true;
//     [SerializeField] private bool allowPressAtoStart = true;
//     [SerializeField] private float fadeSpeed = 10f;

//     // Preset content
//     [System.Serializable]
//     public class PanelPreset
//     {
//         public string title;
//         [TextArea] public string body;
//         public UnityEvent onStart;   // actions to run after Start
//     }

//     [Header("Presets")]
//     [SerializeField] private PanelPreset welcomePreset;
//     [SerializeField] private PanelPreset moxoPreset;

//     private bool _isVisible;
//     private bool _starting;
//     private UnityEvent _currentStart; // what to run after fade

//     private void Awake()
//     {
//         if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
//         if (startButton) startButton.onClick.AddListener(OnStartClicked);

//         // Try to auto-find TMPs if not assigned
//         if (!headerText) headerText = transform.Find("HeaderText")?.GetComponent<TextMeshProUGUI>();
//         if (!bodyText)   bodyText   = transform.Find("BodyText")?.GetComponent<TextMeshProUGUI>();

//         if (showWelcomeOnAwake) ShowWelcome();
//         else ShowInstant(false);
//     }

//     public void ShowMoxoIntro(string title, string body)
//     {
//         ShowMoxo(title, body);
//     }

//     private void Update()
//     {
//         if (!_isVisible || _starting || !allowPressAtoStart) return;

//         bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
//         bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

//         if (pressedA || pressedSpace) OnStartClicked();
//     }

//     // -------- Public API --------
//     public void ShowWelcome()
//     {
//         ApplyPreset(welcomePreset);
//         if (GameManager.Instance) GameManager.Instance.UpdateGameState(GameManager.GameState.Narrative);
//         ShowInstant(true);
//     }

//     public void ShowMoxo(string titleOverride = null, string bodyOverride = null)
//     {
//         ApplyPreset(moxoPreset, titleOverride, bodyOverride);
//         if (GameManager.Instance) GameManager.Instance.UpdateGameState(GameManager.GameState.Narrative);
//         ShowInstant(true);
//     }

//     // -------- Internals --------
//     private void ApplyPreset(PanelPreset preset, string titleOverride = null, string bodyOverride = null)
//     {
//         if (headerText) headerText.text = string.IsNullOrEmpty(titleOverride) ? preset.title : titleOverride;
//         if (bodyText)   bodyText.text   = string.IsNullOrEmpty(bodyOverride)  ? preset.body  : bodyOverride;
//         _currentStart = preset?.onStart;
//     }

//     public void OnStartClicked()
//     {
//         if (_starting) return;
//         _starting = true;
//         StartCoroutine(CoFadeOutAndBegin());
//     }

//     private System.Collections.IEnumerator CoFadeOutAndBegin()
//     {
//         while (canvasGroup.alpha > 0f)
//         {
//             canvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
//             yield return null;
//         }
//         canvasGroup.alpha = 0f;
//         _isVisible = false;
//         gameObject.SetActive(false);

//         _currentStart?.Invoke();
//     }

//     private void ShowInstant(bool visible)
//     {
//         _isVisible = visible;
//         gameObject.SetActive(true);
//         canvasGroup.alpha = visible ? 1f : 0f;
//         canvasGroup.blocksRaycasts = visible;
//         _starting = false;
//     }
// }
// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.UI;
// using TMPro;
// using UnityEngine.Events;
// using MoxoCPT;
// using System.Linq;
// using System.Text.RegularExpressions;
// using UnityEngine.EventSystems; // clear lingering submit focus

// public class IntroScreen : MonoBehaviour
// {
//     [SerializeField] private bool autoWireMoxoOnStart = true;

//     public static IntroScreen Instance { get; private set; }

//     [Header("Wiring")]
//     [SerializeField] private CanvasGroup canvasGroup;
//     [SerializeField] private Button startButton;
//     [SerializeField] private Dialogue.DialogueHandler dialogueHandler; // (only for Welcome)

//     [Header("START DIALOGUE AFTER INTRO (NEW)")]
//     [Tooltip("Assign the NPC TestHandler that should begin speaking right after the intro closes.")]
//     [SerializeField] private Dialogue.TestHandler introTestHandler;

//     [Tooltip("Assign the DialogueState to start from (usually on the same NPC).")]
//     [SerializeField] private Dialogue.DialogueState introDialogueState;

//     [Header("Optional Text")]
//     [SerializeField] private TextMeshProUGUI headerText;
//     [SerializeField] private TextMeshProUGUI bodyText;

//     [Header("Behavior")]
//     [SerializeField] private bool showWelcomeOnAwake = true;
//     [SerializeField] private bool allowPressAtoStart = true;
//     [SerializeField] private float fadeSpeed = 10f;

//     [Header("Results UI")]
//     [SerializeField] private string resultsTitle = "Results";
//     [SerializeField, TextArea]
//     private string resultsBodyTemplate = "Targets hit: {HIT}/{TOTAL}\nNon-target hits: {FA}/{D_TOTAL}";
    
//     [SerializeField] private string resultsButtonLabel = "Continue";
//     [SerializeField] private TextMeshProUGUI startButtonLabel;

//     [Header("Delay Settings")]
//     [SerializeField] private float continueDelaySeconds = 5f;
//     [SerializeField] private bool showCountdownOnButton = true;
//     [SerializeField] private float defaultReadyDelaySeconds = 5f;

//     [System.Serializable]
//     public class PanelPreset
//     {
//         public string title;
//         [TextArea] public string body;
//         public UnityEvent onStart;
//     }

//     [Header("Presets")]
//     [SerializeField] private PanelPreset welcomePreset;
//     [SerializeField] private PanelPreset moxoPreset;

//     private bool _isVisible;
//     private bool _starting;
//     private UnityEvent _currentStart;

//     private Coroutine delayCo;
//     private bool _delaying;

//     private static readonly Regex s_CountdownSuffix = new Regex(@"\s*\(\d+\)\s*$", RegexOptions.Compiled);
//     private static string StripCountdownSuffix(string s) => string.IsNullOrEmpty(s) ? s : s_CountdownSuffix.Replace(s, "");

//     // ---------- Start MOXO on the active island rig ----------
//     private void StartMoxoGame()
//     {
//         var mgr = MoxoCPTManager.Instance;
//         if (mgr == null || !mgr.gameObject.activeInHierarchy)
//             mgr = FindObjectsOfType<MoxoCPTManager>(true)
//                     .FirstOrDefault(x => x && x.gameObject.activeInHierarchy);

//         if (mgr != null)
//         {
//             Debug.Log($"[IntroScreen] Starting MOXO on '{mgr.gameObject.name}'.");
//             mgr.OnGameBegin();
//         }
//         else Debug.LogError("[IntroScreen] No ACTIVE MoxoCPTManager found. Did you enable the island's MOXO rig?");
//     }

//     private void Awake()
//     {
//         Instance = this;

//         if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
//         if (!headerText) headerText = transform.Find("HeaderText")?.GetComponent<TextMeshProUGUI>();
//         if (!bodyText)   bodyText   = transform.Find("BodyText")?.GetComponent<TextMeshProUGUI>();

//         if (!startButtonLabel && startButton)
//             startButtonLabel = startButton.GetComponentInChildren<TextMeshProUGUI>();

//         if (startButton) startButton.onClick.AddListener(OnStartClicked);

//         if (showWelcomeOnAwake) ShowWelcome();
//         else ShowInstant(false);
//     }

//     public void Hide()
//     {
//         StopAllCoroutines();
//         ShowInstant(false);
//         // Make absolutely sure nothing can start after hiding.
//         MoxoStartGate.Disarm();
//         if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
//     }

//     public bool IsVisible => gameObject.activeInHierarchy && _isVisible && canvasGroup && canvasGroup.alpha > 0.001f;

//     public void ShowMoxoIntro(string title, string body) => ShowMoxo(title, body);

//     private void Update()
//     {
//         if (!IsVisible || _starting || !allowPressAtoStart || _delaying || (startButton && !startButton.interactable)) return;

//         var gp = Gamepad.current;
//         var kb = Keyboard.current;
//         if ((gp != null && gp.buttonSouth.wasPressedThisFrame) ||
//             (kb != null && kb.spaceKey.wasPressedThisFrame))
//         {
//             OnStartClicked();
//         }
//     }

//     // ---------- Public API ----------
//     public void ShowWelcome()
//     {
//         ApplyPreset(welcomePreset);
//         if (startButtonLabel) startButtonLabel.text = StripCountdownSuffix(startButtonLabel.text);

//         GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);

//         ShowInstant(true);
//         DelayStartButton(continueDelaySeconds, GetCurrentButtonLabelOr("Continue"));

//         // Do not arm the gate here — welcome is not allowed to start MOXO.
//         MoxoStartGate.Disarm();
//     }

//     public void ShowMoxo(string titleOverride = null, string bodyOverride = null)
//     {
//         if (startButtonLabel) startButtonLabel.text = "Start";
//         ApplyPreset(moxoPreset, titleOverride, bodyOverride);

//         if (_currentStart == null) _currentStart = new UnityEvent();
//         _currentStart.RemoveAllListeners();

//         // IMPORTANT: only start if the gate is armed; consume it exactly once.
//         _currentStart.AddListener(() =>
//         {
//             if (!MoxoStartGate.TryConsume())
//             {
//                 Debug.Log("[IntroScreen] Start ignored (gate not armed).");
//                 return;
//             }
//             StartMoxoGame();
//         });

//         if (startButton)
//         {
//             startButton.onClick.RemoveListener(OnStartClicked);
//             startButton.onClick.AddListener(OnStartClicked);
//         }

//         GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);

//         if (canvasGroup) { canvasGroup.blocksRaycasts = true; canvasGroup.interactable = true; }

//         ShowInstant(true);
//         DelayStartButton(continueDelaySeconds, GetCurrentButtonLabelOr("Start"));
//     }

//     public void ShowReadyAfterTraining(string title = "Ready to start?", string body = "Press Space to begin the real test.", float? delayOverride = null, string buttonLabel = "Start")
//     {
//         // Arm the one-shot start gate: only the ready panel authorizes starting.
//         MoxoStartGate.Arm();

//         ShowMoxo(title, body);
//         DelayStartButton(delayOverride.HasValue ? delayOverride.Value : defaultReadyDelaySeconds, buttonLabel);
//     }

//     public void ArmOnStart(UnityAction onStart, bool delayButton = true, float delaySeconds = 5f)
//     {
//         _currentStart = new UnityEvent();
//         if (onStart != null) _currentStart.AddListener(onStart);

//         if (startButton)
//         {
//             startButton.onClick.RemoveAllListeners();
//             startButton.onClick.AddListener(OnStartClicked);
//         }

//         if (canvasGroup) { canvasGroup.blocksRaycasts = true; canvasGroup.interactable = true; }
//         allowPressAtoStart = true;

//         if (delayButton) DelayStartButton(delaySeconds, GetCurrentButtonLabelOr("Start"));
//     }

//     private string GetCurrentButtonLabelOr(string fallback)
//     {
//         string label = fallback;
//         if (startButtonLabel && !string.IsNullOrWhiteSpace(startButtonLabel.text))
//             label = startButtonLabel.text;
//         return StripCountdownSuffix(label);
//     }

//     // ---------- Internals ----------
//     private void ApplyPreset(PanelPreset preset, string titleOverride = null, string bodyOverride = null)
//     {
//         if (headerText) headerText.text = string.IsNullOrEmpty(titleOverride) ? preset.title : titleOverride;
//         if (bodyText)   bodyText.text   = string.IsNullOrEmpty(bodyOverride)  ? preset.body  : bodyOverride;

//         _currentStart = preset?.onStart;

//         if (autoWireMoxoOnStart && preset == moxoPreset)
//         {
//             bool missing = _currentStart == null || _currentStart.GetPersistentEventCount() == 0;
//             if (missing && MoxoCPTManager.Instance != null)
//             {
//                 if (_currentStart == null) _currentStart = new UnityEvent();
//                 _currentStart.AddListener(() =>
//                 {
//                     if (!MoxoStartGate.TryConsume())
//                     {
//                         Debug.Log("[IntroScreen] Auto-wired start ignored (gate not armed).");
//                         return;
//                     }
//                     MoxoCPTManager.Instance.OnGameBegin();
//                 });
//                 Debug.Log("[IntroScreen] (Compat) Auto-wired MOXO OnStart with start gate.");
//             }
//         }
//     }

//     public void OnStartClicked()
//     {
//         if (_starting || _delaying || (startButton && !startButton.interactable)) return;
//         _starting = true;
//         Debug.Log("[IntroScreen] Start clicked → fading out then starting.");

//         if (canvasGroup) { canvasGroup.blocksRaycasts = false; canvasGroup.interactable = false; }
//         StartCoroutine(CoFadeOutAndBegin());
//     }

//     public void HideInstant()
//     {
//         StopAllCoroutines();
//         if (canvasGroup)
//         {
//             canvasGroup.alpha = 0f;
//             canvasGroup.blocksRaycasts = false;
//             canvasGroup.interactable   = false;
//         }
//         _isVisible = false;
//         _starting  = false;
//         gameObject.SetActive(false);

//         // Kill any focused button so Enter can't submit it later.
//         if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
//         MoxoStartGate.Disarm();
//     }

//     private System.Collections.IEnumerator CoFadeOutAndBegin()
//     {
//         if (canvasGroup) { canvasGroup.blocksRaycasts = false; canvasGroup.interactable = false; }

//         while (canvasGroup && canvasGroup.alpha > 0f)
//         {
//             canvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
//             yield return null;
//         }

//         if (canvasGroup) canvasGroup.alpha = 0f;
//         _isVisible = false;
//         gameObject.SetActive(false);

//         // Clear any lingering submit focus
//         if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

//         // 1) Run whatever was wired (MOXO etc.)
//         var toInvoke = _currentStart;
//         _currentStart = null;
//         startButton?.onClick.RemoveAllListeners();

//         if (toInvoke != null) toInvoke.Invoke();
//         else Debug.LogWarning("[IntroScreen] No onStart action wired. Panel closed, doing nothing.");

//         // 2) If this was the welcome/intro panel, start the intro dialogue immediately.
//         StartDialogueAfterIntroIfConfigured();

//         _starting = false;
//     }

//     private void StartDialogueAfterIntroIfConfigured()
//     {
//         if (introTestHandler == null || introDialogueState == null) return;

//         GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//         introTestHandler.StartIntroDialogue(introDialogueState);
//     }

//     public void ShowInstant(bool visible)
//     {
//         _isVisible = visible;
//         gameObject.SetActive(visible);
//         if (canvasGroup)
//         {
//             canvasGroup.alpha          = visible ? 1f : 0f;
//             canvasGroup.blocksRaycasts = visible;
//             canvasGroup.interactable   = visible;
//         }
//         _starting = false;

//         if (!visible && EventSystem.current)
//             EventSystem.current.SetSelectedGameObject(null);
//     }
    
//     public void ShowResults(int hit, int total, int falseAlarms, int totalDistractors, System.Action onContinue = null)
//     {
//         string body = resultsBodyTemplate
//             .Replace("{HIT}", hit.ToString())
//             .Replace("{TOTAL}", total.ToString())
//             .Replace("{FA}", falseAlarms.ToString())
//             .Replace("{D_TOTAL}", totalDistractors.ToString());

//         ApplyPreset(moxoPreset, resultsTitle, body);

//         if (startButtonLabel) startButtonLabel.text = resultsButtonLabel = StripCountdownSuffix(resultsButtonLabel);

//         _currentStart = new UnityEvent();
//         if (onContinue != null) _currentStart.AddListener(() => onContinue());

//         ShowInstant(true);
//         DelayStartButton(continueDelaySeconds, resultsButtonLabel);

//         MoxoStartGate.Disarm();
//     }

//     // ---------- Delay helpers ----------
//     private void SetStartButtonInteractable(bool on)
//     {
//         if (!startButton) return;
//         startButton.interactable = on;
//         var cg = startButton.GetComponent<CanvasGroup>();
//         if (cg) cg.alpha = on ? 1f : 0.5f;
//     }

//     public void DelayStartButton(float seconds, string finalLabel = "Start")
//     {
//         if (delayCo != null) StopCoroutine(delayCo);
//         finalLabel = StripCountdownSuffix(finalLabel);
//         if (startButtonLabel) startButtonLabel.text = StripCountdownSuffix(startButtonLabel.text);
//         delayCo = StartCoroutine(CoDelayStart(seconds, finalLabel));
//     }

//     private System.Collections.IEnumerator CoDelayStart(float seconds, string finalLabel)
//     {
//         _delaying = true;
//         SetStartButtonInteractable(false);

//         float t = Mathf.Max(0f, seconds);
//         while (t > 0.01f)
//         {
//             if (startButtonLabel && showCountdownOnButton)
//                 startButtonLabel.text = $"{finalLabel} ({Mathf.CeilToInt(t)})";
//             yield return new WaitForSecondsRealtime(1f);
//             t -= 1f;
//         }

//         if (startButtonLabel) startButtonLabel.text = finalLabel;
//         SetStartButtonInteractable(true);
//         _delaying = false;
//     }

//     public void ShowTextOnly(string title, string body)
//     {
//         if (headerText) headerText.text = string.IsNullOrEmpty(title) ? "" : title;
//         if (bodyText)   bodyText.text   = string.IsNullOrEmpty(body)  ? "" : body;

//         _currentStart = null;
//         if (delayCo != null) { StopCoroutine(delayCo); delayCo = null; }
//         SetStartButtonInteractable(true);
//         allowPressAtoStart = true;
//         if (startButtonLabel) startButtonLabel.text = StripCountdownSuffix(startButtonLabel.text);
//         ShowInstant(true);

//         // Text-only panels cannot start MOXO.
//         MoxoStartGate.Disarm();
//     }

//     public static string Fallback(string primary, string fallback) => string.IsNullOrWhiteSpace(primary) ? fallback : primary;
// }

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using MoxoCPT;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems; // clear lingering submit focus

public class IntroScreen : MonoBehaviour
{
    [SerializeField] private bool autoWireMoxoOnStart = true;

    public static IntroScreen Instance { get; private set; }

    [Header("Wiring")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button startButton;

    [Tooltip("Optional island character shown while this intro screen is visible, hidden when it closes. Set at runtime via SetLocalCharacter() from the IntroAnchor.")]
    [SerializeField] private GameObject localCharacter;

    /// <summary>
    /// Called by IslandTravelManager after placing the intro screen at an island anchor.
    /// Pass null to clear the previous island's character.
    /// </summary>
    public void SetLocalCharacter(GameObject character)
    {
        // Hide the outgoing character before switching
        if (localCharacter && localCharacter != character)
            localCharacter.SetActive(false);

        localCharacter = character;
    }

    // NEW: Replay Training button (keep assigned but default hidden)
    [SerializeField] private Button replayTrainingButton;
    [SerializeField] private TextMeshProUGUI replayTrainingButtonLabel;

    [SerializeField] private Dialogue.DialogueHandler dialogueHandler; // (only for Welcome)

    [Header("START DIALOGUE AFTER INTRO (NEW)")]
    [Tooltip("Assign the NPC TestHandler that should begin speaking right after the intro closes.")]
    [SerializeField] private Dialogue.TestHandler introTestHandler;

    [Tooltip("Assign the DialogueState to start from (usually on the same NPC).")]
    [SerializeField] private Dialogue.DialogueState introDialogueState;

    [Header("Optional Text")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("Behavior")]
    [SerializeField] private bool showWelcomeOnAwake = true;
    [SerializeField] private bool allowPressAtoStart = true;
    [SerializeField] private float fadeSpeed = 10f;
    [SerializeField] private bool lockMouseLookWhileIntroVisible = true;

    [Header("Results UI")]
    [SerializeField] private string resultsTitle = "Results";
    [SerializeField, TextArea]
    private string resultsBodyTemplate = "Targets hit: {HIT}/{TOTAL}\nNon-target hits: {FA}/{D_TOTAL}";

    [SerializeField] private string resultsButtonLabel = "Continue";
    [SerializeField] private TextMeshProUGUI startButtonLabel;

    [Header("Delay Settings")]
    [SerializeField] private float continueDelaySeconds = 5f;
    [SerializeField] private bool showCountdownOnButton = true;
    [SerializeField] private float defaultReadyDelaySeconds = 5f;

    [System.Serializable]
    public class PanelPreset
    {
        public string title;
        [TextArea] public string body;
        public UnityEvent onStart;
    }

    [Header("Presets")]
    [SerializeField] private PanelPreset welcomePreset;
    [SerializeField] private PanelPreset moxoPreset;

    [Header("Voice Over")]
    [Tooltip("Optional AudioSource used to play intro voice over clips.")]
    [SerializeField] private AudioSource voiceSource;
    [Tooltip("Voice over clip for the initial welcome screen on game start.")]
    [SerializeField] private AudioClip welcomeVoice;
    [Tooltip("Voice over clip for the generic MOXO intro (ready/start panels).")]
    [SerializeField] private AudioClip moxoVoice;
    [Tooltip("Voice over clip for the results screen (targets hit, etc.). Can be overridden per island via IntroAnchor.")]
    [SerializeField] private AudioClip resultsVoice;

    // Per-island results overrides — set at travel time by IslandTravelManager via SetIslandResultsConfig().
    // All fall back to the shared inspector fields when null/empty.
    private AudioClip _islandResultsVoice;
    private string    _islandResultsTitle;
    private string    _islandResultsBodyTemplate;

    /// <summary>
    /// Called by IslandTravelManager after placing the intro screen at an island anchor.
    /// Pass nulls/empty strings to clear overrides and fall back to the shared defaults.
    /// </summary>
    public void SetIslandResultsConfig(AudioClip voice, string title, string bodyTemplate)
    {
        _islandResultsVoice        = voice;
        _islandResultsTitle        = title;
        _islandResultsBodyTemplate = bodyTemplate;
    }

    [Header("Typing Effect")]
    [Tooltip("Reveal body text word by word when the panel opens.")]
    [SerializeField] private bool enableTypingEffect = true;
    [Tooltip("Words revealed per second in the body text.")]
    [SerializeField] private float wordsPerSecond = 6f;
    [Tooltip("If true, the header appears instantly; only the body uses the typing effect.")]
    [SerializeField] private bool headerInstant = true;
    [Tooltip("Pause between the last header word and the first body word (seconds).")]
    [SerializeField] private float pauseAfterHeader = 0.15f;

    private Coroutine _typingCoroutine;

    private void StartTypingEffect()
    {
        StopTyping();
        if (!enableTypingEffect) return;

        // Pre-hide text NOW (same frame, before any yield) so there is no
        // one-frame flash of the full text while the coroutine hasn't run yet.
        if (headerText && !headerInstant) headerText.maxVisibleWords = 0;
        if (bodyText)                     bodyText.maxVisibleWords   = 0;

        _typingCoroutine = StartCoroutine(CoTypeTexts());
    }

    private void StopTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
        // Always reveal full text so the panel is never left in a half-typed state
        if (headerText) headerText.maxVisibleWords = int.MaxValue;
        if (bodyText)   bodyText.maxVisibleWords   = int.MaxValue;
    }

    private System.Collections.IEnumerator CoTypeTexts()
    {
        float interval = wordsPerSecond > 0f ? 1f / wordsPerSecond : 0f;

        // ── Header ──────────────────────────────────────────────────
        if (headerText)
        {
            headerText.ForceMeshUpdate();
            int totalWords = headerText.textInfo.wordCount;

            if (headerInstant || totalWords <= 0)
            {
                headerText.maxVisibleWords = int.MaxValue;
            }
            else
            {
                // maxVisibleWords = 0 already set in StartTypingEffect before any yield
                for (int w = 1; w <= totalWords; w++)
                {
                    headerText.maxVisibleWords = w;
                    yield return new WaitForSecondsRealtime(interval);
                }
                headerText.maxVisibleWords = int.MaxValue;
            }

            if (pauseAfterHeader > 0f)
                yield return new WaitForSecondsRealtime(pauseAfterHeader);
        }

        // ── Body ─────────────────────────────────────────────────────
        if (bodyText)
        {
            bodyText.ForceMeshUpdate();
            int totalWords = bodyText.textInfo.wordCount;

            if (totalWords <= 0)
            {
                bodyText.maxVisibleWords = int.MaxValue;
            }
        else
        {
            // maxVisibleWords = 0 already set in StartTypingEffect before any yield
            for (int w = 1; w <= totalWords; w++)
            {
                bodyText.maxVisibleWords = w;
                yield return new WaitForSecondsRealtime(interval);
            }
            bodyText.maxVisibleWords = int.MaxValue;
        }
        }

        _typingCoroutine = null;
    }

    private bool _isVisible;
    private bool _starting;
    private UnityEvent _currentStart;
    private DesktopArrowController _desktopLook;
    private bool _isFirstWelcomeActive;

    // NEW: replay training action
    private UnityEvent _currentReplayTraining;

    // Set at the moment the replay training button is clicked (before the fade coroutine runs),
    // so callers can read it as soon as the panel's alpha crosses the hidden threshold.
    public bool ReplayTrainingPending { get; private set; }

    private Coroutine delayCo;
    private bool _delaying;

    // NEW: 2-button navigation state
    private bool _twoButtonMode = false;
    private int _navIndex = 0; // 0 = start, 1 = replay training
    private Button[] _navButtons = null;

    private static readonly Regex s_CountdownSuffix = new Regex(@"\s*\(\d+\)\s*$", RegexOptions.Compiled);
    private static string StripCountdownSuffix(string s) => string.IsNullOrEmpty(s) ? s : s_CountdownSuffix.Replace(s, "");

    private void ApplyWelcomeMouseLookLock(bool visible)
    {
        if (!lockMouseLookWhileIntroVisible || _desktopLook == null) return;

        // Apply only for the first welcome intro panel.
        bool shouldLockLook = visible && _isFirstWelcomeActive;
        _desktopLook.SetLookAngleFrozen(shouldLockLook);

        // Once the first welcome closes, never treat later panels as "first welcome".
        if (!visible && _isFirstWelcomeActive)
            _isFirstWelcomeActive = false;
    }

    // ---------- Voice Over ----------
    private void PlayVoiceInternal(AudioClip clip)
    {
        if (!voiceSource) return;

        if (voiceSource.isPlaying)
            voiceSource.Stop();

        voiceSource.clip = clip;
        if (clip)
            voiceSource.Play();
    }

    public void PlayVoice(AudioClip clip) => PlayVoiceInternal(clip);

    public void StopVoice()
    {
        if (!voiceSource) return;
        voiceSource.Stop();
        voiceSource.clip = null;
    }

    // ---------- Start MOXO on the active island rig ----------
    private void StartMoxoGame()
    {
        var mgr = MoxoCPTManager.Instance;
        if (mgr == null || !mgr.gameObject.activeInHierarchy)
            mgr = FindObjectsOfType<MoxoCPTManager>(true)
                    .FirstOrDefault(x => x && x.gameObject.activeInHierarchy);

        if (mgr != null)
        {
            Debug.Log($"[IntroScreen] Starting MOXO on '{mgr.gameObject.name}'.");
            mgr.OnGameBegin();
        }
        else Debug.LogError("[IntroScreen] No ACTIVE MoxoCPTManager found. Did you enable the island's MOXO rig?");
    }

    private void Awake()
    {
        Instance = this;

        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!headerText) headerText = transform.Find("HeaderText")?.GetComponent<TextMeshProUGUI>();
        if (!bodyText) bodyText = transform.Find("BodyText")?.GetComponent<TextMeshProUGUI>();

        if (!startButtonLabel && startButton)
            startButtonLabel = startButton.GetComponentInChildren<TextMeshProUGUI>();

        if (!replayTrainingButtonLabel && replayTrainingButton)
            replayTrainingButtonLabel = replayTrainingButton.GetComponentInChildren<TextMeshProUGUI>();

        _desktopLook = FindObjectOfType<DesktopArrowController>(true);

        if (startButton) startButton.onClick.AddListener(OnStartClicked);

        // IMPORTANT: replay training button must NOT show on welcome/normal screens
        SetReplayTrainingVisible(false);

        if (showWelcomeOnAwake) ShowWelcome();
        else ShowInstant(false);
    }

    // NEW: if this object gets re-enabled by other flows, force-hide replay training button again
    private void OnEnable()
    {
        SetReplayTrainingVisible(false);
    }

    public void Hide()
    {
        StopAllCoroutines();
        ShowInstant(false);
        MoxoStartGate.Disarm();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    public bool IsVisible => gameObject.activeInHierarchy && _isVisible && canvasGroup && canvasGroup.alpha > 0.001f;

    public void ShowMoxoIntro(string title, string body) => ShowMoxo(title, body);

    private void Update()
    {
        if (!IsVisible || _starting || !allowPressAtoStart || _delaying) return;

        var kb = Keyboard.current;
        var gp = Gamepad.current;

        // ---- Two-button mode navigation (redo UI only) ----
        if (_twoButtonMode && _navButtons != null && _navButtons.Length >= 2)
        {
            bool left  = kb != null && kb.leftArrowKey.wasPressedThisFrame;
            bool right = kb != null && kb.rightArrowKey.wasPressedThisFrame;

            // FIX: no wrapping. Left selects Start, Right selects Replay Training.
            if (left)  { _navIndex = 0; ApplyNavSelection(); }
            if (right) { _navIndex = 1; ApplyNavSelection(); }

            bool confirm =
                (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) ||
                (gp != null && gp.buttonSouth.wasPressedThisFrame);

            if (confirm)
            {
                var b = _navButtons[Mathf.Clamp(_navIndex, 0, _navButtons.Length - 1)];
                if (b != null && b.gameObject.activeInHierarchy && b.interactable)
                    b.onClick.Invoke();
            }

            return; // prevent falling through to single-button behavior
        }

        // ---- Single-button behavior (existing) ----
        if (startButton && !startButton.interactable) return;

        if ((gp != null && gp.buttonSouth.wasPressedThisFrame) ||
            (kb != null && kb.spaceKey.wasPressedThisFrame))
        {
            OnStartClicked();
        }
    }

    // ---------- Public API ----------
    public void ShowWelcome()
    {
        // NEVER show training button here
        SetReplayTrainingVisible(false);
        _isFirstWelcomeActive = true;

        ApplyPreset(welcomePreset);
        if (startButtonLabel) startButtonLabel.text = StripCountdownSuffix(startButtonLabel.text);

        GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);

        ShowInstant(true);
        DelayStartButton(continueDelaySeconds, GetCurrentButtonLabelOr("Continue"));

        // Optional welcome voice over
        if (welcomeVoice)
            PlayVoiceInternal(welcomeVoice);

        MoxoStartGate.Disarm();
    }

    public void ShowMoxo(string titleOverride = null, string bodyOverride = null)
    {
        // NEVER show training button here
        SetReplayTrainingVisible(false);

        if (startButtonLabel) startButtonLabel.text = "Start";
        ApplyPreset(moxoPreset, titleOverride, bodyOverride);

        if (_currentStart == null) _currentStart = new UnityEvent();
        _currentStart.RemoveAllListeners();

        _currentStart.AddListener(() =>
        {
            if (!MoxoStartGate.TryConsume())
            {
                Debug.Log("[IntroScreen] Start ignored (gate not armed).");
                return;
            }
            StartMoxoGame();
        });

        if (startButton)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);
        }

        GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);

        if (canvasGroup) { canvasGroup.blocksRaycasts = true; canvasGroup.interactable = true; }

        ShowInstant(true);
        DelayStartButton(continueDelaySeconds, GetCurrentButtonLabelOr("Start"));

        // Optional generic MOXO intro voice
        if (moxoVoice)
            PlayVoiceInternal(moxoVoice);
    }

    public void ShowReadyAfterTraining(string title = "Ready to start?", string body = "Press Space to begin the real test.", float? delayOverride = null, string buttonLabel = "Start", bool showReplayTraining = false)
    {
        ReplayTrainingPending = false; // reset each time the focus screen is freshly shown
        MoxoStartGate.Arm();

        ShowMoxo(title, body); // ShowMoxo already calls SetReplayTrainingVisible(false)

        // Show replay training button BEFORE starting the delay so that
        // SetStartButtonsInteractable(false) inside CoDelayStart catches it while active,
        // keeping it greyed out for the full countdown duration.
        if (showReplayTraining)
            SetReplayTrainingVisible(true);

        DelayStartButton(delayOverride.HasValue ? delayOverride.Value : defaultReadyDelaySeconds, buttonLabel);
    }

    public void ArmOnStart(UnityAction onStart, bool delayButton = true, float delaySeconds = 5f)
    {
        _currentStart = new UnityEvent();
        if (onStart != null) _currentStart.AddListener(onStart);

        if (startButton)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (canvasGroup) { canvasGroup.blocksRaycasts = true; canvasGroup.interactable = true; }
        allowPressAtoStart = true;

        if (delayButton) DelayStartButton(delaySeconds, GetCurrentButtonLabelOr("Start"));
    }

    // NEW: show/hide training button (redo screen only)
    public void SetReplayTrainingVisible(bool on)
    {
        _twoButtonMode = on;

        if (replayTrainingButton)
            replayTrainingButton.gameObject.SetActive(on);

        BuildNavButtons();
        _navIndex = 0;
        ApplyNavSelection();

        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    // NEW: wire the replay training button action
    public void ArmReplayTraining(UnityAction onReplayTraining)
    {
        _currentReplayTraining = new UnityEvent();
        if (onReplayTraining != null) _currentReplayTraining.AddListener(onReplayTraining);

        if (!replayTrainingButton) return;

        replayTrainingButton.onClick.RemoveAllListeners();
        replayTrainingButton.onClick.AddListener(OnReplayTrainingClicked);
    }

    private void OnReplayTrainingClicked()
    {
        if (_starting || _delaying || (replayTrainingButton && !replayTrainingButton.interactable)) return;
        _starting = true;

        // Mark BEFORE the fade coroutine runs so callers polling alpha can read it immediately.
        ReplayTrainingPending = true;

        Debug.Log("[IntroScreen] Replay Training clicked → fading out then starting replay training.");

        if (canvasGroup) { canvasGroup.blocksRaycasts = false; canvasGroup.interactable = false; }
        StartCoroutine(CoFadeOutAndReplayTraining());
    }

    private System.Collections.IEnumerator CoFadeOutAndReplayTraining()
    {
        StopTyping();
        if (canvasGroup) { canvasGroup.blocksRaycasts = false; canvasGroup.interactable = false; }

        while (canvasGroup && canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }

        if (canvasGroup) canvasGroup.alpha = 0f;
        _isVisible = false;
        if (localCharacter) localCharacter.SetActive(false);
        gameObject.SetActive(false);

        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

        var toInvoke = _currentReplayTraining;
        _currentReplayTraining = null;

        if (toInvoke != null) toInvoke.Invoke();
        else Debug.LogWarning("[IntroScreen] No replay training action wired.");

        _starting = false;
    }

    private void BuildNavButtons()
    {
        if (_twoButtonMode && startButton && replayTrainingButton && replayTrainingButton.gameObject.activeInHierarchy)
            _navButtons = new[] { startButton, replayTrainingButton };
        else
            _navButtons = new[] { startButton }.Where(b => b != null).ToArray();
    }

    private void ApplyNavSelection()
    {
        if (_navButtons == null || _navButtons.Length == 0) return;

        var b = _navButtons[Mathf.Clamp(_navIndex, 0, _navButtons.Length - 1)];
        if (b && b.gameObject.activeInHierarchy)
        {
            b.Select();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
    }

    private string GetCurrentButtonLabelOr(string fallback)
    {
        string label = fallback;
        if (startButtonLabel && !string.IsNullOrWhiteSpace(startButtonLabel.text))
            label = startButtonLabel.text;
        return StripCountdownSuffix(label);
    }

    // ---------- Internals ----------
    private void ApplyPreset(PanelPreset preset, string titleOverride = null, string bodyOverride = null)
    {
        if (headerText) headerText.text = string.IsNullOrEmpty(titleOverride) ? preset.title : titleOverride;
        if (bodyText) bodyText.text = string.IsNullOrEmpty(bodyOverride) ? preset.body : bodyOverride;

        _currentStart = preset?.onStart;

        if (autoWireMoxoOnStart && preset == moxoPreset)
        {
            bool missing = _currentStart == null || _currentStart.GetPersistentEventCount() == 0;
            if (missing && MoxoCPTManager.Instance != null)
            {
                if (_currentStart == null) _currentStart = new UnityEvent();
                _currentStart.AddListener(() =>
                {
                    if (!MoxoStartGate.TryConsume())
                    {
                        Debug.Log("[IntroScreen] Auto-wired start ignored (gate not armed).");
                        return;
                    }
                    MoxoCPTManager.Instance.OnGameBegin();
                });
                Debug.Log("[IntroScreen] (Compat) Auto-wired MOXO OnStart with start gate.");
            }
        }
    }

    public void OnStartClicked()
    {
        if (_starting || _delaying || (startButton && !startButton.interactable)) return;
        _starting = true;
        Debug.Log("[IntroScreen] Start clicked → fading out then starting.");

        if (canvasGroup) { canvasGroup.blocksRaycasts = false; canvasGroup.interactable = false; }
        StartCoroutine(CoFadeOutAndBegin());
    }

    public void HideInstant()
    {
        StopAllCoroutines();
        StopTyping(); // reset maxVisibleWords after StopAllCoroutines nullifies the typing coroutine
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        _isVisible = false;
        _starting = false;
        if (localCharacter) localCharacter.SetActive(false);
        gameObject.SetActive(false);

        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        MoxoStartGate.Disarm();

        // ALWAYS hide training button when hidden
        SetReplayTrainingVisible(false);

        // Safety: if HideInstant is called externally (e.g. a timeout), clear the pending flag
        // so we don't accidentally re-run training when neither button was pressed.
        ReplayTrainingPending = false;

        ApplyWelcomeMouseLookLock(visible: false);
    }

    private System.Collections.IEnumerator CoFadeOutAndBegin()
    {
        StopTyping();
        if (canvasGroup) { canvasGroup.blocksRaycasts = false; canvasGroup.interactable = false; }

        while (canvasGroup && canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }

        if (canvasGroup) canvasGroup.alpha = 0f;
        _isVisible = false;
        if (localCharacter) localCharacter.SetActive(false);
        gameObject.SetActive(false);
        ApplyWelcomeMouseLookLock(visible: false);

        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

        var toInvoke = _currentStart;
        _currentStart = null;
        startButton?.onClick.RemoveAllListeners();

        if (toInvoke != null) toInvoke.Invoke();
        else Debug.LogWarning("[IntroScreen] No onStart action wired. Panel closed, doing nothing.");

        StartDialogueAfterIntroIfConfigured();

        _starting = false;
    }

    private void StartDialogueAfterIntroIfConfigured()
    {
        if (introTestHandler == null || introDialogueState == null) return;

        GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
        introTestHandler.StartIntroDialogue(introDialogueState);
    }

    public void ShowInstant(bool visible)
    {
        _isVisible = visible;
        gameObject.SetActive(visible);
        if (canvasGroup)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
        _starting = false;

        if (!visible && EventSystem.current)
            EventSystem.current.SetSelectedGameObject(null);

        if (localCharacter) localCharacter.SetActive(visible);

        ApplyWelcomeMouseLookLock(visible);

        if (visible) StartTypingEffect();
        else         StopTyping();
    }

    public void ShowResults(int hit, int total, int falseAlarms, int totalDistractors, System.Action onContinue = null)
    {
        // NEVER show training button on normal results
        SetReplayTrainingVisible(false);

        // Resolve per-island overrides, falling back to shared inspector defaults
        string activeTitle        = !string.IsNullOrWhiteSpace(_islandResultsTitle)        ? _islandResultsTitle        : resultsTitle;
        string activeBodyTemplate = !string.IsNullOrWhiteSpace(_islandResultsBodyTemplate) ? _islandResultsBodyTemplate : resultsBodyTemplate;
        AudioClip activeVoice     = _islandResultsVoice ? _islandResultsVoice : resultsVoice;

        string body = activeBodyTemplate
            .Replace("{HIT}",     hit.ToString())
            .Replace("{TOTAL}",   total.ToString())
            .Replace("{FA}",      falseAlarms.ToString())
            .Replace("{D_TOTAL}", totalDistractors.ToString());

        ApplyPreset(moxoPreset, activeTitle, body);

        if (startButtonLabel) startButtonLabel.text = resultsButtonLabel = StripCountdownSuffix(resultsButtonLabel);

        _currentStart = new UnityEvent();
        if (onContinue != null) _currentStart.AddListener(() => onContinue());

        // ShowInstant(true) already enables localCharacter, so the island character
        // will appear alongside the results panel automatically.
        ShowInstant(true);
        DelayStartButton(continueDelaySeconds, resultsButtonLabel);

        if (activeVoice)
            PlayVoiceInternal(activeVoice);

        MoxoStartGate.Disarm();
    }

    // ---------- Delay helpers ----------
    private void SetStartButtonsInteractable(bool on)
    {
        if (startButton)
        {
            startButton.interactable = on;
            var cg = startButton.GetComponent<CanvasGroup>();
            if (cg) cg.alpha = on ? 1f : 0.5f;
        }

        if (replayTrainingButton && replayTrainingButton.gameObject.activeInHierarchy)
        {
            replayTrainingButton.interactable = on;
            var cg2 = replayTrainingButton.GetComponent<CanvasGroup>();
            if (cg2) cg2.alpha = on ? 1f : 0.5f;
        }
    }

    public void DelayStartButton(float seconds, string finalLabel = "Start")
    {
        if (delayCo != null) StopCoroutine(delayCo);
        finalLabel = StripCountdownSuffix(finalLabel);
        if (startButtonLabel) startButtonLabel.text = StripCountdownSuffix(startButtonLabel.text);
        if (replayTrainingButtonLabel) replayTrainingButtonLabel.text = StripCountdownSuffix(replayTrainingButtonLabel.text);
        delayCo = StartCoroutine(CoDelayStart(seconds, finalLabel));
    }

    private System.Collections.IEnumerator CoDelayStart(float seconds, string finalLabel)
    {
        _delaying = true;
        SetStartButtonsInteractable(false);

        // Capture the replay training button's base label once at the start of the countdown
        string replayFinalLabel = replayTrainingButtonLabel
            ? StripCountdownSuffix(replayTrainingButtonLabel.text)
            : "Replay Training";

        float t = Mathf.Max(0f, seconds);
        while (t > 0.01f)
        {
            if (showCountdownOnButton)
            {
                if (startButtonLabel)
                    startButtonLabel.text = $"{finalLabel} ({Mathf.CeilToInt(t)})";

                if (replayTrainingButtonLabel && replayTrainingButton && replayTrainingButton.gameObject.activeInHierarchy)
                    replayTrainingButtonLabel.text = $"{replayFinalLabel} ({Mathf.CeilToInt(t)})";
            }
            yield return new WaitForSecondsRealtime(1f);
            t -= 1f;
        }

        if (startButtonLabel) startButtonLabel.text = finalLabel;
        if (replayTrainingButtonLabel) replayTrainingButtonLabel.text = replayFinalLabel;
        SetStartButtonsInteractable(true);
        _delaying = false;

        ApplyNavSelection();
    }

    public void ShowTextOnly(string title, string body)
    {
        // IMPORTANT: ShowTextOnly is used by Travel/Training/Moxo intro screens.
        // So we ALWAYS hide ReplayTraining here by default.
        SetReplayTrainingVisible(false);

        if (headerText) headerText.text = string.IsNullOrEmpty(title) ? "" : title;
        if (bodyText)   bodyText.text   = string.IsNullOrEmpty(body)  ? "" : body;

        _currentStart = null;
        if (delayCo != null) { StopCoroutine(delayCo); delayCo = null; }

        SetStartButtonsInteractable(true);
        allowPressAtoStart = true;

        if (startButtonLabel) startButtonLabel.text = StripCountdownSuffix(startButtonLabel.text);

        ShowInstant(true);

        // Text-only panels cannot start MOXO unless caller arms it elsewhere.
        MoxoStartGate.Disarm();

        // selection safety
        BuildNavButtons();
        _navIndex = 0;
        ApplyNavSelection();
    }


    public static string Fallback(string primary, string fallback) => string.IsNullOrWhiteSpace(primary) ? fallback : primary;
}
