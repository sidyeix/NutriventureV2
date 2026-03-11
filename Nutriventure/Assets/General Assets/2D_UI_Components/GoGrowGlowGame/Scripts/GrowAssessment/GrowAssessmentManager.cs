using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrowAssessmentManager : MonoBehaviour
{
    public static GrowAssessmentManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject growAssessCanvas;
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private TMP_Text trackerText;
    [SerializeField] private Transform plusOneSpawnPoint;
    [SerializeField] private GameObject plusOnePrefab;

    [Header("Animation Settings")]
    [SerializeField] private float panelSlideDuration = 0.8f;
    [SerializeField] private float panelSlideDistance = 300f;
    [SerializeField] private float panelShowDelay = 0.2f;
    [SerializeField] private float plusOneDuration = 1.5f;
    [SerializeField] private float plusOneFadeDuration = 0.5f;
    [SerializeField] private float plusOneFloatHeight = 50f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip panelSlideInSound;
    [SerializeField] private AudioClip panelSlideOutSound;
    [SerializeField] private AudioClip plusOneSound;
    [SerializeField] private AudioClip completeSound;
    [SerializeField] private float panelSlideSoundDelay = 0.1f;

    [Header("Energy Settings")]
    [SerializeField] private float correctAnswerEnergyGain = 20f;
    [SerializeField] private float wrongAnswerEnergyDeduction = 25f;

    [Header("Point System")]
    [SerializeField] private int correctAnswerPoints = 1000;
    [SerializeField] private int wrongAnswerPoints = 500;

    [Header("Tracking Settings")]
    [SerializeField] private int totalQuestions = 8;
    [SerializeField] private string trackerFormat = "{0}/{1} Assessments";
    [SerializeField] private string completeText = "COMPLETE!";

    [Header("References")]
    [SerializeField] private AssessmentTrigger assessmentTrigger;
    [SerializeField] private List<ObjectGroupManager> groupManagers = new List<ObjectGroupManager>();

    // State
    private int correctAnswersCount = 0;
    private bool isAssessmentActive = false;
    private bool isTrackerVisible = false;
    private Vector3 trackerPanelHiddenPosition;
    private Vector3 trackerPanelVisiblePosition;
    private Coroutine panelSlideCoroutine;
    private AudioSource audioSource;
    private Coroutine checkEnergyCoroutine;
    private bool shouldRespawnAtLatestPoint = false;
    private Vector3 latestRespawnPoint;
    private ThirdPersonController playerController;
    private List<InteractiveObject> allInteractiveObjects = new List<InteractiveObject>();

    // NEW: Track completion state
    private bool hasCompletedAllQuestions = false;
    private bool isWaitingForEndTrigger = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        playerController = FindObjectOfType<ThirdPersonController>();
    }

    private void Start()
    {
        // Initialize tracker first (EXACTLY like GlowPartManager)
        InitializeTracker();

        // Then disable assessment UI
        DisableAssessment();

        // Auto-find group managers if not assigned
        if (groupManagers.Count == 0)
        {
            FindAllGroupManagers();
        }

        // Find all interactive objects from group managers
        CollectAllInteractiveObjects();

        // Debug initial setup
#if UNITY_EDITOR
        Debug.Log("=== GROW ASSESSMENT MANAGER STARTED ===");
#endif
        if (trackerPanel != null)
        {
#if UNITY_EDITOR
            Debug.Log($"Panel Position After Start: {trackerPanel.transform.localPosition}");
            Debug.Log($"Target Visible Position: {trackerPanelVisiblePosition}");
            Debug.Log($"Target Hidden Position: {trackerPanelHiddenPosition}");
#endif
        }
    }

    private void FindAllGroupManagers()
    {
        groupManagers.Clear();
        ObjectGroupManager[] foundManagers = FindObjectsOfType<ObjectGroupManager>();
        foreach (ObjectGroupManager manager in foundManagers)
        {
            if (manager != null)
            {
                groupManagers.Add(manager);
#if UNITY_EDITOR
                Debug.Log($"Found Group Manager: {manager.gameObject.name}");
#endif
            }
        }

#if UNITY_EDITOR
        Debug.Log($"Found {groupManagers.Count} group managers in scene");
#endif
    }

    private void CollectAllInteractiveObjects()
    {
        allInteractiveObjects.Clear();

        foreach (ObjectGroupManager groupManager in groupManagers)
        {
            if (groupManager != null)
            {
                // Get all interactive objects from this group
                InteractiveObject[] objectsInGroup = groupManager.GetComponentsInChildren<InteractiveObject>(true);
                foreach (InteractiveObject obj in objectsInGroup)
                {
                    if (obj != null && !allInteractiveObjects.Contains(obj))
                    {
                        allInteractiveObjects.Add(obj);
                        // Register with this manager
                        obj.SetAssessmentManager(this);

                        // If it's a correct answer, register it
                        if (obj.IsGrowFood())
                        {
                            RegisterAssessmentObject(obj);
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        Debug.Log($"Collected {allInteractiveObjects.Count} interactive objects from all groups");
#endif
    }

    private void InitializeTracker()
    {
        if (trackerPanel != null)
        {
            // EXACTLY like GlowPartManager: Use current position as visible position
            trackerPanelVisiblePosition = trackerPanel.transform.localPosition;
            trackerPanelHiddenPosition = trackerPanelVisiblePosition - new Vector3(panelSlideDistance, 0, 0);

            // Force panel to hidden position at start
            trackerPanel.transform.localPosition = trackerPanelHiddenPosition;
            trackerPanel.SetActive(false);

#if UNITY_EDITOR
            Debug.Log($"=== TRACKER SETUP ===");
            Debug.Log($"Visible Position (END): {trackerPanelVisiblePosition}");
            Debug.Log($"Hidden Position (START): {trackerPanelHiddenPosition}");
            Debug.Log($"Slide Distance: {panelSlideDistance}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("Tracker Panel is null in InitializeTracker!");
#endif
        }

        UpdateTrackerText();
    }

    public void StartGrowAssessment()
    {
        if (isAssessmentActive)
        {
#if UNITY_EDITOR
            Debug.Log("Assessment already active!");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log("=== STARTING GROW ASSESSMENT ===");
#endif

        // COMPLETE RESET FOR NEW GAME
        ResetForNewAssessment();

        // Store latest position for respawn
        if (playerController != null)
        {
            latestRespawnPoint = playerController.transform.position;
            shouldRespawnAtLatestPoint = true;
#if UNITY_EDITOR
            Debug.Log($"Stored latest respawn point: {latestRespawnPoint}");
#endif
        }

        // Enable canvas
        if (growAssessCanvas != null)
        {
            growAssessCanvas.SetActive(true);
        }

        // Show tracker panel with animation
        ShowTrackerPanel();

        // Activate all group managers (they will activate their objects)
        foreach (ObjectGroupManager groupManager in groupManagers)
        {
            if (groupManager != null)
            {
                groupManager.ActivateGroup();
#if UNITY_EDITOR
                Debug.Log($"Activated group: {groupManager.gameObject.name}");
#endif
            }
        }

        isAssessmentActive = true;
        isWaitingForEndTrigger = false;

        // Start energy checking
        StartEnergyCheck();

        // Start One Life check
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.StartOneLifeCheck();
        }

#if UNITY_EDITOR
        Debug.Log("Grow Assessment started with all groups activated");
#endif
    }

    // NEW: Complete reset method for new assessment
    private void ResetForNewAssessment()
    {
#if UNITY_EDITOR
        Debug.Log("=== RESETTING FOR NEW ASSESSMENT ===");
#endif

        // Reset completion flags
        hasCompletedAllQuestions = false;
        isWaitingForEndTrigger = false;

        // Reset correct answers count
        correctAnswersCount = 0;

        // Reset tracker text to default format
        UpdateTrackerText();

        // Reset all groups and objects
        ResetAllGroupsAndObjects();

        // CRITICAL: Reset tracker panel to hidden position (SAME as GlowPartManager)
        if (trackerPanel != null)
        {
            // Force panel to hidden position
            trackerPanel.transform.localPosition = trackerPanelHiddenPosition;
            trackerPanel.SetActive(false);
            isTrackerVisible = false;

#if UNITY_EDITOR
            Debug.Log($"Tracker reset to hidden position: {trackerPanelHiddenPosition}");
#endif
        }

        // Stop any ongoing animations
        if (panelSlideCoroutine != null)
        {
            StopCoroutine(panelSlideCoroutine);
            panelSlideCoroutine = null;
        }

#if UNITY_EDITOR
        Debug.Log("Assessment completely reset for new game");
#endif
    }

    private void ResetAllGroupsAndObjects()
    {
#if UNITY_EDITOR
        Debug.Log("Resetting all groups and objects...");
#endif

        // Deactivate all groups
        foreach (ObjectGroupManager groupManager in groupManagers)
        {
            if (groupManager != null)
            {
                groupManager.DeactivateGroup();
                groupManager.SetGroupEntryAnimation(false);
#if UNITY_EDITOR
                Debug.Log($"Deactivated and reset group: {groupManager.gameObject.name}");
#endif
            }
        }

        // Reset all interactive objects
        foreach (InteractiveObject obj in allInteractiveObjects)
        {
            if (obj != null)
            {
                obj.ResetObject();
            }
        }

#if UNITY_EDITOR
        Debug.Log("All groups and objects reset");
#endif
    }

    public void EndGrowAssessment()
    {
        if (!isAssessmentActive && !isWaitingForEndTrigger) return;

#if UNITY_EDITOR
        Debug.Log("=== ENDING GROW ASSESSMENT ===");
#endif

        // Hide tracker panel
        HideTrackerPanel();

        // Disable canvas after delay
        StartCoroutine(DisableCanvasAfterDelay());

        // Deactivate all group managers
        foreach (ObjectGroupManager groupManager in groupManagers)
        {
            if (groupManager != null)
            {
                groupManager.DeactivateGroup();
                groupManager.SetGroupEntryAnimation(false);
            }
        }

        // Reset flags
        isAssessmentActive = false;
        isWaitingForEndTrigger = false;
        shouldRespawnAtLatestPoint = false;

        // Stop energy checking
        StopEnergyCheck();

        // Stop One Life check
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.StopOneLifeCheck();
        }

        // Reset trigger for next playthrough
        if (assessmentTrigger != null)
        {
            assessmentTrigger.OnAssessmentComplete();
        }

#if UNITY_EDITOR
        Debug.Log("Grow Assessment ended - Ready for next playthrough");
#endif
    }

    // Complete reset for new game (called from GameEndManager)
    public void CompleteResetForNewGame()
    {
#if UNITY_EDITOR
        Debug.Log("=== COMPLETE RESET FOR NEW GAME ===");
#endif

        // End current assessment if active
        if (isAssessmentActive || isWaitingForEndTrigger)
        {
            EndGrowAssessment();
        }

        // Reset everything
        ResetForNewAssessment();

        // Reset trigger
        if (assessmentTrigger != null)
        {
            assessmentTrigger.ResetTrigger();
        }

#if UNITY_EDITOR
        Debug.Log("Grow Assessment completely reset for new game");
#endif
    }

    private IEnumerator DisableCanvasAfterDelay()
    {
        yield return CoroutineYieldCache.WaitForSeconds(panelSlideDuration + 0.3f);

        if (growAssessCanvas != null)
        {
            growAssessCanvas.SetActive(false);
        }
    }

    private void DisableAssessment()
    {
        if (growAssessCanvas != null)
        {
            growAssessCanvas.SetActive(false);
        }

        if (trackerPanel != null)
        {
            trackerPanel.SetActive(false);
            isTrackerVisible = false;
        }

        isAssessmentActive = false;
        isWaitingForEndTrigger = false;
        shouldRespawnAtLatestPoint = false;
    }

    public void OnCorrectAnswerSelected()
    {
        if (!isAssessmentActive) return;

        correctAnswersCount++;
#if UNITY_EDITOR
        Debug.Log($"Correct answer! Total: {correctAnswersCount}/{totalQuestions}");
#endif

        // Add points and energy
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.AddPoints(correctAnswerPoints);
            GoGrowGlowGameManager.Instance.AddEnergy(correctAnswerEnergyGain);
        }

        // Update UI
        UpdateTrackerText();

        // Show +1 effect
        ShowPlusOneEffect();

        // Play sound
        PlaySound(plusOneSound);

        // Check if assessment is complete
        if (correctAnswersCount >= totalQuestions)
        {
            AssessmentComplete();
        }
    }

    public void OnWrongAnswerSelected()
    {
        if (!isAssessmentActive) return;

#if UNITY_EDITOR
        Debug.Log("Wrong answer selected!");
#endif

        // Deduct points and energy
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.AddPoints(-wrongAnswerPoints);
            GoGrowGlowGameManager.Instance.RemoveEnergy(wrongAnswerEnergyDeduction);

            // Check if energy reached zero
            if (GoGrowGlowGameManager.Instance.GetCurrentEnergy() <= 0f)
            {
                HandleEnergyZero();
            }
        }
    }

    private void HandleEnergyZero()
    {
#if UNITY_EDITOR
        Debug.Log("Energy reached zero! Respawning at latest point...");
#endif
        RespawnAtLatestPoint();
    }

    private void RespawnAtLatestPoint()
    {
        if (playerController != null && shouldRespawnAtLatestPoint)
        {
            playerController.transform.position = latestRespawnPoint;

            if (GoGrowGlowGameManager.Instance != null)
            {
                GoGrowGlowGameManager.Instance.SetEnergy(50f);
            }

#if UNITY_EDITOR
            Debug.Log($"Respawned at latest point: {latestRespawnPoint}");
#endif
            ShowRespawnEffect();
        }
    }

    private void ShowRespawnEffect()
    {
#if UNITY_EDITOR
        Debug.Log("Respawn effect triggered");
#endif
        PlaySound(completeSound);
    }

    private void AssessmentComplete()
    {
#if UNITY_EDITOR
        Debug.Log("=== ASSESSMENT COMPLETE! ===");
#endif

        // Play complete sound
        PlaySound(completeSound);

        // Update tracker text
        if (trackerText != null)
        {
            trackerText.text = completeText;
            StartCoroutine(FlashCompleteText());
        }

        // Set completion flags
        hasCompletedAllQuestions = true;
        isWaitingForEndTrigger = true;

        // Keep assessment active but mark as waiting for end trigger
#if UNITY_EDITOR
        Debug.Log("Assessment completed! Waiting for EndGameTrigger...");
#endif
    }

    private IEnumerator FlashCompleteText()
    {
        if (trackerText == null) yield break;

        Color originalColor = trackerText.color;
        float flashDuration = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.PingPong(elapsedTime * 3f, 1f);
            trackerText.color = Color.Lerp(originalColor, Color.yellow, t);

            float scale = 1 + Mathf.Sin(elapsedTime * 5f) * 0.05f;
            trackerText.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        trackerText.color = originalColor;
        trackerText.transform.localScale = Vector3.one;
    }

    private void StartEnergyCheck()
    {
        if (checkEnergyCoroutine != null)
            StopCoroutine(checkEnergyCoroutine);

        checkEnergyCoroutine = StartCoroutine(CheckEnergyRoutine());
    }

    private void StopEnergyCheck()
    {
        if (checkEnergyCoroutine != null)
        {
            StopCoroutine(checkEnergyCoroutine);
            checkEnergyCoroutine = null;
        }
    }

    private IEnumerator CheckEnergyRoutine()
    {
        while (isAssessmentActive)
        {
            yield return CoroutineYieldCache.WaitForSeconds(0.5f);

            if (GoGrowGlowGameManager.Instance != null &&
                GoGrowGlowGameManager.Instance.GetCurrentEnergy() <= 0f)
            {
                HandleEnergyZero();
                yield break;
            }
        }
    }

    private void ShowPlusOneEffect()
    {
        if (plusOnePrefab == null || plusOneSpawnPoint == null) return;

        GameObject plusOneObj = Instantiate(plusOnePrefab, plusOneSpawnPoint.position, Quaternion.identity, plusOneSpawnPoint);
        StartCoroutine(AnimatePlusOne(plusOneObj));
    }

    private IEnumerator AnimatePlusOne(GameObject plusOneObj)
    {
        TMP_Text textComponent = plusOneObj.GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            Color originalColor = textComponent.color;
            Vector3 originalPosition = plusOneObj.transform.localPosition;
            float elapsedTime = 0f;

            while (elapsedTime < plusOneDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / plusOneDuration;

                float yOffset = Mathf.Lerp(0, plusOneFloatHeight, Mathf.Sin(progress * Mathf.PI * 0.5f));
                plusOneObj.transform.localPosition = originalPosition + new Vector3(0, yOffset, 0);

                if (progress > (1 - (plusOneFadeDuration / plusOneDuration)))
                {
                    float fadeProgress = (progress - (1 - (plusOneFadeDuration / plusOneDuration))) / (plusOneFadeDuration / plusOneDuration);
                    textComponent.color = Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0), fadeProgress);
                }

                float scale = 1 + Mathf.Sin(progress * Mathf.PI) * 0.1f;
                plusOneObj.transform.localScale = Vector3.one * scale;

                yield return null;
            }
        }

        Destroy(plusOneObj);
    }

    private void UpdateTrackerText()
    {
        if (trackerText != null)
        {
            // Only show counter if not complete
            if (!hasCompletedAllQuestions)
            {
                trackerText.text = string.Format(trackerFormat, correctAnswersCount, totalQuestions);
            }
        }
    }

    // IDENTICAL TO GLOWPARTMANAGER'S ShowTrackerPanel
    public void ShowTrackerPanel()
    {
        if (isTrackerVisible || trackerPanel == null) return;

#if UNITY_EDITOR
        Debug.Log("=== SHOWING TRACKER PANEL ===");
        Debug.Log($"Starting from: {trackerPanelHiddenPosition}");
        Debug.Log($"Sliding to: {trackerPanelVisiblePosition}");
#endif

        // Force panel to hidden position before animating
        trackerPanel.transform.localPosition = trackerPanelHiddenPosition;
        trackerPanel.SetActive(true);

        isTrackerVisible = true;

        if (panelSlideCoroutine != null)
            StopCoroutine(panelSlideCoroutine);

        panelSlideCoroutine = StartCoroutine(SlidePanel(true));
    }

    // IDENTICAL TO GLOWPARTMANAGER'S HideTrackerPanel
    public void HideTrackerPanel()
    {
        if (!isTrackerVisible || trackerPanel == null) return;

#if UNITY_EDITOR
        Debug.Log("Hiding tracker panel...");
#endif

        if (panelSlideCoroutine != null)
            StopCoroutine(panelSlideCoroutine);

        panelSlideCoroutine = StartCoroutine(SlidePanel(false));
        StartCoroutine(DisablePanelAfterSlide());
    }

    // IDENTICAL TO GLOWPARTMANAGER'S SlidePanel
    private IEnumerator SlidePanel(bool slideIn)
    {
        if (trackerPanel == null) yield break;

        Vector3 startPos = trackerPanel.transform.localPosition;
        Vector3 targetPos = slideIn ? trackerPanelVisiblePosition : trackerPanelHiddenPosition;

        // VERIFY POSITIONS
#if UNITY_EDITOR
        Debug.Log($"=== SLIDE PANEL ===");
        Debug.Log($"Slide Direction: {(slideIn ? "IN" : "OUT")}");
        Debug.Log($"Start: X={startPos.x:F0}");
        Debug.Log($"Target: X={targetPos.x:F0}");
#endif

        float elapsedTime = 0f;

        // IDENTICAL AUDIO LOGIC
        if (slideIn && panelSlideInSound != null)
        {
            StartCoroutine(PlaySoundDelayed(panelSlideInSound, panelSlideSoundDelay));
        }
        else if (!slideIn && panelSlideOutSound != null)
        {
            StartCoroutine(PlaySoundDelayed(panelSlideOutSound, panelSlideSoundDelay));
        }

        // IDENTICAL DELAY LOGIC
        if (slideIn)
        {
            yield return CoroutineYieldCache.WaitForSeconds(panelShowDelay);
        }

        // IDENTICAL ANIMATION LOGIC
        while (elapsedTime < panelSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / panelSlideDuration;

            if (slideIn)
            {
                t = 1 - Mathf.Pow(1 - t, 3); // Ease out (same as GlowPartManager)
            }
            else
            {
                t = Mathf.Pow(t, 3); // Ease in (same as GlowPartManager)
            }

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
            trackerPanel.transform.localPosition = newPos;

            yield return null;
        }

        // FORCE EXACT POSITION
        trackerPanel.transform.localPosition = targetPos;

        // VERIFY FINAL POSITION
#if UNITY_EDITOR
        Debug.Log($"Slide complete. Final X={trackerPanel.transform.localPosition.x:F0}");
#endif

        panelSlideCoroutine = null;
    }

    // IDENTICAL TO GLOWPARTMANAGER'S DisablePanelAfterSlide
    private IEnumerator DisablePanelAfterSlide()
    {
        yield return CoroutineYieldCache.WaitForSeconds(panelSlideDuration + 0.1f);
        trackerPanel.SetActive(false);
        isTrackerVisible = false;
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
    {
        yield return CoroutineYieldCache.WaitForSeconds(delay);
        PlaySound(clip);
    }

    public void RegisterAssessmentObject(InteractiveObject obj)
    {
        if (!allInteractiveObjects.Contains(obj))
        {
            allInteractiveObjects.Add(obj);
#if UNITY_EDITOR
            Debug.Log($"Registered assessment object: {obj.gameObject.name}");
#endif
        }
    }

    // Add group manager to list
    public void RegisterGroupManager(ObjectGroupManager manager)
    {
        if (!groupManagers.Contains(manager))
        {
            groupManagers.Add(manager);
#if UNITY_EDITOR
            Debug.Log($"Registered group manager: {manager.gameObject.name}");
#endif
        }
    }

    public bool IsAssessmentActive() => isAssessmentActive;
    public int GetCorrectAnswersCount() => correctAnswersCount;
    public int GetTotalQuestions() => totalQuestions;
    public float GetCorrectEnergyGain() => correctAnswerEnergyGain;
    public float GetWrongEnergyDeduction() => wrongAnswerEnergyDeduction;

    public void UpdateRespawnPoint(Vector3 newPoint)
    {
        latestRespawnPoint = newPoint;
        shouldRespawnAtLatestPoint = true;
#if UNITY_EDITOR
        Debug.Log($"Updated respawn point to: {newPoint}");
#endif
    }

    // NEW: Getter for completion status (used by EndGameTrigger)
    public bool HasCompletedAllQuestions() => hasCompletedAllQuestions;

    // NEW: Check if waiting for end trigger
    public bool IsWaitingForEndTrigger() => isWaitingForEndTrigger;

    // DEBUG METHODS
    [ContextMenu("Debug Tracker Positions")]
    public void DebugTrackerPositions()
    {
        if (trackerPanel == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Tracker Panel is null!");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log("=== TRACKER POSITION DEBUG ===");
        Debug.Log($"Current Panel Position: {trackerPanel.transform.localPosition}");
        Debug.Log($"Stored Visible Position: {trackerPanelVisiblePosition}");
        Debug.Log($"Stored Hidden Position: {trackerPanelHiddenPosition}");
        Debug.Log($"Panel Slide Distance: {panelSlideDistance}");
        Debug.Log($"Is Tracker Visible: {isTrackerVisible}");
#endif
    }

    [ContextMenu("Test Tracker Animation")]
    public void TestTrackerAnimation()
    {
        if (trackerPanel == null)
        {
#if UNITY_EDITOR
            Debug.LogError("No tracker panel assigned!");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log("Testing tracker animation...");
#endif

        // Reset to hidden
        trackerPanel.transform.localPosition = trackerPanelHiddenPosition;
        trackerPanel.SetActive(true);
        isTrackerVisible = false;

        // Show it
        ShowTrackerPanel();

        // Wait and hide
        StartCoroutine(TestAnimationSequence());
    }

    private IEnumerator TestAnimationSequence()
    {
        yield return CoroutineYieldCache.WaitForSeconds(2f);
        HideTrackerPanel();
        yield return CoroutineYieldCache.WaitForSeconds(2f);
        ShowTrackerPanel();
    }

    // NEW: Reset without moving panel (for GameManager)
    public void ResetForNewAssessmentWithoutMovingPanel()
    {
#if UNITY_EDITOR
        Debug.Log("=== RESETTING WITHOUT MOVING PANEL ===");
#endif

        // Reset completion flags
        hasCompletedAllQuestions = false;
        isWaitingForEndTrigger = false;

        // Reset correct answers count
        correctAnswersCount = 0;

        // Reset tracker text to default format
        UpdateTrackerText();

        // Reset all groups and objects
        ResetAllGroupsAndObjects();

        // CRITICAL: DON'T move the panel, just update state
        if (trackerPanel != null)
        {
            // Keep panel at its current position
            // Just update the internal state
            isTrackerVisible = false;

#if UNITY_EDITOR
            Debug.Log($"Panel position preserved at: {trackerPanel.transform.localPosition}");
#endif
        }

        // Stop any ongoing animations
        if (panelSlideCoroutine != null)
        {
            StopCoroutine(panelSlideCoroutine);
            panelSlideCoroutine = null;
        }

#if UNITY_EDITOR
        Debug.Log("Assessment state reset (panel position preserved)");
#endif
    }

    // ====== SAVE / RESTORE HELPERS ======

    /// <summary>
    /// Restores the assessment progress from a saved game state.
    /// Called by GameStateManager when the player chooses to resume.
    /// </summary>
    public void RestoreProgress(int correctAnswers, bool assessmentCompleted, bool waitingForEndTrigger)
    {
        correctAnswersCount = Mathf.Clamp(correctAnswers, 0, totalQuestions);
        hasCompletedAllQuestions = assessmentCompleted;
        isWaitingForEndTrigger = waitingForEndTrigger;

        UpdateTrackerText();

#if UNITY_EDITOR
        Debug.Log($"GrowAssessmentManager: Restored progress – {correctAnswersCount}/{totalQuestions} correct, " +
                  $"Completed: {hasCompletedAllQuestions}, WaitingForEnd: {isWaitingForEndTrigger}");
#endif
    }
}
