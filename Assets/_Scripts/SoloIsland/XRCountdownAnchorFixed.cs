using UnityEngine;

public class XRCountdownAnchorFixed : MonoBehaviour
{
    [Header("Enable")]
    [SerializeField] private bool enable = true;

    [Header("Reference (assign XR Origin)")]
    [SerializeField] private Transform reference; // drag XR Origin here

    [Header("Offset in reference local space")]
    [Tooltip("X=right, Y=up, Z=forward relative to XR Origin")]
    [SerializeField] private Vector3 localOffset = new Vector3(0.5f, -0.2f, 1.3f);

    [Header("Rotation")]
    [SerializeField] private bool faceReferenceYawOnly = true;

    public void Snap()
    {
        if (!enable) return;

        if (reference == null)
        {
            Debug.LogWarning("[XRCountdownAnchorFixed] Reference is null. Assign XR Origin.");
            return;
        }

        // Place relative to XR Origin (stable)
        transform.position = reference.position + reference.TransformVector(localOffset);

        // Face the reference (player) in yaw only
        Vector3 dir = reference.position - transform.position;
        if (faceReferenceYawOnly) dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private void OnEnable() => Snap();
}
