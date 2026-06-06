using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DesktopArrowController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public Camera playerCamera;
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;

    [Header("Toggles")]
    [Tooltip("If OFF, arrow-key movement (forward/back/strafe) is disabled. Mouse look still works.")]
    public bool allowMovement = true;
    [Tooltip("If OFF, mouse-look yaw/pitch is disabled.")]
    public bool allowMouseLook = true;

    private CharacterController _cc;
    private float _verticalVelocity;
    private float _pitch = 0f;
    private bool _freezeLookAngle;
    private Quaternion _frozenBodyRotation;
    private float _frozenPitch;
    private float _ignoreLookUntilUnscaledTime;
    private float _poseGuardUntilUnscaledTime;
    private Quaternion _guardBodyRotation;
    private float _guardPitch;

    // Optional: flip at runtime
    public void SetMovementEnabled(bool on) => allowMovement = on;
    public void SetMouseLookEnabled(bool on)
    {
        allowMouseLook = on;
    }

    public void SetLookAngleFrozen(bool on)
    {
        _freezeLookAngle = on;
        if (on)
        {
            float yaw = transform.eulerAngles.y;
            _frozenBodyRotation = Quaternion.Euler(0f, yaw, 0f);
            _frozenPitch = _pitch;
        }
    }

    public float GetCurrentPitch()
    {
        if (playerCamera != null)
        {
            float x = playerCamera.transform.localEulerAngles.x;
            if (x > 180f) x -= 360f;
            return Mathf.Clamp(x, -verticalLookLimit, verticalLookLimit);
        }
        return _pitch;
    }

    public void RestoreLookPose(Quaternion bodyRotation, float cameraPitch)
    {
        float yaw = bodyRotation.eulerAngles.y;
        _guardBodyRotation = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = _guardBodyRotation;

        _guardPitch = Mathf.Clamp(cameraPitch, -verticalLookLimit, verticalLookLimit);
        _pitch = _guardPitch;
        if (playerCamera)
            playerCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);

        // Prevent a one-frame/post-transition mouse delta from pulling
        // the camera away from the restored pose.
        Input.ResetInputAxes();
        _ignoreLookUntilUnscaledTime = Time.unscaledTime + 0.2f;
        _poseGuardUntilUnscaledTime = Time.unscaledTime + 0.6f;
    }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        _cc.height = 2f;
        _cc.center = new Vector3(0f, 1f, 0f);
        _cc.radius = 0.25f;
        _cc.skinWidth = 0.02f;

#if !UNITY_WEBGL
        ApplyCursorForLookState();
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL browsers only allow pointer-lock in response to a direct user click.
    // Re-acquire lock whenever the game canvas is focused (user clicked the page).
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
#endif

    void Update()
    {
        KeepBodyUpright();

        if (Time.unscaledTime < _poseGuardUntilUnscaledTime)
        {
            transform.rotation = _guardBodyRotation;
            _pitch = _guardPitch;
            if (playerCamera)
                playerCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);

            HandleMovement();
            return;
        }

        if (_freezeLookAngle)
        {
            transform.rotation = _frozenBodyRotation;
            _pitch = _frozenPitch;
            if (playerCamera)
                playerCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }
        else if (allowMouseLook)
        {
            HandleLook();
        }
        HandleMovement();
    }

    private void KeepBodyUpright()
    {
        var e = transform.eulerAngles;
        if (Mathf.Abs(e.x) > 0.001f || Mathf.Abs(e.z) > 0.001f)
            transform.rotation = Quaternion.Euler(0f, e.y, 0f);
    }

    private void ApplyCursorForLookState()
    {
        if (allowMouseLook)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleLook()
    {
        if (Time.unscaledTime < _ignoreLookUntilUnscaledTime)
            return;

        // Ignore mouse deltas unless pointer lock is active.
        // This prevents camera yaw drift when other systems temporarily unlock
        // the cursor (e.g. MOXO/results transitions across islands).
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        // Mouse X = yaw (turn left/right)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        // Mouse Y = pitch (look up/down)
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate body around Y with mouse X
        transform.Rotate(0f, mouseX, 0f);

        // Pitch camera with mouse Y
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -verticalLookLimit, verticalLookLimit);
        if (playerCamera)
            playerCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        // Planar input
        float forward = 0f;
        float strafe  = 0f;

        if (allowMovement)
        {
            // Forward / back with Up/Down arrows or W/S
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                forward = 1f;
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
                forward = -1f;

            // Strafe left/right with Left/Right arrows or A/D
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                strafe = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                strafe = -1f;
        }
        // else: keep forward/strafe at 0 → fixed position (no planar translation)

        // Combine into world-space movement
        Vector3 moveDir = (transform.forward * forward + transform.right * strafe);
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();
        moveDir *= moveSpeed;

        // Gravity
        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f; // stick to ground

        _verticalVelocity += gravity * Time.deltaTime;
        moveDir.y = _verticalVelocity;

        _cc.Move(moveDir * Time.deltaTime);
    }
}
