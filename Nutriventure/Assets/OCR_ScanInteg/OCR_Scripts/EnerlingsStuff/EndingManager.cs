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
    public GameObject endingCutsceneCanvas;
    public GameObject enerlingEndingCatchCanvas;
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
    public TextMeshProUGUI enerlingNameText;
    public TextMeshProUGUI kingdomText;
    public Button continueButton;

    [Header("Player Defeated UI")]
    public Button playerDefeatedContinueButton;

    [Header("Scene Names")]
    public string scanOCRSceneName = "ScanOCR";

    [Header("Transition Settings")]
    public float fadeDuration = 0.5f;
    public float videoFadeInDuration = 1f;

    // State
    private bool gameEnded = false;
    private GameObject spawnedPlayerEnerling;
    private GameObject spawnedAIEnerling;
    private Animator playerAnimator;
    private Animator aiAnimator;
    private IngredientDatabase.IngredientInfo defeatedAIEnerling;

    void Start()
    {
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

        // Get spawned enerling references
        spawnedPlayerEnerling = FindPlayerEnerling();
        spawnedAIEnerling = FindAIEnerling();

        if (spawnedPlayerEnerling != null)
            playerAnimator = spawnedPlayerEnerling.GetComponent<Animator>();

        if (spawnedAIEnerling != null)
            aiAnimator = spawnedAIEnerling.GetComponent<Animator>();
    }

    GameObject FindPlayerEnerling()
    {
        // Look for player enerling in the scene
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
        // Look for AI enerling in the scene
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

        // Check if either enerling has reached 0 life
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

        // Disable battlefield canvas
        if (battlefieldCanvas != null)
            battlefieldCanvas.SetActive(false);

        // Stop turn system
        if (turnSystem != null)
            turnSystem.Cleanup();

        // Set animator parameters
        SetAnimatorParameters(playerDead, aiDead);

        // Stop battle background audio
        StopBattleBackgroundAudio();

        // Mute all other audio sources
        MuteAllAudioExcept(null);

        if (playerDead)
        {
            // AI wins
            yield return StartCoroutine(HandlePlayerDefeated());
        }
        else if (aiDead)
        {
            // Player wins
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
        // Stop the battle background music and audience SFX
        if (AudioManagerBattleField.Instance != null)
        {
            AudioManagerBattleField.Instance.StopBattleAudio();
        }
    }

    IEnumerator HandlePlayerVictory()
    {
        Debug.Log("Player wins!");

        // Play victory audio
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

        // Check if AI enerling is already unlocked in database
        bool isAlreadyUnlocked = CheckIfAIEnerlingIsUnlocked();

        if (!isAlreadyUnlocked)
        {
            // Play ending cutscene video
            yield return StartCoroutine(PlayEndingCutscene());

            // Show EnerlingEndingCatch canvas
            yield return StartCoroutine(ShowEnerlingEndingCatch());
        }
        else
        {
            // Already unlocked, just go back to scan
            Debug.Log($"{defeatedAIEnerling.ingredientName} is already unlocked. Returning to scan.");
            ReturnToScanOCRScene();
        }
    }

    bool CheckIfAIEnerlingIsUnlocked()
    {
        if (defeatedAIEnerling == null) return false;

        // Check in database
        var dbEnerling = ingredientDatabase?.GetIngredientInfo(defeatedAIEnerling.ingredientName);
        if (dbEnerling != null)
        {
            return dbEnerling.isUnlocked;
        }

        // Check in PersistentDataManager
        if (PersistentDataManager.Instance != null)
        {
            return PersistentDataManager.Instance.IsEnerlingUnlocked(defeatedAIEnerling.ingredientName);
        }

        return false;
    }

    IEnumerator PlayEndingCutscene()
    {
        Debug.Log("Playing ending cutscene...");

        // Get the video clip from the defeated AI enerling
        if (defeatedAIEnerling == null || defeatedAIEnerling.endingCutscene == null)
        {
            Debug.LogWarning($"No ending cutscene video found for {defeatedAIEnerling?.ingredientName}");
            yield break;
        }

        // Set up video player
        if (endingVideoPlayer != null && videoRawImage != null)
        {
            // Configure video player based on your settings
            endingVideoPlayer.source = VideoSource.VideoClip;
            endingVideoPlayer.clip = defeatedAIEnerling.endingCutscene;
            endingVideoPlayer.playOnAwake = false; // We'll control when to play
            endingVideoPlayer.waitForFirstFrame = true;
            endingVideoPlayer.skipOnDrop = true;
            endingVideoPlayer.playbackSpeed = 1f;
            endingVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            endingVideoPlayer.targetTexture = videoRenderTexture;
            endingVideoPlayer.aspectRatio = VideoAspectRatio.FitVertically;
            endingVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            // Set the raw image texture
            videoRawImage.texture = videoRenderTexture;

            // Activate canvas with fade in
            if (endingCutsceneCanvas != null)
            {
                endingCutsceneCanvas.SetActive(true);
                CanvasGroup canvasGroup = endingCutsceneCanvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = endingCutsceneCanvas.AddComponent<CanvasGroup>();

                // Fade in
                float elapsed = 0f;
                while (elapsed < videoFadeInDuration)
                {
                    canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / videoFadeInDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                canvasGroup.alpha = 1;
            }

            // Mute other audio while video plays
            MuteAllAudioExcept(endingVideoPlayer.GetTargetAudioSource(0));

            // Play video
            endingVideoPlayer.Play();

            // Wait for video to finish
            while (endingVideoPlayer.isPlaying)
            {
                yield return null;
            }

            Debug.Log("Ending cutscene video completed");
        }

        // Fade out ending cutscene canvas
        if (endingCutsceneCanvas != null)
        {
            CanvasGroup canvasGroup = endingCutsceneCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                canvasGroup.alpha = 0;
            }
            endingCutsceneCanvas.SetActive(false);
        }
    }

    IEnumerator ShowEnerlingEndingCatch()
    {
        Debug.Log("Showing EnerlingEndingCatch canvas...");

        // Update UI with defeated AI enerling info
        UpdateEnerlingEndingCatchUI();

        // Activate canvas with fade in
        if (enerlingEndingCatchCanvas != null)
        {
            enerlingEndingCatchCanvas.SetActive(true);
            CanvasGroup canvasGroup = enerlingEndingCatchCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = enerlingEndingCatchCanvas.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1;
        }
    }

    void UpdateEnerlingEndingCatchUI()
    {
        if (defeatedAIEnerling == null) return;

        // Set enerling name
        if (enerlingNameText != null)
            enerlingNameText.text = defeatedAIEnerling.ingredientName;

        // Set kingdom
        if (kingdomText != null)
            kingdomText.text = defeatedAIEnerling.kingdom.ToString();

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

        // Set kingdom sprite (you'll need to implement GetKingdomSprite)
        if (kingdomSpriteImage != null)
        {
            Sprite kingdomSprite = GetKingdomSprite(defeatedAIEnerling.kingdom);
            if (kingdomSprite != null)
            {
                kingdomSpriteImage.sprite = kingdomSprite;
                kingdomSpriteImage.preserveAspect = true;
            }
        }
    }

    Sprite GetKingdomSprite(IngredientDatabase.KingdomOrigin kingdom)
    {
        // You'll need to implement this based on your kingdom sprites
        // This should return the appropriate sprite for each kingdom
        // For now, return null - you should assign these in the inspector
        return null;
    }

    void OnEnerlingEndingCatchContinue()
    {
        Debug.Log("EnerlingEndingCatch continue button clicked");

        // Unlock the AI enerling in database
        if (defeatedAIEnerling != null)
        {
            if (PersistentDataManager.Instance != null)
            {
                PersistentDataManager.Instance.UnlockEnerling(defeatedAIEnerling.ingredientName);
            }

            // Also update the database directly
            if (ingredientDatabase != null)
            {
                var dbEnerling = ingredientDatabase.GetIngredientInfo(defeatedAIEnerling.ingredientName);
                if (dbEnerling != null)
                {
                    dbEnerling.isUnlocked = true;
                }
            }

            Debug.Log($"Unlocked {defeatedAIEnerling.ingredientName} in database");
        }

        // Return to scanOCR scene
        ReturnToScanOCRScene();
    }

    IEnumerator HandlePlayerDefeated()
    {
        Debug.Log("Player defeated!");

        // Play defeat audio
        if (audioSource != null && defeatAudio != null)
        {
            audioSource.clip = defeatAudio;
            audioSource.Play();
        }

        // Wait a moment
        yield return new WaitForSeconds(1f);

        // Show player defeated canvas with fade in
        if (playerDefeatedCanvas != null)
        {
            playerDefeatedCanvas.SetActive(true);
            CanvasGroup canvasGroup = playerDefeatedCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = playerDefeatedCanvas.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1;
        }
    }

    void OnPlayerDefeatedContinue()
    {
        Debug.Log("Player defeated continue button clicked");
        ReturnToScanOCRScene();
    }

    void ReturnToScanOCRScene()
    {
        // Restore audio
        RestoreAudio();

        // Load scanOCR scene
        if (!string.IsNullOrEmpty(scanOCRSceneName))
        {
            SceneManager.LoadScene(scanOCRSceneName);
        }
        else
        {
            Debug.LogError("ScanOCR scene name not set!");
        }
    }

    void MuteAllAudioExcept(AudioSource exception)
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
        // Cleanup
        if (endingVideoPlayer != null && endingVideoPlayer.isPlaying)
        {
            endingVideoPlayer.Stop();
        }

        RestoreAudio();
    }
}