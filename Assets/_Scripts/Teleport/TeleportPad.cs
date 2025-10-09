// using System;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class TeleportPad : MonoBehaviour
// {
//     [SerializeField] private InputActionReference controllerInput;
//     [SerializeField] private GameManager.GameState _state;
//     [SerializeField] private int code;
//     [SerializeField] private float _triggerAmountNeeded = 0.75f;
    
//     private float _curTrigger;

//     private void OnTriggerStay(Collider collider)
//     {
//         if (collider.gameObject.CompareTag("Player"))
//         {
//             // Hami: --- Controller Input (commented out for now) ---
//             // _curTrigger = controllerInput.action.ReadValue<float>();
//             // if (_curTrigger >= _triggerAmountNeeded)

//             // --- Temporary Keyboard Input (Spacebar) ---
//             if (Input.GetKeyDown(KeyCode.Space))
//             {
//                 var newPosition = GetTeleportPosition();
//                 GameManager.Instance.UpdateGameState(_state);
//                 collider.gameObject.transform.position = newPosition;
//             }
//         }
//     }
//     private Vector3 GetTeleportPosition()
//     {
//         foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
//         {
//             if (tp.code == code && tp != this)
//             {
//                 var newPosition = tp.transform.position;
//                 newPosition.y += 2;
//                 return newPosition;
//             }
//         }

//         throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
//     }
// }


//Hami: Trigger with spacebar/gamepad stick A

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportPad : MonoBehaviour
{
    [SerializeField] private GameManager.GameState _state;
    [SerializeField] private int code;

    [Header("Debounce")]
    [SerializeField] private float teleportCooldown = 0.2f;

    private bool _playerInside;
    private Transform _playerTransform;
    private float _cooldownTimer;

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        if (!_playerInside || _cooldownTimer > 0f) return;

        // Gamepad A OR Space
        bool pressedA     = Gamepad.current != null  && Gamepad.current.buttonSouth.wasPressedThisFrame;
        bool pressedSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (pressedA || pressedSpace)
        {
            var newPosition = GetTeleportPosition();
            GameManager.Instance.UpdateGameState(_state);
            _playerTransform.position = newPosition;
            _cooldownTimer = teleportCooldown;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = true;
            _playerTransform = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = false;
            if (_playerTransform == other.transform) _playerTransform = null;
        }
    }

    private Vector3 GetTeleportPosition()
    {
        foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
        {
            if (tp.code == code && tp != this)
            {
                var newPosition = tp.transform.position;
                newPosition.y += 2f;
                return newPosition;
            }
        }
        throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
    }
}
