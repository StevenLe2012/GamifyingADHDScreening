using UnityEngine;
using Cinemachine;

public class MoxoCameraDirector : MonoBehaviour
{
    [Header("Virtual Camera (persistent, NOT under island roots)")]
    [SerializeField] private CinemachineVirtualCamera moxoVcam;

    [Header("Priorities")]
    [SerializeField] private int normalPriority = 0;
    [SerializeField] private int moxoPriority = 50;

    [Header("Disable Look During MOXO")]
    [SerializeField] private DesktopArrowController playerController;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorDuringMoxo = true;
    [SerializeField] private bool relockCursorOnExit = true;

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private bool _inMoxo;

    private void Awake()
    {
        if (!playerController) playerController = FindObjectOfType<DesktopArrowController>(true);

        // Start in normal view
        if (moxoVcam)
        {
            if (!moxoVcam.gameObject.activeSelf) moxoVcam.gameObject.SetActive(true); // keep persistent
            moxoVcam.Priority = normalPriority;
        }

        if (playerController) playerController.enabled = true;

        _inMoxo = false;
    }

    public void EnterMoxoView(Transform anchor)
    {
        if (!anchor)
        {
            Debug.LogWarning("[MoxoCameraDirector] EnterMoxoView called with NULL anchor.");
            return;
        }
        if (!moxoVcam)
        {
            Debug.LogWarning("[MoxoCameraDirector] moxoVcam reference is missing.");
            return;
        }

        // Ensure vcam is enabled (in case something disabled it)
        if (!moxoVcam.gameObject.activeInHierarchy)
            moxoVcam.gameObject.SetActive(true);

        // Snap MOXO vcam to anchor pose
        moxoVcam.transform.position = anchor.position;
        moxoVcam.transform.rotation = anchor.rotation;

        moxoVcam.Priority = moxoPriority;
        _inMoxo = true;

        if (playerController) playerController.enabled = false;

        if (lockCursorDuringMoxo)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (log) Debug.Log($"[MoxoCameraDirector] ENTER MOXO VIEW -> anchor='{anchor.name}' pos={anchor.position}");
    }

    public void ExitMoxoView()
    {
        if (!moxoVcam) return;

        moxoVcam.Priority = normalPriority;

        if (playerController) playerController.enabled = true;

        // Restore desktop look control state on MOXO exit.
        // With DesktopArrowController safety gates, unlocked cursor can block look updates.
        if (relockCursorOnExit)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        _inMoxo = false;

        if (log) Debug.Log("[MoxoCameraDirector] EXIT MOXO VIEW");
    }

    public bool IsInMoxoView => _inMoxo;
}
