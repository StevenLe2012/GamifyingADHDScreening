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


using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using MoxoCPT;

public class IntroScreen : MonoBehaviour
{
    
    [SerializeField] private bool autoWireMoxoOnStart = true;
    
    public static IntroScreen Instance { get; private set; }

    [Header("Wiring")]
    [SerializeField] private CanvasGroup canvasGroup;           // WelcomePanel (CanvasGroup)
    [SerializeField] private Button startButton;                // StartButton
    [SerializeField] private Dialogue.DialogueHandler dialogueHandler; // (only used for Welcome)

    [Header("Optional Text (assign these)")]
    [SerializeField] private TextMeshProUGUI headerText;        // HeaderText child
    [SerializeField] private TextMeshProUGUI bodyText;          // BodyText child

    [Header("Behavior")]
    [SerializeField] private bool showWelcomeOnAwake = true;
    [SerializeField] private bool allowPressAtoStart = true;
    [SerializeField] private float fadeSpeed = 10f;

    // Preset content
    [System.Serializable]
    public class PanelPreset
    {
        public string title;
        [TextArea] public string body;
        public UnityEvent onStart;   // actions to run after Start
    }

    [Header("Presets")]
    [SerializeField] private PanelPreset welcomePreset;
    [SerializeField] private PanelPreset moxoPreset;

    private bool _isVisible;
    private bool _starting;
    private UnityEvent _currentStart; // what to run after fade

    private void StartMoxoGame()
    {
        if (MoxoCPT.MoxoCPTManager.Instance != null)
        {
            Debug.Log("[IntroScreen] Starting MOXO (OnGameBegin).");
            MoxoCPT.MoxoCPTManager.Instance.OnGameBegin();
        }
        else
        {
            Debug.LogWarning("[IntroScreen] MoxoCPTManager.Instance not found. CPT will not start.");
        }
    }
    

    private void Awake()
    {
        Instance = this; 

        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (startButton) startButton.onClick.AddListener(OnStartClicked);

        // Try to auto-find TMPs if not assigned
        if (!headerText) headerText = transform.Find("HeaderText")?.GetComponent<TextMeshProUGUI>();
        if (!bodyText)   bodyText   = transform.Find("BodyText")?.GetComponent<TextMeshProUGUI>();

        if (showWelcomeOnAwake) ShowWelcome();
        else ShowInstant(false);
    }

    public void Hide()
    {
        StopAllCoroutines();
        ShowInstant(false);
    }

    public bool IsVisible => gameObject.activeInHierarchy && _isVisible && canvasGroup.alpha > 0.001f;

    public void ShowMoxoIntro(string title, string body)
    {
        ShowMoxo(title, body);
    }

    private void Update()
    {
        // ✅ use IsVisible, not just _isVisible
        if (!IsVisible || _starting || !allowPressAtoStart) return;

        bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
        bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (pressedA || pressedSpace) OnStartClicked();
    }

    // -------- Public API --------
    public void ShowWelcome()
    {
        ApplyPreset(welcomePreset);
        if (GameManager.Instance) GameManager.Instance.UpdateGameState(GameManager.GameState.Narrative);
        ShowInstant(true);
    }


    public void ShowMoxo(string titleOverride = null, string bodyOverride = null)
    {
        ApplyPreset(moxoPreset, titleOverride, bodyOverride);

        // Ensure the game will actually begin when Start is clicked (or A/Space is pressed)
        if (_currentStart == null)
            _currentStart = new UnityEvent();

        if (_currentStart.GetPersistentEventCount() == 0)
            _currentStart.AddListener(StartMoxoGame);

        // IMPORTANT: keep the state as PrepareCPT while the intro is up
        if (GameManager.Instance)
            GameManager.Instance.UpdateGameState(GameManager.GameState.PrepareCPT);

        ShowInstant(true);
    }


    // -------- Internals --------
    private void ApplyPreset(PanelPreset preset, string titleOverride = null, string bodyOverride = null)
    {
        if (headerText) headerText.text = string.IsNullOrEmpty(titleOverride) ? preset.title : titleOverride;
        if (bodyText)   bodyText.text   = string.IsNullOrEmpty(bodyOverride)  ? preset.body  : bodyOverride;

        _currentStart = preset?.onStart;

        // SAFETY-NET: if MOXO preset has no OnStart wired, hook MoxoCPTManager.OnGameBegin()
        if (autoWireMoxoOnStart && preset == moxoPreset)
        {
            bool missing = _currentStart == null || _currentStart.GetPersistentEventCount() == 0;
            if (missing && MoxoCPTManager.Instance != null)
            {
                if (_currentStart == null) _currentStart = new UnityEvent();
                _currentStart.AddListener(MoxoCPTManager.Instance.OnGameBegin);
                Debug.Log("[IntroScreen] Auto-wired MOXO OnStart → MoxoCPTManager.OnGameBegin()");
            }
        }
    }

    public void OnStartClicked()
    {
        if (_starting) return;
        _starting = true;

        // ✅ stop blocking immediately so it can't "stick"
        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
        }

        StartCoroutine(CoFadeOutAndBegin());
    }

    // ✅ optional hard-off you can invoke from UnityEvent (first item)
    public void HideInstant()
    {
        StopAllCoroutines();
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
        }
        _isVisible = false;
        _starting  = false;
        gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator CoFadeOutAndBegin()
    {
        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
        }

        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }

        if (canvasGroup) canvasGroup.alpha = 0f;
        _isVisible = false;
        gameObject.SetActive(false);

        if (_currentStart != null)
            _currentStart.Invoke();
        else
            Debug.LogWarning("[IntroScreen] No onStart action wired in the preset. Panel closed, doing nothing.");
    }


    private void ShowInstant(bool visible)
    {
        _isVisible = visible;
        gameObject.SetActive(visible);
        if (canvasGroup)
        {
            canvasGroup.alpha         = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable   = visible;
        }
        _starting = false;
    }

    
}