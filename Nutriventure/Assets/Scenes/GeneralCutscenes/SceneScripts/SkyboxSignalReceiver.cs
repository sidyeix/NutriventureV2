using UnityEngine;

public class SkyboxSignalReceiver : MonoBehaviour
{
    [Header("Skybox Materials")]
    public Material daySkybox;
    public Material nightSkybox;
    public Material sunsetSkybox;

    // These methods will be called by signals
    public void SetDaySkybox()
    {
        if (daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
            Debug.Log("Skybox changed to: Day");
            DynamicGI.UpdateEnvironment(); // Update lighting
        }
    }

    public void SetNightSkybox()
    {
        if (nightSkybox != null)
        {
            RenderSettings.skybox = nightSkybox;
            Debug.Log("Skybox changed to: Night");
            DynamicGI.UpdateEnvironment();
        }
    }

    public void SetSunsetSkybox()
    {
        if (sunsetSkybox != null)
        {
            RenderSettings.skybox = sunsetSkybox;
            Debug.Log("Skybox changed to: Sunset");
            DynamicGI.UpdateEnvironment();
        }
    }

    // Universal method (if you want one method for all)
    public void ChangeToSkybox(Material newSkybox)
    {
        if (newSkybox != null)
        {
            RenderSettings.skybox = newSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }
}