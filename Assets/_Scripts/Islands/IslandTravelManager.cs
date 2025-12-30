// IslandTravelManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandTravelManager : MonoBehaviour
{
    public static IslandTravelManager I { get; private set; }

    [Header("Wiring")]
    [SerializeField] private FadeScreen fader;

    [Tooltip("If set, this is the transform that will be moved (XR Origin or Desktop root).")]
    [SerializeField] private Transform playerRootOverride;

    [Tooltip("Optional. If you use PlayerModeManager to toggle Desktop/VR, assign it here.")]
    [SerializeField] private PlayerModeManager modeManager;

    [Header("Intro (World Space)")]
    [Tooltip("Force the IntroScreen canvas to use the active rig camera and a high sorting order.")]
    [SerializeField] private int introCanvasSortingOrder = 100;
    [Tooltip("Rotate the IntroScreen to face the player root if true. If false, it uses the IntroAnchor rotation.")]
    [SerializeField] private bool introFacePlayer = false;

    [Header("Debug")]
    [SerializeField] private bool logVerbose = true;

    // Teleport anchors (where the player goes)
    private readonly Dictionary<string, Transform> _anchorById = new();
    // Intro anchors (where the world-space intro panel is placed)
    private readonly Dictionary<string, Transform> _introAnchorById = new();

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

        if (logVerbose)
            Debug.Log($"[IslandTravel] Found {_anchorById.Count} island anchors, {_introAnchorById.Count} intro anchors.");
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

        // Enter MOXO flow
        var gm = GameManager.Instance;
        if (gm) gm.UpdateGameState(GameManager.GameState.PrepareCPT);

        // Try to place/show the world-space intro
        var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
        if (!intro)
        {
            Debug.LogWarning("[IslandTravel] IntroScreen not found in scene.");
            if (island.startsMoxoOnArrival)
                MoxoCPT.MoxoCPTManager.Instance?.OnGameBegin();
            yield break;
        }

        // Position the intro in world space for this island
        PlaceIntroScreenForIsland(id, root, intro);

        // Use island-specific copy (fallback to preset if fields are empty)
        if (island.startsMoxoOnArrival)
        {
            var title = string.IsNullOrWhiteSpace(island.introTitle) ? null : island.introTitle;
            var body  = string.IsNullOrWhiteSpace(island.introBody)  ? null : island.introBody;

            intro.ShowMoxo(title, body);

            if (logVerbose)
                Debug.Log($"[IslandTravel] Showed IntroScreen for island '{island.displayName}' with title='{title ?? "(preset)"}'.");
        }
    }

    // ---------- INTRO PLACEMENT ----------
    private void PlaceIntroScreenForIsland(string islandId, Transform playerRoot, IntroScreen intro)
    {
        // Pick anchor; fall back to player if missing
        _introAnchorById.TryGetValue(islandId, out var anchor);
        var target = anchor ? anchor : playerRoot;

        // Move/rotate the world-space UI
        var t = intro.transform;
        t.position = target.position;
        t.rotation = target.rotation;

        if (introFacePlayer && playerRoot)
        {
            // Face the player while keeping 'up' world-up
            Vector3 lookPos = playerRoot.position;
            t.LookAt(lookPos, Vector3.up);
            t.Rotate(0f, 180f, 0f, Space.Self); // so canvas front faces player
        }

        // Ensure world-space canvas uses the active rig camera and sorts on top
        var canvas = intro.GetComponentInChildren<Canvas>(true);
        if (canvas && canvas.renderMode == RenderMode.WorldSpace)
        {
            var cam = playerRoot.GetComponentInChildren<Camera>(true);
            if (cam) canvas.worldCamera = cam;

            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, introCanvasSortingOrder);
        }

        // Make sure it can receive input when shown
        var cg = intro.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.blocksRaycasts = true;
            cg.interactable   = true;
        }

        if (logVerbose)
        {
            Debug.Log($"[IslandTravel] Positioned IntroScreen at {(anchor ? "IntroAnchor" : "PlayerRoot")} for '{islandId}'. " +
                      $"Canvas worldCamera={(canvas ? (canvas.worldCamera ? canvas.worldCamera.name : "null") : "n/a")}");
        }
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
