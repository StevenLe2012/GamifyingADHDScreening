using UnityEngine;

[DefaultExecutionOrder(60)]
public class CountdownCameraBinder : MonoBehaviour
{
    [Header("Auto (leave empty to auto-find)")]
    [SerializeField] private Canvas worldSpaceCanvas;
    [SerializeField] private FollowCameraBillboard billboard;

    [Header("Behavior")]
    [Tooltip("If ON: in CPT, the countdown will move in front of the camera so it's always visible.")]
    [SerializeField] private bool followDuringCPT = true;

    [Tooltip("If ON: also update the Canvas.worldCamera to the active camera.")]
    [SerializeField] private bool bindCanvasCamera = true;

    private void Awake()
    {
        if (!worldSpaceCanvas) worldSpaceCanvas = GetComponentInChildren<Canvas>(true);
        if (!billboard) billboard = GetComponentInChildren<FollowCameraBillboard>(true);

        // Don't force-follow at startup; keep whatever IslandTravel placed.
        if (billboard) billboard.follow = false;

        BindNow();
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += OnStateChanged;

        // Sync immediately
        if (GameManager.Instance != null) OnStateChanged(GameManager.Instance.State);
        else OnStateChanged(GameManager.GameState.Explore);
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameManager.GameState s)
    {
        BindNow();

        bool inCpt = (s == GameManager.GameState.CPT);

        if (billboard)
            billboard.follow = (followDuringCPT && inCpt);

        // If we just entered CPT, snap in front immediately
        if (billboard && billboard.follow && inCpt)
            billboard.SnapNow();
    }

    private void BindNow()
    {
        var cam = Camera.main ? Camera.main : FindAnyCamera();
        if (!cam) return;

        if (billboard)
            billboard.cam = cam;

        if (bindCanvasCamera && worldSpaceCanvas && worldSpaceCanvas.renderMode == RenderMode.WorldSpace)
            worldSpaceCanvas.worldCamera = cam;
    }

    private Camera FindAnyCamera()
    {
        var cams = Object.FindObjectsOfType<Camera>();
        if (cams != null && cams.Length > 0) return cams[0];
        return null;
    }
}
