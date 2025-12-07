using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.InputSystem;
using System.Collections;

public class K2_DummypTimeline : MonoBehaviour
{
    [Header("Dummy Product Collection Detection")]
    [SerializeField] private CollectProducts collectProductsScript; // Reference to CollectProducts script
    
    [Header("Second Timeline References")]
    [SerializeField] private GameObject cutscene2ParentObject; // "Cutscene2Things" parent object
    [SerializeField] private PlayableDirector npcCutscene2Director; // "NPC_Cutscene2" PlayableDirector
    
    [Header("Player Reference")]
    [SerializeField] private GameObject playerObject; // Reference to player (with ThirdPersonController)
    
    [Header("Game Systems to Control")]
    [SerializeField] private GameObject audioHandler; // "Audio_Handler" GameObject
    [SerializeField] private GameObject gameUICanvas; // "UI_Canvas_StarterAssetsInputs_Joysticks"
    
    [Header("Dialogue Canvas")]
    [SerializeField] private GameObject dialogueCanvas; // Dialogue box canvas for second timeline
    
    [Header("Events")]
    public UnityEvent onSecondCutsceneStart;
    public UnityEvent onSecondCutsceneEnd;
    
    // Simple state tracking
    private bool isSecondCutscenePlaying = false;
    
    void Start()
    {
        Debug.Log("K2_DummypTimeline Start called");
        
        // Initialize everything in a safe way
        SafeInitialize();
    }
    
    void SafeInitialize()
    {
        // Disable cutscene parent at start
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
            Debug.Log("Cutscene2 parent disabled");
        }
        else
        {
            Debug.LogError("Cutscene2 parent object not assigned!");
        }
        
        // Initialize PlayableDirector without touching it much
        if (npcCutscene2Director != null)
        {
            // Don't call Stop() or subscribe to events in Start
            // Just ensure it's not playing
            if (npcCutscene2Director.state == PlayState.Playing)
            {
                Debug.LogWarning("Timeline was already playing, stopping it");
                npcCutscene2Director.Stop();
            }
        }
        else
        {
            Debug.LogError("PlayableDirector not assigned!");
        }
        
        // Disable dialogue canvas
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
            Debug.Log("Dialogue canvas disabled");
        }
        
        // Find player if not assigned
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Debug.Log($"Found player: {playerObject.name}");
            }
        }
        
        // Find CollectProducts if not assigned
        if (collectProductsScript == null)
        {
            collectProductsScript = FindObjectOfType<CollectProducts>();
            if (collectProductsScript != null)
            {
                Debug.Log("Found CollectProducts script");
            }
        }
        
        // Find Audio Handler if not assigned
        if (audioHandler == null)
        {
            audioHandler = GameObject.Find("Audio_Handler");
            if (audioHandler != null)
            {
                Debug.Log("Found Audio Handler");
            }
        }
        
        // Find Game UI Canvas if not assigned
        if (gameUICanvas == null)
        {
            gameUICanvas = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
            if (gameUICanvas != null)
            {
                Debug.Log("Found Game UI Canvas");
            }
        }
        
        Debug.Log("K2_DummypTimeline initialized successfully");
    }
    
    void OnEnable()
    {
        Debug.Log("K2_DummypTimeline enabled");
        
        // Subscribe to timeline events when script is enabled
        if (npcCutscene2Director != null)
        {
            npcCutscene2Director.stopped += OnSecondCutsceneFinished;
            npcCutscene2Director.played += OnSecondCutscenePlayed;
            Debug.Log("Subscribed to timeline events");
        }
    }
    
    void OnDisable()
    {
        Debug.Log("K2_DummypTimeline disabled");
        
        // Unsubscribe from timeline events
        if (npcCutscene2Director != null)
        {
            npcCutscene2Director.stopped -= OnSecondCutsceneFinished;
            npcCutscene2Director.played -= OnSecondCutscenePlayed;
            Debug.Log("Unsubscribed from timeline events");
        }
    }
    
    // Public method to start the second cutscene
    // Call this from ProductInformationManager when dummy product info panel is confirmed
    public void StartSecondCutscene()
    {
        Debug.Log("=== STARTING SECOND CUTSCENE ===");
        
        if (isSecondCutscenePlaying)
        {
            Debug.LogWarning("Cutscene is already playing!");
            return;
        }
        
        // Validate everything before starting
        if (!ValidateComponents())
        {
            Debug.LogError("Failed to validate components for cutscene!");
            return;
        }
        
        isSecondCutscenePlaying = true;
        
        // Freeze player movement
        FreezePlayer();
        
        // Enable cutscene parent first
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(true);
            Debug.Log("Cutscene2 parent enabled");
        }
        
        // Disable game UI
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
            Debug.Log("Game UI disabled");
        }
        
        // Disable audio handler
        if (audioHandler != null)
        {
            audioHandler.SetActive(false);
            Debug.Log("Audio handler disabled");
        }
        
        // IMPORTANT: Wait one frame before playing timeline
        // This ensures everything is properly activated
        StartCoroutine(PlayTimelineAfterFrame());
    }
    
    private IEnumerator PlayTimelineAfterFrame()
    {
        // Wait for end of frame to ensure everything is set up
        yield return new WaitForEndOfFrame();
        
        // Now play the timeline
        if (npcCutscene2Director != null)
        {
            Debug.Log("Playing timeline...");
            npcCutscene2Director.Play();
        }
        else
        {
            Debug.LogError("Cannot play timeline: Director is null!");
        }
        
        // Invoke start event
        onSecondCutsceneStart?.Invoke();
        
        Debug.Log("Second cutscene started successfully");
    }
    
    bool ValidateComponents()
    {
        bool allValid = true;
        
        // Check player
        if (playerObject == null)
        {
            Debug.LogError("Player object is null!");
            allValid = false;
        }
        
        // Check CollectProducts script
        if (collectProductsScript == null)
        {
            Debug.LogError("CollectProducts script is null!");
            allValid = false;
        }
        else if (!collectProductsScript.HasCollectedDummyProduct())
        {
            Debug.LogWarning("Dummy product not collected yet!");
            allValid = false;
        }
        
        // Check timeline director
        if (npcCutscene2Director == null)
        {
            Debug.LogError("Timeline director is null!");
            allValid = false;
        }
        
        // Check cutscene parent
        if (cutscene2ParentObject == null)
        {
            Debug.LogError("Cutscene parent object is null!");
            allValid = false;
        }
        
        Debug.Log($"Component validation: {(allValid ? "PASSED" : "FAILED")}");
        return allValid;
    }
    
    void FreezePlayer()
    {
        if (playerObject == null) 
        {
            Debug.LogError("Cannot freeze player: Player object is null!");
            return;
        }
        
        // Disable ThirdPersonController
        ThirdPersonController controller = playerObject.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.enabled = false;
            Debug.Log("Player controller disabled");
        }
        else
        {
            Debug.LogWarning("ThirdPersonController not found on player!");
        }
        
        // Disable Animator
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
            Debug.Log("Player animator disabled");
        }
        else
        {
            Debug.LogWarning("Animator not found on player!");
        }
        
        // Reset inputs
        StarterAssetsInputs inputs = playerObject.GetComponent<StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.move = Vector2.zero;
            inputs.look = Vector2.zero;
            inputs.sprint = false;
            inputs.jump = false;
            Debug.Log("Player inputs reset");
        }
        else
        {
            Debug.LogWarning("StarterAssetsInputs not found on player!");
        }
        
        // Disable PlayerInput (Input System)
        PlayerInput playerInput = playerObject.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
            Debug.Log("Player input system disabled");
        }
        else
        {
            Debug.LogWarning("PlayerInput component not found on player!");
        }
        
        // Stop audio
        AudioSource audioSource = playerObject.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Stop();
            Debug.Log("Player audio stopped");
        }
        
        // Stop physics movement
        Rigidbody rb = playerObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("Player physics stopped");
        }
        
        Debug.Log("Player frozen successfully");
    }
    
    void OnSecondCutscenePlayed(PlayableDirector director)
    {
        Debug.Log("Timeline started playing");
        
        // Show dialogue canvas when timeline starts
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
            Debug.Log("Dialogue canvas enabled");
        }
    }
    
    void OnSecondCutsceneFinished(PlayableDirector director)
    {
        Debug.Log("Timeline finished playing");
        
        if (isSecondCutscenePlaying)
        {
            FinishSecondCutscene();
        }
    }
    
    void FinishSecondCutscene()
    {
        Debug.Log("=== FINISHING SECOND CUTSCENE ===");
        
        // Hide dialogue canvas
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
            Debug.Log("Dialogue canvas disabled");
        }
        
        // Disable cutscene parent
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
            Debug.Log("Cutscene2 parent disabled");
        }
        
        // Re-enable game UI
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("Game UI enabled");
        }
        
        // Re-enable audio handler
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
            Debug.Log("Audio handler enabled");
        }
        
        // Unfreeze player
        UnfreezePlayer();
        
        // Update state
        isSecondCutscenePlaying = false;
        
        // Invoke end event
        onSecondCutsceneEnd?.Invoke();
        
        Debug.Log("Second cutscene finished successfully");
    }
    
    void UnfreezePlayer()
    {
        if (playerObject == null) 
        {
            Debug.LogError("Cannot unfreeze player: Player object is null!");
            return;
        }
        
        // Re-enable ThirdPersonController
        ThirdPersonController controller = playerObject.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.enabled = true;
            Debug.Log("Player controller enabled");
        }
        
        // Re-enable Animator
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            
            // Reset animation states
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("MotionSpeed", 0f);
            Debug.Log("Player animator enabled and reset");
        }
        
        // Re-enable PlayerInput
        PlayerInput playerInput = playerObject.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = true;
            Debug.Log("Player input system enabled");
        }
        
        Debug.Log("Player unfrozen successfully");
    }
    
    // Public method to manually start second cutscene from other scripts
    public void ManualStartSecondCutscene()
    {
        StartSecondCutscene();
    }
    
    // Check if second cutscene is currently playing
    public bool IsSecondCutscenePlaying()
    {
        return isSecondCutscenePlaying;
    }
    
    // Skip the second cutscene
    public void SkipSecondCutscene()
    {
        if (isSecondCutscenePlaying && npcCutscene2Director != null)
        {
            Debug.Log("Skipping second cutscene");
            npcCutscene2Director.Stop();
        }
    }
    
    // Reset the second cutscene state
    public void ResetSecondCutscene()
    {
        isSecondCutscenePlaying = false;
        
        // Hide dialogue canvas
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
        }
        
        // Disable cutscene parent object
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
        }
        
        // Ensure player is unfrozen
        UnfreezePlayer();
        
        Debug.Log("Second cutscene reset");
    }
    
    // Debug method to test cutscene
    [ContextMenu("Test Start Second Cutscene")]
    public void TestStartSecondCutscene()
    {
        Debug.Log("=== TESTING SECOND CUTSCENE ===");
        StartSecondCutscene();
    }
    
    [ContextMenu("Test Skip Second Cutscene")]
    public void TestSkipSecondCutscene()
    {
        SkipSecondCutscene();
    }
    
    // Editor method to auto-find references
    #if UNITY_EDITOR
    [ContextMenu("Auto-Find References")]
    public void AutoFindReferences()
    {
        // Find player
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            playerObject = foundPlayer;
            Debug.Log("Auto-found player: " + playerObject.name);
        }
        
        // Find CollectProducts script
        CollectProducts foundCollectScript = FindObjectOfType<CollectProducts>();
        if (foundCollectScript != null)
        {
            collectProductsScript = foundCollectScript;
            Debug.Log("Auto-found CollectProducts script");
        }
        
        // Find Audio Handler
        GameObject foundAudioHandler = GameObject.Find("Audio_Handler");
        if (foundAudioHandler != null)
        {
            audioHandler = foundAudioHandler;
            Debug.Log("Auto-found Audio Handler");
        }
        
        // Find Game UI Canvas
        GameObject foundGameUICanvas = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
        if (foundGameUICanvas != null)
        {
            gameUICanvas = foundGameUICanvas;
            Debug.Log("Auto-found Game UI Canvas");
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
    #endif
}