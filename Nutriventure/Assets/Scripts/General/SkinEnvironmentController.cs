using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SkinEnvironmentController : MonoBehaviour
{
    [System.Serializable]
    public class EnvironmentMapping
    {
        [Header("Environment Settings")]
        public string environmentName;
        public GameObject environmentObject;

        [Header("Skin Mappings")]
        public List<SkinMapping> skinMappings = new List<SkinMapping>();
    }

    [System.Serializable]
    public class SkinMapping
    {
        public string skinName;
        public int characterID;
        public int skinID;

        // Optional: For debug purposes
        public string GetSkinIdentifier()
        {
            return $"{skinName} (Char:{characterID}, Skin:{skinID})";
        }
    }

    [Header("Environment Configurations")]
    public List<EnvironmentMapping> environmentMappings = new List<EnvironmentMapping>();

    [Header("Default Environment")]
    public GameObject defaultEnvironment;

    [Header("UNIFIED OBJECTS TO DISABLE")]
    [Tooltip("These objects will be disabled when ANY special environment is active")]
    public List<GameObject> unifiedObjectsToDisable = new List<GameObject>();

    [Header("Objects to Enable in Default State")]
    public List<GameObject> defaultObjectsToEnable = new List<GameObject>();

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Track current state
    private EnvironmentMapping currentActiveEnvironment;
    private List<GameObject> currentlyDisabledObjects = new List<GameObject>();

    private void Awake()
    {
        // Ensure default environment is active at start
        ResetToDefaultEnvironment();
    }

    private void Start()
    {
        // Double-check default environment is active
        ResetToDefaultEnvironment();
    }

    /// <summary>
    /// Resets to default environment (called when entering character selection)
    /// </summary>
    public void ResetToDefaultEnvironment()
    {
        if (enableDebugLogs)
        {
            Debug.Log("SkinEnvironmentController: Resetting to default environment");
        }

        // Deactivate all special environments
        foreach (var mapping in environmentMappings)
        {
            if (mapping.environmentObject != null && mapping.environmentObject.activeSelf)
            {
                mapping.environmentObject.SetActive(false);
            }
        }

        // Re-enable all previously disabled objects
        foreach (var obj in currentlyDisabledObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        currentlyDisabledObjects.Clear();

        // Activate default environment
        if (defaultEnvironment != null && !defaultEnvironment.activeSelf)
        {
            defaultEnvironment.SetActive(true);
        }

        // Enable default objects
        foreach (var obj in defaultObjectsToEnable)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }

        currentActiveEnvironment = null;
    }

    /// <summary>
    /// Called when a skin is selected/previewed
    /// </summary>
    public void OnSkinSelected(int characterID, int skinID, string skinName)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"SkinEnvironmentController: Checking environment for {skinName} (Char:{characterID}, Skin:{skinID})");
        }

        // Find matching environment for this skin
        EnvironmentMapping matchingEnvironment = null;

        foreach (var mapping in environmentMappings)
        {
            foreach (var skinMap in mapping.skinMappings)
            {
                if (skinMap.characterID == characterID && skinMap.skinID == skinID)
                {
                    matchingEnvironment = mapping;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"Found matching environment: {mapping.environmentName} for skin {skinMap.GetSkinIdentifier()}");
                    }
                    break;
                }
            }
            if (matchingEnvironment != null) break;
        }

        // If we found a matching environment, activate it
        if (matchingEnvironment != null)
        {
            ActivateEnvironment(matchingEnvironment);
        }
        else
        {
            // No matching environment, revert to default
            if (enableDebugLogs)
            {
                Debug.Log($"No matching environment for skin, reverting to default");
            }
            RevertToDefaultEnvironment();
        }
    }

    /// <summary>
    /// Called when default/normal skin is selected
    /// </summary>
    public void OnDefaultSkinSelected(int characterID)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"SkinEnvironmentController: Default skin selected for character {characterID}, reverting to default environment");
        }
        RevertToDefaultEnvironment();
    }

    /// <summary>
    /// Called when exiting skin selection (back button, select button, etc.)
    /// </summary>
    public void OnExitSkinSelection()
    {
        if (enableDebugLogs)
        {
            Debug.Log("SkinEnvironmentController: Exiting skin selection, reverting to default environment");
        }
        RevertToDefaultEnvironment();
    }

    private void ActivateEnvironment(EnvironmentMapping environment)
    {
        if (environment == null) return;

        // Skip if this environment is already active
        if (currentActiveEnvironment == environment)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"Environment {environment.environmentName} is already active");
            }
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"Activating environment: {environment.environmentName}");
        }

        // Deactivate current environment if any
        if (currentActiveEnvironment != null)
        {
            if (currentActiveEnvironment.environmentObject != null)
            {
                currentActiveEnvironment.environmentObject.SetActive(false);
            }
        }
        else
        {
            // If no current environment, disable default environment
            if (defaultEnvironment != null && defaultEnvironment.activeSelf)
            {
                defaultEnvironment.SetActive(false);
            }
        }

        // DISABLE UNIFIED OBJECTS (same for all special environments)
        foreach (var obj in unifiedObjectsToDisable)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
                if (!currentlyDisabledObjects.Contains(obj))
                {
                    currentlyDisabledObjects.Add(obj);
                }
            }
        }

        // Activate new environment
        if (environment.environmentObject != null && !environment.environmentObject.activeSelf)
        {
            environment.environmentObject.SetActive(true);
        }

        currentActiveEnvironment = environment;
    }

    private void RevertToDefaultEnvironment()
    {
        if (enableDebugLogs)
        {
            Debug.Log("Reverting to default environment");
        }

        // Deactivate current environment if any
        if (currentActiveEnvironment != null)
        {
            if (currentActiveEnvironment.environmentObject != null && currentActiveEnvironment.environmentObject.activeSelf)
            {
                currentActiveEnvironment.environmentObject.SetActive(false);
            }
            currentActiveEnvironment = null;
        }

        // Deactivate all special environments (just to be safe)
        foreach (var mapping in environmentMappings)
        {
            if (mapping.environmentObject != null && mapping.environmentObject.activeSelf)
            {
                mapping.environmentObject.SetActive(false);
            }
        }

        // RE-ENABLE UNIFIED OBJECTS
        foreach (var obj in currentlyDisabledObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        currentlyDisabledObjects.Clear();

        // Activate default environment
        if (defaultEnvironment != null && !defaultEnvironment.activeSelf)
        {
            defaultEnvironment.SetActive(true);
        }

        // Re-enable default objects
        foreach (var obj in defaultObjectsToEnable)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Helper method to find a skin mapping by ID (for setup purposes)
    /// </summary>
    public SkinMapping FindSkinMapping(int characterID, int skinID)
    {
        foreach (var mapping in environmentMappings)
        {
            foreach (var skinMap in mapping.skinMappings)
            {
                if (skinMap.characterID == characterID && skinMap.skinID == skinID)
                {
                    return skinMap;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Helper method to add an object to the unified disable list (can be called from inspector or code)
    /// </summary>
    public void AddObjectToUnifiedDisableList(GameObject obj)
    {
        if (obj != null && !unifiedObjectsToDisable.Contains(obj))
        {
            unifiedObjectsToDisable.Add(obj);
        }
    }

    /// <summary>
    /// Helper method to remove an object from the unified disable list
    /// </summary>
    public void RemoveObjectFromUnifiedDisableList(GameObject obj)
    {
        if (obj != null && unifiedObjectsToDisable.Contains(obj))
        {
            unifiedObjectsToDisable.Remove(obj);
        }
    }
}