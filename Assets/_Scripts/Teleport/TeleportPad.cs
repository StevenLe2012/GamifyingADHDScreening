using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportPad : MonoBehaviour
{
    [SerializeField] private InputActionReference controllerInput = null;

    [SerializeField] private GameManager.GameState _state;
    [SerializeField] private int code;
    [SerializeField] private float secondsTillTP = 2f;
    [SerializeField] private float _triggerAmountNeeded = 0.75f;

    private float _timer;
    private Vector3 _newPosition;
    private GameObject _player;
    private bool _onPad;
    private float _curTrigger;

    // TODO: add the timer and also fix relly weird bug with center

    private void OnTriggerStay(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            _curTrigger = controllerInput.action.ReadValue<float>();
            if (_curTrigger >= _triggerAmountNeeded)
            {
                _newPosition = GetTeleportPosition();
                GameManager.Instance.UpdateGameState(_state);
                collider.gameObject.transform.position = _newPosition;
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
/*
private IEnumerator OnTriggerEnter(Collider collider)
{
    if (collider.gameObject.CompareTag("Player"))
    {
        print("entered");
        _onPad = true;
        foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
        {
            if (tp.code == code && tp != this)
            {
                _newPosition = tp.transform.position;
                _newPosition.y += 2;
                break;
            }
        }
        yield return new WaitForSeconds(secondsTillTP);
        if (_onPad)
        {
            GameManager.Instance.UpdateGameState(_state);
            collider.gameObject.transform.position = _newPosition;
        }
    }
}

// private void OnCollisionExit(Collision collider)
// {
//     print("exited");
//     _onPad = false;
// }

private void OnTriggerExit(Collider other)
{
    print("Trigger Exited");
    _onPad = false;
}
*/

    /*
    private void OnTriggerEnter(Collider collider) {
        if (collider.gameObject.CompareTag("Player"))
        {
            _player = collider.gameObject;
            foreach (TeleportPad tp in FindObjectsOfType<TeleportPad>())
            {
                if (tp.code == code && tp != this)
                {
                    _newPosition = tp.transform.position;
                    _newPosition.y += 2;
                }
            }
        }
    }
    
    private void OnTriggerStay(Collider collider)
    {
        if (_player != null)
        {
            // TODO: DIDN'T WORK - NEED TO SOMEHOW SLOW DOWN _TIMER.
            while (_timer < secondsTillTP)
            {
                _timer += Time.deltaTime;
                print(_timer);
            }

            if (_timer >= secondsTillTP)
            {
                GameManager.Instance.UpdateGameState(_state);
                _player.transform.position = _newPosition;
                Reset();
            }
        }
    }

    private void OnCollisionExit(Collision other)
    {
        Reset();
    }
    */
    
    /*
    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            Reset();
        }
    }
    */

    /*
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            _player = collider.gameObject;
            foreach(TeleportPad tp in FindObjectsOfType<TeleportPad>())
            {
                if (tp.code == code && tp != this)
                {
                    _newPosition = tp.transform.position;
                    _newPosition.y += 2;
                    StartCoroutine(Teleport(_state));
                }
            }
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        StopAllCoroutines();
        Reset();
    }

    private IEnumerator Teleport(GameManager.GameState state)
    {
        while (_timer < secondsTillTP)
        {
            _timer += Time.deltaTime;
            print(_timer);
            yield return null;
        }
        
        GameManager.Instance.UpdateGameState(state);
        _player.transform.position = _newPosition;
        Reset();
    }
    */

    private void Reset()
    {
        _timer = 0f;
        //_player = null;
    }
}
