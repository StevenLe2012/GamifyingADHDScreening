using UnityEngine;

public class IslandEdgeLimiter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private CharacterController controller;
    [SerializeField] private BoxCollider bounds;

    [Header("Clamp Settings")]
    [SerializeField] private float margin = 0.75f;
    [SerializeField] private bool clampY = false;
    [SerializeField] private float minY = -999f, maxY = 999f;

    // NEW: don’t clamp until we’re told which island to use
    [Header("Init")]
    [SerializeField] private bool clampOnlyAfterSet = true; // turn ON to prevent early snap
    private bool _wasSetOnce = false;                       // set true in SetBounds()

    // --- Debug ---
    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    private void Log(string msg)
    {
        if (debugLog) Debug.Log($"[Limiter:{name}] {msg}", this);
    }

    public void SetBounds(BoxCollider newBounds)
    {
        bounds = newBounds;
        _wasSetOnce = true;   // NEW
        ClampNow();
    }

    public void ClampNow()
    {
        if (!player || !bounds)
        {
            return;
        }

        var b = bounds.bounds;
        var pos = player.position;
        var before = pos;

        pos.x = Mathf.Clamp(pos.x, b.min.x + margin, b.max.x - margin);
        pos.z = Mathf.Clamp(pos.z, b.min.z + margin, b.max.z - margin);
        if (clampY) pos.y = Mathf.Clamp(pos.y, minY, maxY);

        if (controller) controller.enabled = false;
        player.position = pos;
        if (controller) controller.enabled = true;
    }

    private void LateUpdate()
    {
        // NEW: if we require explicit SetBounds and it hasn’t happened yet, do nothing
        if (clampOnlyAfterSet && !_wasSetOnce)
        {
 
            return;
        }

        if (!player || !bounds)
        {
            return;
        }

        var b = bounds.bounds;
        var pos = player.position;
        var before = pos;

        pos.x = Mathf.Clamp(pos.x, b.min.x + margin, b.max.x - margin);
        pos.z = Mathf.Clamp(pos.z, b.min.z + margin, b.max.z - margin);
        if (clampY) pos.y = Mathf.Clamp(pos.y, minY, maxY);

        if (controller) controller.enabled = false;
        player.position = pos;
        if (controller) controller.enabled = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!bounds) return;
        var b = bounds.bounds;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.15f);
        Gizmos.DrawCube(b.center, b.size);

        var inner = new Bounds(b.center, new Vector3(
            Mathf.Max(0.01f, b.size.x - margin * 2f),
            b.size.y,
            Mathf.Max(0.01f, b.size.z - margin * 2f)
        ));
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
        Gizmos.DrawWireCube(inner.center, inner.size);
    }
#endif
}
