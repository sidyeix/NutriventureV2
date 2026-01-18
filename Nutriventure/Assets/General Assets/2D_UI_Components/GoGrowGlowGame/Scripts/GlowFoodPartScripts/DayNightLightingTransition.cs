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

    [Tooltip("Second directional light for daytime")]
    public Light dayLight2;

    [Header("Night Lights (initially INACTIVE GameObject)")]
    [Tooltip("GameObject containing all night lights")]
    public GameObject nightLightsParent;

    [Tooltip("First directional light for nighttime")]
    public Light nightLight1;

    [Tooltip("Second directional light for nighttime")]
    public Light nightLight2;

    [Tooltip("Third directional light for nighttime")]
    public Light nightLight3;

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

    // Stored light values
    private float dayLight1Intensity;
    private float dayLight2Intensity;
    private Color dayLight1Color;
    private Color dayLight2Color;

    private float nightLight1Intensity;
    private float nightLight2Intensity;
    private float nightLight3Intensity;
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
        // Store day light values (active)
        StoreDayLightValues();

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

    private void StoreDayLightValues()
    {
        // Store day light values
        if (dayLight1 != null)
        {
            dayLight1Intensity = dayLight1.intensity;
            dayLight1Color = dayLight1.color;
        }

        if (dayLight2 != null)
        {
            dayLight2Intensity = dayLight2.intensity;
            dayLight2Color = dayLight2.color;
        }
    }

    private void ActivateAndStoreNightLights()
    {
        // Activate the night lights GameObject first
        if (nightLightsParent != null && !nightLightsParent.activeSelf)
        {
            nightLightsParent.SetActive(true);
            nightLightsActivated = true;
        }

        // Now store the night light values
        if (nightLight1 != null)
        {
            nightLight1Intensity = nightLight1.intensity;
            nightLight1Color = nightLight1.color;
        }

        if (nightLight2 != null)
        {
            nightLight2Intensity = nightLight2.intensity;
            nightLight2Color = nightLight2.color;
        }

        if (nightLight3 != null)
        {
            nightLight3Intensity = nightLight3.intensity;
            nightLight3Color = nightLight3.color;
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

        // Ensure day lights are enabled at full intensity
        if (dayLight1 != null)
        {
            dayLight1.enabled = true;
            dayLight1.intensity = dayLight1Intensity;
            dayLight1.color = dayLight1Color;
        }
        if (dayLight2 != null)
        {
            dayLight2.enabled = true;
            dayLight2.intensity = dayLight2Intensity;
            dayLight2.color = dayLight2Color;
        }

        // Ensure night lights GameObject is inactive
        if (nightLightsParent != null)
        {
            nightLightsParent.SetActive(false);
            nightLightsActivated = false;
        }

        isDayTime = true;
        Debug.Log("Initialized to Daytime");
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

            // Activate night lights GameObject and store values
            ActivateAndStoreNightLights();

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

        if (dayLight1 != null)
        {
            // Day light fades from full to 0
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
            // Night light fades from 0 to full AT THE SAME TIME
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

        // At progress = 0.5, both sets of lights are at 50% intensity
        // They "meet halfway" visually
    }

    private void UpdateNightToDayTransition(float progress)
    {
        // SIMULTANEOUS TRANSITION:
        // Night lights fade out AS day lights fade in at the same rate

        if (nightLight1 != null)
        {
            // Night light fades from full to 0
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
            // Day light fades from 0 to full AT THE SAME TIME
            dayLight1.intensity = Mathf.Lerp(0f, dayLight1Intensity, progress);
            dayLight1.color = Color.Lerp(Color.black, dayLight1Color, progress);
        }

        if (dayLight2 != null)
        {
            dayLight2.intensity = Mathf.Lerp(0f, dayLight2Intensity, progress);
            dayLight2.color = Color.Lerp(Color.black, dayLight2Color, progress);
        }

        // At progress = 0.5, both sets of lights are at 50% intensity
        // They "meet halfway" visually
    }

    private void SetToDayInstantly()
    {
        // Set skybox
        if (daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
            DynamicGI.UpdateEnvironment();
        }

        // Enable day lights at full intensity
        if (dayLight1 != null)
        {
            dayLight1.intensity = dayLight1Intensity;
            dayLight1.color = dayLight1Color;
            dayLight1.enabled = true;
        }

        if (dayLight2 != null)
        {
            dayLight2.intensity = dayLight2Intensity;
            dayLight2.color = dayLight2Color;
            dayLight2.enabled = true;
        }

        // Deactivate night lights GameObject
        if (nightLightsParent != null)
        {
            nightLightsParent.SetActive(false);
            nightLightsActivated = false;
        }

        isDayTime = true;
    }

    private void SetToNightInstantly()
    {
        // Activate night lights GameObject first
        if (nightLightsParent != null && !nightLightsParent.activeSelf)
        {
            nightLightsParent.SetActive(true);
            nightLightsActivated = true;

            // Store night light values (they should be at full intensity in the scene)
            if (nightLight1 != null)
            {
                nightLight1Intensity = nightLight1.intensity;
                nightLight1Color = nightLight1.color;
            }
            if (nightLight2 != null)
            {
                nightLight2Intensity = nightLight2.intensity;
                nightLight2Color = nightLight2.color;
            }
            if (nightLight3 != null)
            {
                nightLight3Intensity = nightLight3.intensity;
                nightLight3Color = nightLight3.color;
            }
        }

        // Set skybox
        if (nightSkybox != null)
        {
            RenderSettings.skybox = nightSkybox;
            DynamicGI.UpdateEnvironment();
        }

        // Set night lights to full intensity
        if (nightLight1 != null)
        {
            nightLight1.intensity = nightLight1Intensity;
            nightLight1.color = nightLight1Color;
        }
        if (nightLight2 != null)
        {
            nightLight2.intensity = nightLight2Intensity;
            nightLight2.color = nightLight2Color;
        }
        if (nightLight3 != null)
        {
            nightLight3.intensity = nightLight3Intensity;
            nightLight3.color = nightLight3Color;
        }

        // Disable day lights
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
}