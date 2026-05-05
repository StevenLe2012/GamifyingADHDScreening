using UnityEngine;

public class XRIntroAnchor : MonoBehaviour
{
    [Header("Solo testing toggle")]
    [SerializeField] private bool enable = true;

    [Header("Placement")]
    [SerializeField] private float distance = 1.5f;
    [SerializeField] private float heightOffset = -0.1f;
    [SerializeField] private bool followHeadYawOnly = true;

    private void OnEnable()
    {
        if (!enable) return;

        var cam = Camera.main;
        if (!cam)
        {
            Debug.LogWarning("[XRIntroAnchor] Camera.main not found.");
            return;
        }

        // position in front of the HMD
        transform.position = cam.transform.position + cam.transform.forward * distance + Vector3.up * heightOffset;

        // rotate to face the HMD (usually yaw-only feels best)
        Vector3 fwd = cam.transform.forward;
        if (followHeadYawOnly) fwd.y = 0f;

        if (fwd.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
    }
}
