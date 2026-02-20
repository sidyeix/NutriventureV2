using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class Chest : MonoBehaviour
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

        Debug.Log(ChestName + " is now claimable!");
    }

    // Add this method to get remaining time
    public float GetRemainingTime()
    {
        if (isClaimable) return 0f;

        float elapsed = Time.time - spawnTime;
        float remaining = timeToBecomeClaimable - elapsed;
        return Mathf.Max(0f, remaining);
    }

    // Set chest index from ChestManager
    public void SetChestIndex(int index)
    {
        chestOrder = index;
    }

    // This method will now be called ONLY by the button via ChestManager
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