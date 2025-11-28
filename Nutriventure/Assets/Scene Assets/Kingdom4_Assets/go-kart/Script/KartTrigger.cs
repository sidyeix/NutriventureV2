using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

public class KartTrigger : MonoBehaviour
{
    public GameObject driveUI;
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
    }

    private void Update()
    {
        // Use new Input System for E key
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

        driveUI?.SetActive(false);
    }

    public void ExitKart()
    {
        if (!isDriving) return;

        isDriving = false;

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

        // Show drive UI if player is still in trigger
        if (playerInside && driveUI != null)
            driveUI.SetActive(true);
    }
}