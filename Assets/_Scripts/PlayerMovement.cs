//using Unity.XR.CoreUtils;
//using UnityEngine;

//public class PlayerMovement : MonoBehaviour
//{
//    [SerializeField] private float _minHeight;
//    [SerializeField] private float _maxHeight;

//    private XROrigin _xrOrigin;
//    // private CapsuleCollider _collider;
//    private CharacterController _collider;
//    private void Awake()
//    {
//        _xrOrigin = GetComponent<XROrigin>();
//        //_collider = GetComponentInChildren<CapsuleCollider>();
//        _collider = GetComponent<CharacterController>();
//    }

//    private void Update()
//    {
//        _collider.height = Mathf.Clamp(_xrOrigin.CameraInOriginSpaceHeight, _minHeight, _maxHeight);

//        Vector3 center = _xrOrigin.CameraInOriginSpacePos;
//        _collider.center = new Vector3(center.x, _collider.height / 2, center.z);
//    }
//}

// //Hami's update: I'm trying to switch to keyboard movement

// using Unity.XR.CoreUtils;
// using UnityEngine;

// [RequireComponent(typeof(XROrigin))]
// [RequireComponent(typeof(CharacterController))]
// public class PlayerMovement : MonoBehaviour
// {
//     [Header("Capsule Height Clamp")]
//     [SerializeField] private float _minHeight = 1.0f;
//     [SerializeField] private float _maxHeight = 2.2f;
//     [SerializeField] private float capsuleRadius = 0.2f;

//     [Header("Locomotion (Arrows Only)")]
//     [SerializeField] private float moveSpeed = 1.5f;     // max speed (m/s) � comfort
//     [SerializeField] private float acceleration = 3.0f;  // m/s^2 ramp-up
//     [SerializeField] private float deceleration = 4.0f;  // m/s^2 ramp-down

//     [Header("Snap Turning")]
//     [SerializeField] private float snapAngle = 30f;      // degrees per snap
//     [SerializeField] private float snapCooldown = 0.25f; // seconds between snaps

//     [Header("Gravity")]
//     [SerializeField] private float gravity = -9.81f;

//     private XROrigin _xrOrigin;
//     private CharacterController _cc;

//     private float _currentSpeed = 0f;   // signed (forward/back)
//     private float _fallVelocity = 0f;   // vertical m/s
//     private float _lastSnapTime = -999f;

//     private void Awake()
//     {
//         _xrOrigin = GetComponent<XROrigin>();
//         _cc = GetComponent<CharacterController>();

//         // Reasonable defaults for VR movement capsule
//         _cc.enableOverlapRecovery = true;
//         _cc.skinWidth = 0.02f;
//         _cc.radius = capsuleRadius;
//     }

//     private void Update()
//     {
//         if (_xrOrigin == null || _cc == null) return;
//         var cam = _xrOrigin.Camera;
//         if (cam == null) return;

//         // 1) Keep capsule aligned to HMD height & center (in origin space)
//         float height = Mathf.Clamp(_xrOrigin.CameraInOriginSpaceHeight, _minHeight, _maxHeight);
//         _cc.height = height;
//         Vector3 center = _xrOrigin.CameraInOriginSpacePos;
//         _cc.center = new Vector3(center.x, height * 0.5f, center.z);

//         // 2) Snap turning with Left/Right arrows (no smooth yaw)
//         if (Time.time - _lastSnapTime >= snapCooldown)
//         {
//             if (Input.GetKeyDown(KeyCode.LeftArrow))
//             {
//                 transform.Rotate(0f, -snapAngle, 0f);
//                 _lastSnapTime = Time.time;
//             }
//             else if (Input.GetKeyDown(KeyCode.RightArrow))
//             {
//                 transform.Rotate(0f, +snapAngle, 0f);
//                 _lastSnapTime = Time.time;
//             }
//         }

//         // 3) Forward/back input (Up/Down arrows)
//         int forwardAxis = 0;
//         if (Input.GetKey(KeyCode.UpArrow)) forwardAxis = +1;
//         else if (Input.GetKey(KeyCode.DownArrow)) forwardAxis = -1;

//         // 4) Acceleration / deceleration toward target speed
//         float targetSpeed = forwardAxis * moveSpeed;
//         float rate = (forwardAxis == 0) ? deceleration : acceleration; // m/s^2
//         _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);

//         // 5) Head-relative planar direction
//         Vector3 headForward = cam.transform.forward; headForward.y = 0f; headForward.Normalize();
//         Vector3 planar = headForward * _currentSpeed; // m/s

//         // 6) Gravity
//         if (_cc.isGrounded && _fallVelocity < 0f) _fallVelocity = -2f; // stick to ground
//         _fallVelocity += gravity * Time.deltaTime; // m/s

//         // 7) Apply motion
//         Vector3 motion = planar * Time.deltaTime + Vector3.up * _fallVelocity * Time.deltaTime;
//         _cc.Move(motion);
//     }
// }


//Hami's update: I'm trying to switch to keyboard movement + gamepad

using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(XROrigin))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Capsule Height Clamp")]
    [SerializeField] private float _minHeight = 1.0f;
    [SerializeField] private float _maxHeight = 2.2f;
    [SerializeField] private float capsuleRadius = 0.2f;

    [Header("Locomotion (D-pad Up/Down; arrows fallback)")]
    [SerializeField] private float moveSpeed = 1.5f;     // m/s
    [SerializeField] private float acceleration = 3.0f;  // m/s^2
    [SerializeField] private float deceleration = 4.0f;  // m/s^2

    [Header("Snap Turning (D-pad Left/Right; arrows fallback)")]
    [SerializeField] private float snapAngle = 30f;
    [SerializeField] private float snapCooldown = 0.25f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    private XROrigin _xrOrigin;
    private CharacterController _cc;

    private float _currentSpeed = 0f;
    private float _fallVelocity = 0f;
    private float _lastSnapTime = -999f;

    private void Awake()
    {
        _xrOrigin = GetComponent<XROrigin>();
        _cc = GetComponent<CharacterController>();

        _cc.enableOverlapRecovery = true;
        _cc.skinWidth = 0.02f;
        _cc.radius = capsuleRadius;
    }

    private void Update()
    {
        if (_xrOrigin == null || _cc == null) return;
        var cam = _xrOrigin.Camera;
        if (cam == null) return;

        // 1) Keep capsule aligned to HMD height & center (in origin space)
        float height = Mathf.Clamp(_xrOrigin.CameraInOriginSpaceHeight, _minHeight, _maxHeight);
        _cc.height = height;
        Vector3 center = _xrOrigin.CameraInOriginSpacePos;
        _cc.center = new Vector3(center.x, height * 0.5f, center.z);

        // 2) Snap turning with D-pad Left/Right (fallback: Left/Right arrows)
        if (Time.time - _lastSnapTime >= snapCooldown)
        {
            bool snapLeft  =
                (Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame) ||
                (Gamepad.current  != null && Gamepad.current.dpad.left.wasPressedThisFrame);
            bool snapRight =
                (Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame) ||
                (Gamepad.current  != null && Gamepad.current.dpad.right.wasPressedThisFrame);

            if (snapLeft)
            {
                transform.Rotate(0f, -snapAngle, 0f);
                _lastSnapTime = Time.time;
            }
            else if (snapRight)
            {
                transform.Rotate(0f, +snapAngle, 0f);
                _lastSnapTime = Time.time;
            }
        }

        // 3) Forward/back input (D-pad Up/Down; fallback: Up/Down arrows)
        int forwardAxis = 0;
        bool forwardHeld =
            (Keyboard.current != null && Keyboard.current.upArrowKey.isPressed) ||
            (Gamepad.current  != null && Gamepad.current.dpad.up.isPressed);
        bool backHeld =
            (Keyboard.current != null && Keyboard.current.downArrowKey.isPressed) ||
            (Gamepad.current  != null && Gamepad.current.dpad.down.isPressed);

        if (forwardHeld) forwardAxis = +1;
        else if (backHeld) forwardAxis = -1;

        // 4) Acceleration / deceleration toward target speed
        float targetSpeed = forwardAxis * moveSpeed;
        float rate = (forwardAxis == 0) ? deceleration : acceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);

        // 5) Head-relative planar direction
        Vector3 headForward = cam.transform.forward; headForward.y = 0f; headForward.Normalize();
        Vector3 planar = headForward * _currentSpeed;

        // 6) Gravity
        if (_cc.isGrounded && _fallVelocity < 0f) _fallVelocity = -2f;
        _fallVelocity += gravity * Time.deltaTime;

        // 7) Apply motion
        Vector3 motion = planar * Time.deltaTime + Vector3.up * _fallVelocity * Time.deltaTime;
        _cc.Move(motion);
    }
}
