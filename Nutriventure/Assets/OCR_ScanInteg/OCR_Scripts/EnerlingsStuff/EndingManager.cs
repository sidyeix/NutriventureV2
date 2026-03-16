using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class EndingManager : MonoBehaviour
{
    [Header("References")]
    public BattleEnerlingManager battleManager;
    public AIEnerlingManager aiManager;
    public TurnSystem turnSystem;
    public IngredientDatabase ingredientDatabase;

    [Header("Canvas References")]
    public GameObject battlefieldCanvas;
    public GameObject endingCutsceneCanvas;      // Canvas with VideoPlayer
    public GameObject enerlingEndingCatchCanvas; // Canvas with catch UI
    public GameObject playerDefeatedCanvas;

    [Header("Video Player")]
    public RawImage videoRawImage;
    public VideoPlayer endingVideoPlayer;
    public RenderTexture videoRenderTexture;
    public Button skipCutsceneButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource endCanvasLoopAudioSource;
    public AudioClip playerWinAudio;
    public AudioClip playerLoseAudio;
    public AudioClip victoryAudio;
    public AudioClip defeatAudio;
    [Range(0f, 1f)] public float endCanvasBaseVolume = 1f;
    [Range(1f, 4f)] public float endCanvasVolumeMultiplier = 2f;
    public bool forceEndCanvasVolumeToMax = true;

    [Header("Camera References")]
    public CinemachineVirtualCamera enerlingDefeatCamera;
    public PlayableDirector winningTimelineDirector;
    public PlayableAsset winningTimelineAsset;

    [Header("EnerlingEndingCatch UI")]
    public Image enerlingFrameImage;
    public Image rarityTagImage;
    public Image kingdomSpriteImage;
    public Image enerlingIconImage;
    public TextMeshProUGUI enerlingNameText;
    public TextMeshProUGUI kingdomText;
    public Button continueButton;
    public GameObject unlockedText;
    public GameObject progressUnlockPanel;
    public Slider progressUnlockSlider;
    public TextMeshProUGUI progressUnlockText;

    [Header("Reward UI")]
    public Transform rewardsPanelContainer;
    public GameObject rewardItemPrefab;
    public Sprite coinRewardImage;
    public Sprite gemRewardImage;
    public Sprite xpRewardImage;

    [Header("Player Defeated UI")]
    public Button playerDefeatedContinueButton;
    public Transform playerDefeatedRewardsPanelContainer;

    [Header("Scene Names")]
    public string scanOCRSceneName = "ScanOCR";

    [Header("Fade Settings")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public float videoFadeInDuration = 0.8f;
    public float videoFadeOutDuration = 0.5f;

    [Header("Kingdom Sprites")]
    public Sprite nutriKingdomSprite;
    public Sprite alerthiaSprite;
    public Sprite sugariaSprite;
    public Sprite preserviaSprite;

    [Header("Audio Listener")]
    public AudioListener audioListener;
    public AudioSource continueAudioSource;
    public AudioClip continueSfx;

    private bool gameEnded = false;
    private GameObject spawnedPlayerEnerling;
    private GameObject spawnedAIEnerling;
    private Animator playerAnimator;
    private Animator aiAnimator;
    private IngredientDatabase.IngredientInfo defeatedAIEnerling;
    private bool skipEndingVideoRequested = false;
    private bool isEndingVideoSequenceActive = false;
    private bool isEndCanvasLoopActive = false;
    private readonly List<RewardGrant> pendingRewards = new List<RewardGrant>();
    private readonly List<RewardGrant> pendingDefeatRewards = new List<RewardGrant>();
    private bool rewardsApplied = false;
    private bool defeatRewardsApplied = false;
    private int catchCountBeforeWin = 0;
    private int catchCountAfterWin = 0;

    private enum RewardKind
    {
        Coins,
        Gems,
        XP
    }

    private struct RewardGrant
    {
        public RewardKind Kind;
        public int Amount;

        public RewardGrant(RewardKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }
    }

    void Start()
    {
        EnsureSingleAudioListener();
        InitializeReferences();
        EnsureEndCanvasLoopAudioSource();
        SetupButtonListeners();

        // Initially disable all ending canvases
        if (endingCutsceneCanvas != null)
            endingCutsceneCanvas.SetActive(false);

        if (enerlingEndingCatchCanvas != null)
            enerlingEndingCatchCanvas.SetActive(false);

        if (playerDefeatedCanvas != null)
            playerDefeatedCanvas.SetActive(false);

        ConfigureVideoPlayer();
    }

    void ConfigureVideoPlayer()
    {
        if (endingVideoPlayer == null) return;

        // Configure video player with manual control
        endingVideoPlayer.playOnAwake = false; // Manual control
        endingVideoPlayer.waitForFirstFrame = true;
        endingVideoPlayer.isLooping = false;
        endingVideoPlayer.skipOnDrop = true;
        endingVideoPlayer.playbackSpeed = 1f;
        endingVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        endingVideoPlayer.targetTexture = videoRenderTexture;
        endingVideoPlayer.aspectRatio = VideoAspectRatio.FitVertically;
        endingVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        Debug.Log("Video player configured for manual playback");
    }

    void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            Debug.LogWarning($"Found {listeners.Length} AudioListeners. Disabling extras...");

            AudioListener keepListener = audioListener != null ? audioListener : listeners[0];

            foreach (AudioListener listener in listeners)
            {
                if (listener != keepListener)
                {
                    listener.enabled = false;
                    Debug.Log($"Disabled AudioListener on {listener.gameObject.name}");
                }
            }
        }
    }

    void InitializeReferences()
    {
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleEnerlingManager>();

        if (aiManager == null)
            aiManager = FindObjectOfType<AIEnerlingManager>();

        if (turnSystem == null)
            turnSystem = FindObjectOfType<TurnSystem>();

        if (ingredientDatabase == null)
        {
            ingredientDatabase = FindObjectOfType<PersistentDataManager>()?.ingredientDatabase;
            if (ingredientDatabase == null)
            {
                ingredientDatabase = Resources.Load<IngredientDatabase>("IngredientDatabase");
            }
        }

        spawnedPlayerEnerling = FindPlayerEnerling();
        spawnedAIEnerling = FindAIEnerling();

        if (spawnedPlayerEnerling != null)
            playerAnimator = spawnedPlayerEnerling.GetComponent<Animator>();

        if (spawnedAIEnerling != null)
            aiAnimator = spawnedAIEnerling.GetComponent<Animator>();
    }

    GameObject FindPlayerEnerling()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Player") || obj.name.Contains("Spawned") ||
                (obj.transform.parent != null && obj.transform.parent.name.Contains("Player")))
            {
                if (obj.GetComponent<Animator>() != null)
                    return obj;
            }
        }
        return null;
    }

    GameObject FindAIEnerling()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("AI") || obj.name.Contains("Enemy") ||
                (obj.transform.parent != null && obj.transform.parent.name.Contains("AI")))
            {
                if (obj.GetComponent<Animator>() != null)
                    return obj;
            }
        }
        return null;
    }

    void SetupButtonListeners()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnEnerlingEndingCatchContinue);
        }

        if (skipCutsceneButton != null)
        {
            skipCutsceneButton.onClick.RemoveAllListeners();
            skipCutsceneButton.onClick.AddListener(OnSkipEndingCutsceneClicked);
        }

        if (playerDefeatedContinueButton != null)
        {
            playerDefeatedContinueButton.onClick.RemoveAllListeners();
            playerDefeatedContinueButton.onClick.AddListener(OnPlayerDefeatedContinue);
        }
    }

    void Update()
    {
        if (gameEnded) return;

        var playerEnerling = battleManager?.GetBattleEnerling();
        var aiEnerling = aiManager?.GetAIEnerling();

        if (playerEnerling != null && aiEnerling != null)
        {
            bool playerDead = playerEnerling.currentLife <= 0;
            bool aiDead = aiEnerling.currentLife <= 0;

            if (playerDead || aiDead)
            {
                gameEnded = true;
                StartCoroutine(HandleBattleEnd(playerDead, aiDead));
            }
        }
    }

    IEnumerator HandleBattleEnd(bool playerDead, bool aiDead)
    {
        Debug.Log($"Battle ended: PlayerDead={playerDead}, AIDead={aiDead}");

        if (battlefieldCanvas != null && battlefieldCanvas.activeSelf)
        {
            yield return StartCoroutine(FadeOutCanvas(battlefieldCanvas, fadeOutDuration));
            battlefieldCanvas.SetActive(false);
        }

        if (turnSystem != null)
            turnSystem.Cleanup();

        SetAnimatorParameters(playerDead, aiDead);
        StopBattleBackgroundAudio();
        MuteAllAudioExcept(null);

        if (playerDead)
        {
            yield return StartCoroutine(HandlePlayerDefeated());
        }
        else if (aiDead)
        {
            yield return StartCoroutine(HandlePlayerVictory());
        }
    }

    void SetAnimatorParameters(bool playerDead, bool aiDead)
    {
        if (playerDead && playerAnimator != null)
        {
            playerAnimator.SetBool("isDie", true);
            playerAnimator.SetBool("isWin", false);
        }
        else if (!playerDead && playerAnimator != null)
        {
            playerAnimator.SetBool("isDie", false);
            playerAnimator.SetBool("isWin", true);
        }

        if (aiDead && aiAnimator != null)
        {
            aiAnimator.SetBool("isDie", true);
            aiAnimator.SetBool("isWin", false);
        }
        else if (!aiDead && aiAnimator != null)
        {
            aiAnimator.SetBool("isDie", false);
            aiAnimator.SetBool("isWin", true);
        }
    }

    void StopBattleBackgroundAudio()
    {
        if (AudioManagerBattleField.Instance != null)
        {
            AudioManagerBattleField.Instance.StopBattleAudio();
        }
    }

    IEnumerator HandlePlayerVictory()
    {
        Debug.Log("Player wins!");

        defeatedAIEnerling = aiManager?.GetAIEnerling();
        if (defeatedAIEnerling == null)
        {
            Debug.LogError("Defeated AI enerling not found!");
            yield break;
        }

        catchCountBeforeWin = GetCurrentCatchCount(defeatedAIEnerling.ingredientName);

        // --- Increment catch count via BattlePlayManager ---
        if (BattlePlayManager.Instance != null)
        {
            BattlePlayManager.Instance.OnBattleWin(defeatedAIEnerling.ingredientName);
        }

        catchCountAfterWin = GetCurrentCatchCount(defeatedAIEnerling.ingredientName);
        BuildRewardsByRarity(defeatedAIEnerling.rarity);
        rewardsApplied = false;

        Debug.Log($"Defeated enerling: {defeatedAIEnerling.ingredientName}");
        Debug.Log($"Has ending cutscene: {defeatedAIEnerling.endingCutscene != null}");

        if (defeatedAIEnerling.endingCutscene != null)
        {
            Debug.Log($"Ending cutscene name: {defeatedAIEnerling.endingCutscene.name}");
        }

        if (enerlingDefeatCamera != null)
        {
            enerlingDefeatCamera.Priority = 30;
            Debug.Log("EnerlingDefeat camera priority set to 30");
        }

        if (winningTimelineDirector != null && winningTimelineAsset != null)
        {
            winningTimelineDirector.playableAsset = winningTimelineAsset;
            winningTimelineDirector.time = 0;
            winningTimelineDirector.Play();

            while (winningTimelineDirector.state == PlayState.Playing)
            {
                yield return null;
            }
            Debug.Log("Winning timeline completed");
        }

        if (defeatedAIEnerling.endingCutscene != null)
        {
            yield return StartCoroutine(PlayEndingCutscene());
        }
        else
        {
            Debug.LogWarning($"No ending cutscene assigned for {defeatedAIEnerling.ingredientName} - skipping video");
        }

        yield return StartCoroutine(ShowEnerlingEndingCatch());
    }

    IEnumerator PlayEndingCutscene()
    {
        Debug.Log("=== Playing ending cutscene video ===");
        skipEndingVideoRequested = false;
        isEndingVideoSequenceActive = true;

        if (defeatedAIEnerling == null)
        {
            Debug.LogError("defeatedAIEnerling is null!");
            isEndingVideoSequenceActive = false;
            yield break;
        }

        if (defeatedAIEnerling.endingCutscene == null)
        {
            Debug.LogError($"endingCutscene is null for {defeatedAIEnerling.ingredientName}");
            isEndingVideoSequenceActive = false;
            yield break;
        }

        if (endingVideoPlayer == null)
        {
            Debug.LogError("endingVideoPlayer is null! Cannot play video.");
            isEndingVideoSequenceActive = false;
            yield break;
        }

        if (endingCutsceneCanvas == null)
        {
            Debug.LogError("endingCutsceneCanvas is null! Cannot show video canvas.");
            isEndingVideoSequenceActive = false;
            yield break;
        }

        Debug.Log($"Setting video clip to: {defeatedAIEnerling.endingCutscene.name} for enerling: {defeatedAIEnerling.ingredientName}");

        // Stop any currently playing video
        if (endingVideoPlayer.isPlaying)
        {
            endingVideoPlayer.Stop();
        }

        // Set the video clip from the defeated enerling
        endingVideoPlayer.clip = defeatedAIEnerling.endingCutscene;
        videoRawImage.texture = videoRenderTexture;

        Debug.Log($"VideoPlayer clip after assignment: {(endingVideoPlayer.clip != null ? endingVideoPlayer.clip.name : "NULL")}");

        // Check if video is already prepared (from preloading)
        if (!endingVideoPlayer.isPrepared)
        {
            Debug.Log("Video not prepared yet, preparing now...");
            endingVideoPlayer.Prepare();

            // Wait for preparation to complete
            while (!endingVideoPlayer.isPrepared)
            {
                yield return null;
            }
            Debug.Log("Video prepared successfully");
        }
        else
        {
            Debug.Log("Video already prepared (preloaded) - ready to play");
        }

        // Activate the canvas
        Debug.Log("Activating Ending Cutscene Canvas");
        endingCutsceneCanvas.SetActive(true);

        // Fade in the canvas
        yield return StartCoroutine(FadeInCanvas(endingCutsceneCanvas, videoFadeInDuration));

        // Mute other audio while video plays
        MuteAllAudioExcept(endingVideoPlayer);

        // MANUALLY PLAY THE VIDEO
        Debug.Log("Starting video playback with Play()");
        endingVideoPlayer.Play();

        // Wait for video to actually start playing
        float startTimeout = 2f;
        float startTimer = 0f;
        while (!endingVideoPlayer.isPlaying && startTimer < startTimeout)
        {
            startTimer += Time.deltaTime;
            yield return null;
        }

        if (!endingVideoPlayer.isPlaying)
        {
            Debug.LogError("Video failed to start playing!");
        }
        else
        {
            Debug.Log($"Video successfully playing. Length: {endingVideoPlayer.clip.length} seconds");
        }

        // Wait for video to finish - THIS IS CRITICAL
        while (endingVideoPlayer.isPlaying)
        {
            if (skipEndingVideoRequested)
            {
                Debug.Log("Ending cutscene skip requested. Stopping video now.");
                endingVideoPlayer.Stop();
                break;
            }

            yield return null;
        }

        Debug.Log("Ending cutscene video completed");

        // Fade out the canvas
        yield return StartCoroutine(FadeOutCanvas(endingCutsceneCanvas, videoFadeOutDuration));
        endingCutsceneCanvas.SetActive(false);

        // Restore audio
        RestoreAudio();
        isEndingVideoSequenceActive = false;
        skipEndingVideoRequested = false;
    }

    void OnSkipEndingCutsceneClicked()
    {
        if (!isEndingVideoSequenceActive)
            return;

        skipEndingVideoRequested = true;
    }

    IEnumerator ShowEnerlingEndingCatch()
    {
        Debug.Log("=== Showing EnerlingEndingCatch canvas ===");

        if (defeatedAIEnerling == null)
        {
            Debug.LogError("defeatedAIEnerling is null!");
            yield break;
        }

        if (enerlingEndingCatchCanvas == null)
        {
            Debug.LogError("enerlingEndingCatchCanvas is null!");
            yield break;
        }

        RestoreAudio();
        UpdateEnerlingEndingCatchUI();
        StartEndCanvasLoopAudio(true);

        enerlingEndingCatchCanvas.SetActive(true);
        yield return StartCoroutine(FadeInCanvas(enerlingEndingCatchCanvas, fadeInDuration));

        Debug.Log("EnerlingEndingCatch canvas is now visible");
    }

    void UpdateEnerlingEndingCatchUI()
    {
        if (defeatedAIEnerling == null) return;

        Debug.Log($"Updating EnerlingEndingCatch UI for {defeatedAIEnerling.ingredientName}");

        if (enerlingNameText != null)
            enerlingNameText.text = defeatedAIEnerling.ingredientName;

        if (kingdomText != null)
            kingdomText.text = defeatedAIEnerling.kingdom.ToString();

        if (enerlingIconImage != null)
        {
            if (defeatedAIEnerling.enerlingSprite != null)
            {
                enerlingIconImage.sprite = defeatedAIEnerling.enerlingSprite;
                enerlingIconImage.preserveAspect = true;
                Debug.Log($"Set enerling icon to: {defeatedAIEnerling.enerlingSprite.name}");
            }
            else
            {
                Debug.LogWarning($"Enerling sprite is missing for {defeatedAIEnerling.ingredientName}");
            }
        }

        if (enerlingFrameImage != null && ingredientDatabase != null)
        {
            Sprite frameSprite = ingredientDatabase.GetFrameSprite(defeatedAIEnerling.rarity);
            if (frameSprite != null)
            {
                enerlingFrameImage.sprite = frameSprite;
                enerlingFrameImage.preserveAspect = true;
            }
        }

        if (rarityTagImage != null && ingredientDatabase != null)
        {
            Sprite raritySprite = ingredientDatabase.GetRarityIcon(defeatedAIEnerling.rarity);
            if (raritySprite != null)
            {
                rarityTagImage.sprite = raritySprite;
                rarityTagImage.preserveAspect = true;
            }
        }

        if (kingdomSpriteImage != null)
        {
            Sprite kingdomSprite = GetKingdomSprite(defeatedAIEnerling.kingdom);
            if (kingdomSprite != null)
            {
                kingdomSpriteImage.sprite = kingdomSprite;
                kingdomSpriteImage.preserveAspect = true;
            }
        }

        ConfigureCatchProgressState();
        RefreshRewardPanel();
    }

    void ConfigureCatchProgressState()
    {
        int maxCatch = Mathf.Max(1, defeatedAIEnerling.maxCatch);
        int previousCatch = Mathf.Clamp(catchCountBeforeWin, 0, maxCatch);
        int currentCatch = Mathf.Clamp(catchCountAfterWin, 0, maxCatch);
        bool isFirstCatch = currentCatch <= 1;

        if (unlockedText != null)
            unlockedText.SetActive(isFirstCatch);

        if (progressUnlockPanel != null)
            progressUnlockPanel.SetActive(!isFirstCatch);

        if (progressUnlockSlider != null)
        {
            progressUnlockSlider.minValue = 0f;
            progressUnlockSlider.maxValue = maxCatch;
            progressUnlockSlider.value = isFirstCatch ? currentCatch : previousCatch;
        }

        if (progressUnlockText != null)
            progressUnlockText.text = $"{(isFirstCatch ? currentCatch : previousCatch)}/{maxCatch}";

        if (!isFirstCatch && currentCatch > previousCatch)
            StartCoroutine(AnimateCatchProgress(previousCatch, currentCatch, maxCatch));
        else
            UpdateProgressDisplay(currentCatch, maxCatch);
    }

    IEnumerator AnimateCatchProgress(int from, int to, int maxCatch)
    {
        const float duration = 0.7f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float value = Mathf.Lerp(from, to, t);

            if (progressUnlockSlider != null)
                progressUnlockSlider.value = value;

            int displayValue = Mathf.RoundToInt(value);
            if (progressUnlockText != null)
                progressUnlockText.text = $"{displayValue}/{maxCatch}";

            yield return null;
        }

        UpdateProgressDisplay(to, maxCatch);
    }

    void UpdateProgressDisplay(int current, int maxCatch)
    {
        if (progressUnlockSlider != null)
            progressUnlockSlider.value = current;

        if (progressUnlockText != null)
            progressUnlockText.text = $"{current}/{maxCatch}";
    }

    void BuildRewardsByRarity(IngredientDatabase.Rarity rarity)
    {
        pendingRewards.Clear();

        switch (rarity)
        {
            case IngredientDatabase.Rarity.Rare:
                pendingRewards.Add(new RewardGrant(RewardKind.Gems, 20));
                pendingRewards.Add(new RewardGrant(RewardKind.Coins, 300));
                pendingRewards.Add(new RewardGrant(RewardKind.XP, 200));
                break;

            case IngredientDatabase.Rarity.UltraRare:
                pendingRewards.Add(new RewardGrant(RewardKind.Gems, 40));
                pendingRewards.Add(new RewardGrant(RewardKind.Coins, 500));
                pendingRewards.Add(new RewardGrant(RewardKind.XP, 500));
                break;

            default:
                pendingRewards.Add(new RewardGrant(RewardKind.Gems, 10));
                pendingRewards.Add(new RewardGrant(RewardKind.Coins, 100));
                pendingRewards.Add(new RewardGrant(RewardKind.XP, 100));
                break;
        }
    }

    void RefreshRewardPanel()
    {
        if (rewardsPanelContainer == null)
            return;

        for (int i = rewardsPanelContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(rewardsPanelContainer.GetChild(i).gameObject);
        }

        foreach (RewardGrant reward in pendingRewards)
        {
            SpawnRewardItem(reward, rewardsPanelContainer);
        }
    }

    void RefreshDefeatRewardPanel()
    {
        if (playerDefeatedRewardsPanelContainer == null)
            return;

        for (int i = playerDefeatedRewardsPanelContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(playerDefeatedRewardsPanelContainer.GetChild(i).gameObject);
        }

        foreach (RewardGrant reward in pendingDefeatRewards)
        {
            SpawnRewardItem(reward, playerDefeatedRewardsPanelContainer);
        }
    }

    void SpawnRewardItem(RewardGrant reward, Transform container)
    {
        if (rewardItemPrefab == null || container == null)
            return;

        GameObject rewardObj = Instantiate(rewardItemPrefab, container);
        Sprite rewardIcon = GetRewardImage(reward.Kind);
        string rewardName = GetRewardDisplayName(reward.Kind);

        RewardItemUI rewardItemUI = rewardObj.GetComponent<RewardItemUI>();
        if (rewardItemUI != null)
        {
            if (rewardItemUI.rewardIcon != null)
                rewardItemUI.rewardIcon.sprite = rewardIcon;
            rewardItemUI.amountText.text = $"+{reward.Amount}";
            rewardItemUI.rewardNameText.text = rewardName;
            return;
        }

        RewardItem chestRewardItem = rewardObj.GetComponent<RewardItem>();
        if (chestRewardItem != null)
        {
            if (chestRewardItem.rewardIcon != null)
                chestRewardItem.rewardIcon.sprite = rewardIcon;
            if (chestRewardItem.rewardAmountText != null)
                chestRewardItem.rewardAmountText.text = $"+{reward.Amount}";
            if (chestRewardItem.rewardNameText != null)
                chestRewardItem.rewardNameText.text = rewardName;
            return;
        }

        TextMeshProUGUI[] texts = rewardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            string lowerName = text.gameObject.name.ToLower();
            if (lowerName.Contains("amount"))
                text.text = $"+{reward.Amount}";
            else if (lowerName.Contains("name"))
                text.text = rewardName;
        }

        Image[] images = rewardObj.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image.gameObject.name.ToLower().Contains("rewardimage") || image.gameObject.name.ToLower().Contains("rewardicon"))
            {
                image.sprite = rewardIcon;
            }
        }
    }

    Sprite GetRewardImage(RewardKind kind)
    {
        switch (kind)
        {
            case RewardKind.Coins:
                return coinRewardImage;
            case RewardKind.Gems:
                return gemRewardImage;
            case RewardKind.XP:
                return xpRewardImage;
            default:
                return null;
        }
    }

    string GetRewardDisplayName(RewardKind kind)
    {
        switch (kind)
        {
            case RewardKind.Coins:
                return "Coins";
            case RewardKind.Gems:
                return "Gems";
            case RewardKind.XP:
                return "XP";
            default:
                return "Reward";
        }
    }

    int GetCurrentCatchCount(string enerlingName)
    {
        int countFromPersistent = PersistentDataManager.Instance != null
            ? PersistentDataManager.Instance.GetCatchCount(enerlingName)
            : 0;

        if (countFromPersistent > 0)
            return countFromPersistent;

        if (GameDataManager.Instance != null)
            return GameDataManager.Instance.GetEnerlingCatchCount(enerlingName);

        return 0;
    }

    Sprite GetKingdomSprite(IngredientDatabase.KingdomOrigin kingdom)
    {
        switch (kingdom)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom:
                return nutriKingdomSprite;
            case IngredientDatabase.KingdomOrigin.Alerthia:
                return alerthiaSprite;
            case IngredientDatabase.KingdomOrigin.Sugaria:
                return sugariaSprite;
            case IngredientDatabase.KingdomOrigin.Preservia:
                return preserviaSprite;
            default:
                return nutriKingdomSprite;
        }
    }

    void OnEnerlingEndingCatchContinue()
    {
        Debug.Log("EnerlingEndingCatch continue button clicked");
        StartCoroutine(OnContinueButtonClicked());
    }

    IEnumerator OnContinueButtonClicked()
    {
        StopEndCanvasLoopAudio();
        PlayContinueSfx();
        ApplyPendingRewards();

        if (enerlingEndingCatchCanvas != null)
        {
            yield return StartCoroutine(FadeOutCanvas(enerlingEndingCatchCanvas, fadeOutDuration));
            enerlingEndingCatchCanvas.SetActive(false);
        }

        if (defeatedAIEnerling != null)
        {
            if (PersistentDataManager.Instance != null)
            {
                PersistentDataManager.Instance.UnlockEnerling(defeatedAIEnerling.ingredientName);
                Debug.Log($"Unlocked {defeatedAIEnerling.ingredientName} via PersistentDataManager");
            }

            if (ingredientDatabase != null)
            {
                var dbEnerling = ingredientDatabase.GetIngredientInfo(defeatedAIEnerling.ingredientName);
                if (dbEnerling != null)
                {
                    dbEnerling.isUnlocked = true;
                    Debug.Log($"Updated isUnlocked in database for {defeatedAIEnerling.ingredientName}");
                }
            }

            if (PersistentDataManager.Instance != null)
            {
                PersistentDataManager.Instance.SaveEnerlingCurrentLife(
                    defeatedAIEnerling.ingredientName,
                    defeatedAIEnerling.baseLife
                );
            }
        }

        ReturnToScanOCRScene();
    }

    void PlayContinueSfx()
    {
        if (continueSfx == null)
            return;

        AudioSource source = continueAudioSource != null ? continueAudioSource : audioSource;
        if (source == null)
            return;

        source.PlayOneShot(continueSfx);
    }

    void ApplyPendingRewards()
    {
        if (rewardsApplied || GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
            return;

        ApplyRewardListToGameData(pendingRewards);
        rewardsApplied = true;
    }

    void ApplyPendingDefeatRewards()
    {
        if (defeatRewardsApplied || GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
            return;

        ApplyRewardListToGameData(pendingDefeatRewards);
        defeatRewardsApplied = true;
    }

    void ApplyRewardListToGameData(List<RewardGrant> rewardList)
    {
        var gameData = GameDataManager.Instance.CurrentGameData;

        foreach (RewardGrant reward in rewardList)
        {
            switch (reward.Kind)
            {
                case RewardKind.Coins:
                    gameData.nutriCoins += reward.Amount;
                    break;

                case RewardKind.Gems:
                    gameData.nutriGems += reward.Amount;
                    break;

                case RewardKind.XP:
                    AddXP(gameData, reward.Amount);
                    break;
            }
        }

        GameDataManager.Instance.SaveGameData();
    }

    void BuildDefeatRewardsByRarity(IngredientDatabase.Rarity rarity)
    {
        pendingDefeatRewards.Clear();

        int multiplier = 1;
        if (rarity == IngredientDatabase.Rarity.Rare)
            multiplier = 2;
        else if (rarity == IngredientDatabase.Rarity.UltraRare)
            multiplier = 3;

        pendingDefeatRewards.Add(new RewardGrant(RewardKind.Gems, 1 * multiplier));
        pendingDefeatRewards.Add(new RewardGrant(RewardKind.Coins, 20 * multiplier));
        pendingDefeatRewards.Add(new RewardGrant(RewardKind.XP, 30 * multiplier));
    }

    void AddXP(GameData gameData, int amount)
    {
        if (gameData == null || amount <= 0)
            return;

        gameData.currentXP += amount;

        while (gameData.currentXP >= gameData.xpToNextLevel)
        {
            gameData.currentXP -= gameData.xpToNextLevel;
            gameData.playerLevel++;
            gameData.xpToNextLevel *= 1.5f;
        }
    }

    IEnumerator HandlePlayerDefeated()
    {
        Debug.Log("Player defeated!");

        defeatedAIEnerling = aiManager?.GetAIEnerling();
        IngredientDatabase.Rarity aiRarity = defeatedAIEnerling != null
            ? defeatedAIEnerling.rarity
            : IngredientDatabase.Rarity.Common;

        BuildDefeatRewardsByRarity(aiRarity);
        defeatRewardsApplied = false;
        RefreshDefeatRewardPanel();

        // --- Deduct 1 life via BattlePlayManager ---
        if (BattlePlayManager.Instance != null)
        {
            BattlePlayManager.Instance.OnBattleLose();
        }

        yield return new WaitForSeconds(1f);

        if (playerDefeatedCanvas != null)
        {
            RestoreAudio();
            StartEndCanvasLoopAudio(false);
            playerDefeatedCanvas.SetActive(true);
            yield return StartCoroutine(FadeInCanvas(playerDefeatedCanvas, fadeInDuration));
        }
    }

    void OnPlayerDefeatedContinue()
    {
        Debug.Log("Player defeated continue button clicked");
        StartCoroutine(OnPlayerDefeatedContinueCoroutine());
    }

    IEnumerator OnPlayerDefeatedContinueCoroutine()
    {
        StopEndCanvasLoopAudio();
        PlayContinueSfx();
        ApplyPendingDefeatRewards();

        if (playerDefeatedCanvas != null)
        {
            yield return StartCoroutine(FadeOutCanvas(playerDefeatedCanvas, fadeOutDuration));
            playerDefeatedCanvas.SetActive(false);
        }

        ReturnToScanOCRScene();
    }

    void ReturnToScanOCRScene()
    {
        StopEndCanvasLoopAudio();
        RestoreAudio();

        if (!string.IsNullOrEmpty(scanOCRSceneName))
        {
            Debug.Log($"Loading scene: {scanOCRSceneName}");
            SceneManager.LoadScene(scanOCRSceneName);
        }
        else
        {
            Debug.LogError("ScanOCR scene name not set!");
        }
    }

    IEnumerator FadeInCanvas(GameObject canvas, float duration)
    {
        if (canvas == null) yield break;

        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = canvas.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOutCanvas(GameObject canvas, float duration)
    {
        if (canvas == null) yield break;

        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = canvas.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    void MuteAllAudioExcept(VideoPlayer exception)
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            if (source != exception &&
                source != AudioManagerBattleField.Instance?.battleMusicSource &&
                source != AudioManagerBattleField.Instance?.audienceSFXSource)
            {
                source.mute = true;
            }
        }
    }

    void RestoreAudio()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            source.mute = false;
        }
    }

    void StartEndCanvasLoopAudio(bool isWin)
    {
        AudioSource loopSource = GetEndCanvasLoopAudioSource();
        if (loopSource == null)
            return;

        if (!loopSource.enabled || !loopSource.gameObject.activeInHierarchy)
            return;

        AudioClip loopClip = isWin ? GetWinningLoopClip() : GetLosingLoopClip();
        if (loopClip == null)
            return;

        if (loopSource.clip == loopClip && loopSource.isPlaying && loopSource.loop)
            return;

        loopSource.Stop();
        loopSource.mute = false;
        loopSource.spatialBlend = 0f;
        loopSource.ignoreListenerPause = true;
        loopSource.playOnAwake = false;

        if (forceEndCanvasVolumeToMax)
        {
            loopSource.volume = 1f;
        }
        else
        {
            float baseVolume = endCanvasBaseVolume;
            if (AudioHandler.Instance != null && AudioHandler.Instance.soundEffectsSource != null)
                baseVolume = AudioHandler.Instance.soundEffectsSource.volume;

            loopSource.volume = Mathf.Clamp01(baseVolume * Mathf.Max(1f, endCanvasVolumeMultiplier));
        }

        loopSource.clip = loopClip;
        loopSource.loop = true;
        loopSource.Play();
        isEndCanvasLoopActive = true;
    }

    void StopEndCanvasLoopAudio()
    {
        AudioSource loopSource = GetEndCanvasLoopAudioSource();
        if (loopSource == null)
            return;

        if (loopSource.isPlaying)
            loopSource.Stop();

        loopSource.loop = false;
        isEndCanvasLoopActive = false;
    }

    AudioSource GetEndCanvasLoopAudioSource()
    {
        EnsureEndCanvasLoopAudioSource();

        if (endCanvasLoopAudioSource != null)
            return endCanvasLoopAudioSource;

        return audioSource;
    }

    void EnsureEndCanvasLoopAudioSource()
    {
        if (endCanvasLoopAudioSource != null)
            return;

        if (audioSource != null)
        {
            endCanvasLoopAudioSource = audioSource;

            if (AudioHandler.Instance != null && endCanvasLoopAudioSource == AudioHandler.Instance.soundEffectsSource)
            {
                AudioSource dedicatedSource = gameObject.AddComponent<AudioSource>();
                dedicatedSource.playOnAwake = false;
                dedicatedSource.loop = false;
                dedicatedSource.spatialBlend = 0f;
                dedicatedSource.volume = 1f;
                dedicatedSource.ignoreListenerPause = true;
                endCanvasLoopAudioSource = dedicatedSource;
            }
            return;
        }

        endCanvasLoopAudioSource = gameObject.GetComponent<AudioSource>();
        if (endCanvasLoopAudioSource == null)
            endCanvasLoopAudioSource = gameObject.AddComponent<AudioSource>();

        endCanvasLoopAudioSource.playOnAwake = false;
        endCanvasLoopAudioSource.loop = false;
        endCanvasLoopAudioSource.spatialBlend = 0f;
        endCanvasLoopAudioSource.volume = Mathf.Clamp01(endCanvasBaseVolume);
        endCanvasLoopAudioSource.ignoreListenerPause = true;
        audioSource = endCanvasLoopAudioSource;
    }

    void LateUpdate()
    {
        if (!isEndCanvasLoopActive || !forceEndCanvasVolumeToMax)
            return;

        AudioSource loopSource = endCanvasLoopAudioSource;
        if (loopSource == null)
            return;

        if (loopSource.volume < 0.999f)
            loopSource.volume = 1f;

        if (loopSource.mute)
            loopSource.mute = false;
    }

    AudioClip GetWinningLoopClip()
    {
        if (playerWinAudio != null)
            return playerWinAudio;

        return victoryAudio;
    }

    AudioClip GetLosingLoopClip()
    {
        if (playerLoseAudio != null)
            return playerLoseAudio;

        return defeatAudio;
    }

    void OnDestroy()
    {
        StopEndCanvasLoopAudio();

        if (endingVideoPlayer != null && endingVideoPlayer.isPlaying)
        {
            endingVideoPlayer.Stop();
        }

        RestoreAudio();
    }
}