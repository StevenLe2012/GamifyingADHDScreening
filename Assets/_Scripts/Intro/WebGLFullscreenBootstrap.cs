using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_WEBGL && !UNITY_EDITOR
/// <summary>
/// Fallback when BootIntro is skipped (e.g. ?fast=1): request fullscreen on first input in Main.
/// </summary>
public class WebGLFullscreenBootstrap : MonoBehaviour
{
    bool _requested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindObjectOfType<WebGLFullscreenBootstrap>() != null) return;
        var go = new GameObject("[WebGLFullscreenBootstrap]");
        go.AddComponent<WebGLFullscreenBootstrap>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        if (_requested || WebGLFullscreen.IsActive)
        {
            Destroy(gameObject);
            return;
        }

        if (!AnyInputPressed()) return;

        _requested = true;
        WebGLFullscreen.Request();
        Destroy(gameObject);
    }

    static bool AnyInputPressed()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame ||
                              mouse.rightButton.wasPressedThisFrame ||
                              mouse.middleButton.wasPressedThisFrame))
            return true;

        if (kb != null && kb.anyKey.wasPressedThisFrame)
            return true;

        return Input.anyKeyDown;
    }
}
#endif
