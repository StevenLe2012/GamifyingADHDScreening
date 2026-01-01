// IslandTravelManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoxoCPT; // for ExploreHintUI

public class IslandTravelManager : MonoBehaviour
{
    public static IslandTravelManager I { get; private set; }

    [Header("Wiring")]
    [SerializeField] private FadeScreen fader;

    [Tooltip("If set, this transform will be moved (XR Origin or Desktop root).")]
    [SerializeField] private Transform playerRootOverride;

    [Tooltip("Optional. If you use PlayerModeManager to toggle Desktop/VR, assign it here.")]
    [SerializeField] private PlayerModeManager modeManager;

    [Header("Intro (World Space)")]
    [Tooltip("Sorting order for the world-space Intro canvas.")]
    [SerializeField] private int introCanvasSortingOrder = 100;
    [Tooltip("Rotate the Intro panel to face the player root. If false, use IntroAnchor rotation.")]
    [SerializeField] private bool introFacePlayer = false;

    [Header("Countdown (World Space)")]
    [Tooltip("Sorting order for the world-space Countdown canvas.")]
    [SerializeField] private int countdownCanvasSortingOrder = 120;
    [Tooltip("Rotate the Countdown panel to face the player root. If false, use CountdownAnchor rotation.")]
    [SerializeField] private bool countdownFacePlayer = false;

    // Point directly at your countdown if you want (else we’ll auto-find)
    //[SerializeField] private MoxoCPT.ExploreHintUI countdownUIOverride = null;
    [SerializeField] private ExploreHintUI countdownUIOverride = null;

    // Where to re-parent world-space UIs so island hierarchies don’t affect them
    [SerializeField] private Transform uiRuntimeRoot = null;
    [SerializeField] private bool reparentCountdownToRuntimeRoot = true;


    [Header("Debug")]
    [SerializeField] private bool logVerbose = true;

    // Teleport anchors (where the player goes)
    private readonly Dictionary<string, Transform> _anchorById = new();
    // Intro anchors (where the world-space intro panel is placed)
    private readonly Dictionary<string, Transform> _introAnchorById = new();
    // Countdown anchors (where the world-space countdown panel is placed)
    private readonly Dictionary<string, Transform> _countdownAnchorById = new();

    public IslandData CurrentIsland { get; private set; }

    // ---------- LIFECYCLE ----------
    private void Awake()
    {
        I = this;

        if (!fader)
            fader = GameObject.FindGameObjectWithTag("Fader")?.GetComponent<FadeScreen>();

        RebuildAnchorCache();

        // Give rigs/PlayerModeManager one frame to settle before we resolve the root
        StartCoroutine(LateBindPlayerRoot());
    }

    private IEnumerator LateBindPlayerRoot()
    {
        yield return null; // let other Awake/Start run
        var root = GetPlayerRoot();
        if (!root)
        {
            Debug.LogError("[IslandTravel] No player root found. Assign PlayerModeManager or PlayerRootOverride in the inspector.");
        }
        else if (logVerbose)
        {
            Debug.Log($"[IslandTravel] Player root bound → '{root.name}' @ {root.position}");
        }
    }

    // ---------- PUBLIC API ----------
    public void RebuildAnchorCache()
    {
        _anchorById.Clear();
        _introAnchorById.Clear();
        _countdownAnchorById.Clear();

        // Teleport destinations
        foreach (var a in FindObjectsOfType<IslandAnchor>(true))
        {
            if (string.IsNullOrWhiteSpace(a.islandId)) continue;
            var key = a.islandId.Trim().ToUpperInvariant();
            _anchorById[key] = a.transform;
        }

        // Intro (UI) anchors
        foreach (var ia in FindObjectsOfType<IntroAnchor>(true))
        {
            if (string.IsNullOrWhiteSpace(ia.islandId)) continue;
            var key = ia.islandId.Trim().ToUpperInvariant();
            _introAnchorById[key] = ia.transform;
        }

        // Countdown (UI) anchors
        foreach (var ca in FindObjectsOfType<CountdownAnchor>(true))
        {
            if (string.IsNullOrWhiteSpace(ca.islandId)) continue;
            var key = ca.islandId.Trim().ToUpperInvariant();
            _countdownAnchorById[key] = ca.transform;
        }

        if (logVerbose)
            Debug.Log($"[IslandTravel] Found {_anchorById.Count} island anchors, {_introAnchorById.Count} intro anchors, {_countdownAnchorById.Count} countdown anchors.");
    }

    public void TravelTo(IslandData island)
    {
        if (island == null)
        {
            Debug.LogError("[IslandTravel] TravelTo called with null IslandData.");
            return;
        }
        CurrentIsland = island;
        StartCoroutine(CoTravel(island));
    }

    // ---------- CORE ----------
    private IEnumerator CoTravel(IslandData island)
    {
        var root = GetPlayerRoot();
        if (!root)
        {
            Debug.LogError("[IslandTravel] Cannot travel: player root is null.");
            yield break;
        }

        var id = island.islandId?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[IslandTravel] IslandData.islandId is empty.");
            yield break;
        }

        if (!_anchorById.TryGetValue(id, out var dest) || !dest)
        {
            Debug.LogError($"[IslandTravel] No teleport anchor found for id '{id}'. Call RebuildAnchorCache() or verify IslandAnchor components exist.");
            yield break;
        }

        // Optional fade
        if (fader) fader.TeleportFade();
        yield return new WaitForSeconds(0.2f);

        // Disable CC while warping
        var cc = root.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        root.position = dest.position;
        root.rotation = dest.rotation;

        if (cc) cc.enabled = true;

        if (logVerbose)
            Debug.Log($"[IslandTravel] Moved '{root.name}' to {island.displayName} ({id}) @ {dest.position}");

        // State: we’re preparing to start an island’s task
        var gm = GameManager.Instance;
        if (gm) gm.UpdateGameState(GameManager.GameState.PrepareCPT);

        // Place COUNTDOWN (world-space)
        PlaceCountdownForIsland(id, root);

        // Place/Show INTRO (world-space)
        var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
        if (!intro)
        {
            Debug.LogWarning("[IslandTravel] IntroScreen not found in scene.");
            if (island.startsMoxoOnArrival)
                MoxoCPT.MoxoCPTManager.Instance?.OnGameBegin();
            yield break;
        }

        PlaceIntroScreenForIsland(id, root, intro);

        // Island-specific content (requires extra fields on IslandData; see note below)
        if (island.countdownSeconds > 0f)
        {
            var changer = FindObjectOfType<MoxoCPT.ChangeShapes>(true);
            if (changer) changer.SetCountdown(island.countdownSeconds);
        }

        if (island.startsMoxoOnArrival)
        {
            var title = string.IsNullOrWhiteSpace(island.introTitle) ? null : island.introTitle;
            var body  = string.IsNullOrWhiteSpace(island.introBody)  ? null : island.introBody;

            intro.ShowMoxo(title, body);

            if (logVerbose)
                Debug.Log($"[IslandTravel] Showed IntroScreen for island '{island.displayName}' with title='{title ?? "(preset)"}'.");
        }
    }

    // ---------- INTRO / COUNTDOWN PLACEMENT ----------
    private void PlaceIntroScreenForIsland(string islandId, Transform playerRoot, IntroScreen intro)
    {
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
        if (cg) { cg.blocksRaycasts = true; cg.interactable = true; }

        if (logVerbose)
        {
            Debug.Log($"[IslandTravel] Positioned IntroScreen at {(anchor ? "IntroAnchor" : "PlayerRoot")} for '{islandId}'. " +
                      $"Canvas worldCamera={(canvas ? (canvas.worldCamera ? canvas.worldCamera.name : "null") : "n/a")}");
        }
    }
    
    private void PlaceCountdownForIsland(string islandId, Transform playerRoot)
    {
        // Prefer the serialized one; fallback to find the single instance
        var countdown = countdownUIOverride ? countdownUIOverride
                        : FindObjectOfType<ExploreHintUI>(true);

        // (Optional ultra-robust fallback if ExploreHintUI is in a different asmdef/namespace)
        if (!countdown)
        {
            // Try to find by name even if it lives in a different namespace
            foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
            {
                if (mb && mb.GetType().Name == "ExploreHintUI")
                {
                    countdown = (ExploreHintUI)mb; // will be null-cast if truly different type
                    if (countdown) break;
                }
            }
        }

        if (!countdown)
        {
            if (logVerbose) Debug.Log("[IslandTravel] No ExploreHintUI found.");
            return;
        }

        // Move the ROOT transform so parents can't drag it back
        var t = countdown.transform;

        if (reparentCountdownToRuntimeRoot)
        {
            var newParent = uiRuntimeRoot ? uiRuntimeRoot : playerRoot;
            if (t.parent != newParent) t.SetParent(newParent, true);
        }

        // Choose anchor: CountdownAnchor → IntroAnchor → playerRoot
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
        if (cg) { cg.blocksRaycasts = true; cg.interactable = true; }

        if (!countdown.gameObject.activeSelf) countdown.gameObject.SetActive(true);

        if (logVerbose)
            Debug.Log($"[IslandTravel] Positioned Countdown '{countdown.name}' for '{islandId}', parent={(t.parent ? t.parent.name : "null")}.");
    }



    // ---------- HELPERS ----------
    private Transform GetPlayerRoot()
    {
        // 1) Explicit override wins
        if (playerRootOverride) return playerRootOverride;

        // 2) If you use the Desktop/VR toggle
        if (modeManager && modeManager.CurrentPlayerRoot) return modeManager.CurrentPlayerRoot;

        // 3) Try a GameObject tagged "Player"
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged) return tagged.transform;

        // 4) Try common rig names
        string[] commonNames = { "XR Origin", "XROrigin", "XR Rig", "PlayerRoot", "VR_Player", "Desktop_Rig", "Player" };
        foreach (var n in commonNames)
        {
            var go = GameObject.Find(n);
            if (go) return go.transform;
        }

        // 5) Fallback: scan scene roots for something likely
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var r in scene.GetRootGameObjects())
        {
            if (r.name.Contains("XR", System.StringComparison.OrdinalIgnoreCase) ||
                r.name.Contains("Player", System.StringComparison.OrdinalIgnoreCase))
                return r.transform;
        }

        // 6) Last resort: main camera’s root
        if (Camera.main) return Camera.main.transform.root;

        return null;
    }
}
