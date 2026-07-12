using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Registers document visibility callbacks on WebGL (tab switch in browser).
/// </summary>
public static class WebGLPageVisibility
{
    public static event Action<bool> VisibilityChanged;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UnityPageVisibility_Register(string gameObjectName, string methodName);
#endif

    public static void Register(MonoBehaviour host, string methodName = "OnBrowserVisibilityChanged")
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (host == null) return;
        UnityPageVisibility_Register(host.gameObject.name, methodName);
#endif
    }

    public static void NotifyFromBrowser(int hidden)
    {
        VisibilityChanged?.Invoke(hidden == 0);
    }
}

/// <summary>
/// Forwards browser pointer/keyboard gestures to IntroBoot when Unity's Input System
/// does not receive events (common on WebGL before the canvas is focused).
/// </summary>
public static class WebGLIntroBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void WebGLIntroBridge_Setup(string gameObjectName);
    [DllImport("__Internal")] private static extern void WebGLIntroBridge_Arm();
    [DllImport("__Internal")] private static extern void WebGLIntroBridge_Disarm();
    [DllImport("__Internal")] private static extern void WebGLIntroBridge_Reset();
    [DllImport("__Internal")] private static extern int WebGLIntroBridge_ConsumeGesture();
    [DllImport("__Internal")] private static extern void WebGLIntroBridge_HidePageStartHint();
    [DllImport("__Internal")] private static extern void WebGLIntroBridge_ShowPageStartHint();
#endif

    public static void Setup(string gameObjectName)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!string.IsNullOrEmpty(gameObjectName))
            WebGLIntroBridge_Setup(gameObjectName);
#endif
    }

    public static void Arm()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLIntroBridge_Arm();
#endif
    }

    public static void Disarm()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLIntroBridge_Disarm();
#endif
    }

    public static void Reset()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLIntroBridge_Reset();
#endif
    }

    public static bool ConsumeGesture()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return WebGLIntroBridge_ConsumeGesture() != 0;
#endif
        return false;
    }

    public static void HidePageStartHint()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLIntroBridge_HidePageStartHint();
#endif
    }

    public static void ShowPageStartHint()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLIntroBridge_ShowPageStartHint();
#endif
    }
}
