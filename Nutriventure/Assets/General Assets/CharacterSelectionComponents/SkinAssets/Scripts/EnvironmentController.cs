using UnityEngine;
using System.Collections.Generic;

public class EnvironmentController : MonoBehaviour
{
    [Header("Environment Parents")]
    public GameObject mainMenuEnvironment;
    public List<GameObject> skinShowcaseEnvironments = new List<GameObject>();

    [Header("Platform Object")]
    public GameObject mainMenuPlatform;

    public bool IsInSkinEnvironment { get; private set; }

    void Start()
    {
        // Start with main menu
        SwitchToMainEnvironment();
    }

    public void SwitchToSkinEnvironment(int environmentIndex = 0)
    {
        // Hide main environment and platform
        if (mainMenuEnvironment != null)
            mainMenuEnvironment.SetActive(false);

        if (mainMenuPlatform != null)
            mainMenuPlatform.SetActive(false);

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
        // Hide all skin environments
        ForceHideAllSkinEnvironments();

        // Show main environment and platform
        if (mainMenuEnvironment != null)
            mainMenuEnvironment.SetActive(true);

        if (mainMenuPlatform != null)
            mainMenuPlatform.SetActive(true);

        IsInSkinEnvironment = false;
        Debug.Log("Now in MAIN environment");
    }

    // NEW: Public method to force hide all skin environments
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

    // NEW: Simple method to ensure we're in main environment for normal skins
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
            // Ensure main environment is active
            if (mainMenuEnvironment != null)
                mainMenuEnvironment.SetActive(true);
            if (mainMenuPlatform != null)
                mainMenuPlatform.SetActive(true);
        }
    }

    // Method to switch to a specific skin environment by GameObject reference
    public void SwitchToSpecificSkinEnvironment(GameObject specificEnvironment)
    {
        SwitchToMainEnvironment(); // Reset first
        SwitchToSkinEnvironment(); // Then show skin environment

        // Or if you want to activate a specific one:
        /*
        if (specificEnvironment != null && skinShowcaseEnvironments.Contains(specificEnvironment))
        {
            SwitchToMainEnvironment(); // Reset
            ForceHideAllSkinEnvironments();
            specificEnvironment.SetActive(true);
            IsInSkinEnvironment = true;
        }
        */
    }
}