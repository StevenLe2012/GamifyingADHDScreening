// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using UnityEngine.EventSystems;   // EventSystem focus
// using UnityEngine.InputSystem;    // New Input System

// public class IslandSelectionUI : MonoBehaviour
// {
//     public static IslandSelectionUI I { get; private set; }

//     void Awake()
//     {
//         I = this;
//         Hide(); // start hidden, keep component enabled
//     }

//     [Header("Wiring")]
//     [SerializeField] private GameObject panel;
//     [SerializeField] private Transform buttonParent;
//     [SerializeField] private GameObject buttonPrefab;

//     [Header("Highlight")]
//     [SerializeField] private Color selectedTint = new Color(1.15f, 1.15f, 1.15f, 1f);
//     [SerializeField] private Color normalTint = Color.white;
//     [SerializeField] private float selectedScale = 1.05f;
//     [SerializeField] private float normalScale   = 1.00f;

//     [Header("Grid Navigation")]
//     [SerializeField] private int columnsOverride = 4;
//     [SerializeField] private bool wrapHorizontal = true;
//     [SerializeField] private bool wrapVertical   = false;

//     [Header("Focus / Input Settings")]
//     [SerializeField] private bool autoFocusFirst = true;
//     [SerializeField] private bool enableLegacyInputFallback = true;
//     [SerializeField] private bool logVerbose = true;

//     [Header("Control Scheme")]
//     [SerializeField] private bool useWASD = true;

//     [Header("Input Leak Protection")]
//     [Tooltip("Ignore SPACE for this long after SHOW/CLOSE (prevents the same press from activating something else).")]
//     [SerializeField] private float ignoreConfirmSeconds = 0.20f;

//     [Tooltip("If ON, while picker is open we push UIInputFocus to block world listeners.")]
//     [SerializeField] private bool pushUIInputFocusWhileOpen = true;

//     // --- EventSystem suppression while open ---
//     private bool _esPrevSendNav = true;
//     private GameObject _esPrevSelected = null;
//     private bool _suppressEventSystemSelection = true;

//     private readonly List<Button> _buttons = new();
//     private int _index = 0;

//     private float _ignoreUntilUnscaled = 0f;

//     public bool IsOpen => panel != null && panel.activeInHierarchy;

//     // === Show remaining islands ===
//     public void ShowRemaining()
//     {
//         if (!enabled) enabled = true;

//         Clear();

//         var remaining = IslandProgress.I != null ? IslandProgress.I.Remaining().ToList() : new List<IslandData>();
//         if (remaining.Count == 0) { Hide(); return; }

//         foreach (var island in remaining)
//         {
//             var go = Instantiate(buttonPrefab, buttonParent, false);
//             go.name = $"Btn_{island.displayName}";

//             if (!go.activeSelf) go.SetActive(true);
//             go.transform.localScale = Vector3.one;

//             var cg = go.GetComponent<CanvasGroup>();
//             if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

//             var icon  = go.GetComponentInChildren<Image>(true);
//             if (icon) { icon.enabled = true; icon.sprite = island.icon; }

//             var label = go.GetComponentInChildren<TMP_Text>(true);
//             if (label) { label.enabled = true; label.text = island.displayName; }

//             var btn = go.GetComponent<Button>();
//             if (btn)
//             {
//                 btn.onClick.RemoveAllListeners();
//                 btn.onClick.AddListener(() => OnPick(island));
//                 _buttons.Add(btn);
//             }
//             else if (logVerbose)
//             {
//                 Debug.LogWarning($"[IslandSelectionUI] Button prefab '{buttonPrefab?.name}' has no Button component.", this);
//             }
//         }

//         _index = Mathf.Clamp(_index, 0, Mathf.Max(0, _buttons.Count - 1));
//         ApplyHighlight();

//         Show();
//         if (autoFocusFirst) FocusFirstSelectable();
//     }

//     void Update()
//     {
//         if (!IsOpen) return;
//         if (_buttons.Count == 0) return;

//         bool handled = false;

//         if (Keyboard.current != null)
//         {
//             if (useWASD)
//             {
//                 if (Keyboard.current.wKey.wasPressedThisFrame) { MoveGrid(0, -1); handled = true; }
//                 if (Keyboard.current.sKey.wasPressedThisFrame) { MoveGrid(0, +1); handled = true; }
//                 if (Keyboard.current.aKey.wasPressedThisFrame) { MoveGrid(-1, 0); handled = true; }
//                 if (Keyboard.current.dKey.wasPressedThisFrame) { MoveGrid(+1, 0); handled = true; }
//             }
//             else
//             {
//                 if (Keyboard.current.upArrowKey.wasPressedThisFrame)    { MoveGrid(0, -1); handled = true; }
//                 if (Keyboard.current.downArrowKey.wasPressedThisFrame)  { MoveGrid(0, +1); handled = true; }
//                 if (Keyboard.current.leftArrowKey.wasPressedThisFrame)  { MoveGrid(-1, 0); handled = true; }
//                 if (Keyboard.current.rightArrowKey.wasPressedThisFrame) { MoveGrid(+1, 0); handled = true; }
//             }

//             // SPACE ONLY (Enter ignored)
//             if (Keyboard.current.spaceKey.wasPressedThisFrame)
//             {
//                 if (Time.unscaledTime >= _ignoreUntilUnscaled)
//                 {
//                     Activate();
//                 }
//                 else if (logVerbose)
//                 {
//                     Debug.Log("[IslandSelectionUI] Ignored SPACE (within ignore window).", this);
//                 }
//                 handled = true;
//             }
//         }

//         if (!handled && enableLegacyInputFallback)
//         {
//             if (useWASD)
//             {
//                 if (Input.GetKeyDown(KeyCode.W)) MoveGrid(0, -1);
//                 if (Input.GetKeyDown(KeyCode.S)) MoveGrid(0, +1);
//                 if (Input.GetKeyDown(KeyCode.A)) MoveGrid(-1, 0);
//                 if (Input.GetKeyDown(KeyCode.D)) MoveGrid(+1, 0);
//             }
//             else
//             {
//                 if (Input.GetKeyDown(KeyCode.UpArrow))    MoveGrid(0, -1);
//                 if (Input.GetKeyDown(KeyCode.DownArrow))  MoveGrid(0, +1);
//                 if (Input.GetKeyDown(KeyCode.LeftArrow))  MoveGrid(-1, 0);
//                 if (Input.GetKeyDown(KeyCode.RightArrow)) MoveGrid(+1, 0);
//             }

//             if (Input.GetKeyDown(KeyCode.Space))
//             {
//                 if (Time.unscaledTime >= _ignoreUntilUnscaled) Activate();
//                 else if (logVerbose) Debug.Log("[IslandSelectionUI] Ignored SPACE (legacy; within ignore window).", this);
//             }
//         }
//     }

//     // ---------- Grid nav ----------
//     private void MoveGrid(int dx, int dy)
//     {
//         if (_buttons.Count == 0) return;

//         (int cols, int rows) = GetGridSize(_buttons.Count);
//         (int col, int row) = IndexToColRow(_index, cols);

//         int newCol = col + dx;
//         int newRow = row + dy;

//         if (wrapHorizontal) newCol = (newCol % cols + cols) % cols;
//         else newCol = Mathf.Clamp(newCol, 0, cols - 1);

//         if (wrapVertical) newRow = (newRow % rows + rows) % rows;
//         else newRow = Mathf.Clamp(newRow, 0, rows - 1);

//         int newIndex = ColRowToIndexClamped(newCol, newRow, cols, rows, _buttons.Count);
//         _index = newIndex;
//         ApplyHighlight();

//         if (logVerbose) Debug.Log($"[IslandSelectionUI] MoveGrid → index:{_index} col:{newCol} row:{newRow}", this);
//     }

//     private (int cols, int rows) GetGridSize(int count)
//     {
//         int cols = columnsOverride;
//         int rows;

//         var grid = buttonParent ? buttonParent.GetComponent<GridLayoutGroup>() : null;
//         if (grid != null)
//         {
//             if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
//                 cols = grid.constraintCount;

//             if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount && grid.constraintCount > 0)
//             {
//                 rows = grid.constraintCount;
//                 cols = Mathf.CeilToInt((float)count / rows);
//                 return (Mathf.Max(1, cols), Mathf.Max(1, rows));
//             }
//         }

//         cols = Mathf.Max(1, cols);
//         rows = Mathf.CeilToInt((float)count / cols);
//         rows = Mathf.Max(1, rows);
//         return (cols, rows);
//     }

//     private static (int col, int row) IndexToColRow(int index, int cols)
//     {
//         int row = index / cols;
//         int col = index % cols;
//         return (col, row);
//     }

//     private static int ColRowToIndex(int col, int row, int cols) => row * cols + col;

//     private static int ColRowToIndexClamped(int col, int row, int cols, int rows, int count)
//     {
//         int i = ColRowToIndex(col, row, cols);
//         if (i < count) return i;

//         int lastRow = Mathf.CeilToInt((float)count / cols) - 1;
//         row = Mathf.Min(row, lastRow);
//         i = ColRowToIndex(col, row, cols);
//         if (i < count) return i;

//         while (col > 0)
//         {
//             col--;
//             i = ColRowToIndex(col, row, cols);
//             if (i < count) return i;
//         }
//         return count - 1;
//     }

//     // ---------- Selection / highlight ----------
//     private void ApplyHighlight()
//     {
//         for (int i = 0; i < _buttons.Count; i++)
//         {
//             var btn = _buttons[i];
//             if (!btn) continue;

//             var rootImg = btn.GetComponent<Image>();
//             if (rootImg) rootImg.color = (i == _index) ? selectedTint : normalTint;
//             else
//             {
//                 foreach (var img in btn.GetComponentsInChildren<Image>(true))
//                     if (img) img.color = (i == _index) ? selectedTint : normalTint;
//             }

//             if (btn.transform)
//                 btn.transform.localScale = (i == _index) ? Vector3.one * selectedScale : Vector3.one * normalScale;

//             var highlight = btn.transform.Find("Highlight")?.GetComponent<Image>();
//             if (highlight) highlight.enabled = (i == _index);
//         }

//         if (!_suppressEventSystemSelection && autoFocusFirst && EventSystem.current != null && _index >= 0 && _index < _buttons.Count)
//         {
//             var go = _buttons[_index]?.gameObject;
//             if (go) EventSystem.current.SetSelectedGameObject(go);
//         }
//     }

//     public void FocusFirstSelectable()
//     {
//         if (_buttons.Count == 0) return;

//         _index = Mathf.Clamp(_index, 0, _buttons.Count - 1);
//         ApplyHighlight();

//         if (!_suppressEventSystemSelection && EventSystem.current != null)
//         {
//             var firstGO = _buttons[_index]?.gameObject;
//             if (firstGO) EventSystem.current.SetSelectedGameObject(firstGO);
//         }

//         if (logVerbose) Debug.Log($"[IslandSelectionUI] FocusFirstSelectable → {_buttons[_index].name}", this);
//     }

//     private void Activate()
//     {
//         if (_index < 0 || _index >= _buttons.Count) return;
//         var btn = _buttons[_index];
//         if (btn && btn.interactable)
//         {
//             if (logVerbose) Debug.Log($"[IslandSelectionUI] Activate → {_buttons[_index].name}", this);
//             btn.onClick.Invoke();
//         }
//     }

//     // ---------- Flow ----------
//     void OnPick(IslandData island)
//     {
//         var go = panel ? panel : gameObject;
//         var cg = go.GetComponent<CanvasGroup>();
//         if (cg) { cg.interactable = false; cg.blocksRaycasts = false; }

//         StartCoroutine(DoTravel(island));
//     }

//     System.Collections.IEnumerator DoTravel(IslandData island)
//     {
//         yield return null; // end of frame
//         Hide();
//         IslandTravelManager.I.TravelTo(island);
//     }

//     public void Show()
//     {
//         if (!enabled) enabled = true;

//         // Block gameplay listeners and swallow space for a moment.
//         if (pushUIInputFocusWhileOpen) UIInputFocus.Push(this);
//         UIInputFocus.SuppressForSeconds(ignoreConfirmSeconds);

//         // Disable EventSystem submit/nav while open (prevents Enter firing hidden buttons).
//         if (EventSystem.current != null)
//         {
//             _esPrevSendNav = EventSystem.current.sendNavigationEvents;
//             _esPrevSelected = EventSystem.current.currentSelectedGameObject;
//             EventSystem.current.sendNavigationEvents = false;
//             EventSystem.current.SetSelectedGameObject(null);
//         }
//         _suppressEventSystemSelection = true;

//         _ignoreUntilUnscaled = Time.unscaledTime + ignoreConfirmSeconds;

//         var go = panel ? panel : gameObject;
//         var cg = go.GetComponent<CanvasGroup>();
//         if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
//         go.SetActive(true);

//         if (autoFocusFirst) FocusFirstSelectable();
//     }

//     public void Hide()
//     {
//         var go = panel ? panel : gameObject;
//         var cg = go.GetComponent<CanvasGroup>();
//         if (cg) { cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }
//         go.SetActive(false);

//         // Release block and swallow one more beat so closing SPACE can't hit world.
//         if (pushUIInputFocusWhileOpen) UIInputFocus.Pop(this);
//         UIInputFocus.SuppressForSeconds(ignoreConfirmSeconds);

//         // Restore EventSystem behavior after closing.
//         if (EventSystem.current != null)
//         {
//             EventSystem.current.sendNavigationEvents = _esPrevSendNav;
//             EventSystem.current.SetSelectedGameObject(_esPrevSelected);
//         }
//         _suppressEventSystemSelection = false;

//         _buttons.Clear();
//     }

// #if UNITY_EDITOR
//     void OnValidate()
//     {
//         if (buttonPrefab != null && buttonPrefab.scene.IsValid())
//         {
//             Debug.LogError(
//                 "[IslandSelectionUI] 'buttonPrefab' is a SCENE object. " +
//                 "Please assign a Project prefab asset (drag IslandCard from Project, not Hierarchy).",
//                 this
//             );
//         }
//     }
// #endif

//     void Clear()
//     {
//         _buttons.Clear();
//         for (int i = buttonParent.childCount - 1; i >= 0; i--)
//             Destroy(buttonParent.GetChild(i).gameObject);
//     }
// }


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cinemachine;

public class IslandSelectionUI : MonoBehaviour
{
    public static IslandSelectionUI I { get; private set; }

    void Awake()
    {
        I = this;

        // IMPORTANT:
        // This component MUST live on an ACTIVE GameObject.
        // Only the "panel" GameObject should be toggled.
        Hide();
    }

    void OnEnable()
    {
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    /// <summary>
    /// Picker is only valid when state is Explore. Hide when we leave Explore,
    /// but debounce by one frame so a spurious state flicker on unpause doesn't hide the picker.
    /// </summary>
    private void OnGameStateChanged(GameManager.GameState newState)
    {
        if (newState != allowedState)
        {
            _pendingShow = false;
            if (!IsOpen) return;

            if (_pendingHideCo != null) StopCoroutine(_pendingHideCo);
            _pendingHideCo = StartCoroutine(CoHideAfterStateSettled());
        }
    }

    private IEnumerator CoHideAfterStateSettled()
    {
        yield return null; // wait one frame so unpause/flicker doesn't trigger immediate hide
        _pendingHideCo = null;
        if (GameManager.Instance == null || GameManager.Instance.State == allowedState)
            yield break;
        if (IsOpen)
        {
            if (logVerbose)
                Debug.Log($"[IslandSelectionUI] Hiding (state stayed {GameManager.Instance.State}, picker only in {allowedState}).", this);
            Hide();
        }
    }

    [Header("Wiring")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private GameObject buttonPrefab;
    [Tooltip("Optional. Picks a random remaining island; disabled when only one island is available.")]
    [SerializeField] private Button randomIslandButton;

    [Header("Highlight")]
    [SerializeField] private Color selectedTint = new Color(1.15f, 1.15f, 1.15f, 1f);
    [SerializeField] private Color normalTint = Color.white;
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float normalScale = 1.00f;

    [Header("Grid Navigation")]
    [SerializeField] private int columnsOverride = 4;
    [SerializeField] private bool wrapHorizontal = true;
    [SerializeField] private bool wrapVertical = false;

    [Header("Focus / Input Settings")]
    [SerializeField] private bool autoFocusFirst = true;
    [SerializeField] private bool enableLegacyInputFallback = true;
    [SerializeField] private bool logVerbose = true;

    [Header("Control Scheme")]
    [SerializeField] private bool useWASD = true;

    [Header("Input Leak Protection")]
    [SerializeField] private float ignoreConfirmSeconds = 0.20f;
    [SerializeField] private bool pushUIInputFocusWhileOpen = true;

    [Header("State Gate")]
    [SerializeField] private GameManager.GameState allowedState = GameManager.GameState.Explore;

    // EventSystem suppression
    private bool _esPrevSendNav = true;
    private GameObject _esPrevSelected = null;
    private bool _suppressEventSystemSelection = true;

    private readonly List<Button> _buttons = new();
    private readonly List<IslandData> _islandsInPicker = new();
    private int _index = 0;
    private float _ignoreUntilUnscaled = 0f;

    // NEW: pending show request (when called during Narrative)
    private bool _pendingShow = false;
    private Coroutine _pendingCo = null;
    private Coroutine _pendingHideCo = null;

    public bool IsOpen => panel != null && panel.activeInHierarchy;

    private bool IsAllowedNow()
    {
        if (GameManager.Instance == null) return true; // fail-open in editor/test
        return GameManager.Instance.State == allowedState;
    }

    // ===================== PUBLIC: request show =====================
    public void ShowRemaining()
    {
        // If panel is assigned to THIS gameObject and you deactivate it, coroutines will break.
        if (panel == gameObject)
        {
            Debug.LogWarning("[IslandSelectionUI] 'panel' points to the same GameObject as this script. " +
                             "Move IslandSelectionUI to an always-active parent, and assign 'panel' to a child UI object.", this);
        }

        if (!IsAllowedNow())
        {
            if (logVerbose && GameManager.Instance != null)
                Debug.Log($"[IslandSelectionUI] ShowRemaining delayed (state={GameManager.Instance.State}).", this);

            _pendingShow = true;

            // IMPORTANT: start coroutine on an ALWAYS-ACTIVE runner (GameManager), not on a panel that might be inactive.
            if (_pendingCo == null && GameManager.Instance != null)
                _pendingCo = GameManager.Instance.StartCoroutine(CoWaitForExploreThenShow());

            return;
        }

        // allowed now -> show immediately
        ShowRemainingInternal();
    }

    private IEnumerator CoWaitForExploreThenShow()
    {
        // wait until Explore
        while (GameManager.Instance != null && GameManager.Instance.State != allowedState)
            yield return null;

        _pendingCo = null;

        if (!_pendingShow) yield break;
        _pendingShow = false;

        ShowRemainingInternal();
    }

    // ===================== INTERNAL build+show =====================
    private void ShowRemainingInternal()
    {
        if (!enabled) enabled = true;

        Clear();

        var remaining = IslandProgress.I != null ? IslandProgress.I.Remaining().ToList() : new List<IslandData>();
        if (remaining.Count == 0) { Hide(); return; }

        foreach (var island in remaining)
        {
            var go = Instantiate(buttonPrefab, buttonParent, false);
            go.name = $"Btn_{island.displayName}";

            if (!go.activeSelf) go.SetActive(true);
            go.transform.localScale = Vector3.one;

            var cg = go.GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

            var icon = go.GetComponentInChildren<Image>(true);
            if (icon) { icon.enabled = true; icon.sprite = island.icon; }

            var label = go.GetComponentInChildren<TMP_Text>(true);
            if (label) { label.enabled = true; label.text = island.displayName; }

            var btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnPick(island));
                _buttons.Add(btn);
            }
            else if (logVerbose)
            {
                Debug.LogWarning($"[IslandSelectionUI] Button prefab '{buttonPrefab?.name}' has no Button component.", this);
            }
        }

        _islandsInPicker.Clear();
        _islandsInPicker.AddRange(remaining);
        UpdateRandomIslandButton();

        _index = Mathf.Clamp(_index, 0, Mathf.Max(0, _buttons.Count - 1));
        ApplyHighlight();

        Show();
        if (autoFocusFirst) FocusFirstSelectable();
    }

    void LateUpdate()
    {
        // Only run visibility fix when we're in Explore (picker is allowed).
        if (!IsAllowedNow()) return;

        // Case 1: Picker should be open (we have buttons) but panel was hidden by something (e.g. unpause flicker).
        // Force it back visible so it survives resume-from-pause.
        bool shouldBeOpen = _buttons.Count > 0 && panel != null;
        if (shouldBeOpen && !panel.activeInHierarchy)
        {
            if (logVerbose) Debug.Log("[IslandSelectionUI] Restoring picker (panel was hidden while in Explore).", this);
            ApplyPickerVisibility();
            return;
        }

        // Case 2: Picker is open; keep World Space canvas and alpha correct every frame.
        if (!IsOpen) return;
        ApplyPickerVisibility();
    }

    private void ApplyPickerVisibility()
    {
        if (panel == null) return;

        var root = panel.transform;
        var canvas = root.GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                var cam = GetDisplayCamera();
                if (cam != null)
                    canvas.worldCamera = cam;

                var cg = canvas.GetComponent<CanvasGroup>();
                if (cg != null && cg.alpha < 0.99f) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
            }
        }

        if (!panel.activeSelf)
            panel.SetActive(true);
        var panelCg = panel.GetComponent<CanvasGroup>();
        if (panelCg != null && panelCg.alpha < 0.99f) { panelCg.alpha = 1f; panelCg.blocksRaycasts = true; panelCg.interactable = true; }
    }

    /// <summary>
    /// The camera that is actually rendering the game view. Prefer the one with CinemachineBrain
    /// (so we stay correct when MOXO/cutscene switch virtual cameras), then Camera.main.
    /// </summary>
    private static Camera GetDisplayCamera()
    {
        var brain = Object.FindObjectOfType<CinemachineBrain>(true);
        if (brain != null && brain.isActiveAndEnabled)
        {
            var cam = brain.GetComponent<Camera>();
            if (cam != null && cam.isActiveAndEnabled)
                return cam;
        }
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;
        var cams = Object.FindObjectsOfType<Camera>(true);
        foreach (var c in cams)
            if (c.isActiveAndEnabled && c.targetDisplay == 0)
                return c;
        return null;
    }

    void Update()
    {
        if (!IsOpen) return;

        // Do NOT auto-hide when state changes: the picker is only shown after Koala dialogue
        // when we're already in Explore. Auto-hiding here caused the picker to disappear
        // as soon as it was shown (e.g. one frame of state mismatch or ordering).

        if (_buttons.Count == 0) return;

        bool handled = false;

        if (Keyboard.current != null)
        {
            if (useWASD)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame) { MoveGrid(0, -1); handled = true; }
                if (Keyboard.current.sKey.wasPressedThisFrame) { MoveGrid(0, +1); handled = true; }
                if (Keyboard.current.aKey.wasPressedThisFrame) { MoveGrid(-1, 0); handled = true; }
                if (Keyboard.current.dKey.wasPressedThisFrame) { MoveGrid(+1, 0); handled = true; }
            }
            else
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame) { MoveGrid(0, -1); handled = true; }
                if (Keyboard.current.downArrowKey.wasPressedThisFrame) { MoveGrid(0, +1); handled = true; }
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame) { MoveGrid(-1, 0); handled = true; }
                if (Keyboard.current.rightArrowKey.wasPressedThisFrame) { MoveGrid(+1, 0); handled = true; }
            }

            bool pressedConfirm = Keyboard.current.spaceKey.wasPressedThisFrame ||
                                  Keyboard.current.enterKey.wasPressedThisFrame ||
                                  Keyboard.current.numpadEnterKey.wasPressedThisFrame;
            if (pressedConfirm)
            {
                if (Time.unscaledTime >= _ignoreUntilUnscaled) Activate();
                else if (logVerbose) Debug.Log("[IslandSelectionUI] Ignored confirm (Space/Enter) within ignore window.", this);
                handled = true;
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                if (Time.unscaledTime >= _ignoreUntilUnscaled) OnRandomIslandClicked();
                else if (logVerbose) Debug.Log("[IslandSelectionUI] Ignored R (within ignore window).", this);
                handled = true;
            }
        }

        if (!handled && enableLegacyInputFallback)
        {
            if (useWASD)
            {
                if (Input.GetKeyDown(KeyCode.W)) MoveGrid(0, -1);
                if (Input.GetKeyDown(KeyCode.S)) MoveGrid(0, +1);
                if (Input.GetKeyDown(KeyCode.A)) MoveGrid(-1, 0);
                if (Input.GetKeyDown(KeyCode.D)) MoveGrid(+1, 0);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.UpArrow)) MoveGrid(0, -1);
                if (Input.GetKeyDown(KeyCode.DownArrow)) MoveGrid(0, +1);
                if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveGrid(-1, 0);
                if (Input.GetKeyDown(KeyCode.RightArrow)) MoveGrid(+1, 0);
            }

            if (Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (Time.unscaledTime >= _ignoreUntilUnscaled) Activate();
                else if (logVerbose) Debug.Log("[IslandSelectionUI] Ignored confirm (legacy Space/Enter) within ignore window.", this);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if (Time.unscaledTime >= _ignoreUntilUnscaled) OnRandomIslandClicked();
                else if (logVerbose) Debug.Log("[IslandSelectionUI] Ignored R (legacy; within ignore window).", this);
            }
        }
    }

    // ---------- Grid nav ----------
    private void MoveGrid(int dx, int dy)
    {
        if (_buttons.Count == 0) return;

        (int cols, int rows) = GetGridSize(_buttons.Count);
        (int col, int row) = IndexToColRow(_index, cols);

        int newCol = col + dx;
        int newRow = row + dy;

        if (wrapHorizontal) newCol = (newCol % cols + cols) % cols;
        else newCol = Mathf.Clamp(newCol, 0, cols - 1);

        if (wrapVertical) newRow = (newRow % rows + rows) % rows;
        else newRow = Mathf.Clamp(newRow, 0, rows - 1);

        int newIndex = ColRowToIndexClamped(newCol, newRow, cols, rows, _buttons.Count);
        _index = newIndex;
        ApplyHighlight();

        if (logVerbose) Debug.Log($"[IslandSelectionUI] MoveGrid → index:{_index} col:{newCol} row:{newRow}", this);
    }

    private (int cols, int rows) GetGridSize(int count)
    {
        int cols = columnsOverride;
        int rows;

        var grid = buttonParent ? buttonParent.GetComponent<GridLayoutGroup>() : null;
        if (grid != null)
        {
            if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
                cols = grid.constraintCount;

            if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount && grid.constraintCount > 0)
            {
                rows = grid.constraintCount;
                cols = Mathf.CeilToInt((float)count / rows);
                return (Mathf.Max(1, cols), Mathf.Max(1, rows));
            }
        }

        cols = Mathf.Max(1, cols);
        rows = Mathf.CeilToInt((float)count / cols);
        rows = Mathf.Max(1, rows);
        return (cols, rows);
    }

    private static (int col, int row) IndexToColRow(int index, int cols)
    {
        int row = index / cols;
        int col = index % cols;
        return (col, row);
    }

    private static int ColRowToIndex(int col, int row, int cols) => row * cols + col;

    private static int ColRowToIndexClamped(int col, int row, int cols, int rows, int count)
    {
        int i = ColRowToIndex(col, row, cols);
        if (i < count) return i;

        int lastRow = Mathf.CeilToInt((float)count / cols) - 1;
        row = Mathf.Min(row, lastRow);
        i = ColRowToIndex(col, row, cols);
        if (i < count) return i;

        while (col > 0)
        {
            col--;
            i = ColRowToIndex(col, row, cols);
            if (i < count) return i;
        }
        return count - 1;
    }

    private void ApplyHighlight()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            var btn = _buttons[i];
            if (!btn) continue;

            var rootImg = btn.GetComponent<Image>();
            if (rootImg) rootImg.color = (i == _index) ? selectedTint : normalTint;
            else
            {
                foreach (var img in btn.GetComponentsInChildren<Image>(true))
                    if (img) img.color = (i == _index) ? selectedTint : normalTint;
            }

            if (btn.transform)
                btn.transform.localScale = (i == _index) ? Vector3.one * selectedScale : Vector3.one * normalScale;

            var highlight = btn.transform.Find("Highlight")?.GetComponent<Image>();
            if (highlight) highlight.enabled = (i == _index);
        }

        if (!_suppressEventSystemSelection && autoFocusFirst && EventSystem.current != null &&
            _index >= 0 && _index < _buttons.Count)
        {
            var go = _buttons[_index]?.gameObject;
            if (go) EventSystem.current.SetSelectedGameObject(go);
        }
    }

    public void FocusFirstSelectable()
    {
        if (_buttons.Count == 0) return;

        _index = Mathf.Clamp(_index, 0, _buttons.Count - 1);
        ApplyHighlight();

        if (!_suppressEventSystemSelection && EventSystem.current != null)
        {
            var firstGO = _buttons[_index]?.gameObject;
            if (firstGO) EventSystem.current.SetSelectedGameObject(firstGO);
        }

        if (logVerbose) Debug.Log($"[IslandSelectionUI] FocusFirstSelectable → {_buttons[_index].name}", this);
    }

    private void Activate()
    {
        if (_index < 0 || _index >= _buttons.Count) return;
        var btn = _buttons[_index];
        if (btn && btn.interactable)
        {
            if (logVerbose) Debug.Log($"[IslandSelectionUI] Activate → {_buttons[_index].name}", this);
            btn.onClick.Invoke();
        }
    }

    private void UpdateRandomIslandButton()
    {
        if (!randomIslandButton) return;

        bool canRandom = _islandsInPicker.Count > 1;
        randomIslandButton.interactable = canRandom;

        // Replace the event object so Inspector-persistent listeners cannot bypass
        // this class's random flow (which handles intro cutscene + state guards).
        randomIslandButton.onClick = new Button.ButtonClickedEvent();
        if (canRandom)
            randomIslandButton.onClick.AddListener(OnRandomIslandClicked);
    }

    private void OnRandomIslandClicked()
    {
        if (_islandsInPicker.Count <= 1) return;
        int i = UnityEngine.Random.Range(0, _islandsInPicker.Count);
        OnPick(_islandsInPicker[i]);
    }

    void OnPick(IslandData island)
    {
        var cg = panel ? panel.GetComponent<CanvasGroup>() : null;
        if (cg) { cg.interactable = false; cg.blocksRaycasts = false; }
        StartCoroutine(DoTravel(island));
    }

    IEnumerator DoTravel(IslandData island)
    {
        yield return null;
        Hide();
        // If a special cutscene exists (e.g. DRAGON intro), let it decide;
        // otherwise fall back to normal travel.
        if (IslandIntroCutscene.I != null)
        {
            IslandIntroCutscene.I.PlayIfNeeded(island);
        }
        else
        {
            IslandTravelManager.I.TravelTo(island);
        }
    }

    public void Show()
    {
        if (!IsAllowedNow())
        {
            if (logVerbose && GameManager.Instance != null)
                Debug.Log($"[IslandSelectionUI] Show ignored (state={GameManager.Instance.State}).", this);
            return;
        }

        if (!enabled) enabled = true;

        if (pushUIInputFocusWhileOpen) UIInputFocus.Push(this);
        UIInputFocus.SuppressForSeconds(ignoreConfirmSeconds);

        if (EventSystem.current != null)
        {
            _esPrevSendNav = EventSystem.current.sendNavigationEvents;
            _esPrevSelected = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.sendNavigationEvents = false;
            EventSystem.current.SetSelectedGameObject(null);
        }
        _suppressEventSystemSelection = true;

        _ignoreUntilUnscaled = Time.unscaledTime + ignoreConfirmSeconds;

        if (panel != null)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
            panel.SetActive(true);
        }

        if (autoFocusFirst) FocusFirstSelectable();
    }

    public void Hide()
    {
        if (panel != null)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }
            panel.SetActive(false);
        }

        if (pushUIInputFocusWhileOpen) UIInputFocus.Pop(this);
        UIInputFocus.SuppressForSeconds(ignoreConfirmSeconds);

        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = _esPrevSendNav;
            EventSystem.current.SetSelectedGameObject(_esPrevSelected);
        }
        _suppressEventSystemSelection = false;

        _buttons.Clear();
        _islandsInPicker.Clear();
        if (randomIslandButton)
        {
            randomIslandButton.onClick.RemoveAllListeners();
            randomIslandButton.interactable = false;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (buttonPrefab != null && buttonPrefab.scene.IsValid())
        {
            Debug.LogError(
                "[IslandSelectionUI] 'buttonPrefab' is a SCENE object. Please assign a Project prefab asset.",
                this
            );
        }
    }
#endif

    void Clear()
    {
        _buttons.Clear();
        _islandsInPicker.Clear();
        if (randomIslandButton)
        {
            randomIslandButton.onClick.RemoveAllListeners();
            randomIslandButton.interactable = false;
        }
        for (int i = buttonParent.childCount - 1; i >= 0; i--)
            Destroy(buttonParent.GetChild(i).gameObject);
    }
}
