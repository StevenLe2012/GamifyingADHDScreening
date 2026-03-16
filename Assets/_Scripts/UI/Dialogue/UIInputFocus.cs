using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global focus/suppression gate to prevent input fallthrough.
/// Push(owner) while UI/dialogue/Narrative is active, Pop(owner) when done.
/// Use SuppressForSeconds(...) to swallow Space/Submit after closing UI.
/// </summary>
public static class UIInputFocus
{
    private static readonly HashSet<object> _owners = new HashSet<object>();

    // frame-based + time-based suppression (both are honored)
    private static int   _suppressSpaceFrames = 0;
    private static float _suppressUntilUnscaled = 0f;

    public static bool IsBlocked => _owners.Count > 0;

    /// <summary>True if Space/Submit should be ignored this Update.</summary>
    public static bool SuppressNow =>
        _suppressSpaceFrames > 0 || Time.unscaledTime < _suppressUntilUnscaled;

    /// <summary>Block gameplay/world input while a UI (or Narrative) owns input.</summary>
    public static void Push(object owner)
    {
        if (owner == null) return;
        EnsureRunner();
        _owners.Add(owner); // idempotent for same token
    }

    /// <summary>Release gameplay/world input ownership.</summary>
    public static void Pop(object owner)
    {
        if (owner == null) return;
        _owners.Remove(owner); // idempotent if not present
    }

    /// <summary>Swallow Space/Submit for N frames.</summary>
    public static void SuppressForFrames(int frames = 2)
    {
        EnsureRunner();
        _suppressSpaceFrames = Mathf.Max(_suppressSpaceFrames, Mathf.Max(1, frames));
    }

    /// <summary>Swallow Space/Submit for a duration (unscaled seconds).</summary>
    public static void SuppressForSeconds(float seconds)
    {
        EnsureRunner();
        _suppressUntilUnscaled = Mathf.Max(_suppressUntilUnscaled, Time.unscaledTime + Mathf.Max(0.01f, seconds));
    }

    internal static void Tick()
    {
        if (_suppressSpaceFrames > 0) _suppressSpaceFrames--;
    }

    // --- runner (auto-created; no scene setup needed) ---
    private static bool _runnerReady;

    private static void EnsureRunner()
    {
        if (_runnerReady) return;
        _runnerReady = true;

        var go = new GameObject("__UIInputFocusRunner");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<UIInputFocusRunner>();
    }

    private class UIInputFocusRunner : MonoBehaviour
    {
        private void Update() => UIInputFocus.Tick();
    }
}
