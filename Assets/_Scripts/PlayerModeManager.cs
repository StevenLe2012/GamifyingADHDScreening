using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;
using System.Collections;

public enum PlayerMode { Desktop, VR }

public class PlayerModeManager : MonoBehaviour
{
    public PlayerMode startMode = PlayerMode.Desktop;
    public GameObject desktopRig;
    public GameObject vrRig;

    public PlayerMode CurrentMode => _currentMode;
    private PlayerMode _currentMode;
    private bool _isSwitching;

    public Transform CurrentPlayerRoot
    {
        get
        {
            // Prefer whichever rig is currently active in the hierarchy
            if (vrRig   && vrRig.activeInHierarchy)    return vrRig.transform;
            if (desktopRig && desktopRig.activeInHierarchy) return desktopRig.transform;
            // Fallback to the configured startMode
            return (startMode == PlayerMode.VR ? vrRig : desktopRig)?.transform;
        }
    }

    private void Start()
    {
        StartCoroutine(SetModeCoroutine(startMode));
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame && !_isSwitching)
        {
            var next = _currentMode == PlayerMode.VR ? PlayerMode.Desktop : PlayerMode.VR;
            StartCoroutine(SetModeCoroutine(next));
        }
    }

    private IEnumerator SetModeCoroutine(PlayerMode mode)
    {
        _isSwitching = true;
        _currentMode = mode;

        var xrManager = XRGeneralSettings.Instance.Manager;

        if (mode == PlayerMode.VR)
        {
            // Enable XR
            if (!xrManager.isInitializationComplete)
            {
                yield return xrManager.InitializeLoader();
                xrManager.StartSubsystems();
            }

            desktopRig.SetActive(false);
            vrRig.SetActive(true);
        }
        else // Desktop
        {
            // Disable XR
            if (xrManager.isInitializationComplete)
            {
                xrManager.StopSubsystems();
                xrManager.DeinitializeLoader();
            }

            vrRig.SetActive(false);
            desktopRig.SetActive(true);
        }

        _isSwitching = false;
    }
}
