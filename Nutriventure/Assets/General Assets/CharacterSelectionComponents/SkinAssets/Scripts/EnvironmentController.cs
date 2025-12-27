using UnityEngine;
using System.Collections.Generic;

public class EnvironmentController : MonoBehaviour
{
    [Header("Environment Switching")]
    public List<GameObject> mainEnvironmentObjects = new List<GameObject>(); // Objects to show in main environment
    public List<GameObject> skinShowcaseEnvironments = new List<GameObject>(); // Skin-specific environments

    [Header("Objects to Hide During Timelines")]
    public List<GameObject> objectsToHideDuringTimeline = new List<GameObject>(); // Objects to hide when timeline plays

    private List<GameObject> currentlyHiddenObjects = new List<GameObject>();

    public bool IsInSkinEnvironment { get; private set; }

    void Start()
    {
        // Start with main environment
        SwitchToMainEnvironment();
    }

    public void SwitchToSkinEnvironment(int environmentIndex = 0)
    {
        // Hide main environment objects
        SetMainEnvironmentObjectsActive(false);

        // Hide objects for timeline
        HideTimelineObjects();

        // Deactivate all skin environments first
        ForceHideAllSkinEnvironments();

        // Activate the specific skin environment if index is valid
        if (environmentIndex >= 0 && environmentIndex < skinShowcaseEnvironments.Count &&
            skinShowcaseEnvironments[environmentIndex] != null)
        {
            skinShowcaseEnvironments[environmentIndex].SetActive(true);
        }
        else if (skinShowcaseEnvironments.Count > 0 && skinShowcaseEnvironments[0] != null)
        {
            // Fallback to first environment
            skinShowcaseEnvironments[0].SetActive(true);
        }

        IsInSkinEnvironment = true;
        Debug.Log($"Now in SKIN environment (Index: {environmentIndex})");
    }

    public void SwitchToMainEnvironment()
    {
        // Show all hidden objects
        ShowAllHiddenObjects();

        // Hide all skin environments
        ForceHideAllSkinEnvironments();

        // Show main environment objects
        SetMainEnvironmentObjectsActive(true);

        IsInSkinEnvironment = false;
        Debug.Log("Now in MAIN environment");
    }

    // NEW: Hide specific objects during timeline
    private void HideTimelineObjects()
    {
        currentlyHiddenObjects.Clear();

        foreach (var obj in objectsToHideDuringTimeline)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
                currentlyHiddenObjects.Add(obj);
            }
        }

        Debug.Log($"Hid {currentlyHiddenObjects.Count} objects for timeline");
    }

    // NEW: Show all previously hidden objects
    private void ShowAllHiddenObjects()
    {
        foreach (var obj in currentlyHiddenObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        currentlyHiddenObjects.Clear();
        Debug.Log($"Restored {currentlyHiddenObjects.Count} hidden objects");
    }

    // Public method to force hide all skin environments
    public void ForceHideAllSkinEnvironments()
    {
        foreach (var env in skinShowcaseEnvironments)
        {
            if (env != null)
            {
                env.SetActive(false);
            }
        }
    }

    // Set main environment objects active/inactive
    private void SetMainEnvironmentObjectsActive(bool active)
    {
        foreach (var obj in mainEnvironmentObjects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    // Method to ensure we're in main environment for normal skins
    public void EnsureMainEnvironmentForNormalSkin()
    {
        if (IsInSkinEnvironment)
        {
            SwitchToMainEnvironment();
        }
        else
        {
            // Double-check no skin environments are active
            ForceHideAllSkinEnvironments();
            // Ensure main environment objects are active
            SetMainEnvironmentObjectsActive(true);
        }
    }
}