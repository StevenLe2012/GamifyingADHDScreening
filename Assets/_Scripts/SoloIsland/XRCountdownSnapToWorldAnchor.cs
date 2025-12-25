using UnityEngine;

public class XRCountdownSnapToWorldAnchor : MonoBehaviour
{
    [Header("Solo testing only")]
    [SerializeField] private bool enable = true;

    [Header("Anchor lookup")]
    [SerializeField] private string anchorName = "CountdownAnchorPoint";

    [Header("Rotation")]
    [SerializeField] private bool useAnchorRotation = true;
    [SerializeField] private bool faceCameraInstead = false;

    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Quaternion _originalLocalRot;
    private Vector3 _originalLocalScale;
    private bool _cached;

    public void SnapToAnchor()
    {
        if (!enable) return;

        CacheOriginal();

        var anchorObj = GameObject.Find(anchorName);
        if (anchorObj == null)
        {
            Debug.LogWarning($"[XRCountdownSnapToWorldAnchor] Anchor '{anchorName}' not found (is CardIsland_Solo loaded yet?).");
            return;
        }

        var anchor = anchorObj.transform;

        transform.position = anchor.position;

        if (faceCameraInstead && Camera.main != null)
        {
            Vector3 dir = Camera.main.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
        else if (useAnchorRotation)
        {
            transform.rotation = anchor.rotation;
        }
    }

    public void RestoreOriginal()
    {
        if (!_cached) return;

        transform.SetParent(_originalParent, false);
        transform.localPosition = _originalLocalPos;
        transform.localRotation = _originalLocalRot;
        transform.localScale    = _originalLocalScale;
    }

    private void CacheOriginal()
    {
        if (_cached) return;

        _originalParent     = transform.parent;
        _originalLocalPos   = transform.localPosition;
        _originalLocalRot   = transform.localRotation;
        _originalLocalScale = transform.localScale;
        _cached = true;
    }
}
