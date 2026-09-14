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
    /// Enter only — not Escape (Escape is the browser's native exit-fullscreen key; treating it as
    /// a skip too meant pressing it both exited fullscreen AND skipped the cutscene in the same
    /// keypress), not Space, not anyKey (avoids accidental skip and stuck states).
    /// </summary>
    public static bool AnySkipPressed()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        if (kb != null && (kb.enterKey.wasPressedThisFrame ||
                           kb.numpadEnterKey.wasPressedThisFrame))
            return true;

        if (gp != null && (gp.startButton.wasPressedThisFrame ||
                           gp.aButton.wasPressedThisFrame))
            return true;

#if !UNITY_WEBGL
        if (Input.GetKeyDown(KeyCode.Return) ||
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

    /// <summary>
    /// Same as <see cref="UpdateStallTimer(VideoPlayer, bool, ref float)"/> but also catches the
    /// WebGL freeze case: vp.isPlaying stays true and audio keeps advancing, but the decoded
    /// frame index stops moving (visually frozen). Tracks the last seen frame index across calls.
    /// </summary>
    public static bool UpdateStallTimer(VideoPlayer vp, bool playbackStarted, ref float stallTimer, ref long lastFrame)
    {
        if (vp == null || !playbackStarted)
            return false;

        if (!vp.isPlaying)
        {
            stallTimer += Time.unscaledDeltaTime;
            return stallTimer >= StallTimeoutSeconds;
        }

        long frame = vp.frame;
        if (frame != lastFrame)
        {
            lastFrame = frame;
            stallTimer = 0f;
            return false;
        }

        stallTimer += Time.unscaledDeltaTime;
        return stallTimer >= StallTimeoutSeconds;
    }

    /// <summary>
    /// Tracks a stuck-frame recovery attempt across ticks. vp.frame/vp.time are driven by the
    /// browser's decode timeline, which keeps advancing even when the WebGL canvas stops painting
    /// new video frames (audio-only freeze) — so a plain frame/time check can't detect this case,
    /// and skipOnDrop has no effect since the browser (not Unity's scheduler) is what's stuck.
    /// </summary>
    public struct StallRecoveryState
    {
        public long LastFrame;
        public float StuckSeconds;
        public float TotalStuckSeconds;
        public int NudgeCount;
    }

    public const float NudgeAfterSeconds = 0.6f;
    public const int MaxNudgeAttempts = 6;
    public const float HardStallTimeoutSeconds = 5f;

    /// <summary>
    /// Detects a frozen video frame and actively unsticks it by force-seeking the VideoPlayer
    /// (vp.time = ...), which makes the browser re-decode/re-paint at the new position — the same
    /// mechanism scrubbing a native &lt;video&gt; element uses to recover from a stalled decoder.
    /// Falls back to reporting an unrecoverable stall (caller should skip/finish) only after
    /// repeated nudges fail within HardStallTimeoutSeconds.
    /// </summary>
    public static bool TickStallRecovery(
        VideoPlayer vp, AudioSource audio, bool playbackStarted,
        ref StallRecoveryState state, bool log, string logTag)
    {
        if (vp == null || !playbackStarted)
            return false;

        if (!vp.isPlaying)
        {
            state.TotalStuckSeconds += Time.unscaledDeltaTime;
            return state.TotalStuckSeconds >= HardStallTimeoutSeconds;
        }

        long frame = vp.frame;
        if (frame != state.LastFrame)
        {
            state.LastFrame = frame;
            state.StuckSeconds = 0f;
            state.TotalStuckSeconds = 0f;
            state.NudgeCount = 0;
            return false;
        }

        state.StuckSeconds += Time.unscaledDeltaTime;
        state.TotalStuckSeconds += Time.unscaledDeltaTime;

        if (state.TotalStuckSeconds >= HardStallTimeoutSeconds)
        {
            if (log) Debug.LogWarning($"[{logTag}] Frame frozen for {state.TotalStuckSeconds:0.0}s despite {state.NudgeCount} nudge(s) — giving up.");
            return true;
        }

        if (state.StuckSeconds >= NudgeAfterSeconds && state.NudgeCount < MaxNudgeAttempts)
        {
            state.NudgeCount++;
            state.StuckSeconds = 0f;

            double target = (audio != null && audio.isPlaying) ? audio.time : vp.time + 0.2;
            if (target <= vp.time) target = vp.time + 0.2;

            if (log) Debug.LogWarning($"[{logTag}] Frame frozen — nudging vp.time {vp.time:0.00}s -> {target:0.00}s (attempt {state.NudgeCount}).");
            vp.time = target;
        }

        return false;
    }
}
