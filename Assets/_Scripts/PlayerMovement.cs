using Unity.XR.CoreUtils;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _minHeight = 1.2f;
    [SerializeField] private float _maxHeight = 2.0f;
    [SerializeField] private float _moveSpeed = 3f;        // NEW
    [SerializeField] private float _gravity = -9.81f;      // NEW

    private XROrigin _xrOrigin;
    private CharacterController _collider;

    private Vector3 _velocity; // NEW

    private void Awake()
    {
        _xrOrigin = GetComponent<XROrigin>();
        _collider = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Keep capsule synced to headset
        _collider.height = Mathf.Clamp(_xrOrigin.CameraInOriginSpaceHeight, _minHeight, _maxHeight);

        Vector3 center = _xrOrigin.CameraInOriginSpacePos;
        _collider.center = new Vector3(center.x, _collider.height / 2, center.z);

        // --- NEW: Keyboard movement ---
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h = 1f;

        // Move relative to the headset/camera facing direction
        Vector3 forward = _xrOrigin.Camera.transform.forward;
        Vector3 right = _xrOrigin.Camera.transform.right;

        forward.y = 0; // keep movement flat
        right.y = 0;

        Vector3 move = (forward.normalized * v + right.normalized * h) * _moveSpeed;

        _collider.Move(move * Time.deltaTime);

        // Apply simple gravity
        if (_collider.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        _velocity.y += _gravity * Time.deltaTime;
        _collider.Move(_velocity * Time.deltaTime);
    }
}
