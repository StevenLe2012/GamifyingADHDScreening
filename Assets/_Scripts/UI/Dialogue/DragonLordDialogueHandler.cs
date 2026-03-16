using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Canvas

/// <summary>
/// Minimal, self-cleaning dialogue handler for the Dragon Lord (non-MOXO island),
/// with Scriptable "onEnter" events (UnityEvents and GameObject toggles).
/// UPDATED: reliable option selection + blocks auto-continue when options are present.
/// </summary>
public class DragonLordDialogueHandler : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("DialogueTreeObjects asset for Dragon Lord.")]
    [SerializeField] private ScriptableObject dialogueTreeAsset; // Dialogue.DialogueTreeObjects
    [Tooltip("The Dialogue UI GameObject that has a component with BindDialogueUnit/ContinueDialogue/EndDialogue.")]
    [SerializeField] private GameObject dialogueUIObject;
    [Tooltip("If you know the exact script name on the UI object, put it here (e.g., DialogueUI). Otherwise leave blank and it will auto-detect.")]
    [SerializeField] private string dialogueUIScriptTypeName = "DialogueUI";

    [Header("Dragon Lord Setup")]
    [Tooltip("NPC name stored into the dialogue tree (if your tree expects a name).")]
    [SerializeField] private string npcName = "DragonLord";
    [Tooltip("Dialogue key to start from, e.g. 'Intro'.")]
    [SerializeField] private string startKey = "Intro";

    [Header("World-Space UI (optional)")]
    [SerializeField] private Transform canvasAnchor;
    [SerializeField] private bool facePlayer = false;
    [SerializeField] private int sortingOrder = 200;
    [SerializeField] private Camera worldCameraOverride;

    [Header("Logs")]
    [SerializeField] private bool logVerbose = true;

    [Header("Optional: Inject a DialogueState (else auto-create one)")]
    [SerializeField] private MonoBehaviour dialogueStateOverride; // expects type named "DialogueState"

    // -------- Scriptable on-enter events ----------
    [Header("Scriptable Events")]
    [Tooltip("Name → UnityEvent mappings that will fire when a dialogue line's onEnterEventName matches.")]
    [SerializeField] private Helpers.ScriptableEvent[] scriptableEvents;

    [Serializable]
    public class NamedActivation
    {
        [Tooltip("Event name to listen for (must match DialogueUnit.onEnterEventName).")]
        public string eventName;
        [Tooltip("Object to toggle when the event fires.")]
        public GameObject target;
        [Tooltip("Value passed to SetActive(...) when the event fires.")]
        public bool active = true;
    }

    [Tooltip("Simple GameObject toggles that fire on matching event names (e.g., 'MagicGate').")]
    [SerializeField] private NamedActivation[] activationEvents;

    // ---- Reflection caches ----
    private ScriptableObject _tree;
    private Type _treeType;
    private MethodInfo _treeResetCallbacks;
    private MethodInfo _treeSetUpDict;
    private MethodInfo _treeGetNextUnit;
    private MethodInfo _treeCallScriptAction;
    private MethodInfo _treeGoToState;
    private MethodInfo _treeSetUpState;      // SetUpDialogueState(DialogueState)
    private MethodInfo _treeRegisterScriptCb;// RegisterScriptableCallback(string,Action)
    private FieldInfo  _treeStateField;      // public DialogueState dialogueState
    private FieldInfo  _treeNpcNameField;    // public string npcName

    // callbacks + Continue()
    private FieldInfo _treeContinueCb;       // public Action continueCallback
    private FieldInfo _treeEndCb;            // public Action endDialogueCallback
    private MethodInfo _treeContinue;        // public void Continue()

    // NEW: ContinueFromOption(...)
    private MethodInfo _treeContinueFromOption;

    // DialogueState bits
    private Type _stateType;
    private MethodInfo _stateSetState;      // void SetState(string npc, string key)
    private MethodInfo _stateEnsureInit;    // EnsureInitialized()
    private FieldInfo  _stateDictField;     // Dictionary<string,string> stateDict

    // UI script hooks
    [SerializeField, HideInInspector] private MonoBehaviour dialogueUIScript;
    private MethodInfo _uiBind, _uiContinue, _uiEnd;
    private MethodInfo _uiStopAutoAdvance;  // optional: StopAutoAdvance()

    private bool _talking;
    public bool IsTalking => _talking;

    private Coroutine _advanceCo;

    // NEW: option reliability
    private object _currentUnit;
    private bool _optionAdvanceInFlight;
    private int _lastOptionFrame = -999;

    private void OnValidate() => TryAutoWireDialogueUI();

    private void Awake()
    {
        if (!dialogueTreeAsset)
        {
            Debug.LogError("[DragonLordDialogue] DialogueTreeObjects asset missing.", this);
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

        // Wire tree callbacks so Continue()/End close properly
        if (_treeContinueCb != null)
        {
            Action a = () =>
            {
                // ✅ If the CURRENT unit has options, do NOT auto-continue.
                if (_optionAdvanceInFlight) return;
                if (CurrentUnitHasOptions()) return;

                if (_advanceCo != null) StopCoroutine(_advanceCo);
                _advanceCo = StartCoroutine(CoContinueNextFrame());
            };
            _treeContinueCb.SetValue(_tree, a);
        }

        if (_treeEndCb != null)
        {
            Action endA = () =>
            {
                _uiEnd?.Invoke(dialogueUIScript, null);  // close UI
                HandleEndDialogue();
            };
            _treeEndCb.SetValue(_tree, endA);
        }

        // Register scriptable onEnter events (UnityEvents + GameObject toggles)
        RegisterScriptableEvents();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    // ---------- Scriptable event registration ----------
    private void RegisterScriptableEvents()
    {
        if (_treeRegisterScriptCb == null) return;

        var composed = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        // 1) UnityEvent mappings
        if (scriptableEvents != null)
        {
            foreach (var se in scriptableEvents)
            {
                if (se == null || string.IsNullOrWhiteSpace(se.eventName)) continue;
                void InvokeUnityEvent()
                {
                    if (logVerbose) Debug.Log($"[DragonLordDialogue] ScriptableEvent → {se.eventName}", this);
                    try { se.unityEvent?.Invoke(); } catch (Exception e) { Debug.LogWarning(e.Message, this); }
                }

                if (composed.TryGetValue(se.eventName, out var existing))
                    composed[se.eventName] = existing + InvokeUnityEvent;
                else
                    composed[se.eventName] = InvokeUnityEvent;
            }
        }

        // 2) Simple GameObject toggles
        if (activationEvents != null)
        {
            foreach (var a in activationEvents)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.eventName) || a.target == null) continue;
                void ToggleTarget()
                {
                    if (logVerbose) Debug.Log($"[DragonLordDialogue] ActivationEvent → {a.eventName} setActive({a.active}) on '{a.target.name}'", this);
                    a.target.SetActive(a.active);
                }

                if (composed.TryGetValue(a.eventName, out var existing))
                    composed[a.eventName] = existing + ToggleTarget;
                else
                    composed[a.eventName] = ToggleTarget;
            }
        }

        foreach (var kv in composed)
        {
            try { _treeRegisterScriptCb.Invoke(_tree, new object[] { kv.Key, kv.Value }); }
            catch (Exception e) { Debug.LogWarning($"[DragonLordDialogue] Failed to register '{kv.Key}': {e.Message}", this); }
        }
    }

    // ---------- Public entries ----------
    // ---------- Public entries ----------
    public void StartAfterIntro()
    {
        if (logVerbose) Debug.Log("[DragonLordDialogue] StartAfterIntro() called.", this);

        // ✅ Start next frame so Awake + reflection caches are guaranteed ready
        StartCoroutine(CoStartDialogueNextFrame());
    }

    private IEnumerator CoStartDialogueNextFrame()
    {
        yield return null;

        if (logVerbose) Debug.Log("[DragonLordDialogue] CoStartDialogueNextFrame() firing.", this);
        StartDialogue();
    }

    public void StartDialogue()
    {
        if (logVerbose) Debug.Log("[DragonLordDialogue] StartDialogue() called.", this);
        StartFromKey(startKey, canvasAnchor);
    }

    public void StartTalking() => StartDialogue();
    public void End() { if (_talking) HandleEndDialogue(); }
    /// <summary>
    /// ✅ Called by DialogueOptionAction when DragonLord is talking.
    /// This invokes the option UnityEvent and advances the tree via ContinueFromOption (if available) + GetNextDialogueUnit.
    /// </summary>
    public void SelectOption(int optionIndex)
    {
        if (!_talking) return;
        if (_optionAdvanceInFlight) return;

        if (Time.frameCount == _lastOptionFrame) return;
        _lastOptionFrame = Time.frameCount;

        if (_currentUnit == null)
        {
            if (logVerbose) Debug.LogWarning("[DragonLordDialogue] SelectOption: no current unit.", this);
            return;
        }

        var optionsArr = GetOptionsArray(_currentUnit);
        if (optionsArr == null || optionsArr.Length == 0)
        {
            if (logVerbose) Debug.LogWarning("[DragonLordDialogue] SelectOption: current unit has no options.", this);
            return;
        }

        if (optionIndex < 0 || optionIndex >= optionsArr.Length)
        {
            if (logVerbose) Debug.LogWarning($"[DragonLordDialogue] SelectOption: index {optionIndex} out of range (0..{optionsArr.Length - 1}).", this);
            return;
        }

        var optionObj = optionsArr.GetValue(optionIndex);
        if (logVerbose) Debug.Log($"[DragonLordDialogue] SelectOption({optionIndex}) -> invoke UnityEvent then advance.", this);

        ExploreHintUI.I?.HideDialogueOptionsHint();

        _optionAdvanceInFlight = true;

        // stop any scheduled auto-advances in UI if available
        _uiStopAutoAdvance?.Invoke(dialogueUIScript, null);

        // 1) invoke option.actionToTrigger.Invoke()
        TryInvokeOptionAction(optionObj);

        // 2) advance safely
        StartCoroutine(CoAdvanceAfterOption(optionIndex, optionObj));
    }

    private IEnumerator CoAdvanceAfterOption(int optionIndex, object optionObj)
    {
        yield return null;

        if (!_talking) { _optionAdvanceInFlight = false; yield break; }

        TryInvokeContinueFromOption(optionIndex, optionObj);

        yield return null;

        if (!_talking) { _optionAdvanceInFlight = false; yield break; }

        var next = _treeGetNextUnit?.Invoke(_tree, null);
        _optionAdvanceInFlight = false;
        Handle(next);
    }

    public void StopDialogue()
    {
        if (_advanceCo != null) { StopCoroutine(_advanceCo); _advanceCo = null; }
        _talking = false;
        _uiEnd?.Invoke(dialogueUIScript, null);
        GameManager.Instance?.UpdateGameState(GameManager.GameState.Explore);
    }

    public void Cleanup()
    {
        if (_advanceCo != null) { StopCoroutine(_advanceCo); _advanceCo = null; }
        _talking = false;
        _uiEnd?.Invoke(dialogueUIScript, null);

        _currentUnit = null;
        _optionAdvanceInFlight = false;
        _lastOptionFrame = -999;

        try
        {
            _treeResetCallbacks?.Invoke(_tree, null);

            // Rewire our continue/end for next start
            if (_treeContinueCb != null)
            {
                Action a = () =>
                {
                    if (_optionAdvanceInFlight) return;
                    if (CurrentUnitHasOptions()) return;

                    if (_advanceCo != null) StopCoroutine(_advanceCo);
                    _advanceCo = StartCoroutine(CoContinueNextFrame());
                };
                _treeContinueCb.SetValue(_tree, a);
            }

            if (_treeEndCb != null)
            {
                Action endA = () =>
                {
                    _uiEnd?.Invoke(dialogueUIScript, null);
                    HandleEndDialogue();
                };
                _treeEndCb.SetValue(_tree, endA);
            }

            RegisterScriptableEvents();
        }
        catch { /* best-effort */ }

        GameManager.Instance?.UpdateGameState(GameManager.GameState.Explore);
    }

    // ---------- Start ----------
    public void StartFromKey(string key, Transform anchorOverride = null)
    {
        if (_talking || _treeType == null) return;
        key = string.IsNullOrWhiteSpace(key) ? "Intro" : key;

        _uiEnd?.Invoke(dialogueUIScript, null);

        _currentUnit = null;
        _optionAdvanceInFlight = false;
        _lastOptionFrame = -999;

        if (_treeNpcNameField != null && !string.IsNullOrWhiteSpace(npcName))
            _treeNpcNameField.SetValue(_tree, npcName);

        string islandId = IslandTravelManager.I && IslandTravelManager.I.CurrentIsland != null ? IslandTravelManager.I.CurrentIsland.islandId : "";
        MoxoCPT.LoggingDialogueChoices.BeginConversation(npcName, islandId, "Narrative");


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
                        Debug.LogError("[DragonLordDialogue] Could not locate DialogueState type. Add it to the project.", this);
                        return;
                    }
                    var go = new GameObject($"DragonLordDialogueState_{gameObject.name}");
                    go.transform.SetParent(transform, false);
                    dialogueStateOverride = (MonoBehaviour)go.AddComponent(dsType);
                    if (logVerbose) Debug.Log($"[DragonLordDialogue] Created private DialogueState: {go.name}", this);
                }
            }

            _treeStateField?.SetValue(_tree, dialogueStateOverride);
            stateObj = dialogueStateOverride;

            if (_treeSetUpState != null)
                _treeSetUpState.Invoke(_tree, new object[] { stateObj });

            if (logVerbose) Debug.Log("[DragonLordDialogue] Injected DialogueState into DialogueTree.", this);
        }

        if (_stateEnsureInit != null) _stateEnsureInit.Invoke(stateObj, null);
        else EnsureStateDictionary(stateObj);

        PrepareCanvas(anchorOverride);

        var npc = (_treeNpcNameField?.GetValue(_tree) as string) ?? npcName;
        if (_stateSetState != null) _stateSetState.Invoke(stateObj, new object[] { npc, key });
        else if (_treeGoToState != null) _treeGoToState.Invoke(_tree, new object[] { key });
        else TryDirectStateWrite(stateObj, npc, key);

        if (logVerbose)
            Debug.Log($"[DragonLordDialogue] Begin → npc='{npc}', state='{key}'", this);

        GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
        _talking = true;

        // Put the koala in a neutral standing-idle pose at the start of Dragon island dialogue.
        KoalaAnimBus.BroadcastToActiveKoalas(k => k.EnsureStandingIdle());

        Handle(_treeGetNextUnit?.Invoke(_tree, null));
    }

    // ---------- Core flow ----------
    private void Handle(object dialogueUnit)
    {
        _currentUnit = dialogueUnit;

        string islandId = IslandTravelManager.I && IslandTravelManager.I.CurrentIsland != null ? IslandTravelManager.I.CurrentIsland.islandId : "";

        string requiredKey = "";
        string onEnter = GetOnEnterEvent(dialogueUnit) ?? ""; // you already have this method

        if (dialogueUnit != null)
        {
            var t = dialogueUnit.GetType();
            var f = t.GetField("requiredStateKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) requiredKey = f.GetValue(dialogueUnit) as string ?? "";
        }

        MoxoCPT.LoggingDialogueChoices.SetDialogueContext(npcName, requiredKey, onEnter, islandId, "Narrative");


        if (!_talking || dialogueUnit == null) { HandleEndDialogue(); return; }

        var onEnterName = GetOnEnterEvent(dialogueUnit);
        if (!string.IsNullOrWhiteSpace(onEnterName))
            _treeCallScriptAction?.Invoke(_tree, new object[] { onEnterName });

        _uiBind?.Invoke(dialogueUIScript, new[] { dialogueUnit });
        _uiContinue?.Invoke(dialogueUIScript, null);

        if (CurrentUnitHasOptions())
            ExploreHintUI.I?.ShowDialogueOptionsHint("Use the arrow keys to choose an option, then press Space to confirm.");
        else
            ExploreHintUI.I?.HideDialogueOptionsHint();
    }

    private IEnumerator CoContinueNextFrame()
    {
        yield return null;
        if (!_talking) yield break;

        // ✅ block advance if options exist on current unit
        if (_optionAdvanceInFlight) yield break;
        if (CurrentUnitHasOptions()) yield break;

        var next = _treeGetNextUnit?.Invoke(_tree, null);
        Handle(next);
    }

    private void HandleEndDialogue()
    {
        _talking = false;
        _currentUnit = null;
        _optionAdvanceInFlight = false;

        ExploreHintUI.I?.HideDialogueOptionsHint();

        // Return the koala to standing idle when Dragon island dialogue finishes.
        KoalaAnimBus.BroadcastToActiveKoalas(k => k.EnsureStandingIdle());

        _uiEnd?.Invoke(dialogueUIScript, null);
        GameManager.Instance?.UpdateGameState(GameManager.GameState.Explore);
        if (logVerbose) Debug.Log("[DragonLordDialogue] Dialogue ended/cleaned.", this);
    }

    // ---------- Options helpers ----------
    private bool CurrentUnitHasOptions()
    {
        if (_currentUnit == null) return false;
        var arr = GetOptionsArray(_currentUnit);
        return arr != null && arr.Length > 0;
    }

    private static Array GetOptionsArray(object unit)
    {
        if (unit == null) return null;
        var t = unit.GetType();
        var f = t.GetField("options", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return f?.GetValue(unit) as Array;
    }

    private void TryInvokeOptionAction(object optionObj)
    {
        if (optionObj == null) return;

        var ot = optionObj.GetType();
        var f = ot.GetField("actionToTrigger", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var ue = f?.GetValue(optionObj);
        if (ue == null) return;

        var invoke = ue.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
        try { invoke?.Invoke(ue, null); }
        catch (Exception e) { Debug.LogWarning($"[DragonLordDialogue] Option action invoke failed: {e.Message}", this); }
    }

    private void TryInvokeContinueFromOption(int optionIndex, object optionObj)
    {
        if (_treeContinueFromOption == null) return;

        try
        {
            var ps = _treeContinueFromOption.GetParameters();

            // ContinueFromOption()
            if (ps.Length == 0)
            {
                _treeContinueFromOption.Invoke(_tree, null);
                return;
            }

            // ContinueFromOption(int)
            if (ps.Length == 1 && ps[0].ParameterType == typeof(int))
            {
                _treeContinueFromOption.Invoke(_tree, new object[] { optionIndex });
                return;
            }

            // ContinueFromOption(DialogueOption) or ContinueFromOption(object)
            if (ps.Length == 1)
            {
                object arg = null;

                if (optionObj != null && ps[0].ParameterType.IsInstanceOfType(optionObj))
                    arg = optionObj;
                else if (ps[0].ParameterType == typeof(object))
                    arg = optionObj;

                if (arg != null)
                    _treeContinueFromOption.Invoke(_tree, new object[] { arg });
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DragonLordDialogue] ContinueFromOption invoke failed: {e.Message}", this);
        }
    }

    // ---------- Helpers ----------
    private string GetOnEnterEvent(object dialogueUnit)
    {
        if (dialogueUnit == null) return null;
        var unitType = dialogueUnit.GetType();
        var fld = unitType.GetField("onEnterEventName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return fld != null ? fld.GetValue(dialogueUnit) as string : null;
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
        }
    }

    private void TryDirectStateWrite(object stateObj, string npc, string key)
    {
        if (stateObj == null || _stateDictField == null) return;
        var dict = _stateDictField.GetValue(stateObj);
        if (dict == null) return;

        var trySet = dict.GetType().GetMethod("set_Item"); // indexer
        if (trySet != null) trySet.Invoke(dict, new object[] { npc, key });
    }

    // ---------- Reflection wiring ----------
    private void BindTreeReflection()
    {
        if (_treeType == null)
        {
            Debug.LogError("[DragonLordDialogue] Cannot reflect DialogueTreeObjects type.");
            return;
        }

        _treeResetCallbacks  = _treeType.GetMethod("ResetCallbacks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeSetUpDict       = _treeType.GetMethod("SetUpDialogueUnitsDict", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeGetNextUnit     = _treeType.GetMethod("GetNextDialogueUnit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeCallScriptAction= _treeType.GetMethod("CallScriptableAction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeGoToState       = _treeType.GetMethod("GoToState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);

        _treeStateField      = _treeType.GetField("dialogueState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeNpcNameField    = _treeType.GetField("npcName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        _treeContinueCb      = _treeType.GetField("continueCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeEndCb           = _treeType.GetField("endDialogueCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _treeContinue        = _treeType.GetMethod("Continue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // ✅ NEW: ContinueFromOption(...)
        _treeContinueFromOption = _treeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => string.Equals(m.Name, "ContinueFromOption", StringComparison.OrdinalIgnoreCase));

        _treeSetUpState = _treeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == "SetUpDialogueState" && m.GetParameters().Length == 1);

        var ps = _treeSetUpState?.GetParameters();
        _stateType = ps != null && ps.Length == 1 ? ps[0].ParameterType
                   : FindTypeAnywhere("Dialogue.DialogueState") ?? FindTypeAnywhere("DialogueState");

        if (_stateType != null)
        {
            _stateSetState   = _stateType.GetMethod("SetState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(string) }, null);
            _stateEnsureInit = _stateType.GetMethod("EnsureInitialized", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _stateDictField  = _stateType.GetField("stateDict", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        _treeRegisterScriptCb = _treeType.GetMethod(
            "RegisterScriptableCallback",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(Action) },
            null);
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
        {
            if (logVerbose) Debug.Log($"[DragonLordDialogue] UI bound → {dialogueUIScript.GetType().Name} on '{dialogueUIScript.gameObject.name}'", this);
        }
        else
        {
            Debug.LogWarning("[DragonLordDialogue] Couldn’t find a Dialogue UI script on the dropped object.", this);
        }

        CacheUIMethods();
    }

    private void CacheUIMethods()
    {
        _uiBind = _uiContinue = _uiEnd = _uiStopAutoAdvance = null;
        if (!dialogueUIScript) return;

        var uiType = dialogueUIScript.GetType();
        _uiBind     = uiType.GetMethod("BindDialogueUnit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _uiContinue = uiType.GetMethod("ContinueDialogue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _uiEnd      = uiType.GetMethod("EndDialogue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // optional (your DialogueUI update added this)
        _uiStopAutoAdvance = uiType.GetMethod("StopAutoAdvance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
}
