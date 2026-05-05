using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleDesktopMove : MonoBehaviour
{
    public float speed = 3f;
    private CharacterController _cc;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (_cc == null) return;

        Vector2 input = Vector2.zero;

        // Keyboard WASD
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
        }

        // Gamepad left stick
        if (Gamepad.current != null)
        {
            input += Gamepad.current.leftStick.ReadValue();
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 move = (transform.forward * input.y + transform.right * input.x) * speed;

        // No gravity, no jump – just horizontal
        _cc.Move(move * Time.deltaTime);

        // Debug so you can see input
        if (input != Vector2.zero)
            Debug.Log("Input: " + input);
    }
}
