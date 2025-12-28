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

    [Header("Debug")]
    [SerializeField] private bool logVerbose = true;

    private readonly Dictionary<string, Transform> _anchorById = new();
    public IslandData CurrentIsland { get; private set; }

    // ---------- LIFECYCLE ----------
    private void Awake()
    {
        I = this;

        if (!fader)
            fader = GameObject.FindGameObjectWithTag("Fader")?.GetComponent<FadeScreen>();

        RebuildAnchorCache();
        StartCoroutine(LateBindPlayerRoot());
    }

    private IEnumerator LateBindPlayerRoot()
    {
        // Give PlayerModeManager a frame to initialize/flip rigs.
        yield return null;

        var root = GetPlayerRoot();
        if (!root)
        {
            Debug.LogError("[IslandTravel] No player root found. Assign PlayerModeManager or PlayerRootOverride in the inspector.");
        }
        else
        {
            if (logVerbose)
                Debug.Log($"[IslandTravel] Player root bound → '{root.name}' @ {root.position}");
        }
    }

    // ---------- PUBLIC API ----------
    public void RebuildAnchorCache()
    {
        _anchorById.Clear();
        var found = FindObjectsOfType<IslandAnchor>(true);

        foreach (var a in found)
        {
            if (string.IsNullOrWhiteSpace(a.islandId)) continue;
            var key = a.islandId.Trim().ToUpperInvariant();
            _anchorById[key] = a.transform;
        }

        if (logVerbose)
            Debug.Log($"[IslandTravel] Found {_anchorById.Count} island anchors.");
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
            Debug.LogError($"[IslandTravel] No anchor found for id '{id}'. Call RebuildAnchorCache() or verify IslandAnchor components exist.");
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

        if (island.startsMoxoOnArrival)
        {
            var intro = FindObjectOfType<IntroScreen>(true);
            if (intro) intro.ShowMoxo();
            else MoxoCPT.MoxoCPTManager.Instance?.OnGameBegin();
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

        // 5) Fallback: scan scene root objects to find something likely
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name.Contains("XR", System.StringComparison.OrdinalIgnoreCase) ||
                root.name.Contains("Player", System.StringComparison.OrdinalIgnoreCase))
                return root.transform;
        }

        // 6) Last resort: main camera’s root
        if (Camera.main) return Camera.main.transform.root;

        // If we get here, we truly have nothing
        return null;
    }
}
