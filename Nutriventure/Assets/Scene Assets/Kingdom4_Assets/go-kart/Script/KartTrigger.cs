using UnityEngine;
using StarterAssets;

public class KartTrigger : MonoBehaviour
{
    public GameObject driveUI;               // UI element for "Drive"
    public GameObject kartController;        // The kart driving script (GameObject)
    public Transform kartSeatPosition;       // Empty transform inside kart

    private GameObject player;               // Auto-assigned player
    private bool playerInside = false;

    private void Start()
    {
        // Automatically find the Player in the scene
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("❌ No GameObject tagged 'Player' found! Please tag your player.");
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
        if (kartController)
            kartController.SetActive(true);

        driveUI?.SetActive(false);
    }
}
