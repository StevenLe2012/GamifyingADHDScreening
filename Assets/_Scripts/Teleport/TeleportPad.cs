using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportPad : MonoBehaviour
{
    [SerializeField] private int code;
    [SerializeField] private float secondsTillTP = 2f;
    
    private float _timer;
    private Vector3 _newPosition;
    private GameObject _player;

    // TODO: add the timer and also fix relly weird bug with center

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
                    StartCoroutine(Teleport());
                }
            }
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        //StopCoroutine(Teleport());
        StopAllCoroutines();
        Reset();
    }

    private IEnumerator Teleport()
    {
        while (_timer < secondsTillTP)
        {
            _timer += Time.deltaTime;
            print(_timer);
            yield return null;
        }
        
        
        _player.transform.position = _newPosition;
        Reset();
    }

    private void Reset()
    {
        _timer = 0f;
    }
}
