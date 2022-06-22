using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportPad : MonoBehaviour
{
    public int code;
    public float secondsTillTP;

    private float disableTimer;

    private void Update()
    {
        if (disableTimer > 0)
        {
            disableTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Player" && disableTimer <= 0)
        {
            foreach(TeleportPad tp in FindObjectsOfType<TeleportPad>())
            {
                if (tp.code == code && tp != this)
                {
                    disableTimer = secondsTillTP;
                    Vector3 newPosition = tp.transform.position;
                    newPosition.y += 2;
                    collider.gameObject.transform.position = newPosition;
                }
            }
        }
    }
}
