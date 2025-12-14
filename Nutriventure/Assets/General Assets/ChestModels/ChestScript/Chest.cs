using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

// Add IPointerClickHandler interface
public class Chest : MonoBehaviour, IPointerClickHandler
{
    [Header("Chest Settings")]
    public float timeToBecomeClaimable = 10f;
    public int chestOrder = 0;

    [Header("Components")]
    public Animator animator;
    public Collider chestCollider;

    // Add this new field for mobile touch handling
    public BoxCollider touchCollider; // Separate collider for mobile touch

    [Header("World Space UI")]
    public WorldSpaceChestUI worldSpaceUI;

    [Header("Chest State")]
    public bool isClaimable = false;
    public bool isOpened = false;

    [Header("Click Settings")]
    public float clickDistance = 1000f;
    public float touchColliderSizeMultiplier = 1.5f; // Make touch collider larger

    // Animation parameter hashes
    public readonly int isOpenHash = Animator.StringToHash("isOpen");
    public readonly int isClaimableHash = Animator.StringToHash("isClaimable");

    // Timer tracking
    private float spawnTime;

    public string ChestName => $"Chest {chestOrder + 1}";

    void Start()
    {
        // Ensure collider exists
        if (chestCollider == null)
            chestCollider = GetComponent<Collider>();

        if (chestCollider == null)
            Debug.LogError("No collider found on chest!");

        // Setup touch collider for mobile
        SetupTouchCollider();

        Initialize();
    }

    // Add this method to setup touch collider
    private void SetupTouchCollider()
    {
        // Check if we're on mobile
        if (Application.isMobilePlatform || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            // Add a separate collider for touch if it doesn't exist
            if (touchCollider == null)
            {
                touchCollider = gameObject.AddComponent<BoxCollider>();

                // Copy bounds from existing collider or create default
                if (chestCollider != null)
                {
                    Bounds bounds = chestCollider.bounds;
                    touchCollider.center = transform.InverseTransformPoint(bounds.center);
                    touchCollider.size = bounds.size * touchColliderSizeMultiplier;
                }
                else
                {
                    touchCollider.center = Vector3.zero;
                    touchCollider.size = Vector3.one * 2f; // Default size
                }

                Debug.Log($"Created touch collider for {ChestName} (Mobile)");
            }
        }
    }

    public void Initialize()
    {
        spawnTime = Time.time;
        isClaimable = false;
        isOpened = false;

        // Initialize World Space UI
        if (worldSpaceUI != null)
        {
            worldSpaceUI.UpdateUIState(false);
            worldSpaceUI.ShowUI();
        }
        else
        {
            Debug.LogWarning("No WorldSpaceUI assigned to " + ChestName);
        }

        StartCoroutine(MakeChestClaimableAfterDelay());
    }

    IEnumerator MakeChestClaimableAfterDelay()
    {
        yield return new WaitForSeconds(timeToBecomeClaimable);
        MakeChestClaimable();
    }

    // Make this public for testing
    public void MakeChestClaimable()
    {
        isClaimable = true;

        // Update World Space UI to show "Claim Me"
        if (worldSpaceUI != null)
        {
            worldSpaceUI.UpdateUIState(true);
        }

        if (animator != null)
            animator.SetBool("isClaimable", true);

        Debug.Log(ChestName + " is now claimable and clickable!");
    }

    // Add this method to get remaining time
    public float GetRemainingTime()
    {
        if (isClaimable) return 0f;

        float elapsed = Time.time - spawnTime;
        float remaining = timeToBecomeClaimable - elapsed;
        return Mathf.Max(0f, remaining);
    }

    // NEW METHOD: Set chest index from ChestManager
    public void SetChestIndex(int index)
    {
        chestOrder = index;
    }

    void Update()
    {
        // Handle mouse clicks only on desktop
#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_EDITOR
        if (isClaimable && !isOpened && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckForChestClick();
        }
#endif
    }

    // NEW METHOD: IPointerClickHandler implementation for mobile touch
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only respond to touch if chest is claimable and not opened
        if (isClaimable && !isOpened)
        {
            Debug.Log("CHEST TOUCHED (Mobile): " + ChestName);
            HandleChestClick();
        }
        else if (!isClaimable)
        {
            Debug.Log("Chest not claimable yet! Time remaining: " + GetRemainingTime());
        }
        else if (isOpened)
        {
            Debug.Log("Chest already opened!");
        }
    }

    void CheckForChestClick()
    {
        if (Camera.main == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        // First check if we clicked on a UI button
        if (IsClickOnInteractiveUI())
        {
            Debug.Log("Clicked on interactive UI element - ignoring chest click");
            return;
        }

        // Then check for chest click
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, clickDistance))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                Debug.Log("CHEST CLICKED: " + ChestName + " from " + hit.distance + " units away!");
                HandleChestClick();
            }
        }
    }

    // Smart UI detection - only blocks if clicking on interactive UI elements
    private bool IsClickOnInteractiveUI()
    {
        if (EventSystem.current == null)
            return false;

        // Get what we're clicking on
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Check if we hit any interactive UI elements
        foreach (var result in results)
        {
            // If it's a button, toggle, or other interactive element
            if (result.gameObject.GetComponent<Button>() != null ||
                result.gameObject.GetComponent<Toggle>() != null ||
                result.gameObject.GetComponent<Slider>() != null ||
                result.gameObject.GetComponent<InputField>() != null ||
                result.gameObject.GetComponent<IPointerClickHandler>() != null)
            {
                return true;
            }

            // Or check if it's a specific named button you want to protect
            string[] protectedButtons = { "Play", "Credits", "Settings", "ResetGameData", "Claim" };
            foreach (string buttonName in protectedButtons)
            {
                if (result.gameObject.name.Contains(buttonName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void HandleChestClick()
    {
        if (!isClaimable)
        {
            Debug.Log("Chest not claimable yet! isClaimable = " + isClaimable);
            return;
        }

        if (isOpened)
        {
            Debug.Log("Chest already opened! isOpened = " + isOpened);
            return;
        }

        Debug.Log("Notifying ChestManager about chest click...");

        // HIDE WORLD SPACE UI WHEN CHEST IS CLICKED
        if (worldSpaceUI != null)
        {
            worldSpaceUI.OnChestClicked();
        }

        if (ChestManager.Instance != null)
        {
            ChestManager.Instance.FocusOnChest(this);
        }
        else
        {
            Debug.LogError("ChestManager instance is null!");
        }
    }

    public void OpenChest()
    {
        if (isOpened) return;

        isOpened = true;

        // Hide World Space UI when chest is opened
        if (worldSpaceUI != null)
        {
            worldSpaceUI.OnChestOpened();
        }

        if (animator != null)
        {
            animator.SetBool("isOpened", true);
            animator.SetBool("isClaimable", false);
            Debug.Log("Chest animation: isOpen = true, isClaimable = false");
        }
    }

    public void ClaimChest()
    {
        if (!isOpened) return;

        // Hide World Space UI
        if (worldSpaceUI != null)
        {
            worldSpaceUI.HideUI();
        }

        ChestManager.Instance.OnChestClaimed();
    }
}