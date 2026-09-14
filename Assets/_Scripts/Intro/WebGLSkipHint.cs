using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Browser-level (DOM) notification banner, independent of any in-game Canvas/UI — used for
/// cutscene skip hints so it doesn't have to fight cutscene overlay sorting order or touch
/// existing game UI (e.g. ExploreHintUI).
/// </summary>
public static class WebGLSkipHint
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SkipHint_Show(string message);

    [DllImport("__Internal")]
    private static extern void SkipHint_Hide();
#endif

    public static void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        SkipHint_Show(message);
#else
        Debug.Log($"[WebGLSkipHint] (non-WebGL) Would show: {message}");
#endif
    }

    public static void Hide()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SkipHint_Hide();
#else
        Debug.Log("[WebGLSkipHint] (non-WebGL) Would hide.");
#endif
    }
}
