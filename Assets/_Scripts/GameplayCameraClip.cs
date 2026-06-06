using Cinemachine;
using UnityEngine;

/// <summary>
/// Keeps gameplay cameras at the expected far clip (1500) after travel and Cinemachine switches.
/// </summary>
public static class GameplayCameraClip
{
    public const float FarClipPlane = 1500f;

    public static void Apply()
    {
        var cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            var cam = cameras[i];
            if (cam == null) continue;
            if (!Mathf.Approximately(cam.farClipPlane, FarClipPlane))
                cam.farClipPlane = FarClipPlane;
        }

        var vcams = Object.FindObjectsOfType<CinemachineVirtualCamera>(true);
        for (int i = 0; i < vcams.Length; i++)
        {
            var vcam = vcams[i];
            if (vcam == null) continue;
            var lens = vcam.m_Lens;
            if (!Mathf.Approximately(lens.FarClipPlane, FarClipPlane))
            {
                lens.FarClipPlane = FarClipPlane;
                vcam.m_Lens = lens;
            }
        }
    }
}
