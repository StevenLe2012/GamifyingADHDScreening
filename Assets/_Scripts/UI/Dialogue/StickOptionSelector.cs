using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using Dialogue;

public class StickOptionSelector : MonoBehaviour
{
    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference moveStick; // Vector2 (stick/dpad/arrows)

    [Header("Scope (IMPORTANT)")]
    [Tooltip("Set this to the DialogueOptions Transform (parent of DialogueOption1..5).")]
    [SerializeField] private Transform optionsRoot;

    [Header("Confirm")]
    [SerializeField] private bool allowSpaceConfirm = true;
    [SerializeField] private bool allowGamepadAConfirm = true;

    [Header("Selection")]
    [SerializeField] private bool wrap = true;

    // Navigate left/right, so this is a horizontal deadzone.
    [SerializeField] private float horizontalDeadzone = 0.35f;

    [SerializeField] private float repeatDelay = 0.25f;
    [SerializeField] private bool selectFirstOnSync = true;

    [Header("Robustness")]
    [Tooltip("If SyncOptions is called while DialogueOptions root is inactive, temporarily enable it so children can be discovered.")]
    [SerializeField] private bool forceEnableRootDuringSync = true;

    [Header("Input Leak Protection (NEW)")]
    [Tooltip("Ignore confirm for this long after options become visible (prevents the same press that opened options from selecting).")]
    [SerializeField] private float ignoreConfirmAfterOpenSeconds = 0.20f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<GameObject> _options = new();
    private int _index = -1;

    private bool _canStep = true;
    private float _repeatTimer = 0f;
    private int _lastSubmitFrame = -999;

    private bool _syncedWhileVisible = false;

    // ignore confirm window
    private float _ignoreConfirmUntilUnscaled = 0f;

    // detect transitions in root visibility
    private bool _wasRootVisibleLastFrame = false;

    // ✅ NEW: block resync/reselect during confirm (prevents “option repeats”)
    private bool _confirmInFlight = false;

    // Cache reflection for speed
    private MethodInfo _chooseMethodCached;

    private void OnEnable()
    {
        if (moveStick) moveStick.action.Enable();

        _syncedWhileVisible = false;
        _wasRootVisibleLastFrame = optionsRoot != null && optionsRoot.gameObject.activeInHierarchy;

        if (_wasRootVisibleLastFrame)
        {
            NotifyOptionsOpened();
            SyncOptions();
            if (selectFirstOnSync) SelectIndex(GetDefaultIndex());
            _syncedWhileVisible = true;
        }
    }

    private void OnDisable()
    {
        if (moveStick) moveStick.action.Disable();
        ClearHover();
        _index = -1;
        _lastSubmitFrame = -999;
        _syncedWhileVisible = false;
        _ignoreConfirmUntilUnscaled = 0f;
        _wasRootVisibleLastFrame = false;
        _confirmInFlight = false;
    }

    // ---------- BACKWARD COMPAT (DialogueUI expects this) ----------
    public void SyncAndSelectFirst()
    {
        SyncOptions();
        if (selectFirstOnSync) SelectIndex(GetDefaultIndex());
    }

    // ---------- BACKWARD COMPAT ----------
    public void SyncButtons()
    {
        SyncOptions();
    }

    // ---------- call this from DialogueUI right after options become visible ----------
    public void NotifyOptionsOpened()
    {
        _ignoreConfirmUntilUnscaled = Time.unscaledTime + Mathf.Max(0f, ignoreConfirmAfterOpenSeconds);

        // Block same-frame double trigger
        _lastSubmitFrame = Time.frameCount;

        if (debugLogs)
            Debug.Log("[StickOptionSelector] Options opened -> ignoring confirm briefly.");
    }

    // ---------- Actual implementation ----------
    public void SyncOptions()
    {
        _options.Clear();

        if (optionsRoot == null)
        {
            Debug.LogWarning("[StickOptionSelector] optionsRoot is NOT assigned (should be DialogueOptions).");
            _index = -1;
            return;
        }

        bool rootWasInactive = !optionsRoot.gameObject.activeSelf;
        if (rootWasInactive && forceEnableRootDuringSync)
            optionsRoot.gameObject.SetActive(true);

        // Collect active children with their logical option index (DialogueOptionAction.OptionIndex)
        var entries = new List<(int logicalIndex, GameObject go)>();
        for (int i = 0; i < optionsRoot.childCount; i++)
        {
            var go = optionsRoot.GetChild(i).gameObject;
            if (!go.activeSelf) continue;

            var action = go.GetComponent<DialogueOptionAction>();
            int logicalIndex = action != null ? action.OptionIndex : int.MaxValue;

            entries.Add((logicalIndex, go));
        }

        // Navigation order: logical option 0,1,2,3,4 (i.e., option 1→2→3→4→5)
        foreach (var e in entries.OrderBy(e => e.logicalIndex))
        {
            if (e.logicalIndex == int.MaxValue) continue; // skip if no logical index
            _options.Add(e.go);
        }

        if (rootWasInactive && forceEnableRootDuringSync)
            optionsRoot.gameObject.SetActive(false);

        if (debugLogs)
        {
            Debug.Log($"[StickOptionSelector] SyncOptions found {_options.Count} options under '{optionsRoot.name}'. Order=" +
                      $"{string.Join("=>", _options.Select(o => o.name))}");
        }

        _index = (_options.Count > 0) ? Mathf.Clamp(_index, 0, _options.Count - 1) : -1;
        UpdateHover();
    }

    private void Update()
    {
        if (optionsRoot == null) return;

        bool rootVisible = optionsRoot.gameObject.activeInHierarchy;

        // Detect "just opened" (even if DialogueUI forgot to notify)
        if (rootVisible && !_wasRootVisibleLastFrame)
        {
            // ✅ IMPORTANT: do NOT call NotifyOptionsOpened() here (DialogueUI already calls it).
            // Calling twice extends the ignore window and can desync selection timing.
            SyncOptions();
            if (selectFirstOnSync) SelectIndex(GetDefaultIndex());
            _syncedWhileVisible = true;
        }
        _wasRootVisibleLastFrame = rootVisible;

        if (!rootVisible)
        {
            _syncedWhileVisible = false;
            return;
        }

        // ✅ If we are confirming this frame, don't resync/reselect (prevents option repeat / index reset)
        if (_confirmInFlight) return;

        if (!_syncedWhileVisible)
        {
            SyncOptions();
            if (selectFirstOnSync) SelectIndex(GetDefaultIndex());
            _syncedWhileVisible = true;
        }

        if (_options.Count == 0) return;

        // Navigate Left/Right **only** with keyboard arrow keys (and optionally gamepad dpad)
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        bool leftPressed =
            (kb != null && kb.leftArrowKey.wasPressedThisFrame) ||
            (gp != null && gp.dpad.left.wasPressedThisFrame);

        bool rightPressed =
            (kb != null && kb.rightArrowKey.wasPressedThisFrame) ||
            (gp != null && gp.dpad.right.wasPressedThisFrame);

        if (leftPressed)
            Step(-1);
        else if (rightPressed)
            Step(+1);

        // Confirm (Space / Enter / Gamepad A)
        bool pressedSpace = allowSpaceConfirm && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool pressedEnter = Keyboard.current != null &&
                            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
        bool pressedPadA = allowGamepadAConfirm && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (!(pressedSpace || pressedEnter || pressedPadA)) return;

        // Ignore confirm if options just opened (prevents “random” auto-pick / repeat)
        if (Time.unscaledTime < _ignoreConfirmUntilUnscaled)
        {
            if (debugLogs) Debug.Log("[StickOptionSelector] Ignored CONFIRM (within ignore window).");
            return;
        }

        // Same-frame guard
        if (Time.frameCount == _lastSubmitFrame) return;
        _lastSubmitFrame = Time.frameCount;

        // ✅ block any resync/reselect this frame
        _confirmInFlight = true;

        ActivateCurrent();

        // ✅ clear next frame so normal update can resume
        StartCoroutine(ClearConfirmInFlightNextFrame());
    }

    private System.Collections.IEnumerator ClearConfirmInFlightNextFrame()
    {
        yield return null;
        _confirmInFlight = false;
    }

    private void Step(int dir)
    {
        if (_options.Count == 0) return;

        int newIndex = _index + dir;

        if (wrap)
        {
            if (newIndex < 0) newIndex = _options.Count - 1;
            else if (newIndex >= _options.Count) newIndex = 0;
        }
        else
        {
            newIndex = Mathf.Clamp(newIndex, 0, _options.Count - 1);
        }

        SelectIndex(newIndex);
        MoxoCPT.LoggingDialogueChoices.AddSelectionChange();
    }

    private void SelectIndex(int newIndex)
    {
        if (_options.Count == 0) { _index = -1; return; }

        _index = Mathf.Clamp(newIndex, 0, _options.Count - 1);
        UpdateHover();

        if (debugLogs)
            Debug.Log($"[StickOptionSelector] Selected index {_index}: '{_options[_index].name}'");
    }

    // Default hover index: always hover the central visual slot (DialogueOption1) when possible.
    private int GetDefaultIndex()
    {
        if (_options.Count <= 0) return -1;

        // Prefer the element whose GameObject name corresponds to "DialogueOption1"
        for (int i = 0; i < _options.Count; i++)
        {
            var n = ExtractOptionNumber(_options[i].name);
            if (n == 1) return i;
        }

        // Fallback: last option in navigation list.
        return _options.Count - 1;
    }

    private void UpdateHover()
    {
        for (int i = 0; i < _options.Count; i++)
            _options[i].GetComponent<HoverableButtonFx>()?.SetHovered(i == _index);
    }

    private void ClearHover()
    {
        foreach (var go in _options)
            go.GetComponent<HoverableButtonFx>()?.SetHovered(false);
    }

    private void ActivateCurrent()
    {
        if (_index < 0 || _index >= _options.Count) return;

        var optionGO = _options[_index];

        var action = FindDialogueOptionAction(optionGO);
        if (action == null)
        {
            Debug.LogWarning($"[StickOptionSelector] '{optionGO.name}' has NO DialogueOptionAction component (by name).");
            return;
        }

        if (!TryInvokeChoose(action))
        {
            Debug.LogWarning($"[StickOptionSelector] '{optionGO.name}' DialogueOptionAction has no Choose() method.");
            return;
        }

        if (debugLogs)
            Debug.Log($"[StickOptionSelector] CONFIRM -> Choose() on '{optionGO.name}' (index={_index})");
    }

    private static MonoBehaviour FindDialogueOptionAction(GameObject optionGO)
    {
        var mbs = optionGO.GetComponents<MonoBehaviour>();
        foreach (var mb in mbs)
        {
            if (mb == null) continue;
            if (mb.GetType().Name == "DialogueOptionAction")
                return mb;
        }
        return null;
    }

    private bool TryInvokeChoose(MonoBehaviour action)
    {
        if (action == null) return false;

        if (_chooseMethodCached == null || _chooseMethodCached.DeclaringType != action.GetType())
        {
            _chooseMethodCached = action.GetType().GetMethod(
                "Choose",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        }

        if (_chooseMethodCached == null) return false;

        try
        {
            _chooseMethodCached.Invoke(action, null);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[StickOptionSelector] Exception invoking Choose(): {e.Message}", this);
            return false;
        }
    }

    // parse "DialogueOption4" -> 4
    private static int ExtractOptionNumber(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;

        const string prefix = "DialogueOption";
        int idx = name.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return -1;

        int start = idx + prefix.Length;
        if (start >= name.Length) return -1;

        int n = 0;
        bool any = false;
        for (int i = start; i < name.Length; i++)
        {
            char c = name[i];
            if (c < '0' || c > '9') break;
            any = true;
            n = (n * 10) + (c - '0');
        }

        return any ? n : -1;
    }
}
