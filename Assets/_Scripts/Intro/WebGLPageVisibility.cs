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
