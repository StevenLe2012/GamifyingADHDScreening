using System;
using UnityEngine;
using Cinemachine;

public class MoxoStateCameraSwitch : MonoBehaviour
{
    public static MoxoStateCameraSwitch Instance { get; private set; }

    [Header("References")]
    [Tooltip("Fallback direct CM_Moxo camera control when no MoxoCameraDirector is present.")]
    [SerializeField] private CinemachineVirtualCamera cmMoxo;

    [Tooltip("The player's look script you want to disable during CPT.")]
    [SerializeField] private DesktopArrowController lookController;

    [Header("MOXO Anchor Source")]
    [Tooltip("If true, first try strict island-id anchor; fallback to nearest active anchor if none.")]
    [SerializeField] private bool autoFindAnchorFromIslandRig = true;

    [Tooltip("Max distance from player to consider a rig valid.")]
    [SerializeField] private float maxRigDistance = 60f;

    private bool _isInMoxoView;
    private MoxoCameraDirector _director;

    private void Awake()
    {
        Instance = this;

        // Auto-wire if not assigned
        if (!lookController) lookController = FindObjectOfType<DesktopArrowController>(true);
        _director = FindObjectOfType<MoxoCameraDirector>(true);

        // IMPORTANT: force startup to be Main Camera (MOXO cam off, look on)
        SafeExitMoxoView();
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += OnStateChanged;

        // Sync immediately to whatever state we start in
        if (GameManager.Instance != null)
            OnStateChanged(GameManager.Instance.State);
        else
            SafeExitMoxoView();
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnStateChanged;
        if (Instance == this) Instance = null;
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.PrepareCPT || state == GameManager.GameState.CPT)
            SafeEnterMoxoView();
        // Do not auto-exit on Narrative/Explore. Exit timing is controlled explicitly
        // by MOXO flow (e.g. delayed switch after narrative starts).
    }

    // Explicit hooks so other systems can switch at exact flow points.
    public void EnterMoxoViewNow() => SafeEnterMoxoView();
    public void ExitMoxoViewNow() => SafeExitMoxoView();

    private void SafeEnterMoxoView()
    {
        if (_isInMoxoView) return;

        // Prefer director so camera pose logic matches the original MOXO anchor behavior.
        if (_director)
        {
            Transform directorAnchor = autoFindAnchorFromIslandRig ? FindAnchorForCurrentIslandOrNearest() : null;
            if (directorAnchor != null)
                _director.EnterMoxoView(directorAnchor);
            else
                Debug.LogWarning("[MoxoStateCameraSwitch] No camera anchor found for EnterMoxoViewNow.");

            _isInMoxoView = true;
            Debug.Log("[MoxoStateCameraSwitch] ENTER MOXO VIEW (director).");
            return;
        }

        if (!cmMoxo)
        {
            Debug.LogWarning("[MoxoStateCameraSwitch] CM_Moxo reference missing (and no director found).");
            return;
        }

        // Pick anchor
        Transform anchor = null;

        if (autoFindAnchorFromIslandRig)
            anchor = FindAnchorForCurrentIslandOrNearest();

        if (anchor != null)
        {
            cmMoxo.Follow = anchor;
            cmMoxo.LookAt = anchor;
        }

        // Enable MOXO camera
        cmMoxo.gameObject.SetActive(true);

        // Disable look
        if (lookController) lookController.enabled = false;

        _isInMoxoView = true;
        Debug.Log("[MoxoStateCameraSwitch] ENTER MOXO VIEW (CPT).");
    }

    private void SafeExitMoxoView()
    {
        if (_director)
            _director.ExitMoxoView();

        if (cmMoxo) cmMoxo.gameObject.SetActive(false);
        if (lookController) lookController.enabled = true;

        _isInMoxoView = false;
        Debug.Log("[MoxoStateCameraSwitch] EXIT MOXO VIEW (not CPT).");
    }

    private Transform FindAnchorForCurrentIslandOrNearest()
    {
        string islandId = IslandTravelManager.I?.CurrentIsland?.islandId?.Trim();
        if (!string.IsNullOrEmpty(islandId))
        {
            var rigs = FindObjectsOfType<IslandCameraRig>(true);
            foreach (var r in rigs)
            {
                if (!r || !r.gameObject.activeInHierarchy || !r.moxoCameraAnchor) continue;
                if (string.Equals(r.islandId?.Trim(), islandId, StringComparison.OrdinalIgnoreCase))
                    return r.moxoCameraAnchor;
            }
        }

        return FindNearestActiveRigAnchor();
    }

    private Transform FindNearestActiveRigAnchor()
    {
        var rigs = FindObjectsOfType<IslandCameraRig>(true);
        if (rigs == null || rigs.Length == 0) return null;

        var player = lookController ? lookController.transform : transform;
        var p = player.position;

        float best = float.PositiveInfinity;
        Transform bestAnchor = null;

        foreach (var r in rigs)
        {
            if (!r || !r.gameObject.activeInHierarchy) continue;
            if (!r.moxoCameraAnchor) continue;

            float d = Vector3.Distance(p, r.moxoCameraAnchor.position);
            if (d < best)
            {
                best = d;
                bestAnchor = r.moxoCameraAnchor;
            }
        }

        if (bestAnchor != null && best <= Mathf.Max(0.1f, maxRigDistance))
            return bestAnchor;

        return null;
    }
}
