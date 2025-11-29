using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

public class KartTrigger : MonoBehaviour
{
    [Header("UI References")]
    public GameObject playerUI;             // Normal player UI (health, inventory, etc.)
    public GameObject driveUI;              // "Enter Kart" button UI
    public GameObject kartDrivingUI;        // Kart controls UI (steering buttons, etc.)
    
    [Header("Kart References")]
    public KartController kartController;
    public Transform kartSeatPosition;

    private GameObject player;
    private bool playerInside = false;
    private bool isDriving = false;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ No GameObject tagged 'Player' found!");
        }

        // Ensure all UIs are in correct state at start
        playerUI?.SetActive(true);          // Player UI visible by default
        driveUI?.SetActive(false);          // Drive UI hidden
        kartDrivingUI?.SetActive(false);    // Driving UI hidden
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (playerInside && !isDriving)
            {
                DriveKart();
            }
            else if (isDriving)
            {
                ExitKart();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            
            // Show drive UI, keep player UI visible
            driveUI?.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            
            // Hide drive UI, keep player UI visible
            driveUI?.SetActive(false);
        }
    }

    public void DriveKart()
    {
        if (!playerInside || player == null) return;

        isDriving = true;

        // Switch UI states:
        playerUI?.SetActive(false);         // Hide normal player UI
        driveUI?.SetActive(false);          // Hide "Enter Kart" UI
        kartDrivingUI?.SetActive(true);     // Show kart driving controls

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
            kartController.SetControllable(true);
    }

    public void ExitKart()
    {
        if (!isDriving) return;

        isDriving = false;

        // Switch UI states:
        playerUI?.SetActive(true);          // Show normal player UI
        kartDrivingUI?.SetActive(false);    // Hide kart driving controls
        
        if (playerInside)
        {
            driveUI?.SetActive(true);       // Show "Enter Kart" UI if still in trigger
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
}