using StarterAssets;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Starter Assets References")]
    public GameObject playerController; // Your PlayerArmature GameObject
    public ThirdPersonController thirdPersonController; // Use specific type
    public StarterAssetsInputs starterAssetsInputs; // Use specific type
    public BasicRigidBodyPush basicRigidBodyPush; // Use specific type (if exists)

    private bool inputEnabled = false;

    void Start()
    {
        // Auto-find references if not assigned
        if (playerController == null)
            playerController = GameObject.Find("PlayerArmature");

        if (thirdPersonController == null && playerController != null)
            thirdPersonController = playerController.GetComponent<ThirdPersonController>();

        if (starterAssetsInputs == null && playerController != null)
            starterAssetsInputs = playerController.GetComponent<StarterAssetsInputs>();

        if (basicRigidBodyPush == null && playerController != null)
            basicRigidBodyPush = playerController.GetComponent<BasicRigidBodyPush>();

        // Disable input by default
        DisablePlayerInput();

        Debug.Log("InputManager initialized - Only movement scripts will be disabled");
    }

    public void EnablePlayerInput()
    {
        if (thirdPersonController != null)
            thirdPersonController.enabled = true;

        if (starterAssetsInputs != null)
            starterAssetsInputs.enabled = true;

        if (basicRigidBodyPush != null)
            basicRigidBodyPush.enabled = true;

        inputEnabled = true;
        Debug.Log("Player input ENABLED - Movement scripts activated");
    }

    public void DisablePlayerInput()
    {
        if (thirdPersonController != null)
            thirdPersonController.enabled = false;

        if (starterAssetsInputs != null)
            starterAssetsInputs.enabled = false;

        if (basicRigidBodyPush != null)
            basicRigidBodyPush.enabled = false;

        inputEnabled = false;
        Debug.Log("Player input DISABLED - Only movement scripts disabled, animator remains active");
    }

    public bool IsInputEnabled()
    {
        return inputEnabled;
    }
}