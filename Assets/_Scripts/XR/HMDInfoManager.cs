using UnityEngine;
using UnityEngine.XR;

/*
 * This function is to know if we are using a headset and what headset we are using.
 * It also says if we are using a Mock HMD to test the game.
 */
public class HMDInfoManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (!XRSettings.isDeviceActive)
        {
            Debug.Log("No headset plugged");
            return;
        }
        else if (XRSettings.isDeviceActive && (XRSettings.loadedDeviceName == "Mock HMD"
            || XRSettings.loadedDeviceName == "MockHMDDisplay"))
        {
            Debug.Log("Using Mock HMD");
            return;
        }
        else
        {
            Debug.Log("Using " + XRSettings.loadedDeviceName);
            return;
        }
    }

}
