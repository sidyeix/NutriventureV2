using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic; // Add this for List support

public class KartTrigger : MonoBehaviour
{
    [Header("UI References")]
    public GameObject playerUI;
    public GameObject driveUI;
    public GameObject kartDrivingUI;
    public TextMeshProUGUI destinationText;
    
    [Header("Player UI Elements to Hide")]
    public GameObject[] playerUIElementsToHide; // Drag UI elements here to hide them
    
    [Header("Kart References")]
    public KartController kartController;
    public Transform kartSeatPosition;
    
    [Header("Destination Settings")]
    public Transform[] destinations; // Multiple destinations
    private int currentDestinationIndex = 0;

    private GameObject player;
    private bool playerInside = false;
    private bool isDriving = false;
    
    // Store original active states
    private Dictionary<GameObject, bool> playerUIElementStates = new Dictionary<GameObject, bool>();

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ No GameObject tagged 'Player' found!");
        }

        // Store original active states of UI elements
        if (playerUIElementsToHide != null)
        {
            foreach (GameObject uiElement in playerUIElementsToHide)
            {
                if (uiElement != null)
                {
                    playerUIElementStates[uiElement] = uiElement.activeSelf;
                }
            }
        }

        // Ensure all UIs are in correct state at start
        playerUI?.SetActive(true);
        driveUI?.SetActive(false);
        kartDrivingUI?.SetActive(false);

        // Set first destination if available
        if (destinations != null && destinations.Length > 0 && kartController != null)
        {
            kartController.SetDestination(destinations[0]);
            UpdateDestinationUI();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (playerInside && !isDriving)
            {
                DriveKart();
            }
            else if (isDriving && !kartController.HasArrived) // Don't allow manual exit if arrived
            {
                ExitKart();
            }
        }
        
        // Update destination UI while driving
        if (isDriving)
        {
            UpdateDestinationUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            driveUI?.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            driveUI?.SetActive(false);
        }
    }

    public void DriveKart()
    {
        if (!playerInside || player == null) return;

        isDriving = true;
        
        // Hide specific player UI elements
        HidePlayerUIElements();
        
        driveUI?.SetActive(false);
        kartDrivingUI?.SetActive(true);

        // Parent player to kart seat
        player.transform.SetParent(kartSeatPosition);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        // Disable movement scripts
        CharacterController cc = player.GetComponent<CharacterController>();
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();

        if (cc) cc.enabled = false;
        if (tpc) tpc.enabled = false;

        // Enable kart controller
        if (kartController != null)
        {
            kartController.SetControllable(true);
            UpdateDestinationUI();
        }
    }

    public void ExitKart()
    {
        if (!isDriving) return;

        isDriving = false;

        // Restore player UI elements
        ShowPlayerUIElements();
        
        kartDrivingUI?.SetActive(false);
        
        if (playerInside)
        {
            driveUI?.SetActive(true);
        }

        // Unparent player
        player.transform.SetParent(null);

        // Re-enable player components
        CharacterController cc = player.GetComponent<CharacterController>();
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();

        if (tpc) tpc.enabled = true;
        if (cc) cc.enabled = true;

        // Disable kart controller
        if (kartController != null)
            kartController.SetControllable(false);
    }
    
    // Special method for auto-exit from kart controller
    public void AutoExitKart()
    {
        if (!isDriving) return;

        isDriving = false;

        // Show destination reached UI
        kartDrivingUI?.SetActive(false);
        
        // Wait a moment, then exit
        Invoke("CompleteAutoExit", 1.5f);
    }
    
    void CompleteAutoExit()
    {
        // Restore player UI elements
        ShowPlayerUIElements();
        
        if (playerInside)
        {
            driveUI?.SetActive(true);
        }

        // Unparent player
        player.transform.SetParent(null);

        // Re-enable player components
        CharacterController cc = player.GetComponent<CharacterController>();
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();

        if (tpc) tpc.enabled = true;
        if (cc) cc.enabled = true;

        // Move to next destination
        GoToNextDestination();
    }
    
    void GoToNextDestination()
    {
        // Go to next destination
        currentDestinationIndex++;
        if (currentDestinationIndex >= destinations.Length)
        {
            currentDestinationIndex = 0; // Loop back
        }
        
        // Set next destination
        if (kartController != null && destinations.Length > 0)
        {
            kartController.SetDestination(destinations[currentDestinationIndex]);
            UpdateDestinationUI();
        }
    }
    
    void UpdateDestinationUI()
    {
        if (destinationText != null && kartController != null && kartController.CurrentDestination != null)
        {
            float distance = Vector3.Distance(kartController.transform.position, kartController.CurrentDestination.position);
            destinationText.text = $"Destination: {kartController.CurrentDestination.name}\nDistance: {distance:F1}m";
            
            // Change color when close
            if (distance <= kartController.autoBrakeDistance)
            {
                destinationText.color = Color.yellow;
            }
            else
            {
                destinationText.color = Color.white;
            }
        }
        else if (destinationText != null)
        {
            destinationText.text = "Destination: None";
        }
    }
    
    // UI Button methods
    public void SetNextDestination()
    {
        if (destinations == null || destinations.Length == 0) return;
        
        currentDestinationIndex = (currentDestinationIndex + 1) % destinations.Length;
        if (kartController != null)
        {
            kartController.SetDestination(destinations[currentDestinationIndex]);
            UpdateDestinationUI();
        }
    }
    
    public void ClearDestination()
    {
        if (kartController != null)
        {
            kartController.ClearDestination();
            UpdateDestinationUI();
        }
    }
    
    public void SetDestinationByIndex(int index)
    {
        if (destinations == null || index < 0 || index >= destinations.Length) return;
        
        currentDestinationIndex = index;
        if (kartController != null)
        {
            kartController.SetDestination(destinations[currentDestinationIndex]);
            UpdateDestinationUI();
        }
    }
    
    // Methods to hide/show specific UI elements
    private void HidePlayerUIElements()
    {
        if (playerUIElementsToHide != null)
        {
            foreach (GameObject uiElement in playerUIElementsToHide)
            {
                if (uiElement != null)
                {
                    // Store current state if not already stored
                    if (!playerUIElementStates.ContainsKey(uiElement))
                    {
                        playerUIElementStates[uiElement] = uiElement.activeSelf;
                    }
                    uiElement.SetActive(false);
                }
            }
        }
    }
    
    private void ShowPlayerUIElements()
    {
        if (playerUIElementsToHide != null)
        {
            foreach (GameObject uiElement in playerUIElementsToHide)
            {
                if (uiElement != null && playerUIElementStates.ContainsKey(uiElement))
                {
                    uiElement.SetActive(playerUIElementStates[uiElement]);
                }
            }
        }
    }
    
    // Method to manually hide/show specific element
    public void SetPlayerUIElementActive(GameObject uiElement, bool active)
    {
        if (uiElement != null && playerUIElementsToHide != null && 
            System.Array.Exists(playerUIElementsToHide, element => element == uiElement))
        {
            uiElement.SetActive(active);
            if (playerUIElementStates.ContainsKey(uiElement))
            {
                playerUIElementStates[uiElement] = active;
            }
        }
    }
}