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

    private CharacterController _cc;
    private float _verticalVelocity;
    private float _pitch = 0f;

    // Optional: flip at runtime
    public void SetMovementEnabled(bool on) => allowMovement = on;

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
        // On desktop/standalone we can lock the cursor immediately.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
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
            // Forward / back with Up / Down arrows
            if (Input.GetKey(KeyCode.UpArrow))
                forward = 1f;
            else if (Input.GetKey(KeyCode.DownArrow))
                forward = -1f;

            // Strafe left/right with Left / Right arrows
            if (Input.GetKey(KeyCode.RightArrow))
                strafe = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow))
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
