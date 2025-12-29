using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class NPCGuardController : MonoBehaviour
{
    [Header("Cutscene Configuration")]
    [SerializeField] private GameObject cutsceneObjectParent;
    [SerializeField] private PlayableDirector timelineDirector;
    [SerializeField] private bool autoTrigger = true;
    [SerializeField] private bool oneTimeTrigger = true;
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private float facingThreshold = 60f;
    [SerializeField] private bool requireFacing = true;

    [Header("NPC Visuals")]
    [SerializeField] private GameObject npcArrowIndicator;
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private bool showDialogueDuringCutscene = true;
    [SerializeField] private TMP_Text subtitleText; // Add TMP_Text for subtitles
    [SerializeField] private string welcomeMessage = "Welcome to the Kingdom of Allerthria!";
    [SerializeField] private string questMessage = "Our kingdom faces a great threat from the dark forces...";
    [SerializeField] private string acceptQuestMessage = "Will you accept this quest to save our kingdom?";

    [Header("Interactive Props")]
    [SerializeField] private GameObject interactiveItem;
    private Vector3 itemOriginalPosition;
    private Quaternion itemOriginalRotation;

    [Header("Quest Objects")]
    [SerializeField] private GameObject kingdomGate; // The gate that will be removed
    [SerializeField] private GameObject npcModel; // The NPC model that will disappear
    [SerializeField] private bool hideNpcAfterAccept = true;
    [SerializeField] private bool removeGateAfterAccept = true;

    [Header("Decision UI")]
    [SerializeField] private GameObject decisionCanvas; // UI for Yes/No choice
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private float decisionDisplayDelay = 2f; // Time before showing decision buttons

    [Header("Player Control")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private bool freezePlayerCompletely = true;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject gameUICanvas;
    [SerializeField] private Button skipButton;
    [SerializeField] private bool enableSkip = true;
    [SerializeField] private float skipButtonDelay = 2f;
    
    [Header("Audio Management")]
    [SerializeField] private GameObject audioManagerObject;
    
    [Header("Re-Trigger Settings")]
    [SerializeField] private float reTriggerDelay = 5f; // Delay before NPC can be triggered again after decline
    [SerializeField] private bool showReTriggerCountdown = true; // Show countdown on NPC indicator
    
    [Header("Events")]
    public UnityEvent OnCutsceneBegin;
    public UnityEvent OnCutsceneComplete;
    public UnityEvent OnCutsceneSkipped;
    public UnityEvent OnQuestAccepted; // New event for quest acceptance
    public UnityEvent OnQuestDeclined; // New event for quest decline

    // State tracking
    private bool hasTriggered = false;
    private bool isCutsceneActive = false;
    private bool isQuestDecisionActive = false;
    private bool hasMadeDecision = false;
    private bool isReTriggerDelayed = false;
    private Transform playerTransform;
    
    // Player component references
    private ThirdPersonController playerController;
    private Animator playerAnimator;
    private AudioSource playerAudioSource;
    private StarterAssetsInputs playerInput;
    private PlayerInput inputSystem;
    
    // Original states for restoration
    private bool wasControllerEnabled;
    private bool wasAnimatorEnabled;
    private bool wasAudioEnabled;
    private bool wasInputEnabled;
    
    // Skip button tracking
    private float skipTimer = 0f;
    private bool skipAvailable = false;
    
    // Re-trigger tracking
    private float reTriggerTimer = 0f;
    private Coroutine reTriggerCoroutine;
    private TMP_Text arrowIndicatorText;

    void Awake()
    {
        InitializeSystem();
    }

    void InitializeSystem()
    {
        // Setup cutscene objects
        if (cutsceneObjectParent != null)
            cutsceneObjectParent.SetActive(false);
        
        if (timelineDirector != null)
        {
            timelineDirector.Stop();
            timelineDirector.stopped += OnTimelineFinished;
        }
        
        // Setup interactive item
        if (interactiveItem != null)
        {
            itemOriginalPosition = interactiveItem.transform.position;
            itemOriginalRotation = interactiveItem.transform.rotation;
            interactiveItem.SetActive(false);
        }
        
        // Setup decision UI
        if (decisionCanvas != null)
            decisionCanvas.SetActive(false);
            
        if (acceptButton != null)
            acceptButton.onClick.AddListener(AcceptQuest);
            
        if (declineButton != null)
            declineButton.onClick.AddListener(DeclineQuest);
        
        // Setup UI elements
        if (npcArrowIndicator != null)
        {
            npcArrowIndicator.SetActive(!autoTrigger);
            
            // Get text component for countdown if available
            if (showReTriggerCountdown)
            {
                arrowIndicatorText = npcArrowIndicator.GetComponentInChildren<TMP_Text>(true);
                if (arrowIndicatorText != null)
                {
                    arrowIndicatorText.gameObject.SetActive(false);
                }
            }
        }
            
        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);
            
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipCurrentCutscene);
            skipButton.gameObject.SetActive(false);
        }
        
        // Ensure game UI is active initially
        if (gameUICanvas != null)
            gameUICanvas.SetActive(true);
            
        // Ensure audio manager is active
        if (audioManagerObject != null)
            audioManagerObject.SetActive(true);
    }
    
    void OnDestroy()
    {
        if (timelineDirector != null)
            timelineDirector.stopped -= OnTimelineFinished;
            
        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipCurrentCutscene);
            
        if (acceptButton != null)
            acceptButton.onClick.RemoveListener(AcceptQuest);
            
        if (declineButton != null)
            declineButton.onClick.RemoveListener(DeclineQuest);
            
        // Stop any running coroutines
        if (reTriggerCoroutine != null)
        {
            StopCoroutine(reTriggerCoroutine);
        }
    }

    void Update()
    {
        // Handle skip button timing
        if (isCutsceneActive && enableSkip && !skipAvailable)
        {
            skipTimer += Time.deltaTime;
            if (skipTimer >= skipButtonDelay)
            {
                skipAvailable = true;
                ShowSkipButton();
            }
        }
        
        // Update re-trigger countdown display
        if (isReTriggerDelayed && showReTriggerCountdown && arrowIndicatorText != null && arrowIndicatorText.gameObject.activeSelf)
        {
            float timeLeft = reTriggerDelay - reTriggerTimer;
            if (timeLeft > 0)
            {
                arrowIndicatorText.text = Mathf.CeilToInt(timeLeft).ToString();
            }
        }
        
        // Auto-trigger check - Only trigger if we haven't made a decision yet OR it's not a one-time trigger
        if (autoTrigger && !isCutsceneActive && !isReTriggerDelayed && playerTransform != null)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) <= interactionRange)
            {
                if (!requireFacing || IsPlayerFacingNPC())
                {
                    // Check if we can trigger based on one-time trigger rules
                    if (!oneTimeTrigger || !hasTriggered || !hasMadeDecision)
                    {
                        StartCutsceneSequence();
                    }
                }
            }
        }
        
        // Handle input for quest decision
        if (isQuestDecisionActive && !hasMadeDecision)
        {
            // You can add keyboard shortcuts here if needed
            // For example: Space for accept, Escape for decline
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCutsceneActive && !isReTriggerDelayed)
        {
            playerTransform = other.transform;
            if (playerObject == null)
                playerObject = other.gameObject;
                
            CachePlayerComponents();
            
            // Check if we can interact based on trigger rules
            if (!oneTimeTrigger || !hasTriggered || !hasMadeDecision)
            {
                if (autoTrigger)
                    StartCutsceneSequence();
                else
                    ShowInteractionPrompt();
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!autoTrigger && npcArrowIndicator != null)
            {
                npcArrowIndicator.SetActive(false);
                // Hide countdown text when leaving
                if (arrowIndicatorText != null)
                    arrowIndicatorText.gameObject.SetActive(false);
            }
        }
    }

    void CachePlayerComponents()
    {
        playerController = playerObject?.GetComponent<ThirdPersonController>();
        playerAnimator = playerObject?.GetComponent<Animator>();
        playerAudioSource = playerObject?.GetComponent<AudioSource>();
        playerInput = playerObject?.GetComponent<StarterAssetsInputs>();
        inputSystem = playerObject?.GetComponent<PlayerInput>();
    }

    bool IsPlayerFacingNPC()
    {
        if (playerTransform == null) return false;
        
        Vector3 directionToNPC = (transform.position - playerTransform.position).normalized;
        Vector3 playerForward = playerTransform.forward;
        
        return Vector3.Angle(playerForward, directionToNPC) <= facingThreshold;
    }

    void ShowInteractionPrompt()
    {
        if (npcArrowIndicator != null)
        {
            npcArrowIndicator.SetActive(true);
            // Hide countdown text when showing normal prompt
            if (arrowIndicatorText != null)
                arrowIndicatorText.gameObject.SetActive(false);
        }
    }

    // Public method to trigger from UI button
    public void TriggerCutsceneFromUI()
    {
        if (!isCutsceneActive && !isReTriggerDelayed && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= interactionRange)
            {
                if (!requireFacing || IsPlayerFacingNPC())
                {
                    StartCutsceneSequence();
                }
            }
        }
    }

    public void StartCutsceneSequence()
    {
        if ((hasTriggered && oneTimeTrigger && hasMadeDecision) || isCutsceneActive || isReTriggerDelayed) return;
        
        hasTriggered = true;
        isCutsceneActive = true;
        hasMadeDecision = false; // Reset decision state when starting new cutscene
        isQuestDecisionActive = false; // Reset decision UI state
        
        // Store original states
        SavePlayerState();
        
        // Freeze player if required
        if (freezePlayerCompletely)
            FreezePlayer();
        
        // Hide interaction prompts
        if (npcArrowIndicator != null)
        {
            npcArrowIndicator.SetActive(false);
            if (arrowIndicatorText != null)
                arrowIndicatorText.gameObject.SetActive(false);
        }
        
        // Activate cutscene objects
        if (cutsceneObjectParent != null)
            cutsceneObjectParent.SetActive(true);
        
        // Setup interactive item
        if (interactiveItem != null)
        {
            interactiveItem.transform.position = itemOriginalPosition;
            interactiveItem.transform.rotation = itemOriginalRotation;
            interactiveItem.SetActive(true);
        }
        
        // Disable game UI
        if (gameUICanvas != null)
            gameUICanvas.SetActive(false);
        
        // Disable audio manager
        if (audioManagerObject != null)
            audioManagerObject.SetActive(false);
        
        // Start timeline
        if (timelineDirector != null)
            timelineDirector.Play();
        
        // Show dialogue if enabled
        if (showDialogueDuringCutscene && dialogueCanvas != null)
            dialogueCanvas.SetActive(true);
        
        // Start showing subtitles
        StartCoroutine(ShowSubtitlesSequence());
        
        // Invoke begin event
        OnCutsceneBegin?.Invoke();
        
        Debug.Log("Cutscene sequence started - Decision reset");
    }

    IEnumerator ShowSubtitlesSequence()
    {
        // Welcome message
        if (subtitleText != null)
        {
            subtitleText.text = welcomeMessage;
            yield return new WaitForSeconds(3f); // Display for 3 seconds
            
            // Quest message
            subtitleText.text = questMessage;
            yield return new WaitForSeconds(4f); // Display for 4 seconds
            
            // Decision prompt
            subtitleText.text = acceptQuestMessage;
            
            // Wait a moment before showing decision UI
            yield return new WaitForSeconds(decisionDisplayDelay);
            
            // Show decision UI
            ShowQuestDecision();
        }
    }

    void ShowQuestDecision()
    {
        isQuestDecisionActive = true;
        
        if (decisionCanvas != null)
            decisionCanvas.SetActive(true);
            
        // Optionally pause the timeline here
        if (timelineDirector != null && timelineDirector.playableGraph.IsValid())
        {
            timelineDirector.playableGraph.GetRootPlayable(0).SetSpeed(0); // Pause timeline
        }
    }

    void HideQuestDecision()
    {
        isQuestDecisionActive = false;
        
        if (decisionCanvas != null)
            decisionCanvas.SetActive(false);
            
        if (subtitleText != null)
            subtitleText.text = "";
            
        // Resume timeline if it was paused
        if (timelineDirector != null && timelineDirector.playableGraph.IsValid())
        {
            timelineDirector.playableGraph.GetRootPlayable(0).SetSpeed(1); // Resume timeline
        }
    }

    public void AcceptQuest()
    {
        if (!hasMadeDecision)
        {
            hasMadeDecision = true;
            HideQuestDecision();
            
            // Remove gate
            if (removeGateAfterAccept && kingdomGate != null)
                kingdomGate.SetActive(false);
            
            // Hide NPC
            if (hideNpcAfterAccept && npcModel != null)
                npcModel.SetActive(false);
            
            // Disable NPC interaction for future
            if (npcArrowIndicator != null)
            {
                npcArrowIndicator.SetActive(false);
                if (arrowIndicatorText != null)
                    arrowIndicatorText.gameObject.SetActive(false);
            }
            
            // Show acceptance message
            if (subtitleText != null)
            {
                subtitleText.text = "Thank you for accepting our quest! The gate is now open.";
                StartCoroutine(HideSubtitleAfterDelay(3f));
            }
            
            // Restore game UI immediately
            if (gameUICanvas != null)
                gameUICanvas.SetActive(true);
            
            // Trigger quest accepted event
            OnQuestAccepted?.Invoke();
            
            // Complete the cutscene
            StartCoroutine(CompleteCutsceneAfterDelay(3f, false));
            
            Debug.Log("Quest accepted");
        }
    }

    public void DeclineQuest()
    {
        if (!hasMadeDecision)
        {
            hasMadeDecision = true;
            HideQuestDecision();
            
            // Show decline message
            if (subtitleText != null)
            {
                subtitleText.text = "Perhaps you need more time to consider...";
                StartCoroutine(HideSubtitleAfterDelay(3f));
            }
            
            // Restore game UI immediately
            if (gameUICanvas != null)
                gameUICanvas.SetActive(true);
            
            // Trigger quest declined event
            OnQuestDeclined?.Invoke();
            
            // Start re-trigger delay coroutine
            if (reTriggerCoroutine != null)
                StopCoroutine(reTriggerCoroutine);
            reTriggerCoroutine = StartCoroutine(ReTriggerDelayCoroutine());
            
            // Complete the cutscene (but keep gate and NPC)
            StartCoroutine(CompleteCutsceneAfterDelay(3f, false));
            
            // Reset decision state so player can try again (after delay)
            hasMadeDecision = false; // Allow player to make decision again
            isQuestDecisionActive = false; // Reset decision state
            
            Debug.Log("Quest declined - Starting re-trigger delay");
        }
    }

    IEnumerator ReTriggerDelayCoroutine()
    {
        isReTriggerDelayed = true;
        reTriggerTimer = 0f;
        
        // Show countdown on arrow indicator if available
        if (!autoTrigger && npcArrowIndicator != null && arrowIndicatorText != null && showReTriggerCountdown)
        {
            npcArrowIndicator.SetActive(true);
            arrowIndicatorText.gameObject.SetActive(true);
        }
        
        while (reTriggerTimer < reTriggerDelay)
        {
            reTriggerTimer += Time.deltaTime;
            
            // Update countdown text
            if (arrowIndicatorText != null && arrowIndicatorText.gameObject.activeSelf)
            {
                float timeLeft = reTriggerDelay - reTriggerTimer;
                arrowIndicatorText.text = Mathf.CeilToInt(timeLeft).ToString();
            }
            
            yield return null;
        }
        
        // Delay complete
        isReTriggerDelayed = false;
        
        // Hide countdown text and show normal indicator
        if (!autoTrigger && npcArrowIndicator != null)
        {
            if (arrowIndicatorText != null)
                arrowIndicatorText.gameObject.SetActive(false);
            
            // Only show arrow if player is in range
            if (playerTransform != null && 
                Vector3.Distance(transform.position, playerTransform.position) <= interactionRange)
            {
                npcArrowIndicator.SetActive(true);
            }
        }
        
        Debug.Log("Re-trigger delay complete - NPC can be interacted with again");
    }

    IEnumerator HideSubtitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (subtitleText != null)
            subtitleText.text = "";
    }

    IEnumerator CompleteCutsceneAfterDelay(float delay, bool wasSkipped)
    {
        yield return new WaitForSeconds(delay);
        CompleteCutscene(wasSkipped);
    }

    void SavePlayerState()
    {
        if (playerController != null)
            wasControllerEnabled = playerController.enabled;
        
        if (playerAnimator != null)
            wasAnimatorEnabled = playerAnimator.enabled;
        
        if (playerAudioSource != null)
            wasAudioEnabled = playerAudioSource.enabled;
        
        if (inputSystem != null)
            wasInputEnabled = inputSystem.enabled;
    }

    void FreezePlayer()
    {
        // Disable controller
        if (playerController != null)
            playerController.enabled = false;
        
        // Stop animations
        if (playerAnimator != null)
            playerAnimator.enabled = false;
        
        // Stop audio
        if (playerAudioSource != null)
        {
            playerAudioSource.enabled = false;
            playerAudioSource.Stop();
        }
        
        // Disable input system
        if (inputSystem != null)
            inputSystem.enabled = false;
        
        // Reset player inputs
        if (playerInput != null)
        {
            playerInput.move = Vector2.zero;
            playerInput.look = Vector2.zero;
            playerInput.sprint = false;
            playerInput.jump = false;
            playerInput.enabled = false;
        }
        
        // Handle physics
        Rigidbody rb = playerObject?.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // Disable CharacterController
        CharacterController cc = playerObject?.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;
    }

    void OnTimelineFinished(PlayableDirector director)
    {
        // Only auto-complete if no decision needs to be made
        if (!isQuestDecisionActive)
            CompleteCutscene(false);
    }

    void ShowSkipButton()
    {
        if (skipButton != null && enableSkip)
        {
            skipButton.gameObject.SetActive(true);
            StartCoroutine(AnimateSkipButton());
        }
    }

    IEnumerator AnimateSkipButton()
    {
        // Simple fade-in animation
        CanvasGroup cg = skipButton.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0;
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0, 1, elapsed / duration);
                yield return null;
            }
            cg.alpha = 1;
        }
    }

    public void SkipCurrentCutscene()
    {
        if (isCutsceneActive && timelineDirector != null)
        {
            timelineDirector.Stop();
            
            // Hide decision UI if active
            if (isQuestDecisionActive)
                HideQuestDecision();
                
            CompleteCutscene(true);
            OnCutsceneSkipped?.Invoke();
            Debug.Log("Cutscene skipped by user");
        }
    }

    void CompleteCutscene(bool wasSkipped)
    {
        isCutsceneActive = false;
        skipAvailable = false;
        skipTimer = 0f;
        
        // Hide skip button
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);
        
        // Hide dialogue and subtitles
        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);
            
        if (subtitleText != null)
            subtitleText.text = "";
        
        // Deactivate cutscene objects
        if (cutsceneObjectParent != null)
            cutsceneObjectParent.SetActive(false);
        
        // Restore game UI
        if (gameUICanvas != null)
            gameUICanvas.SetActive(true);
        
        // Restore audio manager
        if (audioManagerObject != null)
            audioManagerObject.SetActive(true);
        
        // Unfreeze player
        RestorePlayer();
        
        // IMPORTANT: If quest was declined OR this is a one-time trigger that hasn't been completed,
        // reset the trigger so player can interact again (after delay if declined)
        if (!hasMadeDecision || !oneTimeTrigger)
        {
            hasTriggered = false;
            
            // Don't show arrow indicator immediately if we're in re-trigger delay
            if (!autoTrigger && npcArrowIndicator != null && !isReTriggerDelayed)
                npcArrowIndicator.SetActive(true);
        }
        
        // If quest was accepted and NPC should be hidden, keep indicator off
        if (hasMadeDecision && hideNpcAfterAccept && npcArrowIndicator != null)
        {
            npcArrowIndicator.SetActive(false);
            if (arrowIndicatorText != null)
                arrowIndicatorText.gameObject.SetActive(false);
        }
        
        // Invoke completion event
        if (wasSkipped)
            OnCutsceneSkipped?.Invoke();
        else
            OnCutsceneComplete?.Invoke();
        
        Debug.Log($"Cutscene {(wasSkipped ? "skipped" : "completed")} - hasMadeDecision: {hasMadeDecision}");
    }

    void RestorePlayer()
    {
        // Restore controller
        if (playerController != null)
            playerController.enabled = wasControllerEnabled;
        
        // Restore animator
        if (playerAnimator != null)
        {
            playerAnimator.enabled = wasAnimatorEnabled;
            if (wasAnimatorEnabled)
            {
                playerAnimator.Rebind();
                playerAnimator.Update(0f);
            }
        }
        
        // Restore audio
        if (playerAudioSource != null)
            playerAudioSource.enabled = wasAudioEnabled;
        
        // Restore input system
        if (inputSystem != null)
            inputSystem.enabled = wasInputEnabled;
        
        // Restore player inputs
        if (playerInput != null)
        {
            playerInput.enabled = true;
            playerInput.move = Vector2.zero;
            playerInput.look = Vector2.zero;
            playerInput.sprint = false;
            playerInput.jump = false;
        }
        
        // Restore physics
        Rigidbody rb = playerObject?.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
        }
        
        // Restore CharacterController
        CharacterController cc = playerObject?.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = true;
    }

    public void ResetCutsceneTrigger()
    {
        hasTriggered = false;
        isCutsceneActive = false;
        hasMadeDecision = false;
        isReTriggerDelayed = false;
        
        // Stop any running re-trigger coroutines
        if (reTriggerCoroutine != null)
        {
            StopCoroutine(reTriggerCoroutine);
            reTriggerCoroutine = null;
        }
        
        if (npcArrowIndicator != null)
        {
            npcArrowIndicator.SetActive(!autoTrigger);
            if (arrowIndicatorText != null)
                arrowIndicatorText.gameObject.SetActive(false);
        }
        
        if (interactiveItem != null)
            interactiveItem.SetActive(false);
        
        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);
        
        if (decisionCanvas != null)
            decisionCanvas.SetActive(false);
            
        if (subtitleText != null)
            subtitleText.text = "";
        
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);
        
        // Restore NPC and gate if they were disabled
        if (npcModel != null)
            npcModel.SetActive(true);
            
        if (kingdomGate != null)
            kingdomGate.SetActive(true);
        
        RestorePlayer();
        
        Debug.Log("Cutscene trigger reset");
    }

    // Utility methods
    public bool IsCutscenePlaying()
    {
        return isCutsceneActive;
    }
    
    public bool HasAcceptedQuest()
    {
        return hasMadeDecision;
    }
    
    public float GetSkipButtonTimeRemaining()
    {
        return Mathf.Max(0, skipButtonDelay - skipTimer);
    }
    
    public void SetSkipEnabled(bool enabled)
    {
        enableSkip = enabled;
        if (!enabled && skipButton != null)
            skipButton.gameObject.SetActive(false);
    }
    
    public void SetSkipDelay(float delay)
    {
        skipButtonDelay = Mathf.Max(0, delay);
    }
    
    public void SetReTriggerDelay(float delay)
    {
        reTriggerDelay = Mathf.Max(0, delay);
    }
    
    public float GetReTriggerTimeRemaining()
    {
        if (!isReTriggerDelayed) return 0f;
        return Mathf.Max(0, reTriggerDelay - reTriggerTimer);
    }
    
    public bool IsReTriggerDelayed()
    {
        return isReTriggerDelayed;
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw facing direction
        if (requireFacing)
        {
            Gizmos.color = Color.cyan;
            Vector3 direction = transform.forward * interactionRange;
            Gizmos.DrawRay(transform.position, direction);
            
            // Draw arc
            UnityEditor.Handles.color = new Color(0, 1, 1, 0.1f);
            UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, 
                Quaternion.Euler(0, -facingThreshold, 0) * transform.forward, 
                facingThreshold * 2, interactionRange);
        }
    }
    #endif
}