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
        //source = GetComponent<AudioSource>();
        //if (source.isPlaying)
        //{
        //    source.Stop();
        //}

        //source.PlayOneShot(clip.clip);

        ////SubtitleUI.instance.SetSubtitle(clip.subtitle, clip.clip.length);
        ///

        // Ensure we have an AudioSource
        if (source == null)
        {
            source = GetComponent<AudioSource>();
            if (source == null)
            {
                Debug.LogError("Vocals: No AudioSource found on this GameObject. Please add one.");
                return;
            }
        }

        // Ensure we got a valid AudioObjects reference
        if (clip == null)
        {
            Debug.LogWarning("Vocals: Say() called with null AudioObjects.");
            return;
        }

        // Ensure the AudioClip inside the AudioObjects is set
        if (clip.clip == null)
        {
            Debug.LogWarning("Vocals: AudioObjects has no AudioClip assigned. Skipping audio.");
            // Still allow subtitles if you want them
            // if (!string.IsNullOrEmpty(clip.subtitle))
            //     SubtitleUI.instance.SetSubtitle(clip.subtitle, 2f); // fallback duration
            return;
        }

        // Stop currently playing audio if any
        if (source.isPlaying)
            source.Stop();

        // Play new audio
        source.PlayOneShot(clip.clip);


    }
}
