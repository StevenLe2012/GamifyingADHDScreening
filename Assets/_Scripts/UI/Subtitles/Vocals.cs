using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vocals : MonoBehaviour
{
    public static Vocals instance;
    
    private AudioSource source;

    

    private void Awake()
    {
         instance = this;
    }
    private void Start()
    {
        //source = gameObject.AddComponent<AudioSource>();
    }

    public void Say(AudioObjects clip) // change to "start conversation" to play through array of audio objects one by one
    {
        source = GetComponent<AudioSource>();
        if (source.isPlaying)
        {
            source.Stop();
        }

        source.PlayOneShot(clip.clip);

        //SubtitleUI.instance.SetSubtitle(clip.subtitle, clip.clip.length);
    }
}
