using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("Skybox Materials")]
    public Material daySkybox;
    public Material nightSkybox;
    public Material sunsetSkybox;

    // Method 1: Change by material reference
    public void ChangeToDaySky()
    {
        if (daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
            UpdateLighting();
            Debug.Log("Changed to Day Skybox");
        }
    }

    public void ChangeToNightSky()
    {
        if (nightSkybox != null)
        {
            RenderSettings.skybox = nightSkybox;
            UpdateLighting();
            Debug.Log("Changed to Night Skybox");
        }
    }

    public void ChangeToSunsetSky()
    {
        if (sunsetSkybox != null)
        {
            RenderSettings.skybox = sunsetSkybox;
            UpdateLighting();
            Debug.Log("Changed to Sunset Skybox");
        }
    }

    // Method 2: Change any material
    public void ChangeSkyboxMaterial(Material newSkybox)
    {
        if (newSkybox != null)
        {
            RenderSettings.skybox = newSkybox;
            UpdateLighting();
        }
    }

    // Method 3: Change by index (0=day, 1=night, 2=sunset)
    public void ChangeSkyboxByIndex(int index)
    {
        switch (index)
        {
            case 0:
                ChangeToDaySky();
                break;
            case 1:
                ChangeToNightSky();
                break;
            case 2:
                ChangeToSunsetSky();
                break;
            default:
                Debug.LogWarning("Invalid skybox index");
                break;
        }
    }

    private void UpdateLighting()
    {
        // Update global illumination if needed
        DynamicGI.UpdateEnvironment();
    }
}