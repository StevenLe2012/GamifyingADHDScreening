using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Pause/resume helpers for cutscene VideoPlayers (especially WebGL tab switches).
/// </summary>
public static class CutsceneVideoSuspend
{
    public struct State
    {
        public bool AppSuspended;
        public bool WantsResume;
        public float PausedAtTime;
    }

    public static void OnVisibilityLost(VideoPlayer vp, ref State state, AudioSource audio = null)
    {
        if (vp == null || state.AppSuspended) return;

        state.AppSuspended = true;
        state.WantsResume = true;
        state.PausedAtTime = (float)vp.time;

        if (vp.isPlaying)
            vp.Pause();

        if (audio != null && audio.isPlaying)
            audio.Pause();
    }

    public static void OnVisibilityGained(
        VideoPlayer vp,
        ref State state,
        MonoBehaviour host,
        bool isActive,
        bool videoEnded,
        AudioSource audio = null,
        Action onResumed = null,
        Action onResumeFailed = null)
    {
        if (vp == null || !state.AppSuspended) return;

        state.AppSuspended = false;

        if (!isActive || videoEnded || !state.WantsResume)
        {
            state.WantsResume = false;
            return;
        }

        float resumeTime = state.PausedAtTime;
        state.WantsResume = false;
        host.StartCoroutine(CoResumePlayback(vp, resumeTime, audio, onResumed, onResumeFailed));
    }

    public static void Reset(ref State state)
    {
        state.AppSuspended = false;
        state.WantsResume = false;
        state.PausedAtTime = 0f;
    }

    static IEnumerator CoResumePlayback(
        VideoPlayer vp,
        float pausedAtTime,
        AudioSource audio,
        Action onComplete,
        Action onResumeFailed)
    {
        yield return null;
        yield return new WaitForSecondsRealtime(0.1f);

        if (vp == null)
        {
            onComplete?.Invoke();
            yield break;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (!vp.isPrepared)
        {
            vp.Prepare();
            float prepWait = 0f;
            while (!vp.isPrepared && prepWait < 3f)
            {
                prepWait += Time.unscaledDeltaTime;
                yield return null;
            }
        }
#endif

        bool resumed = false;
        for (int attempt = 0; attempt < 5 && !resumed; attempt++)
        {
            vp.time = pausedAtTime;
            vp.Play();

            for (int i = 0; i < 10 && !vp.isPlaying; i++)
                yield return new WaitForSecondsRealtime(0.1f);

            if (vp.isPlaying)
            {
                resumed = true;
                break;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            vp.Stop();
            vp.Prepare();
            float prepWait = 0f;
            while (!vp.isPrepared && prepWait < 2f)
            {
                prepWait += Time.unscaledDeltaTime;
                yield return null;
            }
#endif
        }

        if (audio != null && resumed)
            audio.UnPause();

        if (resumed)
            onComplete?.Invoke();
        else
            onResumeFailed?.Invoke();
    }
}
