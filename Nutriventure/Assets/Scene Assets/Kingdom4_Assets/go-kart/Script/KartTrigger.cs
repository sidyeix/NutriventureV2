using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Playables;


public class KartTrigger : MonoBehaviour
{
    [Header("Timeline")]
public PlayableDirector destinationDirector;

    public GameObject playerUI;
    public GameObject driveUI;
    public GameObject kartDrivingUI;
    public TextMeshProUGUI destinationText;

    public GameObject[] playerUIElementsToHide;

    public KartController kartController;
    public Transform kartSeatPosition;

    public Transform[] destinations;
    private int currentDestinationIndex = 0;

    private GameObject player;
    private bool playerInside = false;
    private bool isDriving = false;

    private Dictionary<GameObject, bool> playerUIElementStates = new Dictionary<GameObject, bool>();

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

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

        driveUI?.SetActive(false);
        kartDrivingUI?.SetActive(false);
    }

    private bool hasPlayedTimeline = false;

private void Update()
{
    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
    {
        if (playerInside && !isDriving)
        {
            DriveKart();
        }
        else if (isDriving && !kartController.HasArrived)
        {
            ExitKart();
        }
    }

    if (isDriving)
    {
        UpdateDestinationUI();

        if (kartController.HasArrived && !hasPlayedTimeline)
        {
            PlayDestinationTimeline();
        }
    }
}

void PlayDestinationTimeline()
{
    hasPlayedTimeline = true;

    // Disable kart control
    if (kartController != null)
        kartController.SetControllable(false);

    // Hide driving UI
    kartDrivingUI?.SetActive(false);

    // Play timeline
    if (destinationDirector != null)
    {
        destinationDirector.stopped += OnTimelineFinished;
        destinationDirector.Play();
    }
    else
    {
        // Fallback if no timeline assigned
        AutoExitKart();
    }
}

void OnTimelineFinished(PlayableDirector director)
{
    director.stopped -= OnTimelineFinished;

    AutoExitKart();
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

        HidePlayerUIElements();

        driveUI?.SetActive(false);
        kartDrivingUI?.SetActive(true);

        player.transform.SetParent(kartSeatPosition);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        CharacterController cc = player.GetComponent<CharacterController>();
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();

        if (cc) cc.enabled = false;
        if (tpc) tpc.enabled = false;

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

    // FORCE reset
    playerInside = false;

    ShowPlayerUIElements();

    driveUI?.SetActive(false);
    kartDrivingUI?.SetActive(false);

    player.transform.SetParent(null);

    CharacterController cc = player.GetComponent<CharacterController>();
    ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();

    if (tpc) tpc.enabled = true;
    if (cc) cc.enabled = true;

    if (kartController != null)
        kartController.SetControllable(false);
}


    public void AutoExitKart()
    {
        if (!isDriving) return;

        isDriving = false;
        kartDrivingUI?.SetActive(false);

        Invoke("CompleteAutoExit", 1.5f);
    }

    void CompleteAutoExit()
{
    // FORCE reset
    playerInside = false;

    ShowPlayerUIElements();
    driveUI?.SetActive(false);

    player.transform.SetParent(null);

    CharacterController cc = player.GetComponent<CharacterController>();
    ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();

    if (tpc) tpc.enabled = true;
    if (cc) cc.enabled = true;

    GoToNextDestination();
}


    void GoToNextDestination()
    {
        currentDestinationIndex++;

        if (currentDestinationIndex >= destinations.Length)
        {
            currentDestinationIndex = 0;
        }

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
            float distance = Vector3.Distance(
                kartController.transform.position,
                kartController.CurrentDestination.position
            );

            destinationText.text =
                $"Destination: {kartController.CurrentDestination.name}\nDistance: {distance:F1}m";

            destinationText.color =
                distance <= kartController.autoBrakeDistance ? Color.yellow : Color.white;
        }
        else if (destinationText != null)
        {
            destinationText.text = "Destination: None";
        }
    }

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

    private void HidePlayerUIElements()
    {
        if (playerUIElementsToHide != null)
        {
            foreach (GameObject uiElement in playerUIElementsToHide)
            {
                if (uiElement != null)
                {
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

    public void SetPlayerUIElementActive(GameObject uiElement, bool active)
    {
        if (uiElement != null &&
            playerUIElementsToHide != null &&
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
