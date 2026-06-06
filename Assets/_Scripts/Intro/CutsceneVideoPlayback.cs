using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

/// <summary>
/// Shared WebGL-safe skip + stall detection for cutscene VideoPlayers.
/// </summary>
public static class CutsceneVideoPlayback
{
    public const float StallTimeoutSeconds = 4f;

    /// <summary>
    /// Escape / Enter only — not Space, not anyKey (avoids accidental skip and stuck states).
    /// </summary>
    public static bool AnySkipPressed()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        if (kb != null && (kb.escapeKey.wasPressedThisFrame ||
                           kb.enterKey.wasPressedThisFrame ||
                           kb.numpadEnterKey.wasPressedThisFrame))
            return true;

        if (gp != null && (gp.startButton.wasPressedThisFrame ||
                           gp.aButton.wasPressedThisFrame))
            return true;

#if !UNITY_WEBGL
        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
            return true;
#endif

        return false;
    }

    public static bool ShouldSkip(bool allowSkip, float minUnskippableSeconds, float watchedSeconds, float ignoreSkipUntil)
    {
        if (!allowSkip) return false;
        if (watchedSeconds < minUnskippableSeconds) return false;
        if (Time.unscaledTime < ignoreSkipUntil) return false;
        return AnySkipPressed();
    }

    /// <summary>
    /// True when playback was expected but the player is not running for too long.
    /// </summary>
    public static bool UpdateStallTimer(VideoPlayer vp, bool playbackStarted, ref float stallTimer)
    {
        if (vp == null || !playbackStarted)
            return false;

        if (vp.isPlaying)
        {
            stallTimer = 0f;
            return false;
        }

        stallTimer += Time.unscaledDeltaTime;
        return stallTimer >= StallTimeoutSeconds;
    }
}
