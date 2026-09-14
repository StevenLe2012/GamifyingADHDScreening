using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// WebGL cutscene prepare: wait for full-file prefetch, then prepare on blob URL.
/// Matches IntroBoot — isPrepared alone is not enough when streaming over HTTP.
/// </summary>
public static class CutsceneWebGLPrepare
{
    public static IEnumerator CoPrepare(
        VideoPlayer vp,
        string streamingAssetsFileName,
        VideoLoadingScreen.Settings settings,
        MonoBehaviour host,
        float prepareTimeoutSeconds,
        bool log,
        string logTag = "CutsceneWebGLPrepare")
    {
        if (vp == null || string.IsNullOrWhiteSpace(streamingAssetsFileName))
            yield break;

        var streamUrl = WebGLVideoPrefetch.BuildStreamingAssetsUrl(streamingAssetsFileName);
        WebGLVideoPrefetch.StartFile(streamingAssetsFileName);

        VideoLoadingScreen.Show(settings, host);
        yield return null;

        float elapsed = 0f;
        bool prefetched = false;
        while (elapsed < settings.MaxSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            if (WebGLVideoPrefetch.IsReady(streamUrl) && elapsed >= settings.MinSeconds)
            {
                prefetched = true;
                break;
            }
            yield return null;
        }

        if (log)
        {
            Debug.Log(
                $"[{logTag}] Prefetch {(prefetched ? "ready" : "timeout")} after {elapsed:0.0}s — preparing {streamingAssetsFileName}.");
        }

        var playbackUrl = WebGLVideoPrefetch.ResolvePlaybackFile(streamingAssetsFileName);
        if (vp.url != playbackUrl)
        {
            if (vp.isPlaying) vp.Stop();
            vp.url = playbackUrl;
        }

        // Always drop frames to catch up rather than block on them — matches the
        // skipOnDrop=true set at VideoPlayer creation. Forcing this false once
        // prefetched used to freeze the visible frame (audio kept advancing) if
        // decode/GPU upload hitched even briefly, since the player would refuse
        // to skip the stuck frame to resync.
        vp.skipOnDrop = true;

        if (!vp.isPrepared)
            vp.Prepare();

        float prepareWait = 0f;
        while (!vp.isPrepared && prepareWait < prepareTimeoutSeconds)
        {
            prepareWait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (log)
        {
            Debug.Log(
                $"[{logTag}] Done (prefetched={prefetched}, prepared={vp.isPrepared}, prepareWait={prepareWait:0.0}s).");
        }
    }
}
