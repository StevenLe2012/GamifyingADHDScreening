using UnityEngine;
using Varjo.XR;
using System.Collections;

public class VarjoGazeGuard : MonoBehaviour
{
    public static VarjoGazeGuard I { get; private set; }

    [Tooltip("How long we wait for gaze to become usable during PrepareCPT (seconds)")]
    public float timeoutPrepareSeconds = 20f;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Set preferred stream params early (safe to call multiple times).
        VarjoEyeTracking.SetGazeOutputFilterType(VarjoEyeTracking.GazeOutputFilterType.Standard);
        VarjoEyeTracking.SetGazeOutputFrequency(VarjoEyeTracking.GazeOutputFrequency.MaximumSupported);
    }

    /// Call this from PrepareCPT. Returns when gaze is usable or we hit timeout.
    public IEnumerator EnsureGazeReady(float? timeoutOverride = null)
    {
        if (EyeTrackLogger.I != null && !EyeTrackLogger.I.EyeTrackingEnabled)
        {
            Debug.Log("[GazeGuard] Skipped (eye tracking disabled).");
            yield break;
        }
        float timeout = timeoutOverride ?? timeoutPrepareSeconds;
        float t = 0f;

        // If not calibrated, try to request calibration once.
        if (!VarjoEyeTracking.IsGazeCalibrated())
        {
            VarjoEyeTracking.RequestGazeCalibration(
                VarjoEyeTracking.GazeCalibrationMode.Fast,
                VarjoEyeTracking.HeadsetAlignmentGuidanceMode.AutoContinueOnAcceptableHeadsetPosition);
            Debug.Log("[GazeGuard] Requested Varjo gaze calibration…");
        }

        // Poll until allowed + available + calibrated, or timeout.
        while (!(VarjoEyeTracking.IsGazeAllowed() &&
                 VarjoEyeTracking.IsGazeAvailable() &&
                 VarjoEyeTracking.IsGazeCalibrated()))
        {
            t += Time.unscaledDeltaTime;
            if (t >= timeout) break;

            if (Mathf.FloorToInt(t) % 1 == 0)
            {
                Debug.Log($"[GazeGuard] allowed={VarjoEyeTracking.IsGazeAllowed()} " +
                          $"available={VarjoEyeTracking.IsGazeAvailable()} " +
                          $"calibrated={VarjoEyeTracking.IsGazeCalibrated()} (t={t:0}s)");
            }
            yield return null;
        }

        bool ready = VarjoEyeTracking.IsGazeAllowed() &&
                     VarjoEyeTracking.IsGazeAvailable() &&
                     VarjoEyeTracking.IsGazeCalibrated();

        Debug.Log(ready ? "[GazeGuard] Gaze stream ready ✅" :
                          "[GazeGuard] Gaze stream NOT ready (timeout) ⚠️");
    }
}
