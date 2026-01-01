// FollowCameraBillboard.cs
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(50)] // after most movers
public class FollowCameraBillboard : MonoBehaviour
{
    [Header("Target")]
    public Camera cam;                           // auto-filled if null
    [Tooltip("Meters in front of the camera.")]
    public float distance = 1.8f;
    [Tooltip("Offset relative to camera right/up (meters).")]
    public Vector2 xyOffset = new Vector2(0f, -0.2f);

    [Header("Motion")]
    public bool follow = true;
    public float moveLerp = 12f;
    public float rotLerp  = 12f;
    public bool keepUpright = true;             // yaw-only if true

    public Camera Camera => cam;

    void OnEnable()
    {
        if (!cam) AttachToActiveCamera();
        SnapNow();
    }

    void LateUpdate()
    {
        if (!follow || !cam) return;

        var right = cam.transform.right;
        var up    = cam.transform.up;
        var fwd   = cam.transform.forward;

        var targetPos = cam.transform.position + fwd * distance + right * xyOffset.x + up * xyOffset.y;
        transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-moveLerp * Time.unscaledDeltaTime));

        // face camera
        var lookDir = (transform.position - cam.transform.position);
        if (keepUpright) lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotLerp * Time.unscaledDeltaTime));
        }
    }

    public void AttachToActiveCamera()
    {
        // prefer main, else any active, else even inactive (via Resources)
        if (Camera.main) { cam = Camera.main; return; }

        var active = Object.FindObjectsOfType<Camera>();
        if (active.Length > 0) { cam = active[0]; return; }

        var all = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (var c in all)
        {
            if (c.hideFlags == HideFlags.None) { cam = c; return; }
        }
    }

    public void SnapNow()
    {
        if (!cam) return;
        var right = cam.transform.right;
        var up    = cam.transform.up;
        var fwd   = cam.transform.forward;

        transform.position = cam.transform.position + fwd * distance + right * xyOffset.x + up * xyOffset.y;

        var lookDir = (transform.position - cam.transform.position);
        if (keepUpright) lookDir.y = 0f;
        transform.rotation = Quaternion.LookRotation(lookDir.sqrMagnitude < 0.0001f ? fwd : lookDir.normalized, Vector3.up);
    }

    public void EnableForSeconds(float seconds)
    {
        StopAllCoroutines();
        StartCoroutine(CoEnable(seconds));
    }

    private IEnumerator CoEnable(float seconds)
    {
        follow = true;
        enabled = true;
        SnapNow();
        yield return new WaitForSeconds(seconds);
        follow = false;
        enabled = false;
    }
}
