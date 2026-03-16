using UnityEngine;
using UnityEngine.Rendering;

public class MainLightingReset : MonoBehaviour
{
    [Header("Environment")]
    public Material skybox;

    private void Start()
    {
        ApplyLighting();
    }

    private void ApplyLighting()
    {
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();
        }
    }
}


