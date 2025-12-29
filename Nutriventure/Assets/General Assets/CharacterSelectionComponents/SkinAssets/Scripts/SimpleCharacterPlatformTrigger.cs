using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;

public class SimpleCharacterPlatformTrigger : MonoBehaviour
{
    [Header("UI References")]
    public GameObject enterSelectionButton;
    public Canvas playerInputCanvas;
    public CanvasGroup characterSelectionCanvasGroup; // CHANGED: Now a CanvasGroup reference
    public GameObject clothingIcon;

    [Header("Camera Control")]
    public CinemachineVirtualCamera characterChangeCamera;

    [Header("Player Movement")]
    public Transform playerTransform;
    public Transform platformStandPosition;
    public float moveSpeed = 3f;

    [Header("Fade Settings")]
    public float fadeDuration = 0.3f;

    [Header("Character Selection Controller")]
    public CharacterSelectionController characterSelectionController;

    private CanvasGroup buttonCanvasGroup;
    private CanvasGroup inputCanvasGroup;
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

        // Setup character selection canvas - CRITICAL CHANGE
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
        if (other.CompareTag("Player"))
        {
            if (!playerInRange)
            {
                Debug.Log("Player entered trigger zone");
                playerInRange = true;

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

        // Hide button
        if (enterSelectionButton != null)
        {
            StartCoroutine(FadeAndHideButton());
        }

        // Make sure clothing icon is hidden
        if (clothingIcon != null)
        {
            clothingIcon.SetActive(false);
        }

        // Switch camera FIRST
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

        // SHOW CHARACTER SELECTION CANVAS - ONLY HERE!
        if (characterSelectionCanvasGroup != null)
        {
            characterSelectionCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(characterSelectionCanvasGroup, 0f, 1f, fadeDuration));
            characterSelectionCanvasGroup.interactable = true;
            characterSelectionCanvasGroup.blocksRaycasts = true;
            Debug.Log("Character selection canvas shown and interactive");
        }

        // Tell CharacterSelectionController to start (but DON'T let it show/hide canvas)
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
            // IMPORTANT: Don't let the controller show/hide canvas
            characterSelectionController.ActivateCharacterSelection();
        }
    }

    // Call this to exit character selection
    public IEnumerator ExitCharacterSelection()
    {
        Debug.Log("=== EXIT CHARACTER SELECTION ===");

        if (!isActive)
        {
            yield break;
        }

        // HIDE CHARACTER SELECTION CANVAS - ONLY HERE!
        if (characterSelectionCanvasGroup != null)
        {
            characterSelectionCanvasGroup.interactable = false;
            characterSelectionCanvasGroup.blocksRaycasts = false;
            yield return StartCoroutine(FadeCanvasGroup(characterSelectionCanvasGroup, 1f, 0f, fadeDuration));
            characterSelectionCanvasGroup.gameObject.SetActive(false);
            Debug.Log("Character selection canvas hidden");
        }

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

        // Show button again if player is still in range
        if (playerInRange && enterSelectionButton != null)
        {
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
        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 20;
            Debug.Log("Character Change Camera priority set to 20");
        }
    }
}