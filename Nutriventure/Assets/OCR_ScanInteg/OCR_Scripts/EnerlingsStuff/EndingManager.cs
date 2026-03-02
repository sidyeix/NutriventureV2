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

        if (audioSource != null && victoryAudio != null)
        {
            audioSource.clip = victoryAudio;
            audioSource.Play();
        }

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

        if (defeatedAIEnerling == null)
        {
            Debug.LogError("defeatedAIEnerling is null!");
            yield break;
        }

        if (defeatedAIEnerling.endingCutscene == null)
        {
            Debug.LogError($"endingCutscene is null for {defeatedAIEnerling.ingredientName}");
            yield break;
        }

        if (endingVideoPlayer == null)
        {
            Debug.LogError("endingVideoPlayer is null! Cannot play video.");
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
            yield return null;
        }

        Debug.Log("Ending cutscene video completed");

        // Fade out the canvas
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

        UpdateEnerlingEndingCatchUI();

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

    IEnumerator HandlePlayerDefeated()
    {
        Debug.Log("Player defeated!");

        if (audioSource != null && defeatAudio != null)
        {
            audioSource.clip = defeatAudio;
            audioSource.Play();
        }

        yield return new WaitForSeconds(1f);

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