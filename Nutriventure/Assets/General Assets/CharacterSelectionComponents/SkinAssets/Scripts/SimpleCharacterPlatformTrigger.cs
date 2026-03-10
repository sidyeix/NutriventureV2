using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;

public class SimpleCharacterPlatformTrigger : MonoBehaviour
{
    [Header("UI References")]
    public GameObject enterSelectionButton;
    public GameObject enterSelectionCanvas; // New canvas for the button
    public Canvas playerInputCanvas;
    public CanvasGroup characterSelectionCanvasGroup;
    public GameObject clothingIcon;

    [Header("Camera Control")]
    public CinemachineVirtualCamera characterChangeCamera;
    public CinemachineVirtualCamera playerFollowCamera;

    [Header("Player Movement")]
    public Transform playerTransform;
    public Transform platformStandPosition;
    public float moveSpeed = 3f;

    [Header("Fade Settings")]
    public float fadeDuration = 0.3f;

    [Header("Character Selection Controller")]
    public CharacterSelectionController characterSelectionController;

    [Header("Pet Manager")]
    public EnerlingPetManager petManager;

    [Header("Currency Display")]
    public Player_Data playerData; // Reference to update gem display

    private CanvasGroup buttonCanvasGroup;
    private CanvasGroup inputCanvasGroup;
    private bool playerInRange = false;
    private bool isActive = false;
    private int savedPlayerCameraPriority = 10;

    void Start()
    {
        Debug.Log("=== SimpleCharacterPlatformTrigger START ===");

        // Setup enter selection canvas (initially disabled)
        if (enterSelectionCanvas != null)
        {
            Debug.Log("Enter Selection Canvas found: " + enterSelectionCanvas.name);
            enterSelectionCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError("Enter Selection Canvas is NOT assigned!");
        }

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
        if (characterSelectionCanvasGroup != null)
        {
            Debug.Log("Character Selection CanvasGroup found: " + characterSelectionCanvasGroup.name);
            characterSelectionCanvasGroup.gameObject.SetActive(false);
            characterSelectionCanvasGroup.alpha = 0f;
            characterSelectionCanvasGroup.interactable = false;
            characterSelectionCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogError("Character Selection CanvasGroup is NOT assigned!");
        }

        // Check clothing icon
        if (clothingIcon != null)
        {
            Debug.Log("Clothing Icon GameObject found: " + clothingIcon.name);
            clothingIcon.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Clothing Icon GameObject is NOT assigned - optional field");
        }

        // Check camera
        if (characterChangeCamera != null)
        {
            Debug.Log("Character Change Camera found: " + characterChangeCamera.name);
            characterChangeCamera.Priority = 0;
        }
        else
            Debug.LogError("Character Change Camera is NOT assigned!");

        // Find player data if not assigned
        if (playerData == null)
        {
            playerData = FindObjectOfType<Player_Data>();
            if (playerData != null)
                Debug.Log("Found Player_Data: " + playerData.name);
            else
                Debug.LogWarning("No Player_Data found in scene!");
        }

        // Find pet manager if not assigned
        if (petManager == null)
        {
            petManager = FindObjectOfType<EnerlingPetManager>();
            if (petManager != null)
                Debug.Log("Found EnerlingPetManager: " + petManager.name);
            else
                Debug.LogWarning("No EnerlingPetManager found in scene!");
        }

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
        if (other.CompareTag("Player"))
        {
            if (!playerInRange)
            {
                Debug.Log("Player entered trigger zone");
                playerInRange = true;

                // Enable the canvas first
                if (enterSelectionCanvas != null && !isActive)
                {
                    enterSelectionCanvas.SetActive(true);
                }

                // Then show and fade in the button
                if (enterSelectionButton != null && !isActive)
                {
                    enterSelectionButton.SetActive(true);
                    StartCoroutine(FadeCanvasGroup(buttonCanvasGroup, 0f, 1f, fadeDuration));
                }

                if (clothingIcon != null && !isActive)
                {
                    clothingIcon.SetActive(false);
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerInRange)
            {
                Debug.Log("Player left trigger zone");
                playerInRange = false;

                if (enterSelectionButton != null && !isActive)
                {
                    StartCoroutine(FadeAndHideButton());
                }

                // Disable the canvas after button is hidden
                if (enterSelectionCanvas != null && !isActive)
                {
                    StartCoroutine(DisableCanvasAfterDelay(enterSelectionCanvas, fadeDuration));
                }

                if (clothingIcon != null && !isActive)
                {
                    clothingIcon.SetActive(true);
                }
            }
        }
    }

    public void OnEnterCharacterSelectionClicked()
    {
        if (!playerInRange || isActive) return;

        Debug.Log("Button clicked - Starting character selection");
        StartCoroutine(ActivateCharacterSelection());
    }

    private IEnumerator ActivateCharacterSelection()
    {
        Debug.Log("=== ACTIVATE CHARACTER SELECTION ===");
        isActive = true;

        // Update currency displays before showing selection
        if (playerData != null)
        {
            playerData.UpdateGemDisplayImmediate();
            playerData.UpdateCoinDisplayImmediate();
        }

        // Set pets to platform mode
        if (petManager != null)
        {
            petManager.SetPlatformMode(true);
        }

        // Hide button and its canvas
        if (enterSelectionButton != null)
        {
            StartCoroutine(FadeAndHideButton());
        }

        if (enterSelectionCanvas != null)
        {
            StartCoroutine(DisableCanvasAfterDelay(enterSelectionCanvas, fadeDuration));
        }

        // Make sure clothing icon is hidden
        if (clothingIcon != null)
        {
            clothingIcon.SetActive(false);
        }

        // Switch camera FIRST - set to 30
        SetCharacterChangeCameraActive();
        yield return null;

        // Move player to platform position
        if (playerTransform != null && platformStandPosition != null)
        {
            yield return StartCoroutine(MovePlayerToPosition());
        }

        // Hide player input canvas
        if (playerInputCanvas != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(inputCanvasGroup, 1f, 0f, fadeDuration));
            playerInputCanvas.gameObject.SetActive(false);
        }

        // SHOW CHARACTER SELECTION CANVAS
        if (characterSelectionCanvasGroup != null)
        {
            characterSelectionCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(characterSelectionCanvasGroup, 0f, 1f, fadeDuration));
            characterSelectionCanvasGroup.interactable = true;
            characterSelectionCanvasGroup.blocksRaycasts = true;
            Debug.Log("Character selection canvas shown and interactive");
        }

        // Refresh the character panel data before showing
        TriggerCharacterSelection();

        Debug.Log("=== ACTIVATION COMPLETE ===");
    }

    private IEnumerator MovePlayerToPosition()
    {
        Vector3 startPos = playerTransform.position;
        Quaternion startRot = playerTransform.rotation;

        float distance = Vector3.Distance(startPos, platformStandPosition.position);
        float duration = distance / moveSpeed;

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
    }

    private void TriggerCharacterSelection()
    {
        if (characterSelectionController != null)
        {
            characterSelectionController.ActivateCharacterSelection();
        }
    }

    public IEnumerator ExitCharacterSelection()
    {
        Debug.Log("=== EXIT CHARACTER SELECTION ===");

        if (!isActive)
        {
            yield break;
        }

        // Resume pets from platform mode
        if (petManager != null)
        {
            petManager.SetPlatformMode(false);
        }

        // HIDE CHARACTER SELECTION CANVAS
        if (characterSelectionCanvasGroup != null)
        {
            characterSelectionCanvasGroup.interactable = false;
            characterSelectionCanvasGroup.blocksRaycasts = false;
            yield return StartCoroutine(FadeCanvasGroup(characterSelectionCanvasGroup, 1f, 0f, fadeDuration));
            characterSelectionCanvasGroup.gameObject.SetActive(false);
            Debug.Log("Character selection canvas hidden");
        }

        // Reset camera priority to 0
        ResetCameraPriority();

        // Show player input canvas
        if (playerInputCanvas != null)
        {
            playerInputCanvas.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(inputCanvasGroup, 0f, 1f, fadeDuration));
        }

        // Show clothing icon if player is still in range
        if (clothingIcon != null && playerInRange)
        {
            clothingIcon.SetActive(true);
        }

        // Show button and its canvas again if player is still in range
        if (playerInRange && enterSelectionButton != null)
        {
            if (enterSelectionCanvas != null)
            {
                enterSelectionCanvas.SetActive(true);
            }

            enterSelectionButton.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(buttonCanvasGroup, 0f, 1f, fadeDuration));
        }

        isActive = false;
        Debug.Log("=== EXIT COMPLETE ===");
    }

    private IEnumerator FadeAndHideButton()
    {
        if (buttonCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(buttonCanvasGroup, buttonCanvasGroup.alpha, 0f, fadeDuration));
            enterSelectionButton.SetActive(false);
        }
    }

    private IEnumerator DisableCanvasAfterDelay(GameObject canvas, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (canvas != null)
        {
            canvas.SetActive(false);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

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
    }

    private void SetCharacterChangeCameraActive()
    {
        // Lower the player follow camera priority first
        if (playerFollowCamera != null)
        {
            savedPlayerCameraPriority = playerFollowCamera.Priority;
            playerFollowCamera.Priority = 0;
            Debug.Log("Player Follow Camera priority lowered to 0");
        }

        if (characterChangeCamera != null)
        {
            // Ensure the camera is active and enabled so Cinemachine picks it up
            characterChangeCamera.gameObject.SetActive(true);
            characterChangeCamera.enabled = true;
            characterChangeCamera.Priority = 30;
            Debug.Log("Character Change Camera activated and priority set to 30");
        }
    }

    private void ResetCameraPriority()
    {
        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 0;
            Debug.Log("Character Change Camera priority reset to 0");
        }

        // Restore the player follow camera priority
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = savedPlayerCameraPriority;
            Debug.Log($"Player Follow Camera priority restored to {savedPlayerCameraPriority}");
        }
    }
}