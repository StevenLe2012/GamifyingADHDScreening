using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// WebGL-only full-file video prefetch via browser fetch() + blob URLs.
/// Editor / standalone builds no-op and return the original HTTP URL.
/// </summary>
public static class WebGLVideoPrefetch
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebGLVideoPrefetch_Start(string url);

    [DllImport("__Internal")]
    private static extern int WebGLVideoPrefetch_IsReady(string url);

    [DllImport("__Internal")]
    private static extern IntPtr WebGLVideoPrefetch_GetPlaybackUrl(string url);

    [DllImport("__Internal")]
    private static extern void WebGLVideoPrefetch_Free(IntPtr ptr);
#endif

    static readonly HashSet<string> _started = new HashSet<string>();

    public static string BuildStreamingAssetsUrl(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        return Application.streamingAssetsPath + "/" + fileName.Trim();
    }

    /// <summary>Start downloading a video in the background (deduped).</summary>
    public static void Start(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (!_started.Add(url)) return;
        WebGLVideoPrefetch_Start(url);
#else
        _started.Add(url);
#endif
    }

    public static void StartFile(string streamingAssetsFileName)
    {
        Start(BuildStreamingAssetsUrl(streamingAssetsFileName));
    }

    public static bool IsReady(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
#if UNITY_WEBGL && !UNITY_EDITOR
        return WebGLVideoPrefetch_IsReady(url) != 0;
#else
        return false;
#endif
    }

    public static bool WasStarted(string url)
    {
        return !string.IsNullOrWhiteSpace(url) && _started.Contains(url);
    }

    /// <summary>Returns blob URL when prefetched, otherwise the original url.</summary>
    public static string ResolvePlaybackUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;

#if UNITY_WEBGL && !UNITY_EDITOR
        var ptr = WebGLVideoPrefetch_GetPlaybackUrl(url);
        if (ptr == IntPtr.Zero) return url;
        try
        {
            return Marshal.PtrToStringUTF8(ptr) ?? url;
        }
        finally
        {
            WebGLVideoPrefetch_Free(ptr);
        }
#else
        return url;
#endif
    }

    public static string ResolvePlaybackFile(string streamingAssetsFileName)
    {
        var url = BuildStreamingAssetsUrl(streamingAssetsFileName);
        return ResolvePlaybackUrl(url);
    }
}
