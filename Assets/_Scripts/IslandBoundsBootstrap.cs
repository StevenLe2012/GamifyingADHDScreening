using UnityEngine;

public class IslandBoundsBootstrap : MonoBehaviour
{
    [Header("Set the limiter and the island you start on")]
    [SerializeField] private IslandEdgeLimiter limiter;   // your single limiter
    [SerializeField] private BoxCollider startBounds;     // the island you spawn on

    private void Start()
    {
        if (limiter == null || startBounds == null)
        {
            Debug.LogWarning("[Bootstrap] Missing references. Not setting start bounds.");
            return;
        }

        // If your XR rig or CharacterController enables a frame later,
        // a tiny delay makes sure player/controller are ready.
        StartCoroutine(CoSetNextFrame());
    }

    private System.Collections.IEnumerator CoSetNextFrame()
    {
        yield return null; // wait one frame
        limiter.SetBounds(startBounds);
        Debug.Log($"[Bootstrap] Start bounds set to {startBounds.name}");
    }
}
