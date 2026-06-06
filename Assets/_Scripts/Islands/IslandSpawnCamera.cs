using System.Collections;
using UnityEngine;

/// <summary>
/// Applies a deterministic camera pose when the player teleports to an island.
/// </summary>
public static class IslandSpawnCamera
{
    const float DefaultHoldSeconds = 0.35f;

    public static void Apply(Transform playerRoot, Transform teleportAnchor)
    {
        ExitMoxoView();

        if (playerRoot == null || teleportAnchor == null)
            return;

        GameplayCameraClip.Apply();

        var look = FindLookController(playerRoot);
        if (look == null)
            return;

        look.enabled = true;
        GetSpawnPose(teleportAnchor, out var bodyRotation, out var pitch);
        look.RestoreLookPose(bodyRotation, pitch);
    }

    public static IEnumerator CoHoldSpawnPose(Transform playerRoot, Transform teleportAnchor, float seconds = DefaultHoldSeconds)
    {
        if (playerRoot == null || teleportAnchor == null || seconds <= 0f)
            yield break;

        var look = FindLookController(playerRoot);
        if (look == null)
            yield break;

        GetSpawnPose(teleportAnchor, out var bodyRotation, out var pitch);

        float t = 0f;
        while (t < seconds)
        {
            look.RestoreLookPose(bodyRotation, pitch);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public static void GetSpawnPose(Transform teleportAnchor, out Quaternion bodyRotation, out float pitch)
    {
        var anchor = teleportAnchor.GetComponent<IslandAnchor>();

        float yaw = teleportAnchor.rotation.eulerAngles.y;
        bodyRotation = Quaternion.Euler(0f, yaw, 0f);
        pitch = anchor != null ? anchor.spawnCameraPitch : 0f;
    }

    static void ExitMoxoView()
    {
        MoxoStateCameraSwitch.Instance?.ExitMoxoViewNow();

        var director = Object.FindObjectOfType<MoxoCameraDirector>(true);
        if (director != null)
            director.ExitMoxoView();
    }

    static DesktopArrowController FindLookController(Transform playerRoot)
    {
        if (playerRoot == null) return null;
        return playerRoot.GetComponent<DesktopArrowController>()
               ?? playerRoot.GetComponentInChildren<DesktopArrowController>(true);
    }
}
