using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DesktopArrowController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float gravity = -9.81f;

    [Header("Turning (Arrow Keys)")]
    public float turnSpeed = 120f;   // degrees per second

    [Header("Look Up / Down (Mouse)")]
    public Camera playerCamera;
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;

    private CharacterController _cc;
    private float _verticalVelocity;
    private float _pitch = 0f;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        _cc.height = 2f;
        _cc.center = new Vector3(0f, 1f, 0f);
        _cc.radius = 0.25f;
        _cc.skinWidth = 0.02f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleTurning();
        HandleLook();
        HandleMovement();
    }

    void HandleTurning()
    {
        float turn = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
            turn = -1f;
        else if (Input.GetKey(KeyCode.RightArrow))
            turn = 1f;

        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
    }

    void HandleLook()
    {
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -verticalLookLimit, verticalLookLimit);

        playerCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        float move = 0f;

        if (Input.GetKey(KeyCode.UpArrow))
            move = 1f;
        else if (Input.GetKey(KeyCode.DownArrow))
            move = -1f;

        Vector3 direction = transform.forward * move * moveSpeed;

        // Gravity
        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;
        direction.y = _verticalVelocity;

        _cc.Move(direction * Time.deltaTime);
    }
}
