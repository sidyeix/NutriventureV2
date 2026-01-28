using UnityEngine;

public class DayNightLightingTransition : MonoBehaviour
{
    [Header("Skybox Settings")]
    [Tooltip("Daytime skybox material")]
    public Material daySkybox;

    [Tooltip("Nighttime skybox material")]
    public Material nightSkybox;

    [Header("Day Lights (initially active)")]
    [Tooltip("First directional light for daytime")]
    public Light dayLight1;
    public float dayLight1Intensity = 1.0f; // Explicit intensity

    [Tooltip("Second directional light for daytime")]
    public Light dayLight2;
    public float dayLight2Intensity = 1.0f; // Explicit intensity

    [Header("Night Lights (initially INACTIVE GameObjects)")]
    [Tooltip("GameObject containing all night lights")]
    public GameObject nightLightsParent;

    [Tooltip("First directional light for nighttime")]
    public Light nightLight1;
    public float nightLight1Intensity = 0.51f; // Explicit intensity

    [Tooltip("Second directional light for nighttime")]
    public Light nightLight2;
    public float nightLight2Intensity = 0.51f; // Explicit intensity

    [Tooltip("Third directional light for nighttime")]
    public Light nightLight3;
    public float nightLight3Intensity = 0.51f; // Explicit intensity

    [Header("Transition Settings")]
    [Tooltip("Time in seconds for the lighting transition")]
    public float transitionTime = 5f;

    [Header("Trigger Settings")]
    [Tooltip("Trigger transition when player enters collider")]
    public bool triggerOnEnter = true;

    [Tooltip("Only trigger once")]
    public bool triggerOnce = false;

    [Tooltip("Player tag to check for")]
    public string playerTag = "Player";

    // Store colors (we'll keep these as-is)
    private Color dayLight1Color;
    private Color dayLight2Color;
    private Color nightLight1Color;
    private Color nightLight2Color;
    private Color nightLight3Color;

    // State
    private bool isDayTime = true;
    private bool isTransitioning = false;
    private float transitionProgress = 0f;
    private float transitionStartTime;
    private bool hasTriggered = false;
    private bool nightLightsActivated = false;

    void Start()
    {
        // Store colors only (intensities are already set in Inspector)
        StoreLightColors();

        // Initialize: Day lights active, night lights GameObject inactive
        InitializeDaytime();

        ValidateSetup();
    }

    void Update()
    {
        if (isTransitioning)
        {
            UpdateLightTransition();
        }
    }

    private void StoreLightColors()
    {
        // Store light colors only
        if (dayLight1 != null)
        {
            dayLight1Color = dayLight1.color;
        }

        if (dayLight2 != null)
        {
            dayLight2Color = dayLight2.color;
        }

        if (nightLight1 != null)
        {
            nightLight1Color = nightLight1.color;
        }

        if (nightLight2 != null)
        {
            nightLight2Color = nightLight2.color;
        }

        if (nightLight3 != null)
        {
            nightLight3Color = nightLight3.color;
        }
    }

    private void ActivateAndInitializeNightLights()
    {
        // Activate the night lights GameObject first (ENABLE GAMEOBJECT)
        if (nightLightsParent != null && !nightLightsParent.activeSelf)
        {
            nightLightsParent.SetActive(true); // ENABLE GAMEOBJECT
            nightLightsActivated = true;
        }
    }

    private void InitializeDaytime()
    {
        // Set to day skybox
        if (daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
            DynamicGI.UpdateEnvironment();
        }

        // Ensure day lights are enabled at explicit intensity
        if (dayLight1 != null)
        {
            dayLight1.enabled = true;
            dayLight1.intensity = dayLight1Intensity; // Use explicit intensity
            dayLight1.color = dayLight1Color;
        }
        if (dayLight2 != null)
        {
            dayLight2.enabled = true;
            dayLight2.intensity = dayLight2Intensity; // Use explicit intensity
            dayLight2.color = dayLight2Color;
        }

        // Ensure night lights GameObject is inactive (DISABLE GAMEOBJECT)
        if (nightLightsParent != null)
        {
            nightLightsParent.SetActive(false); // DISABLE GAMEOBJECT
            nightLightsActivated = false;
        }

        isDayTime = true;
        Debug.Log("Initialized to Daytime - Day lights at explicit intensity");
    }

    private void ValidateSetup()
    {
        if (daySkybox == null) Debug.LogError("Day Skybox is not assigned!");
        if (nightSkybox == null) Debug.LogError("Night Skybox is not assigned!");

        if (dayLight1 == null) Debug.LogWarning("Day Light 1 is not assigned!");
        if (dayLight2 == null) Debug.LogWarning("Day Light 2 is not assigned!");

        if (nightLightsParent == null) Debug.LogWarning("Night Lights Parent GameObject is not assigned!");

        if (nightLight1 == null) Debug.LogWarning("Night Light 1 is not assigned!");
        if (nightLight2 == null) Debug.LogWarning("Night Light 2 is not assigned!");
        if (nightLight3 == null) Debug.LogWarning("Night Light 3 is not assigned!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter) return;

        if (other.CompareTag(playerTag))
        {
            if (triggerOnce && hasTriggered) return;

            StartTransition();
            hasTriggered = true;
        }
    }

    public void StartTransition()
    {
        if (isTransitioning) return;

        if (isDayTime)
        {
            Debug.Log("Starting transition: Day ? Night");

            // Activate night lights GameObject (ENABLE GAMEOBJECT)
            ActivateAndInitializeNightLights();

            // Set night lights to 0 intensity for fade in
            if (nightLight1 != null)
            {
                nightLight1.intensity = 0f;
                nightLight1.color = Color.black;
            }
            if (nightLight2 != null)
            {
                nightLight2.intensity = 0f;
                nightLight2.color = Color.black;
            }
            if (nightLight3 != null)
            {
                nightLight3.intensity = 0f;
                nightLight3.color = Color.black;
            }

            // Instantly switch to night skybox
            if (nightSkybox != null)
            {
                RenderSettings.skybox = nightSkybox;
                DynamicGI.UpdateEnvironment();
            }
        }
        else
        {
            Debug.Log("Starting transition: Night ? Day");

            // Enable day lights but set intensity to 0 for fade in
            if (dayLight1 != null)
            {
                dayLight1.enabled = true;
                dayLight1.intensity = 0f;
                dayLight1.color = Color.black;
            }
            if (dayLight2 != null)
            {
                dayLight2.enabled = true;
                dayLight2.intensity = 0f;
                dayLight2.color = Color.black;
            }

            // Instantly switch to day skybox
            if (daySkybox != null)
            {
                RenderSettings.skybox = daySkybox;
                DynamicGI.UpdateEnvironment();
            }
        }

        // Start light transition
        transitionStartTime = Time.time;
        transitionProgress = 0f;
        isTransitioning = true;
    }

    private void UpdateLightTransition()
    {
        float elapsedTime = Time.time - transitionStartTime;
        transitionProgress = Mathf.Clamp01(elapsedTime / transitionTime);

        if (isDayTime)
        {
            // Day ? Night: Fade out day lights, fade in night lights SIMULTANEOUSLY
            UpdateDayToNightTransition(transitionProgress);
        }
        else
        {
            // Night ? Day: Fade out night lights, fade in day lights SIMULTANEOUSLY
            UpdateNightToDayTransition(transitionProgress);
        }

        if (transitionProgress >= 1f)
        {
            // Transition complete
            isTransitioning = false;
            isDayTime = !isDayTime; // Toggle time of day

            // Ensure final values are correct
            if (isDayTime)
            {
                SetToDayInstantly();
            }
            else
            {
                SetToNightInstantly();
            }

            Debug.Log($"Transition complete: {(isDayTime ? "Day" : "Night")}");
        }
    }

    private void UpdateDayToNightTransition(float progress)
    {
        // SIMULTANEOUS TRANSITION:
        // Day lights fade out AS night lights fade in at the same rate
        // Using EXPLICIT intensities from Inspector

        if (dayLight1 != null)
        {
            // Day light fades from explicit intensity to 0
            dayLight1.intensity = Mathf.Lerp(dayLight1Intensity, 0f, progress);
            dayLight1.color = Color.Lerp(dayLight1Color, Color.black, progress);
        }

        if (dayLight2 != null)
        {
            dayLight2.intensity = Mathf.Lerp(dayLight2Intensity, 0f, progress);
            dayLight2.color = Color.Lerp(dayLight2Color, Color.black, progress);
        }

        if (nightLight1 != null)
        {
            // Night light fades from 0 to explicit intensity
            nightLight1.intensity = Mathf.Lerp(0f, nightLight1Intensity, progress);
            nightLight1.color = Color.Lerp(Color.black, nightLight1Color, progress);
        }

        if (nightLight2 != null)
        {
            nightLight2.intensity = Mathf.Lerp(0f, nightLight2Intensity, progress);
            nightLight2.color = Color.Lerp(Color.black, nightLight2Color, progress);
        }

        if (nightLight3 != null)
        {
            nightLight3.intensity = Mathf.Lerp(0f, nightLight3Intensity, progress);
            nightLight3.color = Color.Lerp(Color.black, nightLight3Color, progress);
        }
    }

    private void UpdateNightToDayTransition(float progress)
    {
        // SIMULTANEOUS TRANSITION:
        // Night lights fade out AS day lights fade in at the same rate
        // Using EXPLICIT intensities from Inspector

        if (nightLight1 != null)
        {
            // Night light fades from explicit intensity to 0
            nightLight1.intensity = Mathf.Lerp(nightLight1Intensity, 0f, progress);
            nightLight1.color = Color.Lerp(nightLight1Color, Color.black, progress);
        }

        if (nightLight2 != null)
        {
            nightLight2.intensity = Mathf.Lerp(nightLight2Intensity, 0f, progress);
            nightLight2.color = Color.Lerp(nightLight2Color, Color.black, progress);
        }

        if (nightLight3 != null)
        {
            nightLight3.intensity = Mathf.Lerp(nightLight3Intensity, 0f, progress);
            nightLight3.color = Color.Lerp(nightLight3Color, Color.black, progress);
        }

        if (dayLight1 != null)
        {
            // Day light fades from 0 to explicit intensity
            dayLight1.intensity = Mathf.Lerp(0f, dayLight1Intensity, progress);
            dayLight1.color = Color.Lerp(Color.black, dayLight1Color, progress);
        }

        if (dayLight2 != null)
        {
            dayLight2.intensity = Mathf.Lerp(0f, dayLight2Intensity, progress);
            dayLight2.color = Color.Lerp(Color.black, dayLight2Color, progress);
        }
    }

    private void SetToDayInstantly()
    {
        // Set skybox
        if (daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
            DynamicGI.UpdateEnvironment();
        }

        // Enable day lights at explicit intensity
        if (dayLight1 != null)
        {
            dayLight1.intensity = dayLight1Intensity; // Use explicit intensity
            dayLight1.color = dayLight1Color;
            dayLight1.enabled = true;
        }

        if (dayLight2 != null)
        {
            dayLight2.intensity = dayLight2Intensity; // Use explicit intensity
            dayLight2.color = dayLight2Color;
            dayLight2.enabled = true;
        }

        // Deactivate night lights GameObject (DISABLE GAMEOBJECT)
        if (nightLightsParent != null)
        {
            nightLightsParent.SetActive(false); // DISABLE GAMEOBJECT
            nightLightsActivated = false;
        }

        isDayTime = true;
    }

    private void SetToNightInstantly()
    {
        // Activate night lights GameObject first (ENABLE GAMEOBJECT)
        if (nightLightsParent != null && !nightLightsParent.activeSelf)
        {
            nightLightsParent.SetActive(true); // ENABLE GAMEOBJECT
            nightLightsActivated = true;
        }

        // Set skybox
        if (nightSkybox != null)
        {
            RenderSettings.skybox = nightSkybox;
            DynamicGI.UpdateEnvironment();
        }

        // Set night lights to explicit intensity
        if (nightLight1 != null)
        {
            nightLight1.intensity = nightLight1Intensity; // Use explicit intensity
            nightLight1.color = nightLight1Color;
        }
        if (nightLight2 != null)
        {
            nightLight2.intensity = nightLight2Intensity; // Use explicit intensity
            nightLight2.color = nightLight2Color;
        }
        if (nightLight3 != null)
        {
            nightLight3.intensity = nightLight3Intensity; // Use explicit intensity
            nightLight3.color = nightLight3Color;
        }

        // Disable day lights (light component only, GameObject stays active)
        if (dayLight1 != null) dayLight1.enabled = false;
        if (dayLight2 != null) dayLight2.enabled = false;

        isDayTime = false;
    }

    // Public methods for manual control
    public void TriggerTransition()
    {
        StartTransition();
    }

    public void SetToDay()
    {
        StopTransition();
        SetToDayInstantly();
        Debug.Log("Set to Day (instant)");
    }

    public void SetToNight()
    {
        StopTransition();
        SetToNightInstantly();
        Debug.Log("Set to Night (instant)");
    }

    public void ToggleDayNight()
    {
        if (isDayTime)
        {
            SetToNight();
        }
        else
        {
            SetToDay();
        }
    }

    private void StopTransition()
    {
        isTransitioning = false;
        transitionProgress = 0f;
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    public bool IsDayTime()
    {
        return isDayTime;
    }

    public float GetTransitionProgress()
    {
        return transitionProgress;
    }

    public void SetTransitionTime(float time)
    {
        transitionTime = Mathf.Max(0.1f, time);
        Debug.Log($"Transition time set to: {transitionTime} seconds");
    }

    void OnDestroy()
    {
        StopTransition();
    }

    public void ResetTransition()
    {
        Debug.Log($"Resetting DayNightLightingTransition on {gameObject.name}");

        // Stop any active transition
        isTransitioning = false;
        transitionProgress = 0f;

        // Reset to daytime
        SetToDay();

        // Reset trigger state
        hasTriggered = false;

        // Re-enable collider if it exists and triggerOnce is enabled
        Collider collider = GetComponent<Collider>();
        if (collider != null && triggerOnce)
        {
            collider.enabled = true;
        }

        Debug.Log($"DayNightLightingTransition reset to Daytime");
    }
}