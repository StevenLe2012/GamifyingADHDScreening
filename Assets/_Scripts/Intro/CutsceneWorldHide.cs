using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hides the 3D world while a cutscene video plays so letterboxed margins
/// (common on laptop fullscreen / non-16:9 displays) show black instead of gameplay.
/// </summary>
public static class CutsceneWorldHide
{
    struct SavedCameraState
    {
        public Camera Camera;
        public int CullingMask;
        public CameraClearFlags ClearFlags;
        public Color BackgroundColor;
        public bool Enabled;
    }

    static readonly List<SavedCameraState> Saved = new List<SavedCameraState>();

    public static void Begin(Camera videoCamera)
    {
        End();

        if (videoCamera == null)
            videoCamera = Camera.main;

        var cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            var cam = cameras[i];
            if (cam == null) continue;

            Saved.Add(new SavedCameraState
            {
                Camera = cam,
                CullingMask = cam.cullingMask,
                ClearFlags = cam.clearFlags,
                BackgroundColor = cam.backgroundColor,
                Enabled = cam.enabled
            });

            if (videoCamera != null && cam == videoCamera)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
            else
            {
                cam.enabled = false;
            }
        }
    }

    public static void End()
    {
        for (int i = 0; i < Saved.Count; i++)
        {
            var s = Saved[i];
            if (s.Camera == null) continue;

            s.Camera.cullingMask = s.CullingMask;
            s.Camera.clearFlags = s.ClearFlags;
            s.Camera.backgroundColor = s.BackgroundColor;
            s.Camera.enabled = s.Enabled;
        }

        Saved.Clear();
    }
}
