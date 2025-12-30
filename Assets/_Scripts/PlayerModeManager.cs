using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;

public enum PlayerMode { Desktop, VR }

public class PlayerModeManager : MonoBehaviour
{
    [Header("Startup")]
    public PlayerMode startMode = PlayerMode.Desktop;
    [Tooltip("Persist last used mode across play sessions.")]
    [SerializeField] private bool rememberLastMode = false;

    [Header("Rigs")]
    public GameObject desktopRig;
    public GameObject vrRig;

    // Event so other systems can react (EyeTrackLogger, etc.)
    public event Action<PlayerMode> OnModeChanged;

    public PlayerMode CurrentMode => _currentMode;
    private PlayerMode _currentMode;
    private bool _isSwitching;

    public Transform CurrentPlayerRoot
    {
        get
        {
            if (vrRig && vrRig.activeInHierarchy) return vrRig.transform;
            if (desktopRig && desktopRig.activeInHierarchy) return desktopRig.transform;
            return (startMode == PlayerMode.VR ? vrRig : desktopRig)?.transform;
        }
    }

    private const string kPrefsKey = "PlayerMode.Last";

    private void Start()
    {
        // Optional: restore last mode
        var initial = startMode;
        if (rememberLastMode && PlayerPrefs.HasKey(kPrefsKey))
        {
            initial = (PlayerMode)PlayerPrefs.GetInt(kPrefsKey, (int)startMode);
        }

        StartCoroutine(SetModeCoroutine(initial));
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame && !_isSwitching)
        {
            ToggleMode();
        }
    }

    public void ToggleMode()
    {
        var next = _currentMode == PlayerMode.VR ? PlayerMode.Desktop : PlayerMode.VR;
        StartCoroutine(SetModeCoroutine(next));
    }

    // Public imperative switch (use this from other scripts/UI)
    public void SetMode(PlayerMode mode)
    {
        if (!_isSwitching && mode != _currentMode)
            StartCoroutine(SetModeCoroutine(mode));
    }

    private IEnumerator SetModeCoroutine(PlayerMode mode)
    {
        _isSwitching = true;

        // Cache manager once
        var xrManager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;

        if (mode == PlayerMode.VR)
        {
            // Enable XR if needed
            if (xrManager != null && !xrManager.isInitializationComplete)
            {
                yield return xrManager.InitializeLoader();
                xrManager.StartSubsystems();
            }

            if (desktopRig) desktopRig.SetActive(false);
            if (vrRig) vrRig.SetActive(true);
        }
        else // Desktop
        {
            // Disable XR if active
            if (xrManager != null && xrManager.isInitializationComplete)
            {
                xrManager.StopSubsystems();
                xrManager.DeinitializeLoader();
            }

            if (vrRig) vrRig.SetActive(false);
            if (desktopRig) desktopRig.SetActive(true);
        }

        _currentMode = mode;
        _isSwitching = false;

        if (rememberLastMode) PlayerPrefs.SetInt(kPrefsKey, (int)_currentMode);

        // Notify listeners
        try { OnModeChanged?.Invoke(_currentMode); }
        catch (Exception e) { Debug.LogException(e); }
    }
}
