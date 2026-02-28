using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

public class ChestManager : MonoBehaviour
{
    [Header("Chest Database")]
    public ChestDatabase chestDatabase;

    [Header("Fallback Settings (Use if no Database)")]
    public GameObject[] chestPrefabs;
    public VideoClip[] chestVideoClips;
    public AudioClip[] chestBackgroundMusic;
    public float[] chestRewardDelays = new float[] { 2f, 3f, 4f, 5f };

    [Header("Spawn Point")]
    public Transform chestSpawnPoint;

    [Header("Trigger Area")]
    public Collider triggerArea;

    [Header("Camera Settings")]
    public Cinemachine.CinemachineVirtualCamera chestCamera;

    [Header("Canvas References")]
    public GameObject menuCanvas;
    public GameObject chestCanvas;
    public GameObject claimButtonCanvas; // The canvas with the claim button

    [Header("Objects to Toggle")]
    [Tooltip("These objects will be disabled when claim button is clicked and re-enabled when chest is claimed")]
    public List<GameObject> objectsToToggle = new List<GameObject>();

    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public RenderTexture videoRenderTexture;

    [Header("Audio Settings")]
    public AudioSource chestMusicAudioSource; // Dedicated AudioSource for chest music

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f; // For chest canvas
    public float claimButtonFadeDuration = 0.15f; // Faster fade for claim button (changed from 0.5f to 0.15f)

    [Header("Player Settings")]
    public GameObject playerObject;

    [Header("Reward Feedback UI - COINS")]
    public GameObject coinRewardFeedbackPrefab;
    public RectTransform coinRewardSpawnPoint;

    [Header("Reward Feedback UI - GEMS")]
    public GameObject gemRewardFeedbackPrefab;
    public RectTransform gemRewardSpawnPoint;

    private float lastTriggerExitTime = 0f;
    private float triggerCooldown = 0.5f; // Adjust as needed
    private bool isShowingClaimButton = false;
    private Coroutine showButtonCoroutine;

    [Header("Animation Settings")]
    public Canvas parentCanvas;
    public float feedbackSlideDuration = 0.5f;
    public float feedbackFadeOutDuration = 0.3f;
    public float feedbackSlideUpAmount = 50f;
    public string feedbackPrefix = "+";
    public string coinSuffix = "";
    public string gemSuffix = "";

    [Header("Audio")]
    public AudioClip coinSound;
    public AudioClip gemSound;

    private Queue<int> chestQueue = new Queue<int>();
    private Chest currentChest;
    private ChestUIHandler chestUIHandler;
    private CanvasGroup chestCanvasGroup;
    private CanvasGroup claimButtonCanvasGroup;
    private int currentChestIndex = 0;
    private bool isPlayingChestMusic = false;
    private Coroutine waitForDelayCoroutine;
    private bool isPlayerInTrigger = false;
    private Button claimButton;
    private Player_Data playerData;
    private AudioClip currentChestMusic;

    // Track toggled objects
    private List<GameObject> toggledObjects = new List<GameObject>();

    public static ChestManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerData = FindObjectOfType<Player_Data>();

        if (chestCanvas != null)
        {
            chestUIHandler = chestCanvas.GetComponent<ChestUIHandler>();
            chestCanvasGroup = chestCanvas.GetComponent<CanvasGroup>();

            if (chestCanvasGroup == null)
            {
                chestCanvasGroup = chestCanvas.AddComponent<CanvasGroup>();
            }

            chestCanvasGroup.alpha = 0f;
            chestCanvasGroup.interactable = false;
            chestCanvasGroup.blocksRaycasts = false;
            chestCanvas.SetActive(false);
        }

        // Setup claim button canvas
        if (claimButtonCanvas != null)
        {
            claimButtonCanvasGroup = claimButtonCanvas.GetComponent<CanvasGroup>();
            if (claimButtonCanvasGroup == null)
            {
                claimButtonCanvasGroup = claimButtonCanvas.AddComponent<CanvasGroup>();
            }
            claimButtonCanvas.SetActive(false);
            claimButtonCanvasGroup.alpha = 0f;
            claimButtonCanvasGroup.interactable = false;
            claimButtonCanvasGroup.blocksRaycasts = false;

            // Find and setup the claim button
            claimButton = claimButtonCanvas.GetComponentInChildren<Button>();
            if (claimButton != null)
            {
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(OnClaimButtonClicked);
                Debug.Log("Claim button listener assigned in ChestManager");
            }
            else
            {
                Debug.LogError("No Button component found in claimButtonCanvas!");
            }
        }

        // Setup chest music AudioSource
        if (chestMusicAudioSource == null)
        {
            chestMusicAudioSource = gameObject.AddComponent<AudioSource>();
            chestMusicAudioSource.loop = false;
            chestMusicAudioSource.playOnAwake = false;
        }

        // Find parent canvas if not assigned
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
        }

        InitializeVideoPlayer();
        InitializeChestCamera();
        InitializeChestQueue();
        SpawnNextChest();
    }

    void InitializeVideoPlayer()
    {
        if (videoPlayer != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.skipOnDrop = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }
    }

    void InitializeChestCamera()
    {
        if (chestCamera != null)
        {
            chestCamera.Priority = 0;
            chestCamera.LookAt = null;
            chestCamera.Follow = null;
        }
    }

    void InitializeChestQueue()
    {
        int chestCount = GetChestCount();
        for (int i = 0; i < chestCount; i++)
        {
            chestQueue.Enqueue(i);
        }
    }

    int GetChestCount()
    {
        if (chestDatabase != null && chestDatabase.chestConfigs != null && chestDatabase.chestConfigs.Count > 0)
        {
            return chestDatabase.chestConfigs.Count;
        }
        else if (chestPrefabs != null && chestPrefabs.Length > 0)
        {
            return chestPrefabs.Length;
        }
        return 0;
    }

    void SpawnNextChest()
    {
        if (chestQueue.Count == 0) return;

        int chestIndex = chestQueue.Dequeue();
        GameObject chestPrefab = GetChestPrefab(chestIndex);

        if (chestPrefab == null || chestSpawnPoint == null) return;

        GameObject chestObj = Instantiate(chestPrefab, chestSpawnPoint);
        chestObj.transform.localPosition = Vector3.zero;
        chestObj.transform.localRotation = Quaternion.identity;

        currentChest = chestObj.GetComponent<Chest>();
        if (currentChest != null)
        {
            currentChest.Initialize();
            currentChest.SetChestIndex(chestIndex);
            currentChestIndex = chestIndex;

            // Start monitoring for claimable state
            StartCoroutine(MonitorChestClaimable());
        }
    }

    IEnumerator MonitorChestClaimable()
    {
        while (currentChest != null && !currentChest.isClaimable)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Chest is now claimable
        if (currentChest != null && currentChest.isClaimable)
        {
            // Only show if player is in trigger AND we're not in cooldown
            if (isPlayerInTrigger && (Time.time - lastTriggerExitTime) > triggerCooldown)
            {
                ShowClaimButton();
            }
        }
    }

    GameObject GetChestPrefab(int index)
    {
        if (chestDatabase != null && chestDatabase.chestConfigs != null &&
            chestDatabase.chestConfigs.Count > index && chestDatabase.chestConfigs[index] != null)
        {
            return chestDatabase.chestConfigs[index].chestPrefab;
        }
        else if (chestPrefabs != null && chestPrefabs.Length > index)
        {
            return chestPrefabs[index];
        }
        return null;
    }

    // Replace your OnTriggerEnter method:
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
            lastTriggerExitTime = 0f; // Reset exit time
            
            // Cancel any pending hide operations
            if (showButtonCoroutine != null)
                StopCoroutine(showButtonCoroutine);
            
            // Only show claim button if there's a chest and it's claimable
            if (currentChest != null && currentChest.isClaimable && !currentChest.isOpened)
            {
                showButtonCoroutine = StartCoroutine(ShowClaimButtonWithDelay(0.1f));
            }
        }
    }

    // Replace your OnTriggerExit method:
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            lastTriggerExitTime = Time.time;
            
            // Add a small delay before hiding to prevent flickering
            if (showButtonCoroutine != null)
                StopCoroutine(showButtonCoroutine);
            
            showButtonCoroutine = StartCoroutine(HideClaimButtonWithDelay(0.1f));
        }
    }

    void ShowClaimButton()
    {
        if (claimButtonCanvas != null && claimButtonCanvasGroup != null)
        {
            claimButtonCanvas.SetActive(true);
            StartCoroutine(FadeCanvasGroup(claimButtonCanvasGroup, 0f, 1f, claimButtonFadeDuration));
            claimButtonCanvasGroup.interactable = true;
            claimButtonCanvasGroup.blocksRaycasts = true;
            Debug.Log($"Claim button shown with {claimButtonFadeDuration}s fade");
        }
    }

        // Add these new coroutines:
    IEnumerator ShowClaimButtonWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Double-check conditions after delay
        if (isPlayerInTrigger && currentChest != null && 
            currentChest.isClaimable && !currentChest.isOpened)
        {
            ShowClaimButton();
        }
    }

    void HideClaimButton()
    {
        if (claimButtonCanvas != null && claimButtonCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(claimButtonCanvasGroup, 1f, 0f, claimButtonFadeDuration));
            claimButtonCanvasGroup.interactable = false;
            claimButtonCanvasGroup.blocksRaycasts = false;
            StartCoroutine(DeactivateAfterDelay(claimButtonCanvas, claimButtonFadeDuration));
            Debug.Log($"Claim button hidden with {claimButtonFadeDuration}s fade");
        }
    }

        IEnumerator HideClaimButtonWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Check if player is still not in trigger (debounce)
        if (!isPlayerInTrigger)
        {
            HideClaimButton();
        }
    }
    

    IEnumerator DeactivateAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) obj.SetActive(false);
    }

    // Toggle objects on/off
    private void ToggleObjects(bool setActive)
    {
        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                obj.SetActive(setActive);
                if (!setActive && !toggledObjects.Contains(obj))
                {
                    toggledObjects.Add(obj);
                }
            }
        }
        Debug.Log($"Objects toggled {(setActive ? "ON" : "OFF")}");
    }

    // Called when player clicks the claim button (the first button)
    public void OnClaimButtonClicked()
    {
        if (currentChest == null || !currentChest.isClaimable || currentChest.isOpened)
        {
            Debug.Log("Cannot claim chest: " +
                (currentChest == null ? "No chest" :
                (!currentChest.isClaimable ? "Not claimable" : "Already opened")));
            return;
        }

        // Play click sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        // Hide claim button immediately with faster fade
        HideClaimButton();

        // Toggle OFF the specified objects
        ToggleObjects(false);

        // Call the chest's HandleChestClick method
        if (currentChest != null)
        {
            currentChest.HandleChestClick();
        }
    }

    public void FocusOnChest(Chest chest)
    {
        if (chest != currentChest || chestCamera == null) return;

        int chestIndex = chest.chestOrder;
        float customDelay = GetRewardDelayForChest(chestIndex);

        // ========== GUARANTEED CANVAS ACTIVATION ==========
        // Method 1: Direct activation with CanvasGroup
        if (chestCanvas != null)
        {
            // Force the canvas to be active
            chestCanvas.SetActive(true);

            // Reset the canvas group to ensure it's visible
            if (chestCanvasGroup != null)
            {
                chestCanvasGroup.alpha = 1f;
                chestCanvasGroup.interactable = true;
                chestCanvasGroup.blocksRaycasts = true;
            }

            Debug.Log("CHEST CANVAS ACTIVATED - Method 1 (Direct)");
        }

        // Method 2: Fade in as backup
        if (chestCanvas != null && chestCanvasGroup != null)
        {
            StartCoroutine(FadeCanvas(chestCanvasGroup, 0f, 1f, fadeDuration));
            Debug.Log("CHEST CANVAS FADE STARTED - Method 2 (Fade)");
        }

        // Method 3: Double-check after a tiny delay
        StartCoroutine(VerifyCanvasActivated());

        // Now that canvas is active, set up video and music
        ChangeBackgroundVideo(chestIndex);
        PlayChestBackgroundMusic(chestIndex);

        chestCamera.Priority = 20;
        chestCamera.LookAt = chest.transform;
        chestCamera.Follow = chest.transform;

        if (menuCanvas != null) menuCanvas.SetActive(false);

        chest.OpenChest();

        if (chestUIHandler != null)
        {
            chestUIHandler.SetCurrentChest(chest);
        }

        if (waitForDelayCoroutine != null)
            StopCoroutine(waitForDelayCoroutine);

        waitForDelayCoroutine = StartCoroutine(WaitForCustomDelay(chest, customDelay));
    }

    // Coroutine to verify canvas is active
    IEnumerator VerifyCanvasActivated()
    {
        yield return new WaitForSeconds(0.1f);

        if (chestCanvas != null && !chestCanvas.activeInHierarchy)
        {
            Debug.LogWarning("CHEST CANVAS WAS NOT ACTIVE! Forcing activation...");
            chestCanvas.SetActive(true);

            if (chestCanvasGroup != null)
            {
                chestCanvasGroup.alpha = 1f;
                chestCanvasGroup.interactable = true;
                chestCanvasGroup.blocksRaycasts = true;
            }
        }
        else if (chestCanvas != null)
        {
            Debug.Log("CHEST CANVAS VERIFIED: Active and visible");
        }
    }

    float GetRewardDelayForChest(int chestIndex)
    {
        if (chestDatabase != null && chestDatabase.chestConfigs != null &&
            chestDatabase.chestConfigs.Count > chestIndex && chestDatabase.chestConfigs[chestIndex] != null)
        {
            return chestDatabase.chestConfigs[chestIndex].rewardDelay;
        }
        else if (chestRewardDelays != null && chestRewardDelays.Length > chestIndex)
        {
            return chestRewardDelays[chestIndex];
        }
        return 2f;
    }

    VideoClip GetVideoClipForChest(int chestIndex)
    {
        if (chestDatabase != null && chestDatabase.chestConfigs != null &&
            chestDatabase.chestConfigs.Count > chestIndex && chestDatabase.chestConfigs[chestIndex] != null)
        {
            return chestDatabase.chestConfigs[chestIndex].videoClip;
        }
        else if (chestVideoClips != null && chestVideoClips.Length > chestIndex)
        {
            return chestVideoClips[chestIndex];
        }
        return null;
    }

    AudioClip GetAudioClipForChest(int chestIndex)
    {
        if (chestDatabase != null && chestDatabase.chestConfigs != null &&
            chestDatabase.chestConfigs.Count > chestIndex && chestDatabase.chestConfigs[chestIndex] != null)
        {
            return chestDatabase.chestConfigs[chestIndex].backgroundMusic;
        }
        else if (chestBackgroundMusic != null && chestBackgroundMusic.Length > chestIndex)
        {
            return chestBackgroundMusic[chestIndex];
        }
        return null;
    }

    IEnumerator WaitForCustomDelay(Chest chest, float delay)
    {
        Debug.Log($"Waiting {delay} seconds before showing rewards for {chest.ChestName}");
        yield return new WaitForSeconds(delay);
        Debug.Log($"Delay finished, showing rewards for {chest.ChestName}");

        if (chestUIHandler != null)
        {
            chestUIHandler.StartRevealingRewards(chest);
        }
    }

    void PlayChestBackgroundMusic(int chestIndex)
    {
        // Stop any currently playing music
        if (chestMusicAudioSource != null && chestMusicAudioSource.isPlaying)
        {
            chestMusicAudioSource.Stop();
        }

        AudioClip chestMusic = GetAudioClipForChest(chestIndex);
        currentChestMusic = chestMusic;

        if (chestMusic != null && chestMusicAudioSource != null)
        {
            chestMusicAudioSource.clip = chestMusic;
            chestMusicAudioSource.loop = false;
            chestMusicAudioSource.Play();
            isPlayingChestMusic = true;

            StartCoroutine(StopMusicWhenEnds(chestMusic.length));
            Debug.Log($"Playing chest music: {chestMusic.name}");
        }
    }

    void ChangeBackgroundVideo(int chestIndex)
    {
        if (videoPlayer == null) return;

        VideoClip targetVideo = GetVideoClipForChest(chestIndex);
        if (targetVideo != null)
        {
            StartCoroutine(ChangeVideoRoutine(targetVideo));
        }
        else
        {
            StopVideo();
        }
    }

    IEnumerator ChangeVideoRoutine(VideoClip newClip)
    {
        videoPlayer.Stop();

        if (videoRenderTexture != null)
        {
            RenderTexture.active = videoRenderTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }

        yield return null;

        videoPlayer.clip = newClip;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        videoPlayer.Play();
    }

    IEnumerator StopMusicWhenEnds(float musicLength)
    {
        yield return new WaitForSeconds(musicLength);

        if (isPlayingChestMusic && chestCanvas != null && chestCanvas.activeInHierarchy)
        {
            StopChestMusic();
        }
    }

    void StopChestMusic()
    {
        if (chestMusicAudioSource != null)
        {
            chestMusicAudioSource.Stop();
            isPlayingChestMusic = false;
        }
    }

    void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
    }

    // Process rewards and add to player data
    private void ProcessAndAddRewards(List<ChestDatabase.ChestReward> chestRewards)
    {
        if (playerData == null) return;

        int totalCoins = 0;
        int totalGems = 0;
        int totalXP = 0;

        foreach (var reward in chestRewards)
        {
            switch (reward.rewardType.ToLower())
            {
                case "coin":
                case "coins":
                case "nutricoins":
                    totalCoins += reward.amount;
                    break;

                case "gem":
                case "gems":
                case "nutrigems":
                    totalGems += reward.amount;
                    break;

                case "exp":
                case "xp":
                case "experience":
                    totalXP += reward.amount;
                    break;

                case "key":
                    if (GameDataManager.Instance != null)
                    {
                        GameDataManager.Instance.CurrentGameData.CollectKingdomKey(reward.rewardName);
                    }
                    break;
            }
        }

        // Add to GameData
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            if (totalCoins > 0)
            {
                GameDataManager.Instance.CurrentGameData.nutriCoins += totalCoins;
            }

            if (totalGems > 0)
            {
                GameDataManager.Instance.CurrentGameData.nutriGems += totalGems;
            }

            if (totalXP > 0)
            {
                GameDataManager.Instance.CurrentGameData.currentXP += totalXP;

                // Check for level up
                while (GameDataManager.Instance.CurrentGameData.currentXP >=
                       GameDataManager.Instance.CurrentGameData.xpToNextLevel)
                {
                    GameDataManager.Instance.CurrentGameData.currentXP -=
                        GameDataManager.Instance.CurrentGameData.xpToNextLevel;
                    GameDataManager.Instance.CurrentGameData.playerLevel++;
                    GameDataManager.Instance.CurrentGameData.xpToNextLevel =
                        CalculateNextLevelXP(GameDataManager.Instance.CurrentGameData.playerLevel);
                }
            }

            GameDataManager.Instance.SaveGameData();
        }

        // Notify Player_Data for UI updates
        if (totalCoins > 0)
        {
            playerData.NotifyCoinCollected(totalCoins);
            ShowCoinFeedback(totalCoins);
        }

        if (totalGems > 0)
        {
            playerData.NotifyGemCollected(totalGems);
            ShowGemFeedback(totalGems);
        }

        if (totalXP > 0)
        {
            playerData.NotifyXPGained(totalXP);
        }

        playerData.ForceUpdateAllUI();
    }

    private float CalculateNextLevelXP(int level)
    {
        return 100 * level;
    }

    public void OnChestClaimed()
    {
        if (chestUIHandler != null)
        {
            // Get rewards before closing
            var chestConfig = GetChestConfig(currentChestIndex);
            if (chestConfig != null && chestConfig.chestRewards != null)
            {
                ProcessAndAddRewards(chestConfig.chestRewards);
            }

            chestUIHandler.OnChestUIClosed();
        }

        StopVideo();
        StopChestMusic();

        if (chestCamera != null)
        {
            chestCamera.Priority = 0;
            chestCamera.LookAt = null;
            chestCamera.Follow = null;
        }

        // Toggle ON the specified objects (re-enable them)
        ToggleObjects(true);

        if (currentChest != null)
        {
            Destroy(currentChest.gameObject);
            currentChest = null;
        }

        if (waitForDelayCoroutine != null)
        {
            StopCoroutine(waitForDelayCoroutine);
            waitForDelayCoroutine = null;
        }

        if (chestCanvas != null && chestCanvasGroup != null)
        {
            StartCoroutine(FadeAndHideCanvas());
        }
        else
        {
            SwitchBackToMenu();
            StartCoroutine(SpawnNextChestAfterDelay(1f));
        }
    }

    IEnumerator FadeAndHideCanvas()
    {
        yield return StartCoroutine(FadeCanvas(chestCanvasGroup, 1f, 0f, fadeDuration / 2f));
        SwitchBackToMenu();
        StartCoroutine(SpawnNextChestAfterDelay(1f));
    }

    void SwitchBackToMenu()
    {
        if (chestCamera != null)
        {
            chestCamera.Priority = 0;
            chestCamera.LookAt = null;
            chestCamera.Follow = null;
        }

        if (menuCanvas != null) menuCanvas.SetActive(true);
        if (chestCanvas != null) chestCanvas.SetActive(false);
    }

    IEnumerator FadeCanvas(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        canvasGroup.alpha = startAlpha;

        if (endAlpha > startAlpha)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
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

        canvasGroup.alpha = endAlpha;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
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

    IEnumerator SpawnNextChestAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnNextChest();
    }

    public ChestDatabase.ChestConfig GetChestConfig(int index)
    {
        if (chestDatabase != null)
        {
            return chestDatabase.GetChestConfig(index);
        }
        return null;
    }

    public ChestDatabase.ChestConfig GetChestConfigByName(string name)
    {
        if (chestDatabase != null)
        {
            return chestDatabase.GetChestConfigByName(name);
        }
        return null;
    }

    // ========== REWARD FEEDBACK METHODS ==========

    public void ShowCoinFeedback(int amount)
    {
        if (coinRewardFeedbackPrefab == null || coinRewardSpawnPoint == null || parentCanvas == null || amount <= 0)
            return;

        GameObject feedbackObject = Instantiate(coinRewardFeedbackPrefab, parentCanvas.transform);
        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();

        rectTransform.position = coinRewardSpawnPoint.position;
        rectTransform.anchorMin = coinRewardSpawnPoint.anchorMin;
        rectTransform.anchorMax = coinRewardSpawnPoint.anchorMax;
        rectTransform.pivot = coinRewardSpawnPoint.pivot;

        TMPro.TMP_Text feedbackText = feedbackObject.GetComponentInChildren<TMPro.TMP_Text>();
        if (feedbackText != null)
        {
            feedbackText.text = $"{feedbackPrefix}{amount}{coinSuffix}";
        }

        StartCoroutine(AnimateRewardFeedback(feedbackObject));
    }

    public void ShowGemFeedback(int amount)
    {
        if (gemRewardFeedbackPrefab == null || gemRewardSpawnPoint == null || parentCanvas == null || amount <= 0)
            return;

        GameObject feedbackObject = Instantiate(gemRewardFeedbackPrefab, parentCanvas.transform);
        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();

        rectTransform.position = gemRewardSpawnPoint.position;
        rectTransform.anchorMin = gemRewardSpawnPoint.anchorMin;
        rectTransform.anchorMax = gemRewardSpawnPoint.anchorMax;
        rectTransform.pivot = gemRewardSpawnPoint.pivot;

        TMPro.TMP_Text feedbackText = feedbackObject.GetComponentInChildren<TMPro.TMP_Text>();
        if (feedbackText != null)
        {
            feedbackText.text = $"{feedbackPrefix}{amount}{gemSuffix}";
        }

        StartCoroutine(AnimateRewardFeedback(feedbackObject));
    }

    private IEnumerator AnimateRewardFeedback(GameObject feedbackObject)
    {
        if (feedbackObject == null) yield break;

        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = feedbackObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = feedbackObject.AddComponent<CanvasGroup>();
        }

        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, feedbackSlideUpAmount);

        float elapsedTime = 0f;

        while (elapsedTime < feedbackSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / feedbackSlideDuration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < feedbackFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / feedbackFadeOutDuration);
            yield return null;
        }

        Destroy(feedbackObject);
    }
}