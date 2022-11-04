using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportPad : MonoBehaviour
{
    [SerializeField] private InputActionReference controllerInput = null;
    [SerializeField] private GameManager.GameState _state;
    [SerializeField] private int code;
    [SerializeField] private float _triggerAmountNeeded = 0.75f;
  
    private float _curTrigger;

    private void OnTriggerStay(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            _curTrigger = controllerInput.action.ReadValue<float>();
            if (_curTrigger >= _triggerAmountNeeded)
            {
                var newPosition = GetTeleportPosition();
                GameManager.Instance.UpdateGameState(_state);
                collider.gameObject.transform.position = newPosition;
            }
        }
    }

    private Vector3 GetTeleportPosition()
    {
        foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
        {
            if (tp.code == code && tp != this)
            {
                var newPosition = tp.transform.position;
                newPosition.y += 2;
                return newPosition;
            }
        }

        throw new NullReferenceException($"There are no other teleport pads with this code: {code}");
    }
}