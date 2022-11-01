using Unity.XR.CoreUtils;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _minHeight;
    [SerializeField] private float _maxHeight;
    
    private XROrigin _xrOrigin;
    // private CapsuleCollider _collider;
    private CharacterController _collider;
    private void Awake()
    {
        _xrOrigin = GetComponent<XROrigin>();
        //_collider = GetComponentInChildren<CapsuleCollider>();
        _collider = GetComponent<CharacterController>();
    }

    private void Update()
    {
        _collider.height = Mathf.Clamp(_xrOrigin.CameraInOriginSpaceHeight, _minHeight, _maxHeight);
        
        Vector3 center = _xrOrigin.CameraInOriginSpacePos;
        _collider.center = new Vector3(center.x, _collider.height / 2, center.z);
    }
}
