using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerAudio : MonoBehaviour
{
    public AudioObjects clipToPlay;

    // SET HOW TO TRIGGER IT.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Vocals.instance.Say(clipToPlay);
        }
    }
}
