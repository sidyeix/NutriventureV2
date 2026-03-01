using Cinemachine;
using System.Collections;
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

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip playerWinAudio;
    public AudioClip playerLoseAudio;
    public AudioClip victoryAudio;
    public AudioClip defeatAudio;

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

    [Header("Player Defeated UI")]
    public Button playerDefeatedContinueButton;

    [Header("Scene Names")]
    public string scanOCRSceneName = "ScanOCR";

    [Header("Fade Settings")]
    public float fadeInDuration = 0.5f;      // Time to fade in
    public float fadeOutDuration = 0.5f;      // Time to fade out
    public float videoFadeInDuration = 0.8f;  // Slower fade for video
    public float videoFadeOutDuration = 0.5f; // Fade out after video

    [Header("Kingdom Sprites")]
    public Sprite nutriKingdomSprite;
    public Sprite alerthiaSprite;
    public Sprite sugariaSprite;
    public Sprite preserviaSprite;

    [Header("Audio Listener")]
    public AudioListener audioListener;

    // State
    private bool gameEnded = false;
    private GameObject spawnedPlayerEnerling;
    private GameObject spawnedAIEnerling;
    private Animator playerAnimator;
    private Animator aiAnimator;
    private IngredientDatabase.IngredientInfo defeatedAIEnerling;

    void Start()
    {
        EnsureSingleAudioListener();
        InitializeReferences();
        SetupButtonListeners();

        // Initially disable all ending canvases
        if (endingCutsceneCanvas != null)
            endingCutsceneCanvas.SetActive(false);

        if (enerlingEndingCatchCanvas != null)
            enerlingEndingCatchCanvas.SetActive(false);

        if (playerDefeatedCanvas != null)
            playerDefeatedCanvas.SetActive(false);
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

        // Fade out battlefield canvas if it exists
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

        if (audioSource != null && victoryAudio != null)
        {
            audioSource.clip = victoryAudio;
            audioSource.Play();
        }

        // Get the defeated AI enerling
        defeatedAIEnerling = aiManager?.GetAIEnerling();
        if (defeatedAIEnerling == null)
        {
            Debug.LogError("Defeated AI enerling not found!");
            yield break;
        }

        Debug.Log($"Defeated enerling: {defeatedAIEnerling.ingredientName}");
        Debug.Log($"Has ending cutscene: {defeatedAIEnerling.endingCutscene != null}");

        if (defeatedAIEnerling.endingCutscene != null)
        {
            Debug.Log($"Ending cutscene name: {defeatedAIEnerling.endingCutscene.name}");
        }

        // Set EnerlingDefeat camera priority
        if (enerlingDefeatCamera != null)
        {
            enerlingDefeatCamera.Priority = 30;
            Debug.Log("EnerlingDefeat camera priority set to 30");
        }

        // Play winning timeline
        if (winningTimelineDirector != null && winningTimelineAsset != null)
        {
            winningTimelineDirector.playableAsset = winningTimelineAsset;
            winningTimelineDirector.time = 0;
            winningTimelineDirector.Play();

            // Wait for timeline to complete
            while (winningTimelineDirector.state == PlayState.Playing)
            {
                yield return null;
            }
            Debug.Log("Winning timeline completed");
        }

        // STEP 1: Play ending cutscene video (First canvas with VideoPlayer)
        if (defeatedAIEnerling.endingCutscene != null)
        {
            yield return StartCoroutine(PlayEndingCutscene());
        }
        else
        {
            Debug.LogWarning($"No ending cutscene assigned for {defeatedAIEnerling.ingredientName} - skipping video");
        }

        // STEP 2: Show EnerlingEndingCatch canvas (Second canvas with UI)
        yield return StartCoroutine(ShowEnerlingEndingCatch());
    }

    IEnumerator PlayEndingCutscene()
    {
        Debug.Log("=== Playing ending cutscene video ===");

        if (defeatedAIEnerling == null)
        {
            Debug.LogError("defeatedAIEnerling is null!");
            yield break;
        }

        // Double-check video clip
        if (defeatedAIEnerling.endingCutscene == null)
        {
            Debug.LogError($"endingCutscene is null for {defeatedAIEnerling.ingredientName} even though we checked!");
            yield break;
        }

        // Verify all required components
        if (endingVideoPlayer == null)
        {
            Debug.LogError("endingVideoPlayer is null! Cannot play video.");
            yield break;
        }

        if (videoRawImage == null)
        {
            Debug.LogError("videoRawImage is null! Cannot display video.");
            yield break;
        }

        if (videoRenderTexture == null)
        {
            Debug.LogError("videoRenderTexture is null! Cannot render video.");
            yield break;
        }

        if (endingCutsceneCanvas == null)
        {
            Debug.LogError("endingCutsceneCanvas is null! Cannot show video canvas.");
            yield break;
        }

        Debug.Log($"Setting video clip to: {defeatedAIEnerling.endingCutscene.name} for enerling: {defeatedAIEnerling.ingredientName}");

        // Stop any currently playing video
        if (endingVideoPlayer.isPlaying)
        {
            endingVideoPlayer.Stop();
        }

        // IMPORTANT: Set the video clip from the defeated enerling
        endingVideoPlayer.clip = defeatedAIEnerling.endingCutscene;

        // Configure video player
        endingVideoPlayer.source = VideoSource.VideoClip;
        endingVideoPlayer.playOnAwake = false;
        endingVideoPlayer.waitForFirstFrame = true;
        endingVideoPlayer.isLooping = false;
        endingVideoPlayer.skipOnDrop = true;
        endingVideoPlayer.playbackSpeed = 1f;
        endingVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        endingVideoPlayer.targetTexture = videoRenderTexture;
        endingVideoPlayer.aspectRatio = VideoAspectRatio.FitVertically;
        endingVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        // Verify the clip was set
        Debug.Log($"VideoPlayer clip after assignment: {(endingVideoPlayer.clip != null ? endingVideoPlayer.clip.name : "NULL")}");

        // Set the raw image texture
        videoRawImage.texture = videoRenderTexture;
        Debug.Log($"RawImage texture set to: {videoRawImage.texture}");

        // Prepare video
        Debug.Log("Preparing video...");
        endingVideoPlayer.Prepare();

        // Wait for video to prepare with timeout
        float prepareTimeout = 5f;
        float prepareTimer = 0f;

        while (!endingVideoPlayer.isPrepared && prepareTimer < prepareTimeout)
        {
            prepareTimer += Time.deltaTime;
            if (prepareTimer % 1f < 0.1f)
                Debug.Log($"Preparing video... {prepareTimer:F1}s");
            yield return null;
        }

        if (!endingVideoPlayer.isPrepared)
        {
            Debug.LogError("Video preparation timed out!");
            yield break;
        }

        Debug.Log("Video prepared, playing now...");

        // FADE IN: Activate and fade in the canvas
        endingCutsceneCanvas.SetActive(true);
        yield return StartCoroutine(FadeInCanvas(endingCutsceneCanvas, videoFadeInDuration));

        // Mute other audio while video plays
        MuteAllAudioExcept(endingVideoPlayer);

        // Play video
        endingVideoPlayer.Play();
        Debug.Log($"Video is playing: {endingVideoPlayer.isPlaying}");

        // Wait for video to start playing
        yield return new WaitForSeconds(0.5f);

        if (!endingVideoPlayer.isPlaying)
        {
            Debug.LogError("Video failed to start playing!");
            yield return StartCoroutine(FadeOutCanvas(endingCutsceneCanvas, fadeOutDuration));
            endingCutsceneCanvas.SetActive(false);
            RestoreAudio();
            yield break;
        }

        Debug.Log($"Video successfully playing. Length: {endingVideoPlayer.clip.length} seconds");

        // Wait for video to finish
        while (endingVideoPlayer.isPlaying)
        {
            yield return null;
        }

        Debug.Log("Ending cutscene video completed");

        // FADE OUT: Fade out the canvas
        yield return StartCoroutine(FadeOutCanvas(endingCutsceneCanvas, videoFadeOutDuration));
        endingCutsceneCanvas.SetActive(false);

        // Restore audio
        RestoreAudio();
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

        // Update UI with defeated AI enerling info
        UpdateEnerlingEndingCatchUI();

        // FADE IN: Activate and fade in the canvas
        enerlingEndingCatchCanvas.SetActive(true);
        yield return StartCoroutine(FadeInCanvas(enerlingEndingCatchCanvas, fadeInDuration));

        Debug.Log("EnerlingEndingCatch canvas is now visible");
    }

    void UpdateEnerlingEndingCatchUI()
    {
        if (defeatedAIEnerling == null) return;

        Debug.Log($"Updating EnerlingEndingCatch UI for {defeatedAIEnerling.ingredientName}");

        // Set enerling name
        if (enerlingNameText != null)
            enerlingNameText.text = defeatedAIEnerling.ingredientName;

        // Set kingdom name
        if (kingdomText != null)
            kingdomText.text = defeatedAIEnerling.kingdom.ToString();

        // Set the enerling icon/image
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

        // Set frame based on rarity
        if (enerlingFrameImage != null && ingredientDatabase != null)
        {
            Sprite frameSprite = ingredientDatabase.GetFrameSprite(defeatedAIEnerling.rarity);
            if (frameSprite != null)
            {
                enerlingFrameImage.sprite = frameSprite;
                enerlingFrameImage.preserveAspect = true;
            }
        }

        // Set rarity tag
        if (rarityTagImage != null && ingredientDatabase != null)
        {
            Sprite raritySprite = ingredientDatabase.GetRarityIcon(defeatedAIEnerling.rarity);
            if (raritySprite != null)
            {
                rarityTagImage.sprite = raritySprite;
                rarityTagImage.preserveAspect = true;
            }
        }

        // Set kingdom flag image
        if (kingdomSpriteImage != null)
        {
            Sprite kingdomSprite = GetKingdomSprite(defeatedAIEnerling.kingdom);
            if (kingdomSprite != null)
            {
                kingdomSpriteImage.sprite = kingdomSprite;
                kingdomSpriteImage.preserveAspect = true;
            }
        }

        // Show "Unlocked" text
        if (unlockedText != null)
        {
            unlockedText.SetActive(true);
        }
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
        // Fade out the catch canvas
        if (enerlingEndingCatchCanvas != null)
        {
            yield return StartCoroutine(FadeOutCanvas(enerlingEndingCatchCanvas, fadeOutDuration));
            enerlingEndingCatchCanvas.SetActive(false);
        }

        if (defeatedAIEnerling != null)
        {
            // Unlock in PersistentDataManager
            if (PersistentDataManager.Instance != null)
            {
                PersistentDataManager.Instance.UnlockEnerling(defeatedAIEnerling.ingredientName);
                Debug.Log($"Unlocked {defeatedAIEnerling.ingredientName} via PersistentDataManager");
            }

            // Also update the database directly
            if (ingredientDatabase != null)
            {
                var dbEnerling = ingredientDatabase.GetIngredientInfo(defeatedAIEnerling.ingredientName);
                if (dbEnerling != null)
                {
                    dbEnerling.isUnlocked = true;
                    Debug.Log($"Updated isUnlocked in database for {defeatedAIEnerling.ingredientName}");
                }
            }

            // Save current life
            if (PersistentDataManager.Instance != null)
            {
                PersistentDataManager.Instance.SaveEnerlingCurrentLife(
                    defeatedAIEnerling.ingredientName,
                    defeatedAIEnerling.baseLife
                );
            }
        }

        // Return to scanOCR scene
        ReturnToScanOCRScene();
    }

    IEnumerator HandlePlayerDefeated()
    {
        Debug.Log("Player defeated!");

        if (audioSource != null && defeatAudio != null)
        {
            audioSource.clip = defeatAudio;
            audioSource.Play();
        }

        yield return new WaitForSeconds(1f);

        // FADE IN: Show player defeated canvas
        if (playerDefeatedCanvas != null)
        {
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
        // Fade out player defeated canvas
        if (playerDefeatedCanvas != null)
        {
            yield return StartCoroutine(FadeOutCanvas(playerDefeatedCanvas, fadeOutDuration));
            playerDefeatedCanvas.SetActive(false);
        }

        ReturnToScanOCRScene();
    }

    void ReturnToScanOCRScene()
    {
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

    // ==================== FADE HELPER METHODS ====================

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

    void OnDestroy()
    {
        if (endingVideoPlayer != null && endingVideoPlayer.isPlaying)
        {
            endingVideoPlayer.Stop();
        }

        RestoreAudio();
    }
}