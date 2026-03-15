using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Attach to a persistent GameObject (DontDestroyOnLoad).
/// Applies mobile-friendly quality and performance settings at startup.
/// </summary>
public class MobileOptimizationManager : MonoBehaviour
{
    public static MobileOptimizationManager Instance { get; private set; }

    [Header("Frame Rate")]
    [SerializeField] private int targetFrameRate = 30;
    [SerializeField] private bool useVSync = false;

    [Header("Rendering")]
    [SerializeField] private bool disableShadows = false;
    [SerializeField] private ShadowResolution shadowResolution = ShadowResolution.Low;
    [SerializeField] private int pixelLightCount = 1;
    [SerializeField] private float shadowDistance = 20f;

    [Header("Physics")]
    [SerializeField] private float fixedTimestep = 0.03333f; // ~30 Hz instead of 50 Hz
    [SerializeField] private int defaultSolverIterations = 4;

    [Header("GC Management")]
    [Tooltip("Trigger incremental GC collection periodically to avoid hitches")]
    [SerializeField] private bool enableIncrementalGC = true;

    [Header("LOD / Draw Distance")]
    [SerializeField] private float lodBias = 0.7f;
    [SerializeField] private int maximumLODLevel = 0;

    [Header("Texture")]
    [SerializeField] private int globalTextureMipmapLimit = 1; // Half resolution on mobile

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplySettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ApplySettings()
    {
        // Frame rate
        QualitySettings.vSyncCount = useVSync ? 1 : 0;
        Application.targetFrameRate = targetFrameRate;

        // Rendering
        if (disableShadows)
        {
            QualitySettings.shadows = ShadowQuality.Disable;
        }
        else
        {
            QualitySettings.shadowResolution = shadowResolution;
            QualitySettings.shadowDistance = shadowDistance;
        }
        QualitySettings.pixelLightCount = pixelLightCount;

        // LOD
        QualitySettings.lodBias = lodBias;
        QualitySettings.maximumLODLevel = maximumLODLevel;

        // Texture
        QualitySettings.globalTextureMipmapLimit = globalTextureMipmapLimit;

        // Physics
        Time.fixedDeltaTime = fixedTimestep;
        Physics.defaultSolverIterations = defaultSolverIterations;

        // Disable GPU skinning on low-end devices
        if (SystemInfo.graphicsMemorySize < 1024)
        {
            QualitySettings.skinWeights = SkinWeights.TwoBones;
        }

        // Sleep timeout — prevent screen from dimming during gameplay
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_EDITOR
        Debug.Log($"[MobileOptimization] Applied: {targetFrameRate}fps, shadows={!disableShadows}, " +
                  $"fixedDt={fixedTimestep}, lodBias={lodBias}, texMip={globalTextureMipmapLimit}");
#endif
    }

    /// <summary>
    /// Switch to high-performance settings (e.g. during gameplay timer).
    /// </summary>
    public void SetHighPerformanceMode()
    {
        Application.targetFrameRate = 60;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.globalTextureMipmapLimit = 2;
    }

    /// <summary>
    /// Revert to balanced settings (e.g. menus, cutscenes).
    /// </summary>
    public void SetBalancedMode()
    {
        ApplySettings();
    }
}
