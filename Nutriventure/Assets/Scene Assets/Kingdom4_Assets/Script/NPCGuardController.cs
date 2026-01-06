using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using Cinemachine;

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
    
    [Header("Subtitle Controller")]
    [SerializeField] private K2_SubtitleController subtitleController;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private int maxCharsPerLine = 60;
    
    [Header("Allergthria-Specific Dialogue")]
    [TextArea(3, 6)]
    [SerializeField] private string[] welcomeMessageLines = new string[]
    {
        "When the Enerlings vanished, chaos consumed Allerthria.",
        "Dangerous foods spread, allergen knowledge was lost.",
        "Fear followed every careless meal in the kingdom."
    };
    
    [TextArea(3, 6)]
    [SerializeField] private string[] questMessageLines = new string[]
    {
        "Now, Traveler, the kingdom calls upon you.",
        "Journey through Allerthria, reclaim the Big Nine.",
        "Milk, Egg, Fish, Shellfish, Tree Nuts, Peanuts..."
    };
    
    [TextArea(3, 6)]
    [SerializeField] private string[] dangerMessageLines = new string[]
    {
        "Wheat, Soybeans, and Sesame complete the Nine.",
        "Guide the wagon safely to Queen Alergica.",
        "Beware moving stone trials with food choices."
    };
    
    [TextArea(3, 6)]
    [SerializeField] private string[] finalMessageLines = new string[]
    {
        "Choose safe foods, or lose a heart.",
        "Succeed for the Scroll of Allergenia.",
        "Restore protection to the kingdom."
    };
    
    [TextArea(3, 6)]
    [SerializeField] private string[] acceptQuestLines = new string[]
    {
        "The fate of Allerthria is in your hands.",
        "Will you accept the challenge?",
        ""
    };

    [Header("Narration Settings")]
    [SerializeField] private AudioClip[] narrationClips;
    [SerializeField] private float narrationDelayAfterTimeline = 1f;
    [SerializeField] private bool playNarrationAudio = true;
    [SerializeField] private float lineDisplayDuration = 3f;
    [SerializeField] private float pauseBetweenParagraphs = 1f;
    
    [Header("Interactive Props")]
    [SerializeField] private GameObject interactiveItem;
    private Vector3 itemOriginalPosition;
    private Quaternion itemOriginalRotation;

    [Header("Quest Objects")]
    [SerializeField] private GameObject kingdomGate;
    [SerializeField] private GameObject npcModel;
    [SerializeField] private bool hideNpcAfterAccept = true;
    [SerializeField] private bool removeGateAfterAccept = true;

    [Header("Decision UI")]
    [SerializeField] private GameObject decisionCanvas;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private float decisionDisplayDelay = 2f;

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
    [SerializeField] private AudioSource narrationAudioSource;
    
    [Header("Re-Trigger Settings")]
    [SerializeField] private float reTriggerDelay = 5f;
    [SerializeField] private bool showReTriggerCountdown = true;
    
    [Header("Events")]
    public UnityEvent OnCutsceneBegin;
    public UnityEvent OnCutsceneComplete;
    public UnityEvent OnCutsceneSkipped;
    public UnityEvent OnQuestAccepted;
    public UnityEvent OnQuestDeclined;

    // State tracking
    private bool hasTriggered = false;
    private bool isCutsceneActive = false;
    private bool isQuestDecisionActive = false;
    private bool hasMadeDecision = false;
    private bool isReTriggerDelayed = false;
    private Transform playerTransform;
    private bool isTimelinePlaying = false;
    
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
    
    // Camera control
    private CinemachineVirtualCamera playerVCam;
    private int playerOriginalPriority = 10;
    
    // Skip button tracking
    private float skipTimer = 0f;
    private bool skipAvailable = false;
    
    // Re-trigger tracking
    private float reTriggerTimer = 0f;
    private Coroutine reTriggerCoroutine;
    private TMP_Text arrowIndicatorText;
    
    // Narration tracking
    private Coroutine currentNarrationCoroutine;

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
        {
            dialogueCanvas.SetActive(false);
        }
            
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
        
        // Setup narration audio source
        if (narrationAudioSource == null)
        {
            narrationAudioSource = gameObject.AddComponent<AudioSource>();
            narrationAudioSource.playOnAwake = false;
        }
        
        // Find player camera
        playerVCam = FindAnyObjectByType<CinemachineVirtualCamera>();
        if (playerVCam != null)
        {
            playerOriginalPriority = playerVCam.Priority;
        }
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
            
        if (reTriggerCoroutine != null)
        {
            StopCoroutine(reTriggerCoroutine);
        }
        
        if (currentNarrationCoroutine != null)
        {
            StopCoroutine(currentNarrationCoroutine);
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
        
        // Auto-trigger check
        if (autoTrigger && !isCutsceneActive && !isReTriggerDelayed && playerTransform != null)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) <= interactionRange)
            {
                if (!requireFacing || IsPlayerFacingNPC())
                {
                    if (!oneTimeTrigger || !hasTriggered || !hasMadeDecision)
                    {
                        StartCutsceneSequence();
                    }
                }
            }
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
            if (arrowIndicatorText != null)
                arrowIndicatorText.gameObject.SetActive(false);
        }
    }

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
        hasMadeDecision = false;
        isQuestDecisionActive = false;
        
        // Store original states
        SavePlayerState();
        
        // Freeze player
        if (freezePlayerCompletely)
            FreezePlayer();
        
        // Hide interaction prompts
        if (npcArrowIndicator != null)
        {
            npcArrowIndicator.SetActive(false);
            if (arrowIndicatorText != null)
                arrowIndicatorText.gameObject.SetActive(false);
        }
        
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
        
        // Show dialogue canvas (turn it ON and keep it ON)
        if (showDialogueDuringCutscene && dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
        }
        
        // Clear any existing subtitles
        if (subtitleController != null)
        {
            subtitleController.ClearSubtitle();
        }
        
        // Invoke begin event
        OnCutsceneBegin?.Invoke();
        
        // Start the narration sequence
        currentNarrationCoroutine = StartCoroutine(NarrationSequence());
        
        Debug.Log("NPC narration sequence started");
    }

    IEnumerator NarrationSequence()
    {   
        // Activate cutscene objects if available
        if (cutsceneObjectParent != null)
            cutsceneObjectParent.SetActive(true);
        
        // Setup cutscene cameras
        SetupCutsceneCameras();
        
        // Start timeline if available
        if (timelineDirector != null && timelineDirector.playableAsset != null)
        {
            yield return StartCoroutine(PlayTimelineWithNarration());
        }
        else
        {
            // Fallback: just play narration
            yield return StartCoroutine(PlayNarrationOnly());
        }
    }

    IEnumerator ShowNarrationLine(string message)
    {
        // Format the message to fit within maxCharsPerLine
        string formattedMessage = FormatMessageForLines(message, maxCharsPerLine);
        
        // Clear any existing subtitles
        if (subtitleController != null)
        {
            subtitleController.ClearSubtitle();
        }
        
        // Show subtitle with typing effect
        if (subtitleController != null)
        {
            subtitleController.ShowSubtitle(formattedMessage, typingSpeed);
        }
        
        // Play narration audio if available
        if (playNarrationAudio && narrationAudioSource != null && narrationClips != null && narrationClips.Length > 0)
        {
            if (!narrationAudioSource.isPlaying)
            {
                narrationAudioSource.PlayOneShot(narrationClips[0]);
            }
        }
        
        // Calculate typing duration
        float typingDuration = formattedMessage.Length * typingSpeed;
        
        // Wait for typing to complete plus display time
        yield return new WaitForSeconds(typingDuration + lineDisplayDuration);
        
        // Clear subtitle after display
        if (subtitleController != null)
        {
            subtitleController.ClearSubtitle();
        }
        
        // Small pause between lines
        yield return new WaitForSeconds(0.5f);
    }

    string FormatMessageForLines(string message, int maxCharsPerLine)
    {
        // Simple formatting to ensure text fits
        if (message.Length <= maxCharsPerLine)
            return message;
        
        // Try to break at natural points
        int breakPoint = message.LastIndexOf(' ', maxCharsPerLine);
        if (breakPoint > 0)
        {
            return message.Substring(0, breakPoint) + "\n" + message.Substring(breakPoint + 1);
        }
        
        return message;
    }

    IEnumerator ShowNarrationParagraph(string[] lines)
    {
        foreach (string line in lines)
        {
            if (!string.IsNullOrEmpty(line.Trim()))
            {
                yield return StartCoroutine(ShowNarrationLine(line));
            }
        }
        // Pause between paragraphs
        yield return new WaitForSeconds(pauseBetweenParagraphs);
    }

    void SetupCutsceneCameras()
    {
        // Lower priority of player camera
        if (playerVCam != null)
        {
            playerVCam.Priority = 0;
        }
        
        // Enable all cameras in cutscene
        if (cutsceneObjectParent != null)
        {
            Camera[] cutsceneCams = cutsceneObjectParent.GetComponentsInChildren<Camera>(true);
            foreach (Camera cam in cutsceneCams)
            {
                cam.enabled = true;
            }
            
            // Set high priority for timeline cameras
            CinemachineVirtualCamera[] timelineCams = cutsceneObjectParent.GetComponentsInChildren<CinemachineVirtualCamera>(true);
            foreach (CinemachineVirtualCamera vcam in timelineCams)
            {
                vcam.enabled = true;
                vcam.Priority = 11;
            }
        }
    }

    void RestorePlayerCamera()
    {
        // Restore player camera priority
        if (playerVCam != null)
        {
            playerVCam.Priority = playerOriginalPriority;
        }
        
        // Disable cutscene cameras
        if (cutsceneObjectParent != null)
        {
            Camera[] cutsceneCams = cutsceneObjectParent.GetComponentsInChildren<Camera>();
            foreach (Camera cam in cutsceneCams)
            {
                cam.enabled = false;
            }
            
            CinemachineVirtualCamera[] timelineCams = cutsceneObjectParent.GetComponentsInChildren<CinemachineVirtualCamera>();
            foreach (CinemachineVirtualCamera vcam in timelineCams)
            {
                vcam.enabled = false;
            }
        }
    }

    IEnumerator PlayTimelineWithNarration()
    {
        isTimelinePlaying = true;
        
        // Start timeline
        timelineDirector.Play();
        
        // Wait for establishing shot
        yield return new WaitForSeconds(narrationDelayAfterTimeline);
        
        // Paragraph 1: The problem (3 lines)
        yield return StartCoroutine(ShowNarrationParagraph(welcomeMessageLines));
        
        // Paragraph 2: The call to action (3 lines)
        yield return StartCoroutine(ShowNarrationParagraph(questMessageLines));
        
        // Paragraph 3: The dangers (3 lines)
        yield return StartCoroutine(ShowNarrationParagraph(dangerMessageLines));
        
        // Paragraph 4: The goal (3 lines)
        yield return StartCoroutine(ShowNarrationParagraph(finalMessageLines));
        
        // Wait for timeline to finish
        while (timelineDirector.state == PlayState.Playing)
        {
            yield return null;
        }
        
        isTimelinePlaying = false;
        
        // Final call to action (2 lines)
        yield return StartCoroutine(ShowNarrationParagraph(acceptQuestLines));
        
        // Show decision
        ShowQuestDecision();
    }

    IEnumerator PlayNarrationOnly()
    {
        // Paragraph 1: The problem (3 lines)
        yield return StartCoroutine(ShowNarrationParagraph(welcomeMessageLines));
        
        // Paragraph 2: The call to action (3 lines)
        yield return StartCoroutine(ShowNarrationParagraph(questMessageLines));
        
        // Paragraph 3: The dangers (3 lines)
        yield return StartCoroutine(ShowNarrationParagraph(dangerMessageLines));
        
        // Paragraph 4: The goal (3 lines)
        yield return StartCoroutine(ShowNarrationParagraph(finalMessageLines));
        
        // Final call to action (2 lines)
        yield return StartCoroutine(ShowNarrationParagraph(acceptQuestLines));
        
        // Show decision
        ShowQuestDecision();
    }

    void ShowQuestDecision()
    {
        isQuestDecisionActive = true;
        
        if (decisionCanvas != null)
            decisionCanvas.SetActive(true);
        
        // Pause timeline if playing
        if (timelineDirector != null && timelineDirector.state == PlayState.Playing)
        {
            timelineDirector.Pause();
        }
        
        // Clear subtitles before showing decision
        if (subtitleController != null)
        {
            subtitleController.ClearSubtitle();
        }
    }

    void HideQuestDecision()
    {
        isQuestDecisionActive = false;
        
        if (decisionCanvas != null)
            decisionCanvas.SetActive(false);
    }

    public void AcceptQuest()
    {
        if (!hasMadeDecision)
        {
            hasMadeDecision = true;
            HideQuestDecision();
            
            // Resume timeline briefly for completion if it was playing
            if (timelineDirector != null && timelineDirector.state == PlayState.Paused)
            {
                timelineDirector.Resume();
            }
            
            // Show acceptance message
            StartCoroutine(ShowAcceptanceMessage());
            
            // Remove gate
            if (removeGateAfterAccept && kingdomGate != null)
                kingdomGate.SetActive(false);
            
            // Hide NPC
            if (hideNpcAfterAccept && npcModel != null)
                npcModel.SetActive(false);
            
            // Hide NPC indicator
            if (npcArrowIndicator != null)
            {
                npcArrowIndicator.SetActive(false);
                if (arrowIndicatorText != null)
                    arrowIndicatorText.gameObject.SetActive(false);
            }
            
            // Trigger event
            OnQuestAccepted?.Invoke();
            
            Debug.Log("Quest accepted");
        }
    }

    IEnumerator ShowAcceptanceMessage()
    {
        string[] acceptanceLines = new string[]
        {
            "Thank you for accepting our quest!",
            "The gate is now open.",
            ""
        };
        
        yield return StartCoroutine(ShowNarrationParagraph(acceptanceLines));
        
        // Complete after short delay
        StartCoroutine(CompleteCutsceneAfterDelay(1f, false));
    }

    public void DeclineQuest()
    {
        if (!hasMadeDecision)
        {
            hasMadeDecision = true;
            HideQuestDecision();
            
            // Resume timeline briefly if it was playing
            if (timelineDirector != null && timelineDirector.state == PlayState.Paused)
            {
                timelineDirector.Resume();
            }
            
            // Show decline message
            StartCoroutine(ShowDeclineMessage());
            
            Debug.Log("Quest declined");
        }
    }

    IEnumerator ShowDeclineMessage()
    {
        string[] declineLines = new string[]
        {
            "Perhaps you need more time to consider...",
            "",
            ""
        };
        
        yield return StartCoroutine(ShowNarrationParagraph(declineLines));
        
        // Trigger event
        OnQuestDeclined?.Invoke();
        
        // Start re-trigger delay
        if (reTriggerCoroutine != null)
            StopCoroutine(reTriggerCoroutine);
        reTriggerCoroutine = StartCoroutine(ReTriggerDelayCoroutine());
        
        // Reset for re-trigger
        hasMadeDecision = false;
        
        // Complete after short delay
        StartCoroutine(CompleteCutsceneAfterDelay(1f, false));
    }

    IEnumerator ReTriggerDelayCoroutine()
    {
        isReTriggerDelayed = true;
        reTriggerTimer = 0f;
        
        // Show countdown on arrow indicator
        if (!autoTrigger && npcArrowIndicator != null && arrowIndicatorText != null && showReTriggerCountdown)
        {
            npcArrowIndicator.SetActive(true);
            arrowIndicatorText.gameObject.SetActive(true);
        }
        
        while (reTriggerTimer < reTriggerDelay)
        {
            reTriggerTimer += Time.deltaTime;
            
            if (arrowIndicatorText != null && arrowIndicatorText.gameObject.activeSelf)
            {
                float timeLeft = reTriggerDelay - reTriggerTimer;
                arrowIndicatorText.text = Mathf.CeilToInt(timeLeft).ToString();
            }
            
            yield return null;
        }
        
        // Delay complete
        isReTriggerDelayed = false;
        
        if (!autoTrigger && npcArrowIndicator != null)
        {
            if (arrowIndicatorText != null)
                arrowIndicatorText.gameObject.SetActive(false);
            
            if (playerTransform != null && 
                Vector3.Distance(transform.position, playerTransform.position) <= interactionRange)
            {
                npcArrowIndicator.SetActive(true);
            }
        }
        
        Debug.Log("Re-trigger delay complete");
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
        // If timeline finishes naturally (not paused for decision), complete cutscene
        if (!isQuestDecisionActive)
        {
            isTimelinePlaying = false;
            // Show decision after timeline finishes
            StartCoroutine(ShowDecisionAfterTimeline());
        }
    }

    IEnumerator ShowDecisionAfterTimeline()
    {
        // Small delay before showing decision
        yield return new WaitForSeconds(1f);
        
        if (isCutsceneActive && !isQuestDecisionActive && !hasMadeDecision)
        {
            yield return StartCoroutine(ShowNarrationParagraph(acceptQuestLines));
            ShowQuestDecision();
        }
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
        if (isCutsceneActive)
        {
            if (timelineDirector != null)
                timelineDirector.Stop();
            
            if (isQuestDecisionActive)
                HideQuestDecision();
            
            // Stop any ongoing narration
            if (currentNarrationCoroutine != null)
            {
                StopCoroutine(currentNarrationCoroutine);
            }
            
            // Stop narration audio
            if (narrationAudioSource != null && narrationAudioSource.isPlaying)
            {
                narrationAudioSource.Stop();
            }
            
            // Clear subtitle
            if (subtitleController != null)
            {
                subtitleController.ClearSubtitle();
            }
                
            CompleteCutscene(true);
            OnCutsceneSkipped?.Invoke();
            Debug.Log("Cutscene skipped");
        }
    }

    void CompleteCutscene(bool wasSkipped)
    {
        isCutsceneActive = false;
        skipAvailable = false;
        skipTimer = 0f;
        isTimelinePlaying = false;
        
        // Hide UI elements
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);
        
        // Hide dialogue canvas when cutscene is done
        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);
        
        // Clear subtitle
        if (subtitleController != null)
        {
            subtitleController.ClearSubtitle();
        }
        
        // Deactivate cutscene objects
        if (cutsceneObjectParent != null)
            cutsceneObjectParent.SetActive(false);
        
        // Restore player camera
        RestorePlayerCamera();
        
        // Restore game UI
        if (gameUICanvas != null)
            gameUICanvas.SetActive(true);
        
        // Restore audio manager
        if (audioManagerObject != null)
            audioManagerObject.SetActive(true);
        
        // Unfreeze player
        RestorePlayer();
        
        // Handle trigger reset
        if (!hasMadeDecision || !oneTimeTrigger)
        {
            hasTriggered = false;
            
            if (!autoTrigger && npcArrowIndicator != null && !isReTriggerDelayed)
                npcArrowIndicator.SetActive(true);
        }
        
        // If quest was accepted, keep indicator off
        if (hasMadeDecision && hideNpcAfterAccept && npcArrowIndicator != null)
        {
            npcArrowIndicator.SetActive(false);
            if (arrowIndicatorText != null)
                arrowIndicatorText.gameObject.SetActive(false);
        }
        
        // Stop any narration audio
        if (narrationAudioSource != null && narrationAudioSource.isPlaying)
        {
            narrationAudioSource.Stop();
        }
        
        // Invoke events
        if (wasSkipped)
            OnCutsceneSkipped?.Invoke();
        else
            OnCutsceneComplete?.Invoke();
        
        Debug.Log($"Cutscene {(wasSkipped ? "skipped" : "completed")}");
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
        isTimelinePlaying = false;
        
        if (reTriggerCoroutine != null)
        {
            StopCoroutine(reTriggerCoroutine);
            reTriggerCoroutine = null;
        }
        
        if (currentNarrationCoroutine != null)
        {
            StopCoroutine(currentNarrationCoroutine);
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
        
        if (subtitleController != null)
            subtitleController.ClearSubtitle();
        
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);
        
        // Restore NPC and gate
        if (npcModel != null)
            npcModel.SetActive(true);
            
        if (kingdomGate != null)
            kingdomGate.SetActive(true);
        
        RestorePlayerCamera();
        RestorePlayer();
        
        Debug.Log("Cutscene trigger reset");
    }

    // Utility methods
    public bool IsCutscenePlaying() => isCutsceneActive;
    public bool HasAcceptedQuest() => hasMadeDecision;
    public float GetSkipButtonTimeRemaining() => Mathf.Max(0, skipButtonDelay - skipTimer);
    public void SetSkipEnabled(bool enabled) => enableSkip = enabled;
    public void SetSkipDelay(float delay) => skipButtonDelay = Mathf.Max(0, delay);
    public void SetReTriggerDelay(float delay) => reTriggerDelay = Mathf.Max(0, delay);
    public float GetReTriggerTimeRemaining() => !isReTriggerDelayed ? 0f : Mathf.Max(0, reTriggerDelay - reTriggerTimer);
    public bool IsReTriggerDelayed() => isReTriggerDelayed;

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        if (requireFacing)
        {
            Gizmos.color = Color.cyan;
            Vector3 direction = transform.forward * interactionRange;
            Gizmos.DrawRay(transform.position, direction);
            
            UnityEditor.Handles.color = new Color(0, 1, 1, 0.1f);
            UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, 
                Quaternion.Euler(0, -facingThreshold, 0) * transform.forward, 
                facingThreshold * 2, interactionRange);
        }
    }
    #endif
}