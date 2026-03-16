using UnityEngine;
using Cinemachine;

public class MoxoStateCameraSwitch : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your MOXO Cinemachine Virtual Camera (CM_Moxo). Should be INACTIVE at startup.")]
    [SerializeField] private CinemachineVirtualCamera cmMoxo;

    [Tooltip("The player's look script you want to disable during CPT.")]
    [SerializeField] private DesktopArrowController lookController;

    [Header("MOXO Anchor Source")]
    [Tooltip("If true, we will find the nearest active IslandCameraRig with a moxoCameraAnchor when entering CPT.")]
    [SerializeField] private bool autoFindAnchorFromIslandRig = true;

    [Tooltip("Max distance from player to consider a rig valid.")]
    [SerializeField] private float maxRigDistance = 60f;

    private bool _isInMoxoView;

    private void Awake()
    {
        // Auto-wire if not assigned
        if (!lookController) lookController = FindObjectOfType<DesktopArrowController>(true);

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
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.CPT)
            SafeEnterMoxoView();
        else
            SafeExitMoxoView();
    }

    private void SafeEnterMoxoView()
    {
        if (_isInMoxoView) return;

        if (!cmMoxo)
        {
            Debug.LogWarning("[MoxoStateCameraSwitch] CM_Moxo reference missing.");
            return;
        }

        // Pick anchor
        Transform anchor = null;

        if (autoFindAnchorFromIslandRig)
            anchor = FindNearestActiveRigAnchor();

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
        if (cmMoxo) cmMoxo.gameObject.SetActive(false);
        if (lookController) lookController.enabled = true;

        _isInMoxoView = false;
        Debug.Log("[MoxoStateCameraSwitch] EXIT MOXO VIEW (not CPT).");
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
