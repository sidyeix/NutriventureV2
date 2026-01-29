using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Cinemachine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;

public class BattlePlayManager : MonoBehaviour
{
    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase;

    [Header("Playable Director & Assets")]
    public PlayableDirector playableDirector;  // Single PlayableDirector

    [Header("Timeline Assets")]
    public PlayableAsset nutriKingdomTimeline;
    public PlayableAsset sugariaTimeline;
    public PlayableAsset alerthiaTimeline;
    public PlayableAsset preserviaTimeline;

    [Header("Camera References")]
    public CinemachineVirtualCamera groceryCamera;  // Main grocery store camera
    public CinemachineVirtualCamera battleFocusCamera;  // Battle focus camera
    public CinemachineVirtualCamera nutriKingdomCam;
    public CinemachineVirtualCamera alerthiaCam;
    public CinemachineVirtualCamera sugariaCam;
    public CinemachineVirtualCamera preserviaCam;

    [Header("Canvas References")]
    public GameObject catchEnerlingCanvas;  // Canvas to disable during timeline
    public GameObject enerlingInfoCanvas;  // Opponent info canvas
    public GameObject enerlingPickingCanvas;  // Player selection canvas (from EnerlingSelectionManager)

    [Header("UI References")]
    public TextMeshProUGUI enerlingNameText;
    public Image kingdomOriginImage;
    public TextMeshProUGUI kingdomOriginText;
    public Button skipButton;
    public Button catchFightButton;
    public TextMeshProUGUI catchFightButtonText;

    [Header("Rarity Visuals - Frames")]
    public Image enerlingFrameImage;
    public Sprite commonFrameSprite;
    public Sprite rareFrameSprite;
    public Sprite ultraRareFrameSprite;

    [Header("Rarity Visuals - Tags")]
    public Image rarityTagImage;
    public Sprite commonRaritySprite;
    public Sprite rareRaritySprite;
    public Sprite ultraRareRaritySprite;

    [Header("Kingdom Sprites")]
    public Sprite nutriKingdomSprite;
    public Sprite alerthiaSprite;
    public Sprite sugariaSprite;
    public Sprite preserviaSprite;

    [Header("Spawn Points")]
    public Transform nutriKingdomSpawn;
    public Transform alerthiaSpawn;
    public Transform sugariaSpawn;
    public Transform preserviaSpawn;

    [Header("Battle Managers")]
    public BattleEnerlingManager battleManager;
    public AIEnerlingManager aiManager;
    public PlayerEnerlingManager playerManager;
    public TurnSystem turnSystem;

    [Header("Scene Names")]
    public string scanOCRSceneName = "ScanOCR";  // Scene to return to when skipping

    [Header("Settings")]
    public bool muteAllAudioOnStart = true;
    public bool stopAllAudioImmediately = true;

    // State
    private IngredientDatabase.IngredientInfo opponentEnerling;
    private GameObject spawnedOpponent;
    private PlayableAsset currentTimeline;
    private bool timelinePlaying = false;
    private bool timelineAudioPrepared = false;
    private bool isUnlocked = false;  // Track if opponent enerling is unlocked

    // Store original camera settings
    private CinemachineBlendDefinition originalBattleFocusBlend;

    void Start()
    {
        // Store original blend settings for later restoration
        if (battleFocusCamera != null)
        {
            CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                originalBattleFocusBlend = brain.m_DefaultBlend;
            }
        }

        // Initialize audio control
        InitializeAudioControl();

        // Disable CatchEnerlingCanvas initially
        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(false);

        // Disable enerling info canvas
        if (enerlingInfoCanvas != null)
            enerlingInfoCanvas.SetActive(false);

        // Disable picking canvas
        if (enerlingPickingCanvas != null)
            enerlingPickingCanvas.SetActive(false);

        // Start the battle sequence
        StartCoroutine(InitializeBattleScene());
    }

    void InitializeAudioControl()
    {
        if (playableDirector == null)
        {
            playableDirector = GetComponent<PlayableDirector>();
            if (playableDirector == null)
            {
                Debug.LogError("No PlayableDirector found!");
                return;
            }
        }

        // Set to manual update mode to prevent auto-playing
        playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;

        // Stop and reset immediately
        playableDirector.Stop();
        playableDirector.time = 0;
        playableDirector.Evaluate();

        // Mute all timeline audio tracks
        if (playableDirector.playableAsset is TimelineAsset timeline)
        {
            MuteAllAudioTracks(timeline, true);
        }

        // Additional safety: Stop all AudioSources in the scene
        if (stopAllAudioImmediately)
        {
            StopAllAudioSourcesInScene();
        }

        Debug.Log("Timeline audio control initialized");
    }

    void StopAllAudioSourcesInScene()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>(true);
        foreach (AudioSource audioSource in allAudioSources)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.time = 0;
            }

            if (muteAllAudioOnStart)
            {
                audioSource.mute = true;
            }
        }
    }

    void MuteAllAudioTracks(TimelineAsset timeline, bool mute)
    {
        if (timeline == null) return;

        var audioTracks = timeline.GetOutputTracks()
            .Where(track => track is AudioTrack)
            .Cast<AudioTrack>();

        foreach (AudioTrack audioTrack in audioTracks)
        {
            audioTrack.muted = mute;

            AudioSource audioSource = playableDirector.GetGenericBinding(audioTrack) as AudioSource;
            if (audioSource != null)
            {
                audioSource.mute = mute;
                if (mute)
                {
                    audioSource.Stop();
                    audioSource.time = 0;
                }
            }
        }
    }

    IEnumerator InitializeBattleScene()
    {
        yield return new WaitForSeconds(0.5f);

        // Get opponent enerling from PersistentDataManager
        string opponentName = "";
        if (PersistentDataManager.Instance != null)
        {
            opponentName = PersistentDataManager.Instance.GetOpponentEnerlingName();
            Debug.Log($"Loaded opponent enerling from PersistentData: {opponentName}");
        }

        // If no opponent found, use random
        if (string.IsNullOrEmpty(opponentName))
        {
            opponentName = GetRandomEnerlingName();
            Debug.LogWarning("No opponent found in PersistentData. Using random: " + opponentName);

            // Save to PersistentDataManager
            if (PersistentDataManager.Instance != null)
            {
                PersistentDataManager.Instance.SaveOpponentEnerling(opponentName);
            }
        }

        // Load opponent data from database
        opponentEnerling = ingredientDatabase.GetIngredientInfo(opponentName);
        if (opponentEnerling == null)
        {
            Debug.LogError($"Could not find enerling '{opponentName}' in database. Using first available.");
            opponentEnerling = ingredientDatabase.ingredients[0];

            if (PersistentDataManager.Instance != null)
            {
                PersistentDataManager.Instance.SaveOpponentEnerling(opponentEnerling.ingredientName);
            }
        }

        // Check if opponent is unlocked
        isUnlocked = opponentEnerling.isUnlocked;
        Debug.Log($"Battle against: {opponentEnerling.ingredientName} from {opponentEnerling.kingdom}, Unlocked: {isUnlocked}");

        // Update UI and start introduction
        UpdateEnerlingInfoUI();
        UpdateRarityVisuals();
        UpdateCatchFightButtonText();

        yield return StartCoroutine(PlayIntroductionSequence());
    }

    IEnumerator PlayIntroductionSequence()
    {
        // STEP 1: Disable CatchEnerlingCanvas before timeline starts
        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(false);

        // STEP 2: Set ALL cameras to priority 0 initially
        SetAllCamerasPriority(0);

        // STEP 3: Set the specific kingdom camera priority to 20 based on opponent's origin
        SetKingdomCameraPriorityByOrigin(opponentEnerling.kingdom, 20);

        // STEP 4: Spawn opponent MODEL ONLY (no AI initialization yet)
        SpawnOpponentModel();

        // Wait for spawn
        yield return new WaitForSeconds(0.5f);

        // STEP 5: Play timeline based on kingdom
        yield return StartCoroutine(PlayKingdomTimeline());

        // Wait for timeline to complete
        yield return new WaitForSeconds(1f);

        // STEP 6: After timeline ends, keep the kingdom camera active (priority 20)
        SetKingdomCameraPriorityByOrigin(opponentEnerling.kingdom, 20);

        // STEP 7: Enable CatchEnerlingCanvas
        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(true);

        // STEP 8: Show enerling info canvas
        if (enerlingInfoCanvas != null)
        {
            enerlingInfoCanvas.SetActive(true);
            CanvasGroup canvasGroup = enerlingInfoCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                float fadeTime = 0.5f;
                float elapsed = 0;
                while (elapsed < fadeTime)
                {
                    canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeTime);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                canvasGroup.alpha = 1;
            }
        }

        // STEP 9: Setup button listeners
        SetupButtonListeners();

        timelinePlaying = true;
    }

    void SpawnOpponentModel()
    {
        if (opponentEnerling == null || opponentEnerling.modelPrefab == null)
        {
            Debug.LogError("Cannot spawn opponent: no model prefab");
            return;
        }

        Transform spawnPoint = GetKingdomSpawnPoint(opponentEnerling.kingdom);
        if (spawnPoint == null)
        {
            Debug.LogError($"No spawn point for kingdom: {opponentEnerling.kingdom}");
            return;
        }

        // Clean up any existing spawned opponent
        if (spawnedOpponent != null)
            Destroy(spawnedOpponent);

        // Spawn new opponent MODEL ONLY (no AI components yet)
        spawnedOpponent = Instantiate(opponentEnerling.modelPrefab, spawnPoint);
        spawnedOpponent.transform.localPosition = Vector3.zero;
        spawnedOpponent.transform.localRotation = Quaternion.identity;
        spawnedOpponent.transform.localScale = Vector3.one;

        Debug.Log($"Spawned opponent MODEL: {opponentEnerling.ingredientName} at {opponentEnerling.kingdom} spawn point");
    }

    Transform GetKingdomSpawnPoint(IngredientDatabase.KingdomOrigin kingdom)
    {
        switch (kingdom)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom:
                return nutriKingdomSpawn;
            case IngredientDatabase.KingdomOrigin.Alerthia:
                return alerthiaSpawn;
            case IngredientDatabase.KingdomOrigin.Sugaria:
                return sugariaSpawn;
            case IngredientDatabase.KingdomOrigin.Preservia:
                return preserviaSpawn;
            default:
                return nutriKingdomSpawn;
        }
    }

    IEnumerator PlayKingdomTimeline()
    {
        if (playableDirector == null)
            yield break;

        StopTimelineImmediately();
        currentTimeline = GetKingdomTimelineAsset(opponentEnerling.kingdom);

        if (currentTimeline != null)
        {
            playableDirector.playableAsset = currentTimeline;
            PrepareTimelineAudio(currentTimeline);
            yield return null;
            playableDirector.timeUpdateMode = DirectorUpdateMode.GameTime;
            playableDirector.Play();
            playableDirector.stopped += OnTimelineFinished;
        }
    }

    PlayableAsset GetKingdomTimelineAsset(IngredientDatabase.KingdomOrigin kingdom)
    {
        switch (kingdom)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom:
                return nutriKingdomTimeline;
            case IngredientDatabase.KingdomOrigin.Alerthia:
                return alerthiaTimeline;
            case IngredientDatabase.KingdomOrigin.Sugaria:
                return sugariaTimeline;
            case IngredientDatabase.KingdomOrigin.Preservia:
                return preserviaTimeline;
            default:
                return nutriKingdomTimeline;
        }
    }

    void PrepareTimelineAudio(PlayableAsset timelineAsset)
    {
        if (playableDirector == null || timelineAsset == null) return;

        if (timelineAsset is TimelineAsset timeline)
        {
            MuteAllAudioTracks(timeline, false);

            var audioTracks = timeline.GetOutputTracks()
                .Where(track => track is AudioTrack)
                .Cast<AudioTrack>();

            foreach (AudioTrack audioTrack in audioTracks)
            {
                AudioSource audioSource = playableDirector.GetGenericBinding(audioTrack) as AudioSource;
                if (audioSource != null)
                {
                    audioSource.mute = false;
                    audioSource.Stop();
                    audioSource.time = 0;
                }
            }
        }

        timelineAudioPrepared = true;
    }

    void StopTimelineImmediately()
    {
        if (playableDirector == null) return;

        playableDirector.stopped -= OnTimelineFinished;
        playableDirector.Stop();
        playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
        playableDirector.time = 0;
        playableDirector.Evaluate();

        if (playableDirector.playableAsset is TimelineAsset currentTimeline)
        {
            MuteAllAudioTracks(currentTimeline, true);
        }

        timelinePlaying = false;
        timelineAudioPrepared = false;
    }

    void OnTimelineFinished(PlayableDirector director)
    {
        if (director == playableDirector)
        {
            timelinePlaying = false;
            playableDirector.stopped -= OnTimelineFinished;
        }
    }

    void SetAllCamerasPriority(int priority)
    {
        if (groceryCamera != null)
        {
            groceryCamera.Priority = priority;
        }

        if (battleFocusCamera != null)
        {
            battleFocusCamera.Priority = priority;
        }

        if (nutriKingdomCam != null)
        {
            nutriKingdomCam.Priority = priority;
        }

        if (alerthiaCam != null)
        {
            alerthiaCam.Priority = priority;
        }

        if (sugariaCam != null)
        {
            sugariaCam.Priority = priority;
        }

        if (preserviaCam != null)
        {
            preserviaCam.Priority = priority;
        }

        Debug.Log($"Set all cameras priority to: {priority}");
    }

    void SetKingdomCameraPriorityByOrigin(IngredientDatabase.KingdomOrigin kingdom, int priority)
    {
        // First, set all kingdom cameras to 0
        if (nutriKingdomCam != null) nutriKingdomCam.Priority = 0;
        if (alerthiaCam != null) alerthiaCam.Priority = 0;
        if (sugariaCam != null) sugariaCam.Priority = 0;
        if (preserviaCam != null) preserviaCam.Priority = 0;

        // Set grocery and battle focus cameras to 0 as well
        if (groceryCamera != null) groceryCamera.Priority = 0;
        if (battleFocusCamera != null) battleFocusCamera.Priority = 0;

        // Then set the specific kingdom camera to the desired priority
        switch (kingdom)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom:
                if (nutriKingdomCam != null) nutriKingdomCam.Priority = priority;
                Debug.Log($"Set NutriKingdom camera priority to {priority}");
                break;
            case IngredientDatabase.KingdomOrigin.Alerthia:
                if (alerthiaCam != null) alerthiaCam.Priority = priority;
                Debug.Log($"Set Alerthia camera priority to {priority}");
                break;
            case IngredientDatabase.KingdomOrigin.Sugaria:
                if (sugariaCam != null) sugariaCam.Priority = priority;
                Debug.Log($"Set Sugaria camera priority to {priority}");
                break;
            case IngredientDatabase.KingdomOrigin.Preservia:
                if (preserviaCam != null) preserviaCam.Priority = priority;
                Debug.Log($"Set Preservia camera priority to {priority}");
                break;
            default:
                if (nutriKingdomCam != null) nutriKingdomCam.Priority = priority;
                Debug.Log($"Unknown kingdom, defaulted to NutriKingdom camera priority {priority}");
                break;
        }
    }

    void UpdateEnerlingInfoUI()
    {
        if (enerlingNameText != null)
            enerlingNameText.text = opponentEnerling.ingredientName;

        if (kingdomOriginText != null)
            kingdomOriginText.text = opponentEnerling.kingdom.ToString();

        if (kingdomOriginImage != null)
        {
            Sprite kingdomSprite = GetKingdomSprite(opponentEnerling.kingdom);
            if (kingdomSprite != null)
            {
                kingdomOriginImage.sprite = kingdomSprite;
                kingdomOriginImage.preserveAspect = true;
            }
        }
    }

    void UpdateRarityVisuals()
    {
        if (enerlingFrameImage != null)
        {
            Sprite frameSprite = GetFrameSpriteByRarity(opponentEnerling.rarity);
            if (frameSprite != null)
            {
                enerlingFrameImage.sprite = frameSprite;
                enerlingFrameImage.preserveAspect = true;
            }
        }

        if (rarityTagImage != null)
        {
            Sprite raritySprite = GetRaritySpriteByRarity(opponentEnerling.rarity);
            if (raritySprite != null)
            {
                rarityTagImage.sprite = raritySprite;
                rarityTagImage.preserveAspect = true;
            }
        }
    }

    Sprite GetFrameSpriteByRarity(IngredientDatabase.Rarity rarity)
    {
        switch (rarity)
        {
            case IngredientDatabase.Rarity.Common:
                return commonFrameSprite;
            case IngredientDatabase.Rarity.Rare:
                return rareFrameSprite;
            case IngredientDatabase.Rarity.UltraRare:
                return ultraRareFrameSprite;
            default:
                return commonFrameSprite;
        }
    }

    Sprite GetRaritySpriteByRarity(IngredientDatabase.Rarity rarity)
    {
        switch (rarity)
        {
            case IngredientDatabase.Rarity.Common:
                return commonRaritySprite;
            case IngredientDatabase.Rarity.Rare:
                return rareRaritySprite;
            case IngredientDatabase.Rarity.UltraRare:
                return ultraRareRaritySprite;
            default:
                return commonRaritySprite;
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

    void UpdateCatchFightButtonText()
    {
        if (catchFightButtonText != null)
        {
            // Set text based on isUnlocked status
            catchFightButtonText.text = isUnlocked ? "Fight" : "Catch";
        }
    }

    void SetupButtonListeners()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        if (catchFightButton != null)
        {
            catchFightButton.onClick.RemoveAllListeners();
            catchFightButton.onClick.AddListener(OnCatchFightButtonClicked);
        }
    }

    // ==================== BUTTON HANDLERS ====================

    void OnSkipButtonClicked()
    {
        // Go back to ScanOCR scene
        if (!string.IsNullOrEmpty(scanOCRSceneName))
        {
            SceneManager.LoadScene(scanOCRSceneName);
        }
        else
        {
            Debug.LogError("ScanOCR scene name not set!");
        }
    }

    void OnCatchFightButtonClicked()
    {
        StartCoroutine(StartBattleSequence());
    }

    IEnumerator StartBattleSequence()
    {
        Debug.Log("Fight/Catch button clicked - starting battle sequence");

        // Hide opponent info canvas with fade
        if (enerlingInfoCanvas != null)
        {
            CanvasGroup canvasGroup = enerlingInfoCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                float fadeTime = 0.3f;
                float elapsed = 0;
                while (elapsed < fadeTime)
                {
                    canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeTime);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                canvasGroup.alpha = 0;
            }
            enerlingInfoCanvas.SetActive(false);
        }

        // Disable CatchEnerlingCanvas
        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(false);

        // YOUR FIX: DISABLE THE PLAYABLE DIRECTOR GAMEOBJECT
        if (playableDirector != null)
        {
            Debug.Log("Disabling Playable Director GameObject to release camera control");
            playableDirector.gameObject.SetActive(false); // This stops timeline from controlling cameras
        }

        // STEP 1: Switch to BattleFocus camera
        Debug.Log("Switching to BattleFocus camera");

        // Set all cameras to priority 0
        SetAllCamerasPriority(0);

        // Set BattleFocus camera to priority 20
        if (battleFocusCamera != null)
        {
            battleFocusCamera.Priority = 20;
            Debug.Log($"BattleFocus camera priority set to: {battleFocusCamera.Priority}");
        }
        else
        {
            Debug.LogError("BattleFocus camera not assigned!");
        }

        // Wait for camera to switch
        yield return new WaitForSeconds(0.5f);

        // Debug which camera is active
        CheckActiveCamera();

        // STEP 2: Enable selection canvas
        if (enerlingPickingCanvas != null)
        {
            Debug.Log("Enabling player selection canvas");
            enerlingPickingCanvas.SetActive(true);

            // Find and initialize the EnerlingSelectionManager
            EnerlingSelectionManager selectionManager = enerlingPickingCanvas.GetComponent<EnerlingSelectionManager>();
            if (selectionManager != null)
            {
                Debug.Log("EnerlingSelectionManager ready for selection");
            }
        }
        else
        {
            Debug.LogError("EnerlingPickingCanvas not assigned!");
        }
    }

    // Coroutine version of ForceCameraUpdate
    IEnumerator ForceCameraUpdateCoroutine()
    {
        Debug.Log("Forcing camera system update...");

        // Force Cinemachine to update immediately
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            // Wait for one frame to ensure everything is processed
            yield return null;

            // Force manual update
            brain.ManualUpdate();

            // Wait another frame
            yield return null;

            brain.ManualUpdate();
            Debug.Log("CinemachineBrain manual updates complete");
        }

        // Debug which camera is active
        CheckActiveCamera();

        Debug.Log("Camera system update forced");
    }

    // This method is called by EnerlingSelectionManager when player selects their enerling
    public void OnPlayerEnerlingSelected(string playerEnerlingName)
    {
        StartCoroutine(InitializeBattleAfterSelection(playerEnerlingName));
    }

    IEnumerator InitializeBattleAfterSelection(string playerEnerlingName)
    {
        Debug.Log($"Player selected enerling: {playerEnerlingName}");

        // Fade out the selection canvas
        if (enerlingPickingCanvas != null)
        {
            Debug.Log("Fading out player selection canvas");
            CanvasGroup canvasGroup = enerlingPickingCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                float fadeTime = 0.3f;
                float elapsed = 0;
                while (elapsed < fadeTime)
                {
                    canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeTime);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                canvasGroup.alpha = 0;
            }
            enerlingPickingCanvas.SetActive(false);
        }

        // Wait a moment for canvas to fade out
        yield return new WaitForSeconds(0.3f);

        // Camera is ALREADY on BattleFocus (set in StartBattleSequence)
        // Confirm BattleFocus camera is still active
        if (battleFocusCamera != null && battleFocusCamera.Priority < 20)
        {
            Debug.LogWarning("BattleFocus camera not active, setting priority to 20");
            SetAllCamerasPriority(0);
            battleFocusCamera.Priority = 20;

            // Force camera update again
            yield return StartCoroutine(ForceCameraUpdateCoroutine());
        }

        Debug.Log("Camera is on BattleFocus view, ready for battle");

        // Wait a moment for camera to settle
        yield return new WaitForSeconds(0.5f);

        // Now initialize the battle systems with the selected player enerling
        InitializeBattleSystems(playerEnerlingName);
    }

    void InitializeBattleSystems(string playerEnerlingName)
    {
        Debug.Log("Initializing battle systems...");

        // STEP 1: Initialize PLAYER enerling using EXISTING method
        if (battleManager != null)
        {
            // This method already exists in your BattleEnerlingManager
            battleManager.InitializeBattlefieldWithEnerling(playerEnerlingName);
        }
        else
        {
            Debug.LogWarning("BattleEnerlingManager not assigned!");
        }

        // STEP 2: Initialize AI opponent - NO SPAWN POINT PARAMETER NEEDED!
        if (aiManager != null && opponentEnerling != null)
        {
            // Get the BATTLE SCENE spawn point from AIEnerlingManager (just for logging)
            Transform aiBattleSpawnPoint = aiManager.aiSpawningPoint;

            if (aiBattleSpawnPoint == null)
            {
                Debug.LogWarning("AI Battle spawn point not found in AIEnerlingManager!");
            }
            else
            {
                Debug.Log($"Using BATTLE SCENE spawn point: {aiBattleSpawnPoint.name}");
            }

            // Initialize with JUST 2 parameters now
            aiManager.InitializeAIEnerling(
                opponentEnerling.ingredientName,
                ingredientDatabase
            // NO 3rd parameter - uses its own aiSpawningPoint
            );

            Debug.Log($"AI opponent initialized: {opponentEnerling.ingredientName}");
        }

        // STEP 3: Initialize PlayerEnerlingManager skills UI
        if (playerManager != null && battleManager != null)
        {
            var playerEnerling = battleManager.GetBattleEnerling();
            if (playerEnerling != null)
            {
                playerManager.InitializePlayerEnerling(playerEnerling.ingredientName);
            }
        }

        // STEP 4: Start the turn system using EXISTING method
        if (turnSystem != null)
        {
            // Initialize with managers
            turnSystem.InitializeBattle(battleManager, aiManager);

            // Start the battle
            turnSystem.StartBattle();

            Debug.Log("Turn system started");
        }
        else
        {
            Debug.LogWarning("TurnSystem not assigned!");
        }

        Debug.Log("Battle systems initialized successfully!");
    }

    string GetRandomEnerlingName()
    {
        if (ingredientDatabase != null && ingredientDatabase.ingredients.Count > 0)
        {
            int randomIndex = Random.Range(0, ingredientDatabase.ingredients.Count);
            return ingredientDatabase.ingredients[randomIndex].ingredientName;
        }
        return "DefaultEnerling";
    }

    void OnDestroy()
    {
        if (spawnedOpponent != null)
            Destroy(spawnedOpponent);

        StopTimelineImmediately();
    }

    // Public methods for external control
    public void RestartBattleWithNewOpponent(string newOpponentName)
    {
        if (!string.IsNullOrEmpty(newOpponentName))
        {
            if (spawnedOpponent != null)
                Destroy(spawnedOpponent);

            opponentEnerling = ingredientDatabase.GetIngredientInfo(newOpponentName);
            if (opponentEnerling != null)
            {
                isUnlocked = opponentEnerling.isUnlocked;
                UpdateEnerlingInfoUI();
                UpdateRarityVisuals();
                UpdateCatchFightButtonText();
                StartCoroutine(PlayIntroductionSequence());
            }
        }
    }

    public bool IsTimelinePlaying()
    {
        return timelinePlaying;
    }

    public IngredientDatabase.IngredientInfo GetCurrentOpponent()
    {
        return opponentEnerling;
    }

    [ContextMenu("Debug Camera Priorities")]
    public void DebugCameraPriorities()
    {
        Debug.Log("=== CURRENT CAMERA PRIORITIES ===");
        if (groceryCamera != null)
            Debug.Log($"Grocery Camera: {groceryCamera.Priority}");
        if (battleFocusCamera != null)
            Debug.Log($"BattleFocus Camera: {battleFocusCamera.Priority} {(battleFocusCamera.Priority >= 20 ? "(ACTIVE)" : "(INACTIVE)")}");
        if (nutriKingdomCam != null)
            Debug.Log($"NutriKingdom Camera: {nutriKingdomCam.Priority}");
        if (alerthiaCam != null)
            Debug.Log($"Alerthia Camera: {alerthiaCam.Priority}");
        if (sugariaCam != null)
            Debug.Log($"Sugaria Camera: {sugariaCam.Priority}");
        if (preserviaCam != null)
            Debug.Log($"Preservia Camera: {preserviaCam.Priority}");
        Debug.Log("================================");
    }

    [ContextMenu("Check Active Camera")]
    public void CheckActiveCamera()
    {
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            ICinemachineCamera activeCam = brain.ActiveVirtualCamera;
            if (activeCam != null)
            {
                Debug.Log($"ACTIVE CAMERA: {activeCam.Name} (Priority: {activeCam.Priority})");
            }
            else
            {
                Debug.Log("No active virtual camera found");
            }
        }

        // Also check all cameras
        CinemachineVirtualCamera[] allCams = FindObjectsOfType<CinemachineVirtualCamera>();
        foreach (var cam in allCams)
        {
            Debug.Log($"{cam.name}: Priority={cam.Priority}, {(cam.Priority >= 20 ? "SHOULD BE ACTIVE" : "INACTIVE")}");
        }
    }
}