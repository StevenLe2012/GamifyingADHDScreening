using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Browser fullscreen for WebGL. Must be called from a user gesture (click / key).
/// Browsers do not allow automatic fullscreen on page load.
/// </summary>
public static class WebGLFullscreen
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebGLFullscreen_Request();

    [DllImport("__Internal")]
    private static extern int WebGLFullscreen_IsActive();
#endif

    public static void Request()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLFullscreen_Request();
#endif
    }

    public static bool IsActive
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return WebGLFullscreen_IsActive() != 0;
#else
            return Screen.fullScreen;
#endif
        }
    }
}
