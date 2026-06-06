using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI; // Canvas
using Helpers;       // ScriptableEvent

public class KoalaDialogueHandler : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private ScriptableObject dialogueTreeAsset;  // Dialogue.DialogueTreeObjects
    [SerializeField] private GameObject dialogueUIObject;
    [SerializeField] private string dialogueUIScriptTypeName = "DialogueUI";

    [Header("Per-Island Mapping")]
    [SerializeField] private IslandMapping[] islandMappings;

    [Serializable]
    public class IslandMapping
    {
        public string islandId;
        public string startKey;
        public string npcNameOverride;
        public Transform canvasAnchor;
    }

    [Header("NPC naming (when npcNameOverride is empty)")]
    [SerializeField] private string npcAutoPrefix = "Koala_";
    [SerializeField] private bool npcIslandUpper = true;

    [Header("World-Space UI")]
    [SerializeField] private bool facePlayer = false;
    [SerializeField] private int sortingOrder = 140;
    [SerializeField] private Camera worldCameraOverride;

    [Header("Defaults & Logs")]
    [SerializeField] private string fallbackStartKey = "Intro";
    [SerializeField] private bool logVerbose = true;

    [Header("State Source (per Koala)")]
    [Tooltip("OPTIONAL: Drag a DialogueState component here to isolate Koala's progress. If empty, a private one is created at runtime.")]
    [SerializeField] private MonoBehaviour dialogueStateOverride; // expects type named "DialogueState"

    [Header("Scriptable Events")]
    [Tooltip("Name → UnityEvent actions that can be fired from dialogue (onEnter).")]
    [SerializeField] private ScriptableEvent[] scriptableEvents;

    [Tooltip("If listed here, each event will only fire once per play session from this Koala.")]
    [SerializeField] private string[] oneShotEventNames;

    [Header("Auto-activate fallback")]
    [Tooltip("If true, when an onEnter name matches a GameObject, it will be activated (even if inactive) and its parents too.")]
    [SerializeField] private bool tryAutoActivateByName = true;

    [Header("Continue Safety")]
    [Tooltip("Prevents double-advance when SPACE is seen by multiple listeners.")]
    [SerializeField] private float continueCooldownSeconds = 0.12f;

    [Header("Input Leak Protection")]
    [Tooltip("Swallow Space/Submit for this long after a conversation BEGINS.")]
    [SerializeField] private float suppressOnBeginSeconds = 0.25f;

    [Tooltip("Swallow Space/Submit for this long after a conversation ENDS/Picker opens.")]
    [SerializeField] private float suppressOnEndSeconds = 0.25f;

    [Tooltip("Block re-starting this dialogue for this long after End() (prevents bounce-back).")]
    [SerializeField] private float noRestartWindowSeconds = 0.35f;

    private HashSet<string> _oneShotsDone;
    private readonly HashSet<string> _registeredEventNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // ---- Reflection caches ----
    private ScriptableObject _tree;
    private Type _treeType;
    private MethodInfo _treeResetCallbacks;
    private MethodInfo _treeSetUpDict;
    private MethodInfo _treeGetNextUnit;
    private MethodInfo _treeCallScriptAction;
    private MethodInfo _treeGoToState;
    private FieldInfo _treeStateField;      // DialogueState dialogueState (FIELD)
    private FieldInfo _treeNpcNameField;    // string npcName

    // callbacks
    private FieldInfo _treeContinueCb;      // Action continueCallback
    private FieldInfo _treeEndCb;           // Action endDialogueCallback

    // ---- runtime option-continue support ----
    private MethodInfo _treeContinueFromOption; // ContinueFromOption(...)
    private object _lastSelectedOptionObj;
    private int _lastSelectedOptionIndex = -1;
    private Coroutine _coAfterOption;

    private Type _stateType;                // DialogueState
    private MethodInfo _stateSetState;      // SetState(string,string)
    private FieldInfo _stateDictField;      // Dictionary<string,string> stateDict
    private MethodInfo _stateEnsureInit;    // EnsureInitialized()

    // UI
    [SerializeField, HideInInspector] private MonoBehaviour dialogueUIScript;
    private MethodInfo _uiBind, _uiContinue, _uiEnd;

    private bool _talking;

    // ---- safety guards ----
    private bool _ending;
    private float _continueLockedUntilUnscaled = 0f;
    private int _continueRequestedFrame = -999;
    private float _noRestartUntilUnscaled = 0f;

    // ---- current unit + option reflection ----
    private object _currentUnit;
    private FieldInfo _unitOptionsField;   // DialogueUnit.options
    private FieldInfo _optActionField;     // DialogueOption.actionToTrigger

    // ---- NEW: picker open timing ----
    private Coroutine _openPickerCo;

    public bool IsTalking => _talking && !_ending;

    private void OnValidate() => TryAutoWireDialogueUI();

    private void Awake()
    {
        Debug.Log("[KoalaDialogue] Awake() running", this);

        if (!dialogueTreeAsset)
        {
            Debug.LogError("[KoalaDialogue] DialogueTreeObjects asset missing.", this);
            enabled = false;
            return;
        }

        TryAutoWireDialogueUI();
        CacheUIMethods();

        _tree = Instantiate(dialogueTreeAsset);
        _treeType = _tree.GetType();
        BindTreeReflection();

        _treeResetCallbacks?.Invoke(_tree, null);
        _treeSetUpDict?.Invoke(_tree, null);

        RegisterKoalaScriptableEvents();

        // Continue callback (tree asks to continue)
        if (_treeContinueCb != null)
        {
            Action a = RequestContinue;
            _treeContinueCb.SetValue(_tree, a);
        }

        // End callback (tree ends)
        if (_treeEndCb != null)
        {
            Action endA = () =>
            {
                _uiEnd?.Invoke(dialogueUIScript, null);
                HandleEndDialogue();
            };
            _treeEndCb.SetValue(_tree, endA);
        }
    }

    private void OnDisable() => UIInputFocus.Pop(this);
    private void OnDestroy() => UIInputFocus.Pop(this);

    // ===================== OPTIONS (CALLED BY DialogueOptionAction) =====================
    public void ChooseOptionByIndex(int optionIndex)
    {
        if (!_talking || _ending) return;
        if (_currentUnit == null)
        {
            Debug.LogWarning("[KoalaDialogue] ChooseOptionByIndex called but _currentUnit is null.");
            return;
        }

        // Cache field for "options"
        if (_unitOptionsField == null)
        {
            var unitType = _currentUnit.GetType();
            _unitOptionsField = unitType.GetField("options", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (_unitOptionsField == null)
        {
            Debug.LogWarning("[KoalaDialogue] DialogueUnit has no field named 'options'.");
            return;
        }

        var optionsArray = _unitOptionsField.GetValue(_currentUnit) as System.Array;
        if (optionsArray == null || optionsArray.Length == 0)
        {
            Debug.LogWarning("[KoalaDialogue] Current unit has no options.");
            return;
        }

        if (optionIndex < 0 || optionIndex >= optionsArray.Length)
        {
            Debug.LogWarning($"[KoalaDialogue] optionIndex {optionIndex} out of range (len={optionsArray.Length}).");
            return;
        }

        var opt = optionsArray.GetValue(optionIndex);
        if (opt == null)
        {
            Debug.LogWarning("[KoalaDialogue] Selected option object is null.");
            return;
        }

        // Cache field for "actionToTrigger"
        if (_optActionField == null)
        {
            var optType = opt.GetType();
            _optActionField = optType.GetField("actionToTrigger", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (_optActionField == null)
        {
            Debug.LogWarning("[KoalaDialogue] DialogueOption has no field named 'actionToTrigger'.");
            return;
        }

        var evt = _optActionField.GetValue(opt) as UnityEngine.Events.UnityEvent;
        if (evt == null)
        {
            Debug.LogWarning("[KoalaDialogue] actionToTrigger is null.");
            return;
        }

        // Store selection so we can advance runtime tree correctly
        _lastSelectedOptionIndex = optionIndex;
        _lastSelectedOptionObj = opt;

        ExploreHintUI.I?.HideDialogueOptionsHint();

        // Swallow Space so it can't leak
        UIInputFocus.SuppressForSeconds(Mathf.Max(0.05f, continueCooldownSeconds));

        if (logVerbose)
            Debug.Log($"[KoalaDialogue] Option chosen index={optionIndex} -> invoking UnityEvent, then advancing runtime tree", this);

        // 1) Invoke authored UnityEvent (GoToState/AddToState/etc.)
        try { evt.Invoke(); }
        catch (Exception e) { Debug.LogError($"[KoalaDialogue] Option UnityEvent threw: {e}", this); }

        // 2) Advance dialogue on the RUNTIME _tree
        if (_coAfterOption != null) StopCoroutine(_coAfterOption);
        _coAfterOption = StartCoroutine(CoAdvanceAfterOption());
    }

    private IEnumerator CoAdvanceAfterOption()
    {
        // Let GoToState/AddToState apply
        yield return null;

        if (!_talking || _ending) yield break;

        bool invoked = TryInvokeContinueFromOptionOnRuntimeTree();

        // Some dialogue systems don't immediately expose next unit until another frame
        yield return null;

        if (!_talking || _ending) yield break;

        // Always pull next from the runtime tree to ensure UI updates
        var next = _treeGetNextUnit?.Invoke(_tree, null);
        if (logVerbose)
            Debug.Log($"[KoalaDialogue] AfterOption advance (invoked={invoked}) -> next={(next == null ? "NULL" : next.GetType().Name)}", this);

        Handle(next);
    }

    private bool TryInvokeContinueFromOptionOnRuntimeTree()
    {
        if (_treeContinueFromOption == null) return false;

        try
        {
            var ps = _treeContinueFromOption.GetParameters();

            // ContinueFromOption()
            if (ps.Length == 0)
            {
                _treeContinueFromOption.Invoke(_tree, null);
                return true;
            }

            // ContinueFromOption(int)
            if (ps.Length == 1 && ps[0].ParameterType == typeof(int))
            {
                _treeContinueFromOption.Invoke(_tree, new object[] { _lastSelectedOptionIndex });
                return true;
            }

            // ContinueFromOption(object) / ContinueFromOption(DialogueOption)
            if (ps.Length == 1)
            {
                object arg = null;

                if (_lastSelectedOptionObj != null && ps[0].ParameterType.IsInstanceOfType(_lastSelectedOptionObj))
                    arg = _lastSelectedOptionObj;
                else if (ps[0].ParameterType == typeof(object))
                    arg = _lastSelectedOptionObj;

                if (arg != null)
                {
                    _treeContinueFromOption.Invoke(_tree, new object[] { arg });
                    return true;
                }
            }

            Debug.LogWarning($"[KoalaDialogue] ContinueFromOption has unsupported signature: ({string.Join(", ", ps.Select(p => p.ParameterType.Name))})", this);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[KoalaDialogue] Exception invoking ContinueFromOption on runtime tree: {e.Message}", this);
        }

        return false;
    }

    // ===================== Public API =====================
    public void StartAfterMoxo()
    {
        var islandId = GetCurrentIslandId();
        var key = GetStartKeyForIsland(islandId, out var anchor);

        if (logVerbose)
            Debug.Log($"[KoalaDialogue] StartAfterMoxo → islandId='{islandId}', key='{key}', anchor={(anchor ? anchor.name : "null")}", this);

        StartFromKey(key, anchor, islandId);
    }

    public void OnInteract(object _ignoredInteractor = null)
    {
        var islandId = GetCurrentIslandId();
        var key = GetStartKeyForIsland(islandId, out var anchor);
        StartFromKey(key, anchor, islandId);
    }

    public void StartFromKey(string key, Transform anchorOverride = null, string islandIdForNpc = null)
    {
        if (_talking || Time.unscaledTime < _noRestartUntilUnscaled) return;

        if (GameManager.Instance)
        {
            var s = GameManager.Instance.State;
            if (s != GameManager.GameState.Explore && s != GameManager.GameState.Narrative)
                return;
        }

        // Ensure island picker is closed when dialogue starts (only visible during Explore).
        var picker = IslandSelectionUI.I ?? FindObjectOfType<IslandSelectionUI>(true);
        if (picker != null && picker.IsOpen)
            picker.Hide();

        _ending = false;
        _continueLockedUntilUnscaled = 0f;
        _continueRequestedFrame = -999;

        key = string.IsNullOrWhiteSpace(key) ? fallbackStartKey : key;

        _uiEnd?.Invoke(dialogueUIScript, null);

        var npcNameForIsland = GetNpcNameForIsland(islandIdForNpc);
        if (_treeNpcNameField != null && !string.IsNullOrWhiteSpace(npcNameForIsland))
        {
            _treeNpcNameField.SetValue(_tree, npcNameForIsland);
            if (logVerbose) Debug.Log($"[KoalaDialogue] Using npcName='{npcNameForIsland}' for island '{islandIdForNpc}'.", this);
        }

        string islandId = GetCurrentIslandId();
        string phase = "Narrative"; // or "" if you prefer

        MoxoCPT.LoggingDialogueChoices.BeginConversation(npcNameForIsland, islandId, phase);


        var stateObj = _treeStateField?.GetValue(_tree);
        if (stateObj == null)
        {
            if (dialogueStateOverride == null)
            {
                dialogueStateOverride = GetComponentsInChildren<MonoBehaviour>(true)
                    .FirstOrDefault(mb => mb && mb.GetType().Name == "DialogueState");

                if (dialogueStateOverride == null)
                {
                    var dsType = FindTypeAnywhere("Dialogue.DialogueState") ?? FindTypeAnywhere("DialogueState");
                    if (dsType == null)
                    {
                        Debug.LogError("[KoalaDialogue] Could not locate DialogueState type. Add it to the project.", this);
                        return;
                    }

                    var go = new GameObject($"KoalaDialogueState_{gameObject.name}");
                    go.transform.SetParent(transform, false);
                    dialogueStateOverride = (MonoBehaviour)go.AddComponent(dsType);
                    if (logVerbose) Debug.Log($"[KoalaDialogue] Created private DialogueState: {go.name}", this);
                }
            }

            _treeStateField?.SetValue(_tree, dialogueStateOverride);
            stateObj = dialogueStateOverride;

            var setUp = _treeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "SetUpDialogueState" && m.GetParameters().Length == 1);
            setUp?.Invoke(_tree, new object[] { stateObj });

            if (logVerbose) Debug.Log("[KoalaDialogue] Injected DialogueState into DialogueTree (FIELD).", this);
        }

        if (_stateEnsureInit != null) _stateEnsureInit.Invoke(stateObj, null);
        else EnsureStateDictionary(stateObj);

        PrepareCanvas(anchorOverride);

        var npcName = (_treeNpcNameField?.GetValue(_tree) as string) ?? "Koala";

        bool wroteViaSetState = false;
        if (_stateSetState != null)
        {
            _stateSetState.Invoke(stateObj, new object[] { npcName, key });
            wroteViaSetState = true;
        }
        else if (_treeGoToState != null)
        {
            _treeGoToState.Invoke(_tree, new object[] { key });
        }
        else
        {
            TryDirectStateWrite(stateObj, npcName, key);
        }

        if (logVerbose)
            Debug.Log($"[KoalaDialogue] Begin → npc='{npcName}', state='{key}' (setVia={(wroteViaSetState ? "SetState" : "GoToState/Direct")})", this);

        GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);

        _talking = true;
        KoalaAnimBus.BroadcastToAllKoalas(k => k.SetTalking(true));

        UIInputFocus.Push(this);
        UIInputFocus.SuppressForSeconds(Mathf.Max(0.05f, suppressOnBeginSeconds));

        var first = _treeGetNextUnit?.Invoke(_tree, null);
        Handle(first);
    }

    // ---------- Continue ----------
    private void RequestContinue()
    {
        if (!_talking || _ending) return;

        if (_continueRequestedFrame == Time.frameCount) return;
        _continueRequestedFrame = Time.frameCount;

        if (Time.unscaledTime < _continueLockedUntilUnscaled) return;
        _continueLockedUntilUnscaled = Time.unscaledTime + continueCooldownSeconds;

        StartCoroutine(CoContinueNextFrame());
    }

    private IEnumerator CoContinueNextFrame()
    {
        yield return null;
        if (!_talking || _ending) yield break;

        var next = _treeGetNextUnit?.Invoke(_tree, null);
        Handle(next);
    }

    // ---------- Core Flow ----------
    private void Handle(object dialogueUnit)
    {
        _currentUnit = dialogueUnit;

        // Extract requiredStateKey + onEnterEventName via reflection (you already do similar)
        string requiredKey = "";
        string onEnter = "";

        if (dialogueUnit != null)
        {
            var t = dialogueUnit.GetType();
            var fKey = t.GetField("requiredStateKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fKey != null) requiredKey = fKey.GetValue(dialogueUnit) as string;

            // you already have GetOnEnterEventName(dialogueUnit)
            onEnter = GetOnEnterEventName(dialogueUnit) ?? "";
        }

        string islandId = GetCurrentIslandId();
        string phase = "Narrative";

        MoxoCPT.LoggingDialogueChoices.SetDialogueContext(GetNpcNameForIsland(islandId), requiredKey, onEnter, islandId, phase);


        if (!_talking || _ending || dialogueUnit == null)
        {
            End(); // normal null-advance end
            return;
        }

        var onEnterName = GetOnEnterEventName(dialogueUnit);
        if (!string.IsNullOrWhiteSpace(onEnterName))
        {
            if (logVerbose) Debug.Log($"[KoalaDialogue] onEnter → '{onEnterName}'", this);

            _treeCallScriptAction?.Invoke(_tree, new object[] { onEnterName });

            if (tryAutoActivateByName)
                TryAutoActivateByName(onEnterName);

            if (!_registeredEventNames.Contains(onEnterName))
                Debug.LogWarning($"[KoalaDialogue] onEnter name '{onEnterName}' not in registered ScriptableEvents. " +
                                 $"Registered: {string.Join(", ", _registeredEventNames)}", this);
        }

        _uiBind?.Invoke(dialogueUIScript, new[] { dialogueUnit });
        _uiContinue?.Invoke(dialogueUIScript, null);

        var opts = GetOptionsArrayFromUnit(dialogueUnit);
        if (opts != null && opts.Length > 0)
            ExploreHintUI.I?.ShowDialogueOptionsHint("You can now use your mouse to look around. Use the arrow keys to select an option, then press Space to confirm.");
        else
            ExploreHintUI.I?.HideDialogueOptionsHint();
    }

    private static System.Array GetOptionsArrayFromUnit(object unit)
    {
        if (unit == null) return null;
        var f = unit.GetType().GetField("options",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(unit) as System.Array;
    }

    private void End()
    {
        if (!_talking || _ending) return;

        _ending = true;

        UIInputFocus.Pop(this);
        UIInputFocus.SuppressForSeconds(Mathf.Max(0.05f, suppressOnEndSeconds));

        _noRestartUntilUnscaled = Time.unscaledTime + Mathf.Max(noRestartWindowSeconds, suppressOnEndSeconds);

        _talking = false;
        _currentUnit = null;

        ExploreHintUI.I?.HideDialogueOptionsHint();

        _uiEnd?.Invoke(dialogueUIScript, null);

        // IMPORTANT: force Explore here
        GameManager.Instance?.ForceUpdateGameState(GameManager.GameState.Explore);
    }

    // Add this field near your other fields:
    private Coroutine _endAndPickerCo;
    private Coroutine _endFlowCo;

    // REPLACE your HandleEndDialogue() with this:
    private void HandleEndDialogue()
    {
        // Stop any existing end flow
        if (_endFlowCo != null)
        {
            StopCoroutine(_endFlowCo);
            _endFlowCo = null;
        }
        KoalaAnimBus.EnsureStandingIdleAll();

        _endFlowCo = StartCoroutine(CoEndAndOpenPicker());
    }
    private IEnumerator CoEndAndOpenPicker()
    {
        // Mark ending immediately so no other dialogue advance runs
        _ending = true;
        _talking = false;
        _currentUnit = null;

        // End UI + release input focus now
        _uiEnd?.Invoke(dialogueUIScript, null);
        UIInputFocus.Pop(this);
        UIInputFocus.SuppressForSeconds(Mathf.Max(0.05f, suppressOnEndSeconds));


        // Mark island complete (your current behavior)
        var id = GetCurrentIslandId();
        IslandProgress.I?.MarkCompletedById(id);

        if (logVerbose)
            Debug.Log($"[KoalaDialogue] Ended dialogue on island '{id}'. Marked completed. Opening island picker… (state={GameManager.Instance?.State})", this);
        
        // Wait a couple frames so dialogue/UI input settles
        yield return null;
        yield return null;

        // FORCE state to Explore (picker should only be visible in Explore)
        if (GameManager.Instance != null)
            GameManager.Instance.ForceUpdateGameState(GameManager.GameState.Explore);

        // Wait until state is actually Explore (or timeout)
        float timeout = Time.unscaledTime + 1.0f;
        while (GameManager.Instance != null &&
               GameManager.Instance.State != GameManager.GameState.Explore &&
               Time.unscaledTime < timeout)
        {
            yield return null;
        }

        // Now position the picker in world and show the UI
        IslandTravelManager.I?.PlacePickerForIsland(id);

        var picker = IslandSelectionUI.I ?? FindObjectOfType<IslandSelectionUI>(true);
        if (picker != null) picker.ShowRemaining();
        else Debug.LogWarning("[KoalaDialogue] IslandSelectionUI not found. Skipping picker display.", this);

        _endFlowCo = null;
    }

    private IEnumerator CoOpenPickerNextFrame()
    {
        // Wait one frame so the state change settles
        yield return null;

        // Hard-enforce Explore (in case something flips it back)
        GameManager.Instance?.ForceUpdateGameState(GameManager.GameState.Explore);

        // If your GameManager updates State asynchronously, wait briefly
        float timeout = Time.unscaledTime + 1.0f;
        while (GameManager.Instance != null &&
               GameManager.Instance.State != GameManager.GameState.Explore &&
               Time.unscaledTime < timeout)
        {
            yield return null;
        }

        var id = GetCurrentIslandId();
        IslandProgress.I?.MarkCompletedById(id);

        if (logVerbose)
            Debug.Log($"[KoalaDialogue] Ended dialogue on island '{id}'. Marked completed. Opening island picker… (state={GameManager.Instance?.State})", this);

        UIInputFocus.SuppressForSeconds(Mathf.Max(0.05f, suppressOnEndSeconds));

        IslandTravelManager.I?.PlacePickerForIsland(id);

        var picker = IslandSelectionUI.I ?? FindObjectOfType<IslandSelectionUI>(true);
        if (picker != null) picker.ShowRemaining();
        else Debug.LogWarning("[KoalaDialogue] IslandSelectionUI not found. Skipping picker display.", this);

        _openPickerCo = null;
    }

    // ---------- Helpers ----------
    private string GetCurrentIslandId()
    {
        var island = IslandTravelManager.I ? IslandTravelManager.I.CurrentIsland : null;
        return island?.islandId?.Trim() ?? "";
    }

    private string GetStartKeyForIsland(string islandId, out Transform anchor)
    {
        anchor = null;
        if (string.IsNullOrWhiteSpace(islandId)) return fallbackStartKey;

        var map = islandMappings?.FirstOrDefault(m =>
            !string.IsNullOrWhiteSpace(m.islandId) &&
            string.Equals(m.islandId.Trim(), islandId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (map != null)
        {
            anchor = map.canvasAnchor;
            return string.IsNullOrWhiteSpace(map.startKey) ? fallbackStartKey : map.startKey.Trim();
        }

        if (logVerbose)
            Debug.LogWarning($"[KoalaDialogue] No mapping for island '{islandId}'. Using fallback '{fallbackStartKey}'.", this);

        return fallbackStartKey;
    }

    private string GetNpcNameForIsland(string islandId)
    {
        if (string.IsNullOrWhiteSpace(islandId))
            return (_treeNpcNameField?.GetValue(_tree) as string) ?? "Koala";

        var map = islandMappings?.FirstOrDefault(m =>
            !string.IsNullOrWhiteSpace(m.islandId) &&
            string.Equals(m.islandId.Trim(), islandId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (map != null && !string.IsNullOrWhiteSpace(map.npcNameOverride))
            return map.npcNameOverride.Trim();

        var suffix = npcIslandUpper ? islandId.Trim().ToUpperInvariant() : islandId.Trim();
        return $"{npcAutoPrefix}{suffix}";
    }

    private void PrepareCanvas(Transform anchor)
    {
        if (!dialogueUIObject) return;
        var canvas = dialogueUIObject.GetComponentInChildren<Canvas>(true);
        if (!canvas) return;

        if (anchor)
        {
            var t = canvas.transform;
            t.position = anchor.position;
            t.rotation = anchor.rotation;

            if (facePlayer)
            {
                var look = GetPlayerRootPos();
                t.LookAt(look, Vector3.up);
                t.Rotate(0f, 180f, 0f, Space.Self);
            }
        }

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            var cam = worldCameraOverride ?? GetActiveCamera();
            if (cam) canvas.worldCamera = cam;
            if (canvas.sortingOrder < sortingOrder) canvas.sortingOrder = sortingOrder;

            var cg = canvas.GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
        }

        if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
    }

    private static Camera GetActiveCamera()
    {
        var main = Camera.main;
        if (main) return main;
        var cams = GameObject.FindObjectsOfType<Camera>(true);
        return cams.FirstOrDefault(c => c.isActiveAndEnabled) ?? cams.FirstOrDefault();
    }

    private static Vector3 GetPlayerRootPos()
    {
        var pm = GameObject.FindObjectOfType<PlayerModeManager>(true);
        if (pm && pm.CurrentPlayerRoot) return pm.CurrentPlayerRoot.position;
        var main = Camera.main;
        return main ? main.transform.position : Vector3.zero;
    }

    private void EnsureStateDictionary(object stateObj)
    {
        if (stateObj == null || _stateType == null) return;
        if (_stateDictField == null)
            _stateDictField = _stateType.GetField("stateDict", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_stateDictField != null && _stateDictField.GetValue(stateObj) == null)
        {
            _stateDictField.SetValue(stateObj, new Dictionary<string, string>());
            if (logVerbose) Debug.Log("[KoalaDialogue] Initialized DialogueState.stateDict.", this);
        }
    }

    private void TryDirectStateWrite(object stateObj, string npc, string key)
    {
        if (stateObj == null || _stateDictField == null) return;
        var dict = _stateDictField.GetValue(stateObj);
        if (dict == null) return;

        var tryAdd = dict.GetType().GetMethod("set_Item");
        if (tryAdd != null) tryAdd.Invoke(dict, new object[] { npc, key });
    }

    // ---------- Reflection wiring ----------
    private void BindTreeReflection()
    {
        if (_treeType == null)
        {
            Debug.LogError("[KoalaDialogue] Cannot reflect DialogueTreeObjects type.");
            return;
        }

        _treeResetCallbacks = _treeType.GetMethod("ResetCallbacks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeSetUpDict = _treeType.GetMethod("SetUpDialogueUnitsDict", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeGetNextUnit = _treeType.GetMethod("GetNextDialogueUnit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeCallScriptAction = _treeType.GetMethod("CallScriptableAction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeGoToState = _treeType.GetMethod("GoToState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);

        _treeStateField = _treeType.GetField("dialogueState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeNpcNameField = _treeType.GetField("npcName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        _treeContinueCb = _treeType.GetField("continueCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeEndCb = _treeType.GetField("endDialogueCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // --- Find ContinueFromOption with ANY signature ---
        _treeContinueFromOption = _treeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => string.Equals(m.Name, "ContinueFromOption", StringComparison.OrdinalIgnoreCase));

        if (logVerbose)
        {
            if (_treeContinueFromOption != null)
                Debug.Log($"[KoalaDialogue] Found ContinueFromOption signature: ({string.Join(", ", _treeContinueFromOption.GetParameters().Select(p => p.ParameterType.Name))})", this);
            else
                Debug.LogWarning("[KoalaDialogue] ContinueFromOption NOT found on runtime tree. Will still attempt GetNextDialogueUnit after option.", this);
        }

        _stateType = FindTypeAnywhere("Dialogue.DialogueState") ?? FindTypeAnywhere("DialogueState");
        if (_stateType != null)
        {
            _stateSetState = _stateType.GetMethod("SetState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(string) }, null);
            _stateEnsureInit = _stateType.GetMethod("EnsureInitialized", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _stateDictField = _stateType.GetField("stateDict", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }

    private void TryAutoWireDialogueUI()
    {
        dialogueUIScript = null;
        if (!dialogueUIObject) return;

        var exactType = FindTypeAnywhere(dialogueUIScriptTypeName);
        if (exactType != null)
            dialogueUIScript = (MonoBehaviour)dialogueUIObject.GetComponentInChildren(exactType, true);

        if (!dialogueUIScript)
        {
            var candidates = dialogueUIObject.GetComponentsInChildren<MonoBehaviour>(true);
            dialogueUIScript = candidates.FirstOrDefault(mb =>
            {
                var t = mb.GetType();
                return t.GetMethod("BindDialogueUnit") != null &&
                       t.GetMethod("ContinueDialogue") != null &&
                       t.GetMethod("EndDialogue") != null;
            });
        }

        if (dialogueUIScript)
            Debug.Log($"[KoalaDialogue] UI bound → {dialogueUIScript.GetType().Name} on '{dialogueUIScript.gameObject.name}'", this);
        else
            Debug.LogWarning("[KoalaDialogue] Couldn’t find a Dialogue UI script on the dropped object.");

        CacheUIMethods();
    }

    private void CacheUIMethods()
    {
        _uiBind = _uiContinue = _uiEnd = null;
        if (!dialogueUIScript) return;
        var uiType = dialogueUIScript.GetType();
        _uiBind = uiType.GetMethod("BindDialogueUnit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _uiContinue = uiType.GetMethod("ContinueDialogue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _uiEnd = uiType.GetMethod("EndDialogue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                t = asm.GetTypes().FirstOrDefault(tp => tp.Name == shortName);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }

    // ===== Register ScriptableEvent[] so dialogue can invoke them by name =====
    private void RegisterKoalaScriptableEvents()
    {
        _oneShotsDone = new HashSet<string>(
            (oneShotEventNames ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim()),
            StringComparer.OrdinalIgnoreCase
        );

        _registeredEventNames.Clear();

        if (scriptableEvents == null || scriptableEvents.Length == 0) return;

        var reg = _treeType.GetMethod("RegisterScriptableCallback",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (reg == null)
        {
            Debug.LogWarning("[KoalaDialogue] DialogueTreeObjects.RegisterScriptableCallback not found.");
            return;
        }

        foreach (var se in scriptableEvents)
        {
            if (se == null) continue;
            var name = (se.eventName ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("[KoalaDialogue] ScriptableEvent has empty eventName.", this);
                continue;
            }

            var evt = se.unityEvent;
            Action wrapper = () =>
            {
                if (_oneShotsDone.Contains(name))
                {
                    if (logVerbose) Debug.Log($"[KoalaDialogue] (one-shot) '{name}' already fired; skipping.", this);
                    return;
                }

                if (logVerbose) Debug.Log($"[KoalaDialogue] Invoking scriptable event '{name}'.", this);
                try { evt?.Invoke(); }
                catch (Exception e) { Debug.LogWarning($"[KoalaDialogue] Exception invoking '{name}': {e.Message}", this); }

                if (oneShotEventNames != null &&
                    oneShotEventNames.Any(s => string.Equals(s?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    _oneShotsDone.Add(name);
                }
            };

            reg.Invoke(_tree, new object[] { name, wrapper });
            _registeredEventNames.Add(name);

            if (logVerbose)
                Debug.Log($"[KoalaDialogue] Registered scriptable event '{name}'.", this);
        }
    }

    // ===== Robust on-enter name resolver =====
    private string GetOnEnterEventName(object dialogueUnit)
    {
        if (dialogueUnit == null) return null;
        var t = dialogueUnit.GetType();

        string[] candidates =
        {
            "onEnterEventName",
            "onEnterEvent",
            "onEnter",
            "scriptableEventName",
            "scriptableCallbackName"
        };

        foreach (var name in candidates)
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (f != null && f.FieldType == typeof(string))
            {
                var val = f.GetValue(dialogueUnit) as string;
                if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
            }
        }

        foreach (var name in candidates)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (p != null && p.PropertyType == typeof(string))
            {
                var val = p.GetValue(dialogueUnit, null) as string;
                if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
            }
        }

        var anyStringField = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(ff => ff.FieldType == typeof(string) &&
                                  (ff.Name.ToLowerInvariant().Contains("enter") ||
                                   ff.Name.ToLowerInvariant().Contains("event")));
        if (anyStringField != null)
        {
            var val = anyStringField.GetValue(dialogueUnit) as string;
            if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
        }

        return null;
    }

    // ===== Fallback: activate object (and parents) by name, even if inactive =====
    private void TryAutoActivateByName(string name)
    {
        var go = FindAnyByName(name);
        if (!go)
        {
            Debug.LogWarning($"[KoalaDialogue] Fallback couldn’t find a GameObject named '{name}' (including inactive).", this);
            return;
        }

        ActivateWithParents(go);
        Debug.Log($"[KoalaDialogue] Fallback activated '{go.name}' (and any inactive parents).", this);
    }

    private static GameObject FindAnyByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var active = GameObject.Find(name);
        if (active) return active;

        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t && t.name == name)
            {
                if (!t.gameObject.scene.IsValid()) continue;
                return t.gameObject;
            }
        }
        return null;
    }

    private static void ActivateWithParents(GameObject go)
    {
        if (!go) return;

        var stack = new Stack<Transform>();
        var p = go.transform;
        while (p != null)
        {
            stack.Push(p);
            p = p.parent;
        }
        while (stack.Count > 0)
        {
            var tr = stack.Pop();
            if (!tr.gameObject.activeSelf)
                tr.gameObject.SetActive(true);
        }
    }
}
