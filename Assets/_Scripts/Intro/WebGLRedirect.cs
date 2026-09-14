using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Navigates the browser tab to a URL (WebGL only). Unlike Application.OpenURL
/// (which opens a new tab), this replaces the current page — used for post-study
/// redirects such as Prolific completion links.
/// </summary>
public static class WebGLRedirect
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebGLRedirect_Go(string url);
#endif

    public static void Go(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLRedirect_Go(url);
#else
        Debug.Log($"[WebGLRedirect] (non-WebGL) Would redirect to: {url}");
#endif
    }
}
