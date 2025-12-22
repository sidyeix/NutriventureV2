using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using Cinemachine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class NPCQuestInteraction : MonoBehaviour
{
    [Header("Database Reference")]
    [SerializeField] private QuestDatabase questDatabase;

    [Header("NPC Configuration")]
    [SerializeField] private string kingdomID;

    [Tooltip("Quest categories this NPC can give")]
    [SerializeField]
    private List<QuestCategory> allowedCategories = new List<QuestCategory>
    {
        QuestCategory.Tutorial,
        QuestCategory.MainStory
    };

    [SerializeField] private GameObject questButton;
    [SerializeField] private GameObject missionCanvas;
    [SerializeField] private GameObject uiControllerCanvas;

    [Header("Cinemachine Virtual Camera")]
    [SerializeField] private CinemachineVirtualCamera npcVirtualCamera;
    private int originalCameraPriority = 10;

    [Header("Quest Display References")]
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private TMP_Text questStatusText;
    [SerializeField] private Transform tasksContainer;
    [SerializeField] private Transform rewardsContainer;

    [Header("UI Buttons")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button okayButton; // NEW: For InProgress quests
    [SerializeField] private Button replayButton;
    [SerializeField] private Button backButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject taskItemPrefab;
    [SerializeField] private GameObject rewardItemPrefab;

    [Header("Director")]
    [SerializeField] private PlayableDirector playableDirector;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDelay = 1f;
    [SerializeField] private CanvasGroup missionCanvasGroup;

    private Quest currentQuest;
    private bool playerInRange = false;
    private bool isPlayingTimeline = false;
    private bool isExiting = false;
    private bool hasPlayedTimelineForCurrentQuest = false;

    private void Start()
    {
        // Ensure UI elements are properly initialized
        if (questButton != null)
            questButton.SetActive(false);

        if (missionCanvas != null)
            missionCanvas.SetActive(false);

        // Store original camera priority
        if (npcVirtualCamera != null)
            originalCameraPriority = npcVirtualCamera.Priority;

        // Initialize database if needed
        if (questDatabase != null)
        {
            questDatabase.InitializeDatabase();
            Debug.Log($"QuestDatabase initialized with {questDatabase.kingdoms.Count} kingdoms");
        }
        else
        {
            Debug.LogError("QuestDatabase is not assigned in the inspector!");
        }

        // Setup button listeners
        SetupButtonListeners();

        // Initialize canvas group if exists
        if (missionCanvasGroup == null && missionCanvas != null)
        {
            missionCanvasGroup = missionCanvas.GetComponent<CanvasGroup>();
        }
    }

    private void SetupButtonListeners()
    {
        // Accept Button - for NotStarted quests
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(OnAcceptButtonClicked);
            acceptButton.gameObject.SetActive(false); // Hide initially
        }

        // Okay Button - for InProgress quests
        if (okayButton != null)
        {
            okayButton.onClick.RemoveAllListeners();
            okayButton.onClick.AddListener(OnOkayButtonClicked);
            okayButton.gameObject.SetActive(false); // Hide initially
        }

        // Replay Button
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(OnReplayButtonClicked);
            replayButton.gameObject.SetActive(false); // Hide initially
        }

        // Back Button
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            CheckForAvailableQuest();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            HideQuestButton();
        }
    }

    private void CheckForAvailableQuest()
    {
        if (questDatabase == null)
        {
            Debug.LogError("QuestDatabase is null!");
            HideQuestButton();
            return;
        }

        Debug.Log($"Checking for quests in kingdom: {kingdomID}");
        Debug.Log($"Allowed categories: {string.Join(", ", allowedCategories)}");

        // Get all quests in the specified kingdom
        var kingdomQuests = questDatabase.GetQuestsByKingdom(kingdomID);

        if (kingdomQuests == null || kingdomQuests.Count == 0)
        {
            Debug.Log($"No quests found in kingdom: {kingdomID}");
            HideQuestButton();
            return;
        }

        Debug.Log($"Found {kingdomQuests.Count} quests in kingdom");

        // Find the first quest that matches our criteria
        foreach (var quest in kingdomQuests)
        {
            Debug.Log($"Checking quest: {quest.questName}, Status: {quest.status}, Category: {quest.category}");

            // Check if quest status is either NotStarted or InProgress
            bool hasValidStatus = quest.status == QuestStatus.NotStarted || quest.status == QuestStatus.InProgress;

            // Check if quest category is allowed
            bool hasAllowedCategory = allowedCategories.Contains(quest.category);

            if (hasValidStatus && hasAllowedCategory)
            {
                currentQuest = quest;
                // Reset timeline flag when switching to a new quest
                if (hasPlayedTimelineForCurrentQuest && quest.questID != currentQuest?.questID)
                {
                    hasPlayedTimelineForCurrentQuest = false;
                }
                Debug.Log($"Found available quest: {quest.questName}, Status: {quest.status}, Category: {quest.category}");
                ShowQuestButton();
                return;
            }
        }

        // No quest found that matches criteria
        Debug.Log($"No available quests matching criteria in kingdom: {kingdomID}");
        HideQuestButton();
    }

    private void ShowQuestButton()
    {
        if (questButton != null)
        {
            questButton.SetActive(true);
            Debug.Log("Quest button shown");
        }
    }

    private void HideQuestButton()
    {
        if (questButton != null)
        {
            questButton.SetActive(false);
            Debug.Log("Quest button hidden");
        }
    }

    // Call this when the quest button is clicked
    public void OnQuestButtonClicked()
    {
        Debug.Log("===== QUEST BUTTON CLICKED =====");

        if (currentQuest == null)
        {
            Debug.LogError("Current quest is null!");
            return;
        }

        Debug.Log($"Processing quest: {currentQuest.questName}");
        Debug.Log($"Quest status: {currentQuest.status}");
        Debug.Log($"Has timeline asset: {currentQuest.timelineAsset != null}");

        // Hide quest button immediately
        HideQuestButton();

        // Set camera priority to 30 immediately
        if (npcVirtualCamera != null)
        {
            npcVirtualCamera.Priority = 30;
            Debug.Log($"Camera priority set to: {npcVirtualCamera.Priority}");
        }

        // Disable main UI controller canvas
        if (uiControllerCanvas != null)
        {
            uiControllerCanvas.SetActive(false);
            Debug.Log("UIControllerCanvas disabled");
        }

        // Determine if we should play timeline automatically
        bool shouldPlayTimelineAutomatically =
            currentQuest.timelineAsset != null &&
            playableDirector != null &&
            currentQuest.status == QuestStatus.NotStarted &&
            !hasPlayedTimelineForCurrentQuest;

        if (shouldPlayTimelineAutomatically)
        {
            Debug.Log("Playing timeline automatically for new quest...");
            PlayTimeline();
            hasPlayedTimelineForCurrentQuest = true;
        }
        else
        {
            Debug.Log("Showing mission canvas immediately...");
            ShowMissionCanvas();
        }
    }

    private void PlayTimeline()
    {
        if (playableDirector == null || currentQuest.timelineAsset == null)
        {
            Debug.LogError("Cannot play timeline - missing director or timeline asset");
            ShowMissionCanvas();
            return;
        }

        Debug.Log($"Setting timeline asset: {currentQuest.timelineAsset.name}");

        // Set the timeline asset
        playableDirector.playableAsset = currentQuest.timelineAsset;

        // Set up stopped event
        playableDirector.stopped += OnTimelineStopped;

        // Play the timeline
        playableDirector.Play();

        isPlayingTimeline = true;

        Debug.Log("Timeline started playing");
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        Debug.Log("Timeline stopped!");

        // Unsubscribe from the event
        director.stopped -= OnTimelineStopped;

        // Show mission canvas after timeline
        ShowMissionCanvas();

        isPlayingTimeline = false;
    }

    private void ShowMissionCanvas()
    {
        Debug.Log("Showing mission canvas...");

        // Update UI before showing
        UpdateQuestUI();

        // Update button visibility based on quest status
        UpdateButtonVisibility();

        // Start fade in sequence
        StartCoroutine(FadeInMissionCanvas());
    }

    private void UpdateButtonVisibility()
    {
        // Show/hide replay button based on timeline availability
        UpdateReplayButtonVisibility();

        // Show/hide accept/okay buttons based on quest status
        if (currentQuest != null)
        {
            if (currentQuest.status == QuestStatus.NotStarted)
            {
                // Show Accept button, hide Okay button
                if (acceptButton != null)
                {
                    acceptButton.gameObject.SetActive(true);
                    Debug.Log("Accept button shown (quest is NotStarted)");
                }
                if (okayButton != null)
                {
                    okayButton.gameObject.SetActive(false);
                }
            }
            else if (currentQuest.status == QuestStatus.InProgress)
            {
                // Show Okay button, hide Accept button
                if (okayButton != null)
                {
                    okayButton.gameObject.SetActive(true);
                    Debug.Log("Okay button shown (quest is InProgress)");
                }
                if (acceptButton != null)
                {
                    acceptButton.gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateReplayButtonVisibility()
    {
        if (replayButton != null)
        {
            // Show replay button if quest has a timeline AND playable director is available
            bool hasTimelineAndDirector = currentQuest.timelineAsset != null && playableDirector != null;
            replayButton.gameObject.SetActive(hasTimelineAndDirector);

            if (hasTimelineAndDirector)
            {
                Debug.Log($"Replay button shown - Quest has timeline: {currentQuest.timelineAsset.name}");
            }
            else
            {
                Debug.Log("Replay button hidden - No timeline or director available");
            }
        }
    }

    private IEnumerator FadeInMissionCanvas()
    {
        // Set isExit to true
        isExiting = false; // Reset first
        isExiting = true;
        Debug.Log("isExit set to TRUE - Starting fade sequence");

        // Wait for 1 second
        yield return new WaitForSeconds(fadeDelay);

        // Set isExit back to false
        isExiting = false;
        Debug.Log("isExit set to FALSE - Showing canvas");

        // Show mission canvas
        if (missionCanvas != null)
        {
            missionCanvas.SetActive(true);

            // Optional fade in effect
            if (missionCanvasGroup != null)
            {
                missionCanvasGroup.alpha = 0f;
                float timer = 0f;
                float fadeDuration = 0.5f;

                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    missionCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                    yield return null;
                }
                missionCanvasGroup.alpha = 1f;
            }

            Debug.Log("Mission canvas is now active");
        }
        else
        {
            Debug.LogError("Mission canvas reference is null!");
        }
    }

    private void UpdateQuestUI()
    {
        if (currentQuest == null)
        {
            Debug.LogError("Cannot update quest UI - current quest is null!");
            return;
        }

        Debug.Log($"Updating UI for quest: {currentQuest.questName}");

        // Set basic quest info
        if (questNameText != null)
        {
            questNameText.text = currentQuest.questName;
            Debug.Log($"Set quest name to: {currentQuest.questName}");
        }

        if (questDescriptionText != null)
        {
            questDescriptionText.text = currentQuest.description;
            Debug.Log($"Set quest description");
        }

        if (questStatusText != null)
        {
            questStatusText.text = currentQuest.status.ToString();
            Debug.Log($"Set quest status to: {currentQuest.status}");
        }

        // Clear existing tasks and rewards
        ClearContainers();

        // Display tasks
        if (tasksContainer != null && taskItemPrefab != null)
        {
            Debug.Log($"Displaying {currentQuest.tasks.Count} tasks");
            foreach (var task in currentQuest.tasks)
            {
                GameObject taskObj = Instantiate(taskItemPrefab, tasksContainer);
                SetupTaskItem(taskObj, task);
            }
        }

        // Display rewards
        if (rewardsContainer != null && rewardItemPrefab != null)
        {
            Debug.Log($"Displaying {currentQuest.rewards.Count} rewards");
            foreach (var reward in currentQuest.rewards)
            {
                GameObject rewardObj = Instantiate(rewardItemPrefab, rewardsContainer);
                SetupRewardItem(rewardObj, reward);
            }
        }
    }

    private void SetupTaskItem(GameObject taskItem, QuestTask task)
    {
        // Based on your hierarchy: TaskItem -> TaskTextContainer -> TaskDescriptionText
        Transform taskTextContainer = taskItem.transform.Find("TaskTextContainer");
        if (taskTextContainer == null)
        {
            Debug.LogError("TaskTextContainer not found in TaskItem prefab!");
            return;
        }

        // Find TaskDescriptionText inside TaskTextContainer
        TMP_Text taskDescription = taskTextContainer.Find("TaskDescriptionText")?.GetComponent<TMP_Text>();
        TMP_Text progressText = taskItem.transform.Find("ProgressText")?.GetComponent<TMP_Text>();
        Toggle checkbox = taskItem.transform.Find("CheckBox")?.GetComponent<Toggle>();

        if (checkbox != null)
        {
            checkbox.isOn = task.isCompleted;
            checkbox.interactable = false;
            Debug.Log($"Task checkbox set to: {task.isCompleted}");
        }
        else
        {
            Debug.LogError("CheckBox not found in TaskItem prefab!");
        }

        if (taskDescription != null)
        {
            taskDescription.text = task.description;
            Debug.Log($"Task description set: {task.description}");
        }
        else
        {
            Debug.LogError("TaskDescriptionText not found in TaskItem prefab!");
        }

        if (progressText != null)
        {
            if (task.requiredAmount > 0)
            {
                progressText.gameObject.SetActive(true);
                progressText.text = $"({task.currentAmount} / {task.requiredAmount})";
                Debug.Log($"Progress text: {progressText.text}");
            }
            else
            {
                progressText.gameObject.SetActive(false);
                Debug.Log("Progress text hidden (required amount = 0)");
            }
        }
        else
        {
            Debug.LogWarning("ProgressText not found in TaskItem prefab (this might be intentional)");
        }
    }

    private void SetupRewardItem(GameObject rewardItem, QuestReward reward)
    {
        if (rewardItem == null)
        {
            Debug.LogError("rewardItem is null!");
            return;
        }

        // Get references to the reward item components
        Image rewardImage = rewardItem.transform.Find("RewardItemImage")?.GetComponent<Image>();
        TMP_Text amountText = rewardItem.transform.Find("Amount")?.GetComponent<TMP_Text>();
        TMP_Text nameText = rewardItem.transform.Find("RewardName")?.GetComponent<TMP_Text>();

        if (rewardImage != null)
        {
            if (reward.rewardIcon != null)
            {
                rewardImage.sprite = reward.rewardIcon;
                Debug.Log($"Set reward icon: {reward.rewardIcon.name}");
            }
            else
            {
                Debug.LogWarning("Reward icon is null");
                rewardImage.gameObject.SetActive(false); // Hide image if no icon
            }
        }

        if (amountText != null)
        {
            if (reward.amount > 0)
            {
                amountText.gameObject.SetActive(true);
                amountText.text = $"+{reward.amount}"; // Add + symbol before amount
                Debug.Log($"Reward amount: {amountText.text}");
            }
            else
            {
                amountText.gameObject.SetActive(false); // Deactivate if amount is 0
                Debug.Log("Amount text hidden (amount = 0)");
            }
        }

        if (nameText != null)
        {
            nameText.text = reward.rewardName;
            Debug.Log($"Reward name: {reward.rewardName}");
        }
    }

    private void ClearContainers()
    {
        // Clear tasks container
        if (tasksContainer != null)
        {
            int childCount = tasksContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Destroy(tasksContainer.GetChild(i).gameObject);
            }
            Debug.Log($"Cleared {childCount} tasks from container");
        }

        // Clear rewards container
        if (rewardsContainer != null)
        {
            int childCount = rewardsContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Destroy(rewardsContainer.GetChild(i).gameObject);
            }
            Debug.Log($"Cleared {childCount} rewards from container");
        }
    }

    // Replay button clicked
    private void OnReplayButtonClicked()
    {
        Debug.Log("Replay button clicked!");

        if (currentQuest != null && currentQuest.timelineAsset != null && playableDirector != null)
        {
            // Hide mission canvas with fade
            StartCoroutine(FadeOutAndReplay());
        }
        else
        {
            Debug.LogError("Cannot replay - missing timeline asset or playable director!");
        }
    }

    private IEnumerator FadeOutAndReplay()
    {
        // Fade out mission canvas
        if (missionCanvasGroup != null && missionCanvas != null && missionCanvas.activeSelf)
        {
            float timer = 0f;
            float fadeDuration = 0.3f;
            float startAlpha = missionCanvasGroup.alpha;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                missionCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
                yield return null;
            }
        }

        // Hide mission canvas
        if (missionCanvas != null)
            missionCanvas.SetActive(false);

        // Hide all buttons temporarily
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (okayButton != null) okayButton.gameObject.SetActive(false);

        // Play timeline again
        PlayTimeline();

        // Mark that timeline has been played for this quest
        hasPlayedTimelineForCurrentQuest = true;
    }

    // Accept button clicked - for NotStarted quests
    private void OnAcceptButtonClicked()
    {
        Debug.Log("Accept button clicked!");

        if (currentQuest != null && questDatabase != null)
        {
            // Only update status if quest is NotStarted
            if (currentQuest.status == QuestStatus.NotStarted)
            {
                currentQuest.StartQuest();

                // If using QuestManager, also update through it
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.StartQuest(currentQuest.questID);
                }

                Debug.Log($"Quest accepted and status updated to: {currentQuest.status}");
            }

            // Close mission canvas
            ResetToGameplay();

            // Update quest availability
            CheckForAvailableQuest();
        }
        else
        {
            Debug.LogError("Cannot accept quest - currentQuest or questDatabase is null!");
        }
    }

    // Okay button clicked - for InProgress quests
    private void OnOkayButtonClicked()
    {
        Debug.Log("Okay button clicked! (Quest is already InProgress)");

        // Just close the mission canvas without changing status
        ResetToGameplay();

        // Show quest button again if player is still in range
        if (playerInRange)
        {
            Debug.Log("Player still in range - checking for available quests");
            CheckForAvailableQuest();
        }
    }

    // Back button clicked
    private void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked!");
        ResetToGameplay();

        // Show quest button again if player is still in range
        if (playerInRange)
        {
            Debug.Log("Player still in range - checking for available quests");
            CheckForAvailableQuest();
        }
    }

    private void ResetToGameplay()
    {
        Debug.Log("Resetting to gameplay...");

        // Start fade out sequence
        StartCoroutine(FadeOutAndReset());
    }

    private IEnumerator FadeOutAndReset()
    {
        // Set isExit to true
        isExiting = true;
        Debug.Log("isExit set to TRUE - Starting reset sequence");

        // Optional fade out effect
        if (missionCanvasGroup != null && missionCanvas != null && missionCanvas.activeSelf)
        {
            float timer = 0f;
            float fadeDuration = 0.3f;
            float startAlpha = missionCanvasGroup.alpha;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                missionCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
                yield return null;
            }
        }

        // Wait for 1 second
        yield return new WaitForSeconds(fadeDelay);

        // Set isExit back to false
        isExiting = false;
        Debug.Log("isExit set to FALSE - Completing reset");

        // Reset camera priority back to 10
        if (npcVirtualCamera != null)
        {
            npcVirtualCamera.Priority = originalCameraPriority;
            Debug.Log($"Reset virtual camera priority to: {npcVirtualCamera.Priority}");
        }

        // Hide mission canvas
        if (missionCanvas != null)
        {
            missionCanvas.SetActive(false);
            Debug.Log("Mission canvas hidden");
        }

        // Hide all buttons
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (okayButton != null) okayButton.gameObject.SetActive(false);

        // Re-enable main UI controller canvas
        if (uiControllerCanvas != null)
        {
            uiControllerCanvas.SetActive(true);
            Debug.Log("UIControllerCanvas enabled");
        }

        // Stop any playing timeline
        if (playableDirector != null && playableDirector.state == PlayState.Playing)
        {
            playableDirector.Stop();
            Debug.Log("Timeline stopped");
        }
    }

    private void Update()
    {
        if (playerInRange && currentQuest != null &&
            (currentQuest.status == QuestStatus.Completed || currentQuest.status == QuestStatus.Abandoned))
        {
            // Quest has been completed or abandoned, re-check
            Debug.Log($"Quest status changed to {currentQuest.status} - re-checking availability");
            CheckForAvailableQuest();
        }
    }

    // Public property to check if exiting (for other scripts)
    public bool IsExiting => isExiting;

    // Public method to manually check for quests
    public void ForceCheckForQuests()
    {
        CheckForAvailableQuest();
    }
}