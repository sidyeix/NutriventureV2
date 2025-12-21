using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;

public class SimpleCharacterPlatformTrigger : MonoBehaviour
{
    [Header("UI References")]
    public GameObject enterSelectionButton; // The button that appears
    public Canvas playerInputCanvas; // Canvas_StarterAssetInputs (will be hidden)
    public Canvas characterSelectionCanvas; // Character selection UI (will be shown)
    public GameObject clothingIcon; // Clothing icon GameObject

    [Header("Camera Control")]
    public CinemachineVirtualCamera characterChangeCamera;

    [Header("Player Movement")]
    public Transform playerTransform;
    public Transform platformStandPosition;
    public float moveSpeed = 3f;

    [Header("Fade Settings")]
    public float fadeDuration = 0.3f;

    [Header("Character Selection Controller")]
    public CharacterSelectionController characterSelectionController; // Assign this in Inspector

    private CanvasGroup buttonCanvasGroup;
    private CanvasGroup inputCanvasGroup;
    private CanvasGroup selectionCanvasGroup;
    private bool playerInRange = false;
    private bool isActive = false;

    void Start()
    {
        Debug.Log("=== SimpleCharacterPlatformTrigger START ===");

        // Setup button
        if (enterSelectionButton != null)
        {
            Debug.Log("Button found: " + enterSelectionButton.name);
            enterSelectionButton.SetActive(false);
            buttonCanvasGroup = enterSelectionButton.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup == null)
                buttonCanvasGroup = enterSelectionButton.AddComponent<CanvasGroup>();
            buttonCanvasGroup.alpha = 0f;
        }
        else
        {
            Debug.LogError("Enter Selection Button is NOT assigned!");
        }

        // Setup player input canvas
        if (playerInputCanvas != null)
        {
            Debug.Log("Player Input Canvas found: " + playerInputCanvas.name);
            inputCanvasGroup = playerInputCanvas.GetComponent<CanvasGroup>();
            if (inputCanvasGroup == null)
                inputCanvasGroup = playerInputCanvas.gameObject.AddComponent<CanvasGroup>();
        }
        else
        {
            Debug.LogError("Player Input Canvas is NOT assigned!");
        }

        // Setup character selection canvas
        if (characterSelectionCanvas != null)
        {
            Debug.Log("Character Selection Canvas found: " + characterSelectionCanvas.name);
            characterSelectionCanvas.gameObject.SetActive(false);
            selectionCanvasGroup = characterSelectionCanvas.GetComponent<CanvasGroup>();
            if (selectionCanvasGroup == null)
                selectionCanvasGroup = characterSelectionCanvas.gameObject.AddComponent<CanvasGroup>();
            selectionCanvasGroup.alpha = 0f;
        }
        else
        {
            Debug.LogError("Character Selection Canvas is NOT assigned!");
        }

        // Check clothing icon
        if (clothingIcon != null)
        {
            Debug.Log("Clothing Icon GameObject found: " + clothingIcon.name);
            // Make sure it's visible at start
            clothingIcon.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Clothing Icon GameObject is NOT assigned - optional field");
        }

        // Check camera
        if (characterChangeCamera != null)
            Debug.Log("Character Change Camera found: " + characterChangeCamera.name);
        else
            Debug.LogError("Character Change Camera is NOT assigned!");

        // Try to find character selection controller if not assigned
        if (characterSelectionController == null)
        {
            Debug.Log("Looking for CharacterSelectionController...");
            characterSelectionController = FindAnyObjectByType<CharacterSelectionController>();
            if (characterSelectionController != null)
                Debug.Log("Found CharacterSelectionController: " + characterSelectionController.name);
            else
                Debug.LogError("NO CharacterSelectionController found in scene!");
        }
        else
        {
            Debug.Log("CharacterSelectionController assigned: " + characterSelectionController.name);
        }

        Debug.Log("=== START COMPLETE ===");
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter called with: " + other.name + " | Tag: " + other.tag);

        if (other.CompareTag("Player"))
        {
            // Only proceed if not already in range
            if (!playerInRange)
            {
                Debug.Log("? Player entered trigger zone - FIRST TIME");
                playerInRange = true;

                // Show button with fade
                if (enterSelectionButton != null && !isActive)
                {
                    Debug.Log("Showing button...");
                    enterSelectionButton.SetActive(true);
                    StartCoroutine(FadeCanvasGroup(buttonCanvasGroup, 0f, 1f, fadeDuration));
                }
                else if (isActive)
                {
                    Debug.Log("Button not shown because isActive = " + isActive);
                }

                // HIDE clothing icon GameObject when player enters
                if (clothingIcon != null && !isActive)
                {
                    Debug.Log("Hiding clothing icon GameObject...");
                    clothingIcon.SetActive(false);
                }
            }
            else
            {
                Debug.Log("Player already in range - ignoring duplicate trigger");
            }
        }
        else
        {
            Debug.Log("? Not a Player: " + other.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("OnTriggerExit called with: " + other.name);

        if (other.CompareTag("Player"))
        {
            // Only proceed if currently in range
            if (playerInRange)
            {
                Debug.Log("? Player left trigger zone");
                playerInRange = false;

                // Hide button with fade
                if (enterSelectionButton != null && !isActive)
                {
                    Debug.Log("Hiding button...");
                    StartCoroutine(FadeAndHideButton());
                }

                // SHOW clothing icon GameObject when player leaves (only if not active)
                if (clothingIcon != null && !isActive)
                {
                    Debug.Log("Showing clothing icon GameObject...");
                    clothingIcon.SetActive(true);
                }
            }
            else
            {
                Debug.Log("Player not in range - ignoring duplicate exit");
            }
        }
    }

    // Call this from the button's onClick event
    public void OnEnterCharacterSelectionClicked()
    {
        Debug.Log("=== BUTTON CLICKED ===");
        Debug.Log("playerInRange: " + playerInRange);
        Debug.Log("isActive: " + isActive);

        if (!playerInRange)
        {
            Debug.LogError("? Button clicked but player not in range!");
            return;
        }

        if (isActive)
        {
            Debug.LogError("? Button clicked but already active!");
            return;
        }

        Debug.Log("? Starting activation...");
        StartCoroutine(ActivateCharacterSelection());
    }

    private IEnumerator ActivateCharacterSelection()
    {
        Debug.Log("=== ACTIVATE CHARACTER SELECTION ===");
        isActive = true;
        Debug.Log("Set isActive = true");

        // Hide button
        if (enterSelectionButton != null)
        {
            Debug.Log("Hiding button...");
            StartCoroutine(FadeAndHideButton());
        }

        // Make sure clothing icon is hidden when activating character selection
        if (clothingIcon != null)
        {
            clothingIcon.SetActive(false);
        }

        // Switch camera FIRST
        Debug.Log("Switching camera FIRST...");
        SetCharacterChangeCameraActive();

        // Wait one frame for camera to take effect
        yield return null;

        // Move player to platform position
        if (playerTransform != null && platformStandPosition != null)
        {
            Debug.Log("Moving player to platform...");
            Debug.Log("Player position: " + playerTransform.position);
            Debug.Log("Platform position: " + platformStandPosition.position);
            yield return StartCoroutine(MovePlayerToPosition());
        }
        else
        {
            Debug.LogWarning("Player transform or platform position not assigned, skipping movement");
            yield return null;
        }

        // Hide player input canvas
        if (playerInputCanvas != null)
        {
            Debug.Log("Hiding player input canvas...");
            yield return StartCoroutine(FadeCanvasGroup(inputCanvasGroup, 1f, 0f, fadeDuration));
            playerInputCanvas.gameObject.SetActive(false);
            Debug.Log("Player input canvas hidden");
        }

        // Show character selection canvas
        if (characterSelectionCanvas != null)
        {
            Debug.Log("Showing character selection canvas...");
            characterSelectionCanvas.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(selectionCanvasGroup, 0f, 1f, fadeDuration));
            Debug.Log("Character selection canvas shown");
        }

        // Trigger the character selection system
        Debug.Log("Triggering character selection...");
        TriggerCharacterSelection();

        Debug.Log("=== ACTIVATION COMPLETE ===");
    }

    private IEnumerator MovePlayerToPosition()
    {
        Vector3 startPos = playerTransform.position;
        Quaternion startRot = playerTransform.rotation;

        float distance = Vector3.Distance(startPos, platformStandPosition.position);
        float duration = distance / moveSpeed;
        Debug.Log("Moving distance: " + distance + " | Duration: " + duration);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            playerTransform.position = Vector3.Lerp(startPos, platformStandPosition.position, t);
            playerTransform.rotation = Quaternion.Slerp(startRot, platformStandPosition.rotation, t);

            yield return null;
        }

        playerTransform.position = platformStandPosition.position;
        playerTransform.rotation = platformStandPosition.rotation;
        Debug.Log("Player moved to platform position");
    }

    private void TriggerCharacterSelection()
    {
        Debug.Log("=== TRIGGER CHARACTER SELECTION ===");

        if (characterSelectionController != null)
        {
            Debug.Log("Calling ActivateCharacterSelection on controller...");
            characterSelectionController.ActivateCharacterSelection();
        }
        else
        {
            Debug.LogError("NO CharacterSelectionController found!");

            // Try to find it one more time
            characterSelectionController = FindAnyObjectByType<CharacterSelectionController>();
            if (characterSelectionController != null)
            {
                Debug.Log("Found controller, calling ActivateCharacterSelection...");
                characterSelectionController.ActivateCharacterSelection();
            }
            else
            {
                Debug.LogError("Still no controller found after search!");
            }
        }
    }

    // Call this to exit character selection
    public IEnumerator ExitCharacterSelection()
    {
        Debug.Log("=== EXIT CHARACTER SELECTION ===");

        if (!isActive)
        {
            Debug.Log("Not active, exiting early");
            yield break;
        }

        // Hide character selection canvas
        if (characterSelectionCanvas != null && selectionCanvasGroup != null)
        {
            Debug.Log("Hiding character selection canvas...");
            yield return StartCoroutine(FadeCanvasGroup(selectionCanvasGroup, 1f, 0f, fadeDuration));
            characterSelectionCanvas.gameObject.SetActive(false);
            Debug.Log("Character selection canvas hidden");
        }

        // Show player input canvas
        if (playerInputCanvas != null)
        {
            Debug.Log("Showing player input canvas...");
            playerInputCanvas.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(inputCanvasGroup, 0f, 1f, fadeDuration));
            Debug.Log("Player input canvas shown");
        }

        // Show clothing icon GameObject when exiting (if player is still in range)
        if (clothingIcon != null && playerInRange)
        {
            Debug.Log("Showing clothing icon GameObject...");
            clothingIcon.SetActive(true);
        }

        // Show button again if player is still in range
        if (playerInRange && enterSelectionButton != null)
        {
            Debug.Log("Showing button again...");
            enterSelectionButton.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(buttonCanvasGroup, 0f, 1f, fadeDuration));
        }

        isActive = false;
        Debug.Log("Set isActive = false");
        Debug.Log("=== EXIT COMPLETE ===");
    }

    private IEnumerator FadeAndHideButton()
    {
        Debug.Log("Fading and hiding button...");

        if (buttonCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(buttonCanvasGroup, buttonCanvasGroup.alpha, 0f, fadeDuration));
            enterSelectionButton.SetActive(false);
            Debug.Log("Button faded and hidden");
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        Debug.Log($"Fading CanvasGroup from {startAlpha} to {endAlpha} over {duration}s");

        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null!");
            yield break;
        }

        float elapsedTime = 0f;
        canvasGroup.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        Debug.Log($"Fade complete: alpha = {endAlpha}");
    }

    private void SetCharacterChangeCameraActive()
    {
        Debug.Log("Setting Character Change Camera active...");

        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 20;
            Debug.Log("Character Change Camera priority set to 20");
        }
        else
        {
            Debug.LogError("Character Change Camera is null!");
        }
    }
}