
// //Jan 19: Add training
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using UnityEngine;
// using MoxoCPT; // uses MoxoCPTManager + MoxoSessionCoordinator

// public class IslandTravelManager : MonoBehaviour
// {
//     public static IslandTravelManager I { get; private set; }

//     [Header("Wiring")]
//     [SerializeField] private FadeScreen fader;
//     [SerializeField] private Transform playerRootOverride;
//     [SerializeField] private PlayerModeManager modeManager;

//     [Header("Intro (World Space)")]
//     [SerializeField] private int introCanvasSortingOrder = 100;
//     [SerializeField] private bool introFacePlayer = false;

//     [Header("Countdown (World Space)")]
//     [SerializeField] private int countdownCanvasSortingOrder = 120;
//     [SerializeField] private bool countdownFacePlayer = false;

//     [Tooltip("Optional: drag your ExploreHintUI component here (any namespace).")]
//     [SerializeField] private MonoBehaviour countdownUIOverride = null;

//     [SerializeField] private Transform uiRuntimeRoot = null;
//     [SerializeField] private bool reparentCountdownToRuntimeRoot = true;

//     [Header("Picker (World Space)")]
//     [SerializeField] private int pickerCanvasSortingOrder = 130;
//     [SerializeField] private bool pickerFacePlayer = false;
//     [SerializeField] private Canvas pickerCanvasOverride = null;
//     [SerializeField] private bool reparentPickerToRuntimeRoot = true;

//     [Header("Training")]
//     [SerializeField] private bool alwaysRunTraining = false; // force retraining each visit (for testing)

//     [Header("Debug")]
//     [SerializeField] private bool logVerbose = true;

//     private readonly Dictionary<string, Transform> _anchorById      = new();
//     private readonly Dictionary<string, Transform> _introAnchorById = new();
//     private readonly Dictionary<string, Transform> _countdownAnchorById = new();
//     private readonly Dictionary<string, Transform> _pickerAnchorById    = new();

//     public IslandData CurrentIsland { get; private set; }

//     private IntroScreen _lastIntro;
//     private MonoBehaviour _lastCountdown;

//     private bool _didTrainingThisTravel;

//     private void Awake()
//     {
//         I = this;
//         if (!fader)
//             fader = GameObject.FindGameObjectWithTag("Fader")?.GetComponent<FadeScreen>();
//         RebuildAnchorCache();
//         StartCoroutine(LateBindPlayerRoot());
//     }

//     private IEnumerator LateBindPlayerRoot()
//     {
//         yield return null;
//         var root = GetPlayerRoot();
//         if (!root) Debug.LogError("[IslandTravel] No player root found.");
//         else if (logVerbose) Debug.Log($"[IslandTravel] Player root bound → '{root.name}' @ {root.position}");
//     }

//     // ---------- PUBLIC ----------
//     public void RebuildAnchorCache()
//     {
//         _anchorById.Clear();
//         _introAnchorById.Clear();
//         _countdownAnchorById.Clear();
//         _pickerAnchorById.Clear();

//         foreach (var a in FindObjectsOfType<IslandAnchor>(true))
//             if (!string.IsNullOrWhiteSpace(a.islandId))
//                 _anchorById[a.islandId.Trim().ToUpperInvariant()] = a.transform;

//         foreach (var ia in FindObjectsOfType<IntroAnchor>(true))
//             if (!string.IsNullOrWhiteSpace(ia.islandId))
//                 _introAnchorById[ia.islandId.Trim().ToUpperInvariant()] = ia.transform;

//         foreach (var ca in FindObjectsOfType<CountdownAnchor>(true))
//             if (!string.IsNullOrWhiteSpace(ca.islandId))
//                 _countdownAnchorById[ca.islandId.Trim().ToUpperInvariant()] = ca.transform;

//         foreach (var pa in FindObjectsOfType<PickerAnchor>(true))
//             if (!string.IsNullOrWhiteSpace(pa.islandId))
//                 _pickerAnchorById[pa.islandId.Trim().ToUpperInvariant()] = pa.transform;

//         if (logVerbose)
//             Debug.Log($"[IslandTravel] Anchors → teleport:{_anchorById.Count} intro:{_introAnchorById.Count} countdown:{_countdownAnchorById.Count} picker:{_pickerAnchorById.Count}");
//     }

//     public void TravelTo(IslandData island)
//     {
//         if (!island)
//         {
//             Debug.LogError("[IslandTravel] TravelTo(null).");
//             return;
//         }
//         CurrentIsland = island;
//         _didTrainingThisTravel = false;
//         StartCoroutine(CoTravel(island));
//     }

//     private void ActivateMoxoForIsland(string islandId)
//     {
//         var groups = FindObjectsOfType<IslandMoxoGroup>(true);
//         foreach (var g in groups)
//         {
//             if (!g.moxoRoot) continue;
//             bool on = string.Equals(g.islandId?.Trim(), islandId, System.StringComparison.OrdinalIgnoreCase);
//             g.moxoRoot.SetActive(on);
//         }
//     }

//     // ---------- CORE ----------
//     private IEnumerator CoTravel(IslandData island)
//     {
//         MoxoSessionCoordinator.CleanupPreviousIsland();

//         var root = GetPlayerRoot();
//         if (!root) { Debug.LogError("[IslandTravel] No player root."); yield break; }

//         var id = island.islandId?.Trim().ToUpperInvariant();
//         if (string.IsNullOrEmpty(id)) { Debug.LogError("[IslandTravel] Island id empty."); yield break; }

//         // Testing: force retraining this island
//         if (alwaysRunTraining) TrainingCPTRunner.ClearCompletedForIsland(id);

//         if (!_anchorById.TryGetValue(id, out var dest) || !dest)
//         {
//             Debug.LogError($"[IslandTravel] No teleport anchor for '{id}'.");
//             yield break;
//         }

//         if (fader) fader.TeleportFade();
//         yield return new WaitForSeconds(0.2f);

//         var cc = root.GetComponent<CharacterController>();
//         if (cc) cc.enabled = false;
//         root.position = dest.position;
//         root.rotation = dest.rotation;
//         if (cc) cc.enabled = true;

//         if (logVerbose) Debug.Log($"[IslandTravel] Moved to {island.displayName} ({id})");

//         GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);
//         KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnPrepareCPT());

//         ActivateMoxoForIsland(id);

//         _lastCountdown = PlaceCountdownForIsland(id, root);
//         _lastIntro     = PlaceIntroScreenForIsland(id, root);

//         MoxoSessionCoordinator.PrepareForNewIsland(id);

//         // PRE-TRAINING INTRO (text only)
//         if (_lastIntro && island.startsMoxoOnArrival)
//         {
//             var preTitle = IntroScreen.Fallback(island.trainingIntroTitle, island.introTitle);
//             var preBody  = IntroScreen.Fallback(island.trainingIntroBody,  island.introBody);

//             _lastIntro.ShowTextOnly(preTitle, preBody);
//             _lastIntro.ArmOnStart(onStart: null, delayButton: true, delaySeconds: island.countdownSeconds > 0f ? island.countdownSeconds : 2f);
//             if (logVerbose) Debug.Log($"[IslandTravel] Showed PRE-TRAINING intro for '{island.displayName}'.");
//         }

//         StartCoroutine(CoWaitIntroThenCountdown(id, island, root));
//         yield break;
//     }

//     // Wait Intro dismissed → run TRAINING (once) → show READY intro → start MOXO
//     private IEnumerator CoWaitIntroThenCountdown(string islandId, IslandData island, Transform playerRoot)
//     {
//         // 1) Wait for the first intro to be dismissed — with a hard timeout
//         if (_lastIntro)
//         {
//             float timeout = 20f;
//             bool hidden = false;
//             Debug.Log($"[IslandTravel] Waiting for first intro to close on '{islandId}'…");

//             while (timeout > 0f)
//             {
//                 hidden =
//                     !_lastIntro.isActiveAndEnabled ||
//                     !_lastIntro.gameObject.activeInHierarchy ||
//                     (TryGetCanvasGroup(_lastIntro.gameObject, out var cg) && cg.alpha <= 0.001f);

//                 if (hidden) break;

//                 timeout -= Time.unscaledDeltaTime;
//                 yield return null;
//             }

//             if (!hidden)
//             {
//                 Debug.LogWarning("[IslandTravel] Intro didn’t fully close before timeout. Forcing continue + hide.");
//                 _lastIntro.HideInstant();
//             }
//             else
//             {
//                 Debug.Log("[IslandTravel] First intro closed.");
//             }
//         }

//         GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);

//         // 2) Run TRAINING (once per travel + once per island per session)
//         if (!_didTrainingThisTravel && !TrainingCPTRunner.HasCompletedForIsland(islandId))
//         {
//             var trainer = FindTrainerForIsland(islandId);
//             if (trainer)
//             {
//                 if (!trainer.gameObject.activeInHierarchy)
//                 {
//                     Debug.Log("[IslandTravel] Trainer found but inactive → enabling GameObject.");
//                     trainer.gameObject.SetActive(true);
//                 }

//                 _didTrainingThisTravel = true;
//                 Debug.Log($"[IslandTravel] Training start for island '{islandId}'.");
//                 yield return StartCoroutine(trainer.RunTrainingForActiveIsland(islandId));
//                 Debug.Log($"[IslandTravel] Training finished for island '{islandId}'.");
//             }
//             else
//             {
//                 Debug.LogWarning($"[IslandTravel] No TrainingCPTRunner found for island '{islandId}'. Skipping training.");
//             }
//         }
//         else
//         {
//             Debug.Log($"[IslandTravel] Training already completed for '{islandId}'.");
//         }

//         GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);

//         // 3) READY intro (wired to start real MOXO)
//         var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
//         if (intro)
//         {
//             var readyTitle = IntroScreen.Fallback(island.readyIntroTitle, "Ready to start?");
//             var readyBody  = IntroScreen.Fallback(island.readyIntroBody,  "Press Space to begin the real test.");

//             intro.ShowMoxo(readyTitle, readyBody);
//             intro.DelayStartButton(0f, "Start"); // clickable immediately

//             float timeout = 20f;
//             bool hidden = false;
//             Debug.Log("[IslandTravel] Waiting for READY intro to close…");
//             while (timeout > 0f)
//             {
//                 hidden =
//                     !intro.isActiveAndEnabled ||
//                     !intro.gameObject.activeInHierarchy ||
//                     (TryGetCanvasGroup(intro.gameObject, out var cg2) && cg2.alpha <= 0.001f);

//                 if (hidden) break;

//                 timeout -= Time.unscaledDeltaTime;
//                 yield return null;
//             }
//             if (!hidden)
//             {
//                 Debug.LogWarning("[IslandTravel] READY intro didn’t close before timeout. Forcing start.");
//                 intro.HideInstant();
//             }
//         }
//         else
//         {
//             Debug.LogWarning("[IslandTravel] IntroScreen not found for READY step.");
//         }

//         // 4) Safety: ensure trainer remnants are off, then nudge runner
//         TrainingCPTRunner.ForceStopAll();

//         var runner = FindObjectsOfType<MoxoCPT.ChangeShapes>(true)
//                     .FirstOrDefault(r => r.isActiveAndEnabled && r.gameObject.activeInHierarchy);
//         if (runner)
//         {
//             if (logVerbose) Debug.Log($"[IslandTravel] Nudge runner: {runner.gameObject.name} → ForceStart()");
//             runner.ForceStart();
//         }
//         else
//         {
//             Debug.LogWarning("[IslandTravel] No active ChangeShapes found to nudge.");
//         }
//     }

//     // More forgiving trainer finder with strong logs
//     private TrainingCPTRunner FindTrainerForIsland(string islandId)
//     {
//         islandId = (islandId ?? "").Trim().ToUpperInvariant();
//         var all = FindObjectsOfType<TrainingCPTRunner>(true);

//         if (all.Length == 0)
//         {
//             Debug.LogWarning("[IslandTravel] No TrainingCPTRunner exists in scene.");
//             return null;
//         }

//         // Try under THIS island’s MOXO rig
//         var group = FindObjectsOfType<IslandMoxoGroup>(true)
//                     .FirstOrDefault(g => g && string.Equals(g.islandId?.Trim(), islandId, System.StringComparison.OrdinalIgnoreCase));

//         if (group && group.moxoRoot)
//         {
//             var rigRoot = group.moxoRoot.transform;
//             var child = all.FirstOrDefault(t => t && t.transform.IsChildOf(rigRoot));
//             if (child)
//             {
//                 Debug.Log($"[IslandTravel] Trainer found under island rig '{islandId}': {child.gameObject.name}");
//                 return child;
//             }
//             else
//             {
//                 Debug.LogWarning($"[IslandTravel] No trainer under MOXO rig for '{islandId}'.");
//             }
//         }
//         else
//         {
//             Debug.LogWarning($"[IslandTravel] IslandMoxoGroup missing or moxoRoot null for '{islandId}'.");
//         }

//         // Fallbacks
//         var enabledOne = all.FirstOrDefault(t => t && t.isActiveAndEnabled);
//         if (enabledOne)
//         {
//             Debug.Log($"[IslandTravel] Using enabled trainer in scene: {enabledOne.gameObject.name}");
//             return enabledOne;
//         }

//         var any = all.FirstOrDefault(t => t);
//         if (any)
//         {
//             Debug.Log($"[IslandTravel] Using fallback trainer in scene (will enable): {any.gameObject.name}");
//             return any;
//         }

//         return null;
//     }

//     private static bool TryGetCanvasGroup(GameObject go, out CanvasGroup cg)
//     {
//         cg = go ? go.GetComponent<CanvasGroup>() : null;
//         return cg;
//     }

//     // ---------- INTRO ----------
//     private IntroScreen PlaceIntroScreenForIsland(string islandId, Transform playerRoot)
//     {
//         var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
//         if (!intro) { Debug.LogWarning("[IslandTravel] IntroScreen not found."); return null; }

//         _introAnchorById.TryGetValue(islandId, out var anchor);
//         var target = anchor ? anchor : playerRoot;

//         var t = intro.transform;
//         t.position = target.position;
//         t.rotation = target.rotation;

//         if (introFacePlayer && playerRoot)
//         {
//             t.LookAt(playerRoot.position, Vector3.up);
//             t.Rotate(0f, 180f, 0f, Space.Self);
//         }

//         var canvas = intro.GetComponentInChildren<Canvas>(true);
//         if (canvas && canvas.renderMode == RenderMode.WorldSpace)
//         {
//             var cam = playerRoot.GetComponentInChildren<Camera>(true);
//             if (cam) canvas.worldCamera = cam;
//             canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, introCanvasSortingOrder);
//         }

//         var cg = intro.GetComponent<CanvasGroup>();
//         if (cg) { cg.blocksRaycasts = true; cg.interactable = true; }

//         if (logVerbose) Debug.Log($"[IslandTravel] Intro placed for '{islandId}'.");
//         return intro;
//     }

//     // ---------- COUNTDOWN ----------
//     private MonoBehaviour PlaceCountdownForIsland(string islandId, Transform playerRoot)
//     {
//         MonoBehaviour countdown = countdownUIOverride;
//         if (!countdown)
//         {
//             foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
//                 if (mb && mb.GetType().Name == "ExploreHintUI") { countdown = mb; break; }
//         }
//         if (!countdown) { if (logVerbose) Debug.Log("[IslandTravel] No ExploreHintUI found."); return null; }

//         var t = countdown.transform;

//         if (reparentCountdownToRuntimeRoot)
//         {
//             var newParent = uiRuntimeRoot ? uiRuntimeRoot : playerRoot;
//             if (t.parent != newParent) t.SetParent(newParent, true);
//         }

//         _countdownAnchorById.TryGetValue(islandId, out var anchor);
//         if (!anchor) _introAnchorById.TryGetValue(islandId, out anchor);
//         if (!anchor) anchor = playerRoot;

//         t.position = anchor.position;
//         t.rotation = anchor.rotation;

//         if (countdownFacePlayer && playerRoot)
//         {
//             t.LookAt(playerRoot.position, Vector3.up);
//             t.Rotate(0f, 180f, 0f, Space.Self);
//         }

//         var canvas = countdown.GetComponentInChildren<Canvas>(true);
//         if (canvas && canvas.renderMode == RenderMode.WorldSpace)
//         {
//             var cam = playerRoot.GetComponentInChildren<Camera>(true);
//             if (!cam && Camera.main) cam = Camera.main;
//             if (cam) canvas.worldCamera = cam;

//             if (canvas.sortingOrder < countdownCanvasSortingOrder)
//                 canvas.sortingOrder = countdownCanvasSortingOrder;
//         }

//         var cg = countdown.GetComponent<CanvasGroup>();
//         if (cg) { cg.blocksRaycasts = true; cg.interactable = true; }
//         if (!countdown.gameObject.activeSelf) countdown.gameObject.SetActive(true);

//         if (logVerbose) Debug.Log($"[IslandTravel] Countdown placed for '{islandId}' via {countdown.GetType().FullName}.");
//         return countdown;
//     }

//     // ---------- PICKER ----------
//     public void PlacePickerForIsland(string islandId)
//     {
//         var playerRoot = GetPlayerRoot();
//         if (!playerRoot) return;

//         var pickerCanvas = pickerCanvasOverride
//                            ?? FindObjectsOfType<Canvas>(true)
//                                .FirstOrDefault(c => c.name.IndexOf("Picker", System.StringComparison.OrdinalIgnoreCase) >= 0);

//         if (!pickerCanvas) { if (logVerbose) Debug.Log("[IslandTravel] No Picker canvas found."); return; }

//         if (pickerCanvas.renderMode != RenderMode.WorldSpace)
//         {
//             pickerCanvas.renderMode = RenderMode.WorldSpace;
//             if (logVerbose) Debug.Log("[IslandTravel] Picker renderMode forced to WorldSpace.");
//         }

//         var t = pickerCanvas.transform as RectTransform;
//         var newParent = (reparentPickerToRuntimeRoot && uiRuntimeRoot) ? uiRuntimeRoot : playerRoot;
//         if (t.parent != newParent)
//         {
//             t.SetParent(newParent, false);
//             t.localScale = Vector3.one;
//             t.anchoredPosition3D = Vector3.zero;
//             t.localRotation = Quaternion.identity;
//         }

//         _pickerAnchorById.TryGetValue(islandId, out var anchor);
//         var target = anchor ? anchor : playerRoot;

//         t.position = target.position;
//         t.rotation = target.rotation;

//         if (pickerFacePlayer && playerRoot)
//         {
//             t.LookAt(playerRoot.position, Vector3.up);
//             t.Rotate(0f, 180f, 0f, Space.Self);
//         }

//         var cam = playerRoot.GetComponentInChildren<Camera>(true) ?? Camera.main;
//         if (cam) pickerCanvas.worldCamera = cam;
//         if (pickerCanvas.sortingOrder < pickerCanvasSortingOrder)
//             pickerCanvas.sortingOrder = pickerCanvasSortingOrder;

//         var cg = pickerCanvas.GetComponent<CanvasGroup>();
//         if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
//         if (!pickerCanvas.gameObject.activeSelf) pickerCanvas.gameObject.SetActive(true);

//         if (logVerbose) Debug.Log($"[IslandTravel] Positioned Picker '{pickerCanvas.name}' for '{islandId}'.");
//     }

//     public void PlacePickerForCurrentIsland()
//     {
//         var id = (CurrentIsland?.islandId ?? "").Trim().ToUpperInvariant();
//         if (string.IsNullOrEmpty(id)) return;
//         PlacePickerForIsland(id);
//     }

//     // ---------- HELPERS ----------
//     private Transform GetPlayerRoot()
//     {
//         if (playerRootOverride) return playerRootOverride;
//         if (modeManager && modeManager.CurrentPlayerRoot) return modeManager.CurrentPlayerRoot;

//         var tagged = GameObject.FindGameObjectWithTag("Player");
//         if (tagged) return tagged.transform;

//         string[] common = { "XR Origin", "XROrigin", "XR Rig", "PlayerRoot", "VR_Player", "Desktop_Rig", "Player" };
//         foreach (var n in common)
//         {
//             var go = GameObject.Find(n);
//             if (go) return go.transform;
//         }

//         var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
//         foreach (var r in scene.GetRootGameObjects())
//             if (r.name.Contains("XR") || r.name.Contains("Player"))
//                 return r.transform;

//         if (Camera.main) return Camera.main.transform.root;
//         return null;
//     }
// }
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoxoCPT;

public class IslandTravelManager : MonoBehaviour
{
    public static IslandTravelManager I { get; private set; }

    [Header("Wiring")]
    [SerializeField] private FadeScreen fader;
    [SerializeField] private Transform playerRootOverride;
    [SerializeField] private PlayerModeManager modeManager;

    [Header("Intro (World Space)")]
    [SerializeField] private int introCanvasSortingOrder = 100;
    [SerializeField] private bool introFacePlayer = false;

    [Header("Countdown (World Space)")]
    [SerializeField] private int countdownCanvasSortingOrder = 120;
    [SerializeField] private bool countdownFacePlayer = false;

    [Tooltip("Optional: drag your ExploreHintUI component here (any namespace).")]
    [SerializeField] private MonoBehaviour countdownUIOverride = null;

    [SerializeField] private Transform uiRuntimeRoot = null;
    [SerializeField] private bool reparentCountdownToRuntimeRoot = true;

    [Header("Picker (World Space)")]
    [SerializeField] private bool pickerFacePlayer = false;
    [SerializeField] private Canvas pickerCanvasOverride = null;
    [SerializeField] private bool reparentPickerToRuntimeRoot = true;

    [Header("Training")]
    [SerializeField] private bool alwaysRunTraining = false;
    [SerializeField] private bool forceTrainingThisTravel = false; // one-shot override

    [Header("Debug")]
    [SerializeField] private bool logVerbose = true;

    [Header("Non-MOXO Intro Behavior")]
    [SerializeField] private bool autoCloseNonMoxoIntro = false;
    [SerializeField] private float nonMoxoIntroAutoCloseSeconds = 0f; // if <=0 uses countdownSeconds+5 behavior

    private readonly Dictionary<string, Transform> _anchorById = new();
    private readonly Dictionary<string, Transform> _introAnchorById = new();
    private readonly Dictionary<string, Transform> _countdownAnchorById = new();
    private readonly Dictionary<string, Transform> _pickerAnchorById = new();

    public IslandData CurrentIsland { get; private set; }
    public bool HasCurrentIslandSpawnRotation { get; private set; }
    public Quaternion CurrentIslandSpawnRotation { get; private set; } = Quaternion.identity;

    private IntroScreen _lastIntro;
    private MonoBehaviour _lastCountdown;
    private MonoBehaviour _lastNonMoxoHandler;
    private bool _didTrainingThisTravel;
    private Coroutine _cptWatchdog;

    private void Awake()
    {
        I = this;

        if (!fader)
            fader = GameObject.FindGameObjectWithTag("Fader")?.GetComponent<FadeScreen>();

        // Auto-wire modeManager so CurrentPlayerRoot is always available,
        // even if the Inspector reference was not assigned.
        if (!modeManager)
            modeManager = FindObjectOfType<PlayerModeManager>();

        RebuildAnchorCache();
        StartCoroutine(LateBindPlayerRoot());
    }

    private IEnumerator LateBindPlayerRoot()
    {
        yield return null;

        var root = GetPlayerRoot();
        if (!root)
            Debug.LogError("[IslandTravel] No player root found.");
        else if (logVerbose)
            Debug.Log($"[IslandTravel] Player root bound → '{root.name}' @ {root.position}");
    }

    // ---------- PUBLIC ----------

    public void RebuildAnchorCache()
    {
        _anchorById.Clear();
        _introAnchorById.Clear();
        _countdownAnchorById.Clear();
        _pickerAnchorById.Clear();

        foreach (var a in FindObjectsOfType<IslandAnchor>(true))
        {
            if (!string.IsNullOrWhiteSpace(a.islandId))
                _anchorById[a.islandId.Trim().ToUpperInvariant()] = a.transform;
        }

        foreach (var ia in FindObjectsOfType<IntroAnchor>(true))
        {
            if (!string.IsNullOrWhiteSpace(ia.islandId))
                _introAnchorById[ia.islandId.Trim().ToUpperInvariant()] = ia.transform;
        }

        foreach (var ca in FindObjectsOfType<CountdownAnchor>(true))
        {
            if (!string.IsNullOrWhiteSpace(ca.islandId))
                _countdownAnchorById[ca.islandId.Trim().ToUpperInvariant()] = ca.transform;
        }

        foreach (var pa in FindObjectsOfType<PickerAnchor>(true))
        {
            if (!string.IsNullOrWhiteSpace(pa.islandId))
                _pickerAnchorById[pa.islandId.Trim().ToUpperInvariant()] = pa.transform;
        }

        if (logVerbose)
        {
            Debug.Log(
                $"[IslandTravel] Anchors → teleport:{_anchorById.Count} intro:{_introAnchorById.Count} countdown:{_countdownAnchorById.Count} picker:{_pickerAnchorById.Count}"
            );
        }
    }

    public void TravelTo(IslandData island)
    {
        if (!island)
        {
            Debug.LogError("[IslandTravel] TravelTo(null).");
            return;
        }

        CleanupLastNonMoxoHandler();
        StopCptWatchdog();

        CurrentIsland = island;
        _didTrainingThisTravel = false;
        HasCurrentIslandSpawnRotation = false;

        StartCoroutine(CoTravel(island));
    }

    private void ActivateMoxoForIsland(string islandId)
    {
        // If islandId is empty => force OFF for all islands
        bool forceOffAll = string.IsNullOrWhiteSpace(islandId);

        var groups = FindObjectsOfType<IslandMoxoGroup>(true);
        foreach (var g in groups)
        {
            if (!g.moxoRoot) continue;

            bool on = false;

            if (!forceOffAll)
            {
                on = string.Equals(
                    g.islandId?.Trim(),
                    islandId,
                    StringComparison.OrdinalIgnoreCase
                );
            }

            g.moxoRoot.SetActive(on);
        }

        int drivers = 0;
        foreach (var d in FindObjectsOfType<KoalaAnimatorDriver>(true))
        {
            if (d && d.isActiveAndEnabled && d.gameObject.activeInHierarchy)
                drivers++;
        }

        if (logVerbose)
            Debug.Log($"[IslandTravel] Koala drivers active: {drivers}. Nudge to idle.");

        KoalaAnimBus.BroadcastToActiveKoalas(k => k.EnsureStandingIdle());
    }

    private void DeactivateAllMoxo()
    {
        var groups = FindObjectsOfType<IslandMoxoGroup>(true);
        foreach (var g in groups)
        {
            if (!g || !g.moxoRoot) continue;
            g.moxoRoot.SetActive(false);
        }
    }

    // ---------- CORE ----------

    private IEnumerator CoTravel(IslandData island)
    {
        MoxoSessionCoordinator.CleanupPreviousIsland(); // safe either way

        var root = GetPlayerRoot();
        if (!root)
        {
            Debug.LogError("[IslandTravel] No player root.");
            yield break;
        }

        var id = island.islandId?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[IslandTravel] Island id empty.");
            yield break;
        }

        // one-shot or always-on training reset
        if (forceTrainingThisTravel)
        {
            TrainingCPTRunner.ClearCompletedForIsland(id);
            Debug.Log($"[IslandTravel] forceTrainingThisTravel → cleared completion for '{id}'.");
            forceTrainingThisTravel = false;
        }

        if (alwaysRunTraining)
        {
            TrainingCPTRunner.ClearCompletedForIsland(id);
            if (logVerbose)
                Debug.Log($"[IslandTravel] alwaysRunTraining → cleared completion for '{id}'.");
        }

        if (!_anchorById.TryGetValue(id, out var dest) || !dest)
        {
            Debug.LogError($"[IslandTravel] No teleport anchor for '{id}'.");
            yield break;
        }

        if (fader) fader.TeleportFade();
        yield return new WaitForSeconds(0.2f);

        var cc = root.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        root.position = dest.position;
        root.rotation = dest.rotation;
        CurrentIslandSpawnRotation = Quaternion.Euler(0f, dest.rotation.eulerAngles.y, 0f);
        HasCurrentIslandSpawnRotation = true;

        if (cc) cc.enabled = true;

        if (logVerbose)
            Debug.Log($"[IslandTravel] Moved to {island.displayName} ({id})");

        _lastCountdown = PlaceCountdownForIsland(id, root);
        _lastIntro = PlaceIntroScreenForIsland(id, root);

        // NON-MOXO flow?
        if (!island.hasMoxoGame)
        {
            // Ensure MOXO rigs/cameras/cards are OFF for non-MOXO islands
            DeactivateAllMoxo();

            GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
            yield return StartCoroutine(CoRunNonMoxoFlow(island, id));
            yield break;
        }

        // MOXO flow (only for islands that actually have MOXO)
        ActivateMoxoForIsland(id);
        MoxoStateCameraSwitch.Instance?.EnterMoxoViewNow();

        GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);
        KoalaAnimBus.BroadcastToActiveKoalas(k => k.OnPrepareCPT());

        MoxoSessionCoordinator.PrepareForNewIsland(id);

        // pre-training intro
        if (_lastIntro)
        {
            var preTitle = IntroScreen.Fallback(island.trainingIntroTitle, island.introTitle);
            var preBody = IntroScreen.Fallback(island.trainingIntroBody, island.introBody);

            _lastIntro.ShowTextOnly(preTitle, preBody);

            // Optional voice over for this island's pre-training intro
            if (island.trainingIntroVoice && IntroScreen.Instance != null)
                IntroScreen.Instance.PlayVoice(island.trainingIntroVoice);

            _lastIntro.ArmOnStart(
                null,
                true,
                island.countdownSeconds > 0f ? island.countdownSeconds : 2f
            );

            if (logVerbose)
                Debug.Log($"[IslandTravel] PRE-TRAINING intro for '{island.displayName}'.");
        }

        StartCoroutine(CoWaitIntroThenCountdown(id, island, root));
    }

    // ---------- NON-MOXO FLOW ----------

    private IEnumerator CoRunNonMoxoFlow(IslandData island, string islandId)
    {
        if (_lastIntro)
        {
            _lastIntro.ShowTextOnly(island.introTitle, island.introBody);

            // Optional voice over for this island's NON-MOXO intro
            if (island.nonMoxoIntroVoice && IntroScreen.Instance != null)
                IntroScreen.Instance.PlayVoice(island.nonMoxoIntroVoice);

            // ✅ When player dismisses intro, start the non-MOXO dialogue immediately.
            _lastIntro.ArmOnStart(
                onStart: () =>
                {
                    GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
                    StartNonMoxoDialogueAfterIntro(island, islandId);
                },
                delayButton: true,
                delaySeconds: island.countdownSeconds
            );
        }
        else
        {
            // If no intro UI, start dialogue right away
            GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
            StartNonMoxoDialogueAfterIntro(island, islandId);
            yield break;
        }

        // Optional: wait until intro is actually gone (so it doesn't overlap the dialogue UI)
        while (_lastIntro && _lastIntro.gameObject.activeInHierarchy)
        {
            var cg = _lastIntro.GetComponent<CanvasGroup>();
            bool hidden = (cg && cg.alpha <= 0.001f);
            if (hidden) break;
            yield return null;
        }
    }

    // ---------- MOXO FLOW ----------

    private IEnumerator CoWaitIntroThenCountdown(string islandId, IslandData island, Transform playerRoot)
    {
        if (_lastIntro)
        {
            // Wait until player dismisses the intro (NO auto-timeout)
            while (true)
            {
                bool hidden =
                    !_lastIntro.isActiveAndEnabled ||
                    !_lastIntro.gameObject.activeInHierarchy ||
                    (TryGetCanvasGroup(_lastIntro.gameObject, out var cg) && cg.alpha <= 0.001f);

                if (hidden) break;
                yield return null;
            }

            if (_lastIntro && _lastIntro.gameObject.activeInHierarchy)
                _lastIntro.HideInstant();
        }


        GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);

        // ----- TRAINING GATE (strict + loud logs) -----
        bool completed = TrainingCPTRunner.HasCompletedForIsland(islandId);
        Debug.Log(
            $"[IslandTravel] Training gate for '{islandId}' → didThisTravel={_didTrainingThisTravel}, completed={completed}, alwaysRunTraining={alwaysRunTraining}"
        );

        if (_didTrainingThisTravel == false && completed == false)
        {
            var trainer = FindTrainerForIsland(islandId);

            if (trainer)
            {
                if (!trainer.gameObject.activeInHierarchy)
                    trainer.gameObject.SetActive(true);

                _didTrainingThisTravel = true;

                Debug.Log(
                    $"[IslandTravel] Training START for island '{islandId}' using '{trainer.name}'."
                );

                yield return StartCoroutine(trainer.RunTrainingForActiveIsland(islandId));

                Debug.Log($"[IslandTravel] Training FINISH for island '{islandId}'.");
            }
            else
            {
                Debug.LogWarning(
                    $"[IslandTravel] No TrainingCPTRunner strictly bound to island '{islandId}'. Skipping training."
                );
            }
        }
        else
        {
            Debug.Log($"[IslandTravel] Training SKIPPED for '{islandId}'.");
        }

        GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);

        // READY intro + countdown (loops if player requests replay training)
        var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
        if (intro)
        {
            bool replayTrainingRequested;
            do
            {
                replayTrainingRequested = false;

                var readyTitle = IntroScreen.Fallback(island.readyIntroTitle, "Ready to start?");
                var readyBody  = IntroScreen.Fallback(island.readyIntroBody,  "Press Space to begin the real test.");

                intro.ShowReadyAfterTraining(readyTitle, readyBody, 5f, "Start", showReplayTraining: true);
                // Wire the button click (the flag is set on IntroScreen itself before the fade starts)
                intro.ArmReplayTraining(null);

                // Optional per-island voice over for the READY intro
                if (island.readyIntroVoice)
                    intro.PlayVoice(island.readyIntroVoice);

                // Wait until player dismisses the focus screen (NO auto-timeout)
                while (true)
                {
                    bool hidden =
                        !intro.isActiveAndEnabled ||
                        !intro.gameObject.activeInHierarchy ||
                        (TryGetCanvasGroup(intro.gameObject, out var cg2) && cg2.alpha <= 0.001f);

                    if (hidden) break;
                    yield return null;
                }

                // Read the flag NOW: ReplayTrainingPending is set at button-click time (before the
                // fade), so it is already true even if the loop broke on the alpha threshold.
                replayTrainingRequested = intro.ReplayTrainingPending;

                if (intro && intro.gameObject.activeInHierarchy)
                    intro.HideInstant();

                // Player chose to replay training: re-run it before showing the focus screen again
                if (replayTrainingRequested)
                {
                    Debug.Log($"[IslandTravel] Replay Training requested from focus screen for island '{islandId}'.");

                    try { TrainingCPTRunner.ClearCompletedForIsland(islandId); } catch { }
                    _didTrainingThisTravel = false;

                    GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);

                    var replayTrainer = FindTrainerForIsland(islandId);
                    if (replayTrainer)
                    {
                        if (!replayTrainer.gameObject.activeInHierarchy)
                            replayTrainer.gameObject.SetActive(true);

                        _didTrainingThisTravel = true;
                        Debug.Log($"[IslandTravel] Replay Training START for island '{islandId}' using '{replayTrainer.name}'.");
                        yield return StartCoroutine(replayTrainer.RunTrainingForActiveIsland(islandId));
                        Debug.Log($"[IslandTravel] Replay Training FINISH for island '{islandId}'.");
                    }
                    else
                    {
                        Debug.LogWarning($"[IslandTravel] Replay Training: No TrainingCPTRunner found for island '{islandId}'. Showing focus screen anyway.");
                    }

                    GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);
                }
            }
            while (replayTrainingRequested);
        }

        TrainingCPTRunner.ForceStopAll();

        // make sure we are actually in CPT
        if (GameManager.Instance && GameManager.Instance.State != GameManager.GameState.CPT)
        {
            var mgr = MoxoCPTManager.Instance ??
                      FindObjectsOfType<MoxoCPTManager>(true)
                          .FirstOrDefault(x => x && x.gameObject.activeInHierarchy);

            if (mgr != null)
            {
                Debug.Log("[IslandTravel] Ready intro closed but state is not CPT → calling MoxoCPTManager.OnGameBegin().");

                try
                {
                    mgr.OnGameBegin();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[IslandTravel] OnGameBegin threw: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[IslandTravel] No active MoxoCPTManager found to start the game.");
            }
        }

        // Nudge runner (safety)
        var runner = FindObjectsOfType<MoxoCPT.ChangeShapes>(true)
            .FirstOrDefault(r => r && r.isActiveAndEnabled && r.gameObject.activeInHierarchy);

        if (runner)
        {
            if (logVerbose)
                Debug.Log($"[IslandTravel] Nudge runner: {runner.gameObject.name} → ForceStart()");

            runner.ForceStart();
        }
        else
        {
            Debug.LogWarning("[IslandTravel] No active ChangeShapes found to nudge.");
        }

        StartCptWatchdog();
    }

    // ---------- CPT failsafe ----------

    private void StartCptWatchdog()
    {
        StopCptWatchdog();
        _cptWatchdog = StartCoroutine(CoCptWatchdog());
    }

    private void StopCptWatchdog()
    {
        if (_cptWatchdog != null)
            StopCoroutine(_cptWatchdog);

        _cptWatchdog = null;
    }

    private IEnumerator CoCptWatchdog()
    {
        while (GameManager.Instance && GameManager.Instance.State != GameManager.GameState.CPT)
            yield return null;

        if (logVerbose)
            Debug.Log("[IslandTravel] CPT watchdog armed.");

        var mgr = MoxoCPTManager.Instance;

        while (GameManager.Instance && GameManager.Instance.State == GameManager.GameState.CPT)
        {
            bool anyRunner = FindObjectsOfType<MoxoCPT.ChangeShapes>(true)
                .Any(r => r && r.gameObject.activeInHierarchy);

            if (!anyRunner)
            {
                if (logVerbose)
                    Debug.LogWarning("[IslandTravel] Watchdog: runner missing while in CPT → forcing OnGameEnd().");

                mgr?.OnGameEnd();
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    // ---------- Helpers for non-MOXO ----------

    private void CleanupLastNonMoxoHandler()
    {
        if (!_lastNonMoxoHandler) return;

        InvokeIfExists(_lastNonMoxoHandler, "Cleanup");
        InvokeIfExists(_lastNonMoxoHandler, "StopDialogue");
        InvokeIfExists(_lastNonMoxoHandler, "End");

        _lastNonMoxoHandler = null;
    }

    private MonoBehaviour FindHandlerUnderIslandRig(string islandId, string typeName)
    {
        islandId = (islandId ?? "").Trim().ToUpperInvariant();

        var group = FindObjectsOfType<IslandMoxoGroup>(true)
            .FirstOrDefault(g =>
                g && string.Equals(g.islandId?.Trim(), islandId, StringComparison.OrdinalIgnoreCase));

        var t = FindTypeAnywhere(typeName);
        if (t == null) return null;

        if (group && group.moxoRoot)
        {
            var mb = group.moxoRoot.GetComponentsInChildren(t, true)
                .FirstOrDefault() as MonoBehaviour;

            if (mb) return mb;
        }

        return FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(mb => mb && t.IsAssignableFrom(mb.GetType()));
    }

    private static bool InvokeIfExists(MonoBehaviour target, string method)
    {
        if (!target) return false;

        var mi = target.GetType().GetMethod(
            method,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (mi == null) return false;

        mi.Invoke(target, null);
        return true;
    }

    private static Type FindTypeAnywhere(string shortName)
    {
        if (string.IsNullOrWhiteSpace(shortName))
            return null;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(shortName, false);
            if (t != null) return t;

            try
            {
                t = asm.GetTypes().FirstOrDefault(tp => tp.Name == shortName);
                if (t != null) return t;
            }
            catch
            {
                // ignored (some assemblies throw on GetTypes)
            }
        }

        return null;
    }

    private void StartNonMoxoDialogueAfterIntro(IslandData island, string islandId)
    {
        if (island == null) return;

        if (island.startsDialogueOnArrival &&
            !string.IsNullOrWhiteSpace(island.dialogueHandlerType))
        {
            _lastNonMoxoHandler = FindHandlerUnderIslandRig(islandId, island.dialogueHandlerType);

            if (_lastNonMoxoHandler)
            {
                InvokeIfExists(_lastNonMoxoHandler, "Cleanup");

                if (!InvokeIfExists(_lastNonMoxoHandler, "StartAfterIntro"))
                {
                    if (!InvokeIfExists(_lastNonMoxoHandler, "StartDialogue"))
                        InvokeIfExists(_lastNonMoxoHandler, "StartTalking");
                }

                if (logVerbose)
                    Debug.Log($"[IslandTravel] Started non-MOXO handler '{island.dialogueHandlerType}'.");
            }
            else
            {
                Debug.LogWarning(
                    $"[IslandTravel] Non-MOXO handler '{island.dialogueHandlerType}' not found for '{islandId}'."
                );
            }
        }
    }

    // ---------- Trainer selection (STRICT) ----------

    private TrainingCPTRunner FindTrainerForIsland(string islandId)
    {
        islandId = (islandId ?? "").Trim().ToUpperInvariant();

        var group = FindObjectsOfType<IslandMoxoGroup>(true)
            .FirstOrDefault(g =>
                g && string.Equals(g.islandId?.Trim(), islandId, StringComparison.OrdinalIgnoreCase));

        Transform rigRoot = (group && group.moxoRoot) ? group.moxoRoot.transform : null;

        var trainers = FindObjectsOfType<TrainingCPTRunner>(true);

        // Priority 1: explicit tag match
        var tagged = trainers.Where(t => t && TrainerBelongsToIsland(t, islandId)).ToList();

        if (tagged.Count == 1)
        {
            if (logVerbose) Debug.Log($"[IslandTravel] Trainer (tag match) → {tagged[0].name}");
            return tagged[0];
        }

        if (tagged.Count > 1 && rigRoot)
        {
            var underRig = tagged.FirstOrDefault(t => t.transform.IsChildOf(rigRoot));
            if (underRig)
            {
                if (logVerbose) Debug.Log($"[IslandTravel] Trainer (tag+rig) → {underRig.name}");
                return underRig;
            }

            if (logVerbose)
                Debug.LogWarning($"[IslandTravel] Multiple tagged trainers for '{islandId}'. Choosing the first.");

            return tagged[0];
        }

        // Priority 2: hierarchy under active rig
        if (rigRoot)
        {
            var childTrainer = trainers.FirstOrDefault(t => t && t.transform.IsChildOf(rigRoot));
            if (childTrainer)
            {
                if (logVerbose) Debug.Log($"[IslandTravel] Trainer (under rig) → {childTrainer.name}");
                return childTrainer;
            }
        }

        if (logVerbose)
            Debug.LogWarning($"[IslandTravel] No strict trainer found for '{islandId}'.");

        return null;
    }

    private static bool TrainerBelongsToIsland(TrainingCPTRunner t, string islandId)
    {
        var mi = typeof(TrainingCPTRunner).GetMethod(
            "BelongsToIsland",
            BindingFlags.Public | BindingFlags.Instance);

        if (mi == null) return false;

        try
        {
            return (bool)mi.Invoke(t, new object[] { islandId });
        }
        catch
        {
            return false;
        }
    }

    // ---------- UI helpers ----------

    private static bool TryGetCanvasGroup(GameObject go, out CanvasGroup cg)
    {
        cg = go ? go.GetComponent<CanvasGroup>() : null;
        return cg != null;
    }

    private IntroScreen PlaceIntroScreenForIsland(string islandId, Transform playerRoot)
    {
        var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
        if (!intro)
        {
            Debug.LogWarning("[IslandTravel] IntroScreen not found.");
            return null;
        }

        _introAnchorById.TryGetValue(islandId, out var anchor);
        var target = anchor ? anchor : playerRoot;

        var t = intro.transform;
        t.position = target.position;
        t.rotation = target.rotation;

        if (introFacePlayer && playerRoot)
        {
            t.LookAt(playerRoot.position, Vector3.up);
            t.Rotate(0f, 180f, 0f, Space.Self);
        }

        var canvas = intro.GetComponentInChildren<Canvas>(true);
        if (canvas && canvas.renderMode == RenderMode.WorldSpace)
        {
            var cam = playerRoot.GetComponentInChildren<Camera>(true);
            if (cam) canvas.worldCamera = cam;

            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, introCanvasSortingOrder);
        }

        var cg = intro.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        // Pass per-island character and results config (null/empty = fall back to shared defaults).
        var introAnchor = anchor ? anchor.GetComponent<IntroAnchor>() : null;
        intro.SetLocalCharacter(introAnchor ? introAnchor.localCharacter : null);
        intro.SetIslandResultsConfig(
            introAnchor ? introAnchor.resultsVoice        : null,
            introAnchor ? introAnchor.resultsTitle        : null,
            introAnchor ? introAnchor.resultsBodyTemplate : null
        );

        if (logVerbose)
            Debug.Log($"[IslandTravel] Intro placed for '{islandId}'.");

        return intro;
    }

    private MonoBehaviour PlaceCountdownForIsland(string islandId, Transform playerRoot)
    {
        MonoBehaviour countdown = countdownUIOverride;

        if (!countdown)
        {
            foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
            {
                if (mb && mb.GetType().Name == "ExploreHintUI")
                {
                    countdown = mb;
                    break;
                }
            }
        }

        if (!countdown)
        {
            if (logVerbose)
                Debug.Log("[IslandTravel] No ExploreHintUI found.");

            return null;
        }

        var t = countdown.transform;

        if (reparentCountdownToRuntimeRoot)
        {
            var newParent = uiRuntimeRoot ? uiRuntimeRoot : playerRoot;
            if (t.parent != newParent)
                t.SetParent(newParent, true);
        }

        _countdownAnchorById.TryGetValue(islandId, out var anchor);
        if (!anchor) _introAnchorById.TryGetValue(islandId, out anchor);
        if (!anchor) anchor = playerRoot;

        t.position = anchor.position;
        t.rotation = anchor.rotation;

        if (countdownFacePlayer && playerRoot)
        {
            t.LookAt(playerRoot.position, Vector3.up);
            t.Rotate(0f, 180f, 0f, Space.Self);
        }

        var canvas = countdown.GetComponentInChildren<Canvas>(true);
        if (canvas && canvas.renderMode == RenderMode.WorldSpace)
        {
            var cam = playerRoot.GetComponentInChildren<Camera>(true);
            if (!cam && Camera.main) cam = Camera.main;
            if (cam) canvas.worldCamera = cam;

            if (canvas.sortingOrder < countdownCanvasSortingOrder)
                canvas.sortingOrder = countdownCanvasSortingOrder;
        }

        var cg = countdown.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        if (!countdown.gameObject.activeSelf)
            countdown.gameObject.SetActive(true);

        if (logVerbose)
            Debug.Log($"[IslandTravel] Countdown placed for '{islandId}' via {countdown.GetType().FullName}.");

        return countdown;
    }

    public void PlacePickerForIsland(string islandId)
    {
        var playerRoot = GetPlayerRoot();
        if (!playerRoot) return;

        var pickerCanvas =
            pickerCanvasOverride ??
            FindObjectsOfType<Canvas>(true)
                .FirstOrDefault(c => c.name.IndexOf("Picker", StringComparison.OrdinalIgnoreCase) >= 0);

        if (!pickerCanvas)
        {
            if (logVerbose) Debug.Log("[IslandTravel] No Picker canvas found.");
            return;
        }

        if (pickerCanvas.renderMode != RenderMode.WorldSpace)
        {
            pickerCanvas.renderMode = RenderMode.WorldSpace;
            if (logVerbose) Debug.Log("[IslandTravel] Picker renderMode forced to WorldSpace.");
        }

        var t = pickerCanvas.transform as RectTransform;
        // Only reparent when we have an explicit uiRuntimeRoot. Do NOT reparent to playerRoot:
        // reparenting to the player can put the canvas under a transform with non-1 scale or
        // a moving hierarchy, making the picker invisible or misplaced.
        if (reparentPickerToRuntimeRoot && uiRuntimeRoot != null && t.parent != uiRuntimeRoot)
        {
            t.SetParent(uiRuntimeRoot, false);
            t.localScale = Vector3.one;
            t.anchoredPosition3D = Vector3.zero;
            t.localRotation = Quaternion.identity;
        }

        _pickerAnchorById.TryGetValue(islandId, out var anchor);
        var target = anchor ? anchor : playerRoot;

        t.position = target.position;
        t.rotation = target.rotation;

        if (pickerFacePlayer && playerRoot)
        {
            t.LookAt(playerRoot.position, Vector3.up);
            t.Rotate(0f, 180f, 0f, Space.Self);
        }

        var cam = playerRoot.GetComponentInChildren<Camera>(true) ?? Camera.main;
        if (cam) pickerCanvas.worldCamera = cam;

        // Do not set pickerCanvas.sortingOrder here — leave the canvas at its prefab value (e.g. 0).
        // Forcing a high value (e.g. 130) makes the picker invisible after unpause with some camera setups.

        var cg = pickerCanvas.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        if (!pickerCanvas.gameObject.activeSelf)
            pickerCanvas.gameObject.SetActive(true);

        if (logVerbose)
            Debug.Log($"[IslandTravel] Positioned Picker '{pickerCanvas.name}' for '{islandId}' (reparent skipped when no uiRuntimeRoot).");
    }

    public void PlacePickerForCurrentIsland()
    {
        var id = (CurrentIsland?.islandId ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(id)) return;
        PlacePickerForIsland(id);
    }

    private Transform GetPlayerRoot()
    {
        if (playerRootOverride) return playerRootOverride;
        if (modeManager && modeManager.CurrentPlayerRoot) return modeManager.CurrentPlayerRoot;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged) return tagged.transform;

        // Desktop_Rig is the moveable root (parent of the Player child).
        // "Player" intentionally comes AFTER Desktop_Rig: GameObject.Find("Player") would
        // otherwise return the Player child object, not the rig root, causing teleports to
        // move the child in local-space instead of the whole rig.
        // VR_Player / XR names are at the end; they are inactive and skipped by GameObject.Find anyway.
        string[] common =
        {
            "Desktop_Rig", "PlayerRoot", "Player",
            "XR Origin", "XROrigin", "XR Rig", "VR_Player"
        };

        foreach (var n in common)
        {
            var go = GameObject.Find(n);
            if (go) return go.transform;
        }

        var scene = SceneManager.GetActiveScene();
        // Only consider active root objects — disabled XR rigs must not shadow the desktop player.
        foreach (var r in scene.GetRootGameObjects())
        {
            if (!r.activeInHierarchy) continue;
            if (r.name.Contains("XR") || r.name.Contains("Player"))
                return r.transform;
        }

        if (Camera.main) return Camera.main.transform.root;
        return null;
    }

    // ---------- Debug / Convenience ----------

    [ContextMenu("Debug/Clear ALL training (session)")]
    private void CM_ClearAllTraining()
    {
        var mi = typeof(TrainingCPTRunner).GetMethod(
            "ClearAllTrainingProgress",
            BindingFlags.Public | BindingFlags.Static);

        if (mi != null)
            mi.Invoke(null, null);
        else
            Debug.LogWarning("[IslandTravel] ClearAllTrainingProgress() not found on TrainingCPTRunner.");
    }

    [ContextMenu("Debug/Clear CURRENT island training (session)")]
    private void CM_ClearCurrentIslandTraining()
    {
        var id = (CurrentIsland?.islandId ?? "").Trim().ToUpperInvariant();

        if (!string.IsNullOrEmpty(id))
        {
            TrainingCPTRunner.ClearCompletedForIsland(id);
            Debug.Log($"[IslandTravel] Cleared training completion for '{id}'.");
        }
        else
        {
            Debug.LogWarning("[IslandTravel] No current island id to clear.");
        }
    }
}
