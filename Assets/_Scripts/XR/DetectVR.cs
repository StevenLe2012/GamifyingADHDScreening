using UnityEngine;
using UnityEngine.XR.Management;

public class DetectVR : MonoBehaviour  //TODO: Change to Scriptable Object for it to load beginning of game rather than scene.
{
    public bool toggleVR = true;
    public GameObject XROrigin;
    public GameObject desktopCharacter;

    // Start is called before the first frame update
    void Start()
    {
        if (!toggleVR)
        {
            Debug.Log("Toggle VR is Off");
            XROrigin.SetActive(false);
            desktopCharacter.SetActive(true);
            return;
        }
        var xrSettings = XRGeneralSettings.Instance;
        if (xrSettings == null)
        {
            Debug.Log("XRGeneralSettings is null");
            return;
        }

        var xrManager = xrSettings.Manager;
        if (xrManager == null)
        {
            Debug.Log("XRManagerSettings is null");
            return;
        }

        // this will run if there is no headset connected
        var xrLoader = xrManager.activeLoader;
        if (xrLoader == null)
        {
            Debug.Log("XRLoaderSettings is null");
            XROrigin.SetActive(false);
            desktopCharacter.SetActive(true);
            return;
        }

        // this will run if there is a headset
        Debug.Log("XRLoaderSettings is NOT null");
        XROrigin.SetActive(true);
        desktopCharacter.SetActive(false);

        return;
    }
}
