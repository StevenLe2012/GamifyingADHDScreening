using UnityEngine;
using UnityEngine.XR.Management;

public class DetectVR : MonoBehaviour  //TODO: Change to Scriptable Object for it to load beginning of game rather than scene.
{
    public bool toggleVR = true;
    public GameObject XROrigin;
    public GameObject desktopCharacter;

    void Start()
    {
        // If PlayerModeManager is present let it own the rig switching entirely.
        // Running both causes them to activate different objects simultaneously.
        if (FindObjectOfType<PlayerModeManager>() != null)
        {
            Debug.Log("[DetectVR] PlayerModeManager found – deferring rig activation to it.");
            return;
        }

        if (!toggleVR)
        {
            Debug.Log("[DetectVR] Toggle VR is Off – using desktop character.");
            UseDesktop();
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // XR is not supported in WebGL; always fall back to desktop character.
        Debug.Log("[DetectVR] WebGL build detected – XR not supported, using desktop character.");
        UseDesktop();
        return;
#endif

        var xrSettings = XRGeneralSettings.Instance;
        if (xrSettings == null)
        {
            Debug.Log("[DetectVR] XRGeneralSettings is null – defaulting to desktop character.");
            UseDesktop();
            return;
        }

        var xrManager = xrSettings.Manager;
        if (xrManager == null)
        {
            Debug.Log("[DetectVR] XRManagerSettings is null – defaulting to desktop character.");
            UseDesktop();
            return;
        }

        // No headset connected
        var xrLoader = xrManager.activeLoader;
        if (xrLoader == null)
        {
            Debug.Log("[DetectVR] No VR headset detected – using desktop character.");
            UseDesktop();
            return;
        }

        // VR headset found
        Debug.Log("[DetectVR] VR headset detected – using XR rig.");
        if (XROrigin)        XROrigin.SetActive(true);
        if (desktopCharacter) desktopCharacter.SetActive(false);
    }

    void UseDesktop()
    {
        if (XROrigin)        XROrigin.SetActive(false);
        if (desktopCharacter) desktopCharacter.SetActive(true);
    }
}
