using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChestManager : MonoBehaviour
{
    [Header("Chest Prefabs")]
    public GameObject[] chestPrefabs;

    [Header("Spawn Point")]
    public Transform chestSpawnPoint;

    [Header("Camera Settings")]
    public Cinemachine.CinemachineVirtualCamera chestCamera;

    [Header("Canvas References")]
    public GameObject menuCanvas;
    public GameObject chestCanvas;

    [Header("Fade Settings")]
    public float fadeDuration = 1f; // How long the fade takes

    private Queue<GameObject> chestQueue = new Queue<GameObject>();
    private Chest currentChest;
    private ChestUIHandler chestUIHandler;
    private CanvasGroup chestCanvasGroup; // Reference to Canvas Group

    public static ChestManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("ChestManager Instance created");
        }
        else
        {
            Debug.Log("Destroying duplicate ChestManager");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("ChestManager Start() called");

        // Get ChestUIHandler component
        if (chestCanvas != null)
        {
            chestUIHandler = chestCanvas.GetComponent<ChestUIHandler>();
            chestCanvasGroup = chestCanvas.GetComponent<CanvasGroup>(); // Get Canvas Group

            if (chestCanvasGroup == null)
            {
                // Add Canvas Group if it doesn't exist
                chestCanvasGroup = chestCanvas.AddComponent<CanvasGroup>();
                Debug.Log("Added Canvas Group to chest canvas");
            }

            // Initialize canvas as invisible
            chestCanvasGroup.alpha = 0f;
            chestCanvasGroup.interactable = false;
            chestCanvasGroup.blocksRaycasts = false;

            if (chestUIHandler == null)
            {
                Debug.LogError("No ChestUIHandler found on chestCanvas!");
            }
            else
            {
                Debug.Log("ChestUIHandler found and assigned");
            }
        }
        else
        {
            Debug.LogError("ChestCanvas is not assigned!");
        }

        // Check other critical components
        if (chestCamera == null) Debug.LogError("Chest Camera is not assigned!");
        if (menuCanvas == null) Debug.LogError("Menu Canvas is not assigned!");
        if (chestSpawnPoint == null) Debug.LogError("Chest Spawn Point is not assigned!");
        if (chestPrefabs == null || chestPrefabs.Length == 0) Debug.LogError("No chest prefabs assigned!");

        InitializeChestCamera();
        InitializeChestQueue();
        SpawnNextChest();
    }

    void InitializeChestCamera()
    {
        // Set chest camera as inactive (menu camera stays at priority 10 in scene)
        if (chestCamera != null)
        {
            chestCamera.Priority = 0; // Set to 0 to make menu camera active
            chestCamera.LookAt = null;
            chestCamera.Follow = null;
            Debug.Log("Chest camera initialized with priority: " + chestCamera.Priority + " (Menu camera should be priority 10)");
        }
    }

    void InitializeChestQueue()
    {
        Debug.Log("Initializing chest queue with " + chestPrefabs.Length + " chests");

        // Add all chest prefabs to the queue in order
        foreach (GameObject chestPrefab in chestPrefabs)
        {
            if (chestPrefab != null)
            {
                chestQueue.Enqueue(chestPrefab);
                Debug.Log("Added " + chestPrefab.name + " to chest queue");
            }
            else
            {
                Debug.LogError("Found null chest prefab in array!");
            }
        }

        Debug.Log("Chest queue count: " + chestQueue.Count);
    }

    void SpawnNextChest()
    {
        if (chestQueue.Count == 0)
        {
            Debug.Log("All chests have been collected!");
            return;
        }

        GameObject nextChestPrefab = chestQueue.Dequeue();
        Debug.Log("Spawning next chest: " + nextChestPrefab.name);

        if (nextChestPrefab == null)
        {
            Debug.LogError("Next chest prefab is null! Skipping...");
            return;
        }

        if (chestSpawnPoint == null)
        {
            Debug.LogError("Cannot spawn chest - spawn point is null!");
            return;
        }

        // Spawn as child of the spawn point
        GameObject chestObj = Instantiate(nextChestPrefab, chestSpawnPoint);
        chestObj.transform.localPosition = Vector3.zero;
        chestObj.transform.localRotation = Quaternion.identity;

        currentChest = chestObj.GetComponent<Chest>();
        if (currentChest != null)
        {
            currentChest.Initialize();
            Debug.Log("Spawned " + currentChest.ChestName + ". Wait " + currentChest.timeToBecomeClaimable + " seconds to claim.");
        }
        else
        {
            Debug.LogError("Failed to get Chest component from spawned object!");
        }
    }

    public void FocusOnChest(Chest chest)
    {
        Debug.Log("FocusOnChest called for: " + chest.ChestName);

        if (chest != currentChest)
        {
            Debug.LogError("Chest mismatch! Current: " + (currentChest?.ChestName) + ", Requested: " + chest.ChestName);
            return;
        }

        if (chestCamera == null)
        {
            Debug.LogError("Chest Camera is not assigned!");
            return;
        }

        // SWITCH TO CHEST CAMERA
        chestCamera.Priority = 20;
        chestCamera.LookAt = chest.transform;
        chestCamera.Follow = chest.transform;

        Debug.Log("Chest camera activated with priority: " + chestCamera.Priority);

        // Switch canvases
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
            Debug.Log("Menu canvas hidden");
        }

        if (chestCanvas != null)
        {
            chestCanvas.SetActive(true);
            StartCoroutine(FadeCanvas(chestCanvasGroup, 0f, 1f, fadeDuration)); // Fade in
            Debug.Log("Chest canvas fading in");
        }
        else
        {
            Debug.LogError("Chest canvas is null!");
        }

        // Open the chest
        chest.OpenChest();

        // Update UI - Start reward reveal process
        if (chestUIHandler != null)
        {
            chestUIHandler.SetCurrentChest(chest);
            Debug.Log("Chest UI updated - starting reward reveal");
        }
        else
        {
            Debug.LogError("ChestUIHandler is null!");
        }
    }

    public void OnChestClaimed()
    {
        Debug.Log("OnChestClaimed called");

        // Clean up chest UI before destroying chest
        if (chestUIHandler != null)
        {
            chestUIHandler.OnChestUIClosed();
        }

        // DESTROY THE CURRENT CHEST
        if (currentChest != null)
        {
            Debug.Log("Destroying chest: " + currentChest.ChestName);
            Destroy(currentChest.gameObject);
            currentChest = null;
        }
        else
        {
            Debug.LogWarning("OnChestClaimed called but currentChest is null");
        }

        // Fade out before hiding
        if (chestCanvas != null && chestCanvasGroup != null)
        {
            StartCoroutine(FadeAndHideCanvas());
        }
        else
        {
            // Fallback to original behavior
            SwitchBackToMenu();
            StartCoroutine(SpawnNextChestAfterDelay(1f));
        }
    }

    private IEnumerator FadeAndHideCanvas()
    {
        // Fade out
        yield return StartCoroutine(FadeCanvas(chestCanvasGroup, 1f, 0f, fadeDuration / 2f));

        // Then switch back to menu
        SwitchBackToMenu();

        // Spawn next chest after a short delay
        StartCoroutine(SpawnNextChestAfterDelay(1f));
    }

    private void SwitchBackToMenu()
    {
        // RETURN TO MENU CAMERA
        if (chestCamera != null)
        {
            chestCamera.Priority = 0;
            chestCamera.LookAt = null;
            chestCamera.Follow = null;
            Debug.Log("Chest camera deactivated with priority: " + chestCamera.Priority);
        }

        // Switch canvases back to menu
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
            Debug.Log("Menu canvas shown");
        }

        if (chestCanvas != null)
        {
            chestCanvas.SetActive(false);
            Debug.Log("Chest canvas hidden");
        }
    }

    // Fade Coroutine
    private IEnumerator FadeCanvas(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;

        // Set initial state
        canvasGroup.alpha = startAlpha;

        // Enable interaction only when fully visible
        if (endAlpha > startAlpha) // Fading in
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else // Fading out
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            canvasGroup.alpha = currentAlpha;
            yield return null;
        }

        // Ensure final alpha is exact
        canvasGroup.alpha = endAlpha;

        Debug.Log("Canvas faded from " + startAlpha + " to " + endAlpha + " over " + duration + "s");
    }

    IEnumerator SpawnNextChestAfterDelay(float delay)
    {
        Debug.Log("Waiting " + delay + " seconds before spawning next chest...");
        yield return new WaitForSeconds(delay);
        SpawnNextChest();
    }

    void Update()
    {
        // Debug status with Input System - Press D key
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            DebugChestStatus();
        }

        // Force spawn next chest - Press N key (for testing)
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            Debug.Log("Force spawning next chest...");
            if (currentChest != null)
            {
                Destroy(currentChest.gameObject);
                currentChest = null;
            }
            SpawnNextChest();
        }

        // Force make chest claimable - Press C key (for testing)
        if (Keyboard.current.cKey.wasPressedThisFrame && currentChest != null)
        {
            Debug.Log("Force making chest claimable...");
            currentChest.MakeChestClaimable();
        }
    }

    public void DebugChestStatus()
    {
        Debug.Log("===== CHEST MANAGER STATUS =====");
        Debug.Log("   - Current Chest: " + (currentChest != null ? currentChest.ChestName : "NULL"));
        Debug.Log("   - Chests in queue: " + chestQueue.Count);
        Debug.Log("   - Chest Camera Priority: " + (chestCamera != null ? chestCamera.Priority.ToString() : "NULL"));
        Debug.Log("   - Menu Canvas Active: " + (menuCanvas != null ? menuCanvas.activeInHierarchy.ToString() : "NULL"));
        Debug.Log("   - Chest Canvas Active: " + (chestCanvas != null ? chestCanvas.activeInHierarchy.ToString() : "NULL"));
        Debug.Log("   - Chest Canvas Alpha: " + (chestCanvasGroup != null ? chestCanvasGroup.alpha.ToString("F2") : "NULL"));

        if (currentChest != null)
        {
            Debug.Log("   - Current Chest State:");
            Debug.Log("     * isClaimable: " + currentChest.isClaimable);
            Debug.Log("     * isOpened: " + currentChest.isOpened);

            // Check animator parameters
            if (currentChest.animator != null)
            {
                bool isOpenAnim = currentChest.animator.GetBool(currentChest.isOpenHash);
                bool isClaimableAnim = currentChest.animator.GetBool(currentChest.isClaimableHash);
                Debug.Log("     * Animator - isOpen: " + isOpenAnim + ", isClaimable: " + isClaimableAnim);
            }
        }
        Debug.Log("===== END STATUS =====");
    }

    // Public method to manually trigger chest focus (for testing)
    public void TestFocusCurrentChest()
    {
        if (currentChest != null)
        {
            Debug.Log("Manually focusing on current chest...");
            FocusOnChest(currentChest);
        }
        else
        {
            Debug.LogError("Cannot test focus - no current chest!");
        }
    }
}