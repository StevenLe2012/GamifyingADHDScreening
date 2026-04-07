using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Registers direct browser DOM event listeners that call AudioContext.resume()
/// when the user first interacts (click / keydown / touch).
///
/// Setup: attach to any persistent GameObject in your first scene.
/// The DOM listeners stay active for the lifetime of the page, so the audio
/// context will unlock on the very first user gesture regardless of timing.
/// </summary>
public class WebGLAudioUnlock : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void SetupAudioAutoUnlock();
    [DllImport("__Internal")] private static extern int  IsUnityAudioContextRunning();
#endif

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Register DOM listeners once — no per-frame polling needed.
        SetupAudioAutoUnlock();
        StartCoroutine(CoLogStatus());
#else
        enabled = false;
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    IEnumerator CoLogStatus()
    {
        // Wait a couple of seconds then log the current state for debugging.
        yield return new WaitForSecondsRealtime(2f);
        if (IsUnityAudioContextRunning() == 1)
            Debug.Log("[WebGLAudioUnlock] AudioContext is running.");
        else
            Debug.Log("[WebGLAudioUnlock] AudioContext still suspended — will unlock on first user gesture.");
        enabled = false;
    }
#endif
}
