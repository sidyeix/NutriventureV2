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
    public static BattlePlayManager Instance { get; private set; }

    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase;

    [Header("Playable Director & Assets")]
    public PlayableDirector playableDirector;

    [Header("Timeline Assets")]
    public PlayableAsset nutriKingdomTimeline;
    public PlayableAsset sugariaTimeline;
    public PlayableAsset alerthiaTimeline;
    public PlayableAsset preserviaTimeline;

    [Header("Camera References")]
    public CinemachineVirtualCamera groceryCamera;
    public CinemachineVirtualCamera battleFocusCamera;
    public CinemachineVirtualCamera nutriKingdomCam;
    public CinemachineVirtualCamera alerthiaCam;
    public CinemachineVirtualCamera sugariaCam;
    public CinemachineVirtualCamera preserviaCam;

    [Header("Grocery Camera Settings")]
    public CinemachineTrackedDolly groceryCameraDolly;

    [Header("Initial Camera Positions (before catch/fight)")]
    public float nutriKingdomStartPosition = 0f;
    public float alerthiaStartPosition = 32.1f;
    public float sugariaStartPosition = 0f;
    public float preserviaStartPosition = 0f;

    [Header("Battle Camera Position (after catch/fight)")]
    public float battleCameraPosition = 0f;

    [Header("Canvas References")]
    public GameObject catchEnerlingCanvas;
    public GameObject enerlingInfoCanvas;
    public GameObject enerlingPickingCanvas;

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

    [Header("Player Character Spawn")]
    [Tooltip("Transform where the player's character/skin prefab will be spawned")]
    public Transform playerCharacterSpawnPoint;

    [Header("EnerlingPanelFight UI")]
    [Tooltip("The canvas/panel shown before a fight with life/energy display")]
    public GameObject enerlingPanelFight;
    [Tooltip("Warning text shown when player has no energy or no lives")]
    public TextMeshProUGUI noResourceWarningText;

    [Header("Heart Panel (Life System)")]
    [Tooltip("Parent transform inside the heart panel to hold heart images (add HorizontalLayoutGroup)")]
    public Transform heartContainer;
    [Tooltip("Sprite for a full heart")]
    public Sprite fullHeartSprite;
    [Tooltip("Sprite for an empty heart")]
    public Sprite emptyHeartSprite;
    [Tooltip("Size of each heart image (width x height)")]
    public Vector2 heartSize = new Vector2(64f, 64f);

    [Header("Energy & Regen UI")]
    [Tooltip("Energy text with format '15/15'")]
    public TextMeshProUGUI energyText;
    [Tooltip("Text that shows remaining regen time for life (hidden when full)")]
    public TextMeshProUGUI lifeRegenTimerText;
    [Tooltip("Text that shows remaining regen time for energy (hidden when full)")]
    public TextMeshProUGUI energyRegenTimerText;

    [Header("Scene Names")]
    public string scanOCRSceneName = "ScanOCR";

    private IngredientDatabase.IngredientInfo opponentEnerling;
    private GameObject spawnedOpponent;
    private GameObject spawnedPlayerCharacter;
    private PlayableAsset currentTimeline;
    private bool timelinePlaying = false;
    private bool isUnlocked = false;
    private bool battleStarted = false;
    private System.Collections.Generic.List<Image> heartImages = new System.Collections.Generic.List<Image>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Debug.Log("=== BATTLE PLAY MANAGER START ===");

        // Process any offline regen before UI setup
        if (GameDataManager.Instance != null)
            GameDataManager.Instance.ProcessOCRBattleRegen();

        // Ensure all damaged enerlings have regen running
        if (PersistentDataManager.Instance != null)
            PersistentDataManager.Instance.EnsureAllDamagedEnerlingsRegenerating();

        // Spawn the player's equipped character/skin at the spawn point
        SpawnPlayerCharacterModel();

        // Build life heart images & initial UI
        Debug.Log($"[LifeEnergy] heartContainer={heartContainer != null}, energyText={energyText != null}, lifeRegenTimerText={lifeRegenTimerText != null}, energyRegenTimerText={energyRegenTimerText != null}");
        Debug.Log($"[LifeEnergy] fullHeartSprite={fullHeartSprite != null}, emptyHeartSprite={emptyHeartSprite != null}");
        Debug.Log($"[LifeEnergy] GameDataManager={GameDataManager.Instance != null}");
        if (GameDataManager.Instance != null)
            Debug.Log($"[LifeEnergy] Lives={GameDataManager.Instance.GetOCRBattleLives()}/{GameDataManager.Instance.GetOCRBattleMaxLives()}, Energy={GameDataManager.Instance.GetOCRBattleEnergy()}/{GameDataManager.Instance.GetOCRBattleMaxEnergy()}");
        BuildHeartImages();
        RefreshLifeEnergyUI();
        Debug.Log($"[LifeEnergy] Hearts built: {heartImages.Count}");

        // STOP ALL OTHER PLAYABLE DIRECTORS IN THE SCENE
        StopAllOtherPlayableDirectors();

        // Initialize timeline
        if (playableDirector != null)
        {
            playableDirector.Stop();
            playableDirector.time = 0;
            playableDirector.Evaluate();
        }

        // Setup UI
        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(false);

        if (enerlingInfoCanvas != null)
            enerlingInfoCanvas.SetActive(false);

        if (enerlingPickingCanvas != null)
            enerlingPickingCanvas.SetActive(false);

        Debug.Log("=== BATTLE PLAY MANAGER INITIALIZED ===");

        // Start battle scene
        StartCoroutine(InitializeBattleScene());
    }

    void Update()
    {
        if (GameDataManager.Instance == null) return;

        // Lazy-build hearts if they weren't created yet (e.g. container was inactive at Start)
        if (heartImages.Count == 0 && heartContainer != null)
            BuildHeartImages();

        // Tick regen once per second (avoids SaveGameData every frame)
        regenTickTimer += Time.deltaTime;
        if (regenTickTimer >= REGEN_TICK_INTERVAL)
        {
            regenTickTimer = 0f;
            GameDataManager.Instance.ProcessOCRBattleRegen();
        }

        RefreshLifeEnergyUI();
    }

    private float regenTickTimer = 0f;
    private const float REGEN_TICK_INTERVAL = 1f;

    void StopAllOtherPlayableDirectors()
    {
        Debug.Log("=== STOPPING ALL OTHER PLAYABLE DIRECTORS ===");

        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>(true);
        Debug.Log($"Found {allDirectors.Length} PlayableDirectors in scene");

        foreach (PlayableDirector director in allDirectors)
        {
            // Skip our own director
            if (director == playableDirector) continue;

            Debug.Log($"Stopping PlayableDirector: {director.name} (Asset: {director.playableAsset?.name})");
            director.Stop();
            director.time = 0;
            director.Evaluate();

            // Optionally disable the GameObject if it's not needed
            if (director.gameObject != this.gameObject && director.gameObject.name.Contains("Timeline"))
            {
                Debug.Log($"Disabling unnecessary PlayableDirector GameObject: {director.gameObject.name}");
                director.gameObject.SetActive(false);
            }
        }

        Debug.Log("All other PlayableDirectors stopped");
    }

    void SpawnPlayerCharacterModel()
    {
        if (playerCharacterSpawnPoint == null)
        {
            Debug.LogWarning("BattlePlayManager: playerCharacterSpawnPoint not assigned!");
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("BattlePlayManager: GameDataManager not found — cannot spawn player character.");
            return;
        }

        GameObject prefab = GameDataManager.Instance.GetEquippedCharacterOrSkinPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("BattlePlayManager: No character/skin prefab found!");
            return;
        }

        if (spawnedPlayerCharacter != null)
            Destroy(spawnedPlayerCharacter);

        spawnedPlayerCharacter = Instantiate(prefab, playerCharacterSpawnPoint.position,
            playerCharacterSpawnPoint.rotation, playerCharacterSpawnPoint);
        spawnedPlayerCharacter.transform.localPosition = Vector3.zero;
        spawnedPlayerCharacter.transform.localRotation = Quaternion.identity;
        spawnedPlayerCharacter.name = "PlayerCharacter_Spawned";

        Debug.Log($"BattlePlayManager: Spawned player character '{prefab.name}' at {playerCharacterSpawnPoint.name}");
    }

    IEnumerator InitializeBattleScene()
    {
        Debug.Log("=== INITIALIZING BATTLE SCENE ===");

        yield return new WaitForSeconds(0.5f); // Small delay

        string opponentName = "";
        if (PersistentDataManager.Instance != null)
        {
            opponentName = PersistentDataManager.Instance.GetOpponentEnerlingName();
            Debug.Log($"Loaded opponent enerling from PersistentData: {opponentName}");
        }

        if (string.IsNullOrEmpty(opponentName))
        {
            opponentName = GetRandomEnerlingName();
            Debug.LogWarning("No opponent found in PersistentData. Using random: " + opponentName);

            if (PersistentDataManager.Instance != null)
            {
                PersistentDataManager.Instance.SaveOpponentEnerling(opponentName);
            }
        }

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

        isUnlocked = opponentEnerling.isUnlocked;
        Debug.Log($"Battle against: {opponentEnerling.ingredientName} from {opponentEnerling.kingdom}, Unlocked: {isUnlocked}");

        // SET INITIAL GROCERY CAMERA POSITION BASED ON KINGDOM
        SetInitialGroceryCameraPosition(opponentEnerling.kingdom);

        UpdateEnerlingInfoUI();
        UpdateRarityVisuals();
        UpdateCatchFightButtonText();

        // Start the introduction sequence
        yield return StartCoroutine(PlayIntroductionSequence());
    }

    void SetInitialGroceryCameraPosition(IngredientDatabase.KingdomOrigin kingdom)
    {
        if (groceryCameraDolly == null)
        {
            // Try to find it automatically if not assigned
            groceryCameraDolly = groceryCamera?.GetCinemachineComponent<CinemachineTrackedDolly>();

            if (groceryCameraDolly == null)
            {
                Debug.LogError("CinemachineTrackedDolly component not found on grocery camera!");
                return;
            }
        }

        float cameraPosition = 0f;

        switch (kingdom)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom:
                cameraPosition = nutriKingdomStartPosition;
                Debug.Log($"Setting INITIAL grocery camera to NutriKingdom position: {cameraPosition}");
                break;

            case IngredientDatabase.KingdomOrigin.Alerthia:
                cameraPosition = alerthiaStartPosition;
                Debug.Log($"Setting INITIAL grocery camera to Alerthia position: {cameraPosition}");
                break;

            case IngredientDatabase.KingdomOrigin.Sugaria:
                cameraPosition = sugariaStartPosition;
                Debug.Log($"Setting INITIAL grocery camera to Sugaria position: {cameraPosition}");
                break;

            case IngredientDatabase.KingdomOrigin.Preservia:
                cameraPosition = preserviaStartPosition;
                Debug.Log($"Setting INITIAL grocery camera to Preservia position: {cameraPosition}");
                break;

            default:
                cameraPosition = nutriKingdomStartPosition;
                Debug.Log($"Unknown kingdom, using default INITIAL camera position: {cameraPosition}");
                break;
        }

        // Set the initial camera path position
        groceryCameraDolly.m_PathPosition = cameraPosition;
        Debug.Log($"INITIAL grocery camera path position set to: {cameraPosition}");
    }

    IEnumerator PlayIntroductionSequence()
    {
        Debug.Log("=== STARTING INTRODUCTION SEQUENCE ===");

        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(false);

        SetAllCamerasPriority(0);
        SetKingdomCameraPriorityByOrigin(opponentEnerling.kingdom, 20);
        SpawnOpponentModel();

        // Wait for camera transitions
        yield return new WaitForSeconds(0.5f);

        // SIMPLE TIMELINE PLAY - NO AUDIO MUTING COMPLEXITY
        yield return StartCoroutine(PlayKingdomTimeline());

        // Wait for timeline to complete
        yield return new WaitForSeconds(1.0f);

        SetKingdomCameraPriorityByOrigin(opponentEnerling.kingdom, 20);

        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(true);

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

        SetupButtonListeners();
        Debug.Log("=== INTRODUCTION SEQUENCE COMPLETE ===");
    }

    IEnumerator PlayKingdomTimeline()
    {
        Debug.Log($"=== PLAYING KINGDOM TIMELINE ===");

        if (playableDirector == null)
        {
            Debug.LogError("PlayableDirector is null!");
            yield break;
        }

        currentTimeline = GetKingdomTimelineAsset(opponentEnerling.kingdom);

        if (currentTimeline == null)
        {
            Debug.LogError($"No timeline found for kingdom: {opponentEnerling.kingdom}");
            yield break;
        }

        Debug.Log($"Playing timeline: {currentTimeline.name}");

        playableDirector.playableAsset = currentTimeline;
        playableDirector.time = 0;
        playableDirector.Play();

        timelinePlaying = true;

        // Wait a bit to ensure it started
        yield return new WaitForSeconds(0.1f);
        Debug.Log($"Timeline playing: {playableDirector.state}");
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

        if (spawnedOpponent != null)
            Destroy(spawnedOpponent);

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

    void SetAllCamerasPriority(int priority)
    {
        if (groceryCamera != null)
        {
            groceryCamera.Priority = priority;
            // INSTANT CAMERA SWITCH: Disable camera transition
            CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
            }
        }
        if (battleFocusCamera != null) battleFocusCamera.Priority = priority;
        if (nutriKingdomCam != null) nutriKingdomCam.Priority = priority;
        if (alerthiaCam != null) alerthiaCam.Priority = priority;
        if (sugariaCam != null) sugariaCam.Priority = priority;
        if (preserviaCam != null) preserviaCam.Priority = priority;
    }

    void SetKingdomCameraPriorityByOrigin(IngredientDatabase.KingdomOrigin kingdom, int priority)
    {
        // INSTANT CAMERA SWITCH: Set blend to cut (instant) before changing priorities
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
        }

        // Set all cameras to low priority first
        if (nutriKingdomCam != null) nutriKingdomCam.Priority = 0;
        if (alerthiaCam != null) alerthiaCam.Priority = 0;
        if (sugariaCam != null) sugariaCam.Priority = 0;
        if (preserviaCam != null) preserviaCam.Priority = 0;
        if (groceryCamera != null) groceryCamera.Priority = 0;
        if (battleFocusCamera != null) battleFocusCamera.Priority = 0;

        // Set the target camera to high priority
        switch (kingdom)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom:
                if (nutriKingdomCam != null) nutriKingdomCam.Priority = priority;
                break;
            case IngredientDatabase.KingdomOrigin.Alerthia:
                if (alerthiaCam != null) alerthiaCam.Priority = priority;
                break;
            case IngredientDatabase.KingdomOrigin.Sugaria:
                if (sugariaCam != null) sugariaCam.Priority = priority;
                break;
            case IngredientDatabase.KingdomOrigin.Preservia:
                if (preserviaCam != null) preserviaCam.Priority = priority;
                break;
            default:
                if (nutriKingdomCam != null) nutriKingdomCam.Priority = priority;
                break;
        }

        // Force immediate camera update
        if (brain != null)
        {
            brain.ManualUpdate();
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

    void OnSkipButtonClicked()
    {
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
        Debug.Log("=== CATCH/FIGHT BUTTON CLICKED ===");

        if (GameDataManager.Instance != null)
        {
            int livesBefore = GameDataManager.Instance.GetOCRBattleLives();
            Debug.Log($"[Fight] Before — Lives: {livesBefore}");

            if (livesBefore <= 0)
            {
                ShowNoResourceWarning("No lives remaining! Wait for regeneration.");
                return;
            }
        }
        else
        {
            Debug.LogWarning("BattlePlayManager: GameDataManager not found — skipping life check.");
        }

        // RESET GROCERY CAMERA TO BATTLE POSITION (0) WHEN CATCH/FIGHT IS CLICKED
        SetBattleGroceryCameraPosition();

        StartCoroutine(ShowPlayerSelectionScreen());
    }

    void ShowNoResourceWarning(string message)
    {
        if (noResourceWarningText != null)
        {
            noResourceWarningText.gameObject.SetActive(true);
            noResourceWarningText.text = message;
            // Auto-hide after 2 seconds
            StartCoroutine(HideWarningAfterDelay(2f));
        }
        else
        {
            Debug.LogWarning($"BattlePlayManager: {message}");
        }
    }

    IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (noResourceWarningText != null)
            noResourceWarningText.gameObject.SetActive(false);
    }

    void SetBattleGroceryCameraPosition()
    {
        if (groceryCameraDolly == null)
        {
            groceryCameraDolly = groceryCamera?.GetCinemachineComponent<CinemachineTrackedDolly>();

            if (groceryCameraDolly == null)
            {
                Debug.LogError("CinemachineTrackedDolly component not found on grocery camera!");
                return;
            }
        }

        // Set to battle position (0 for all kingdoms)
        groceryCameraDolly.m_PathPosition = battleCameraPosition;
        Debug.Log($"BATTLE grocery camera path position set to: {battleCameraPosition} (after catch/fight click)");
    }

    IEnumerator ShowPlayerSelectionScreen()
    {
        Debug.Log("Fight/Catch button clicked - showing player selection screen");

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

        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(false);

        // INSTANT CAMERA SWITCH: No transition, just cut to battle focus camera
        SetAllCamerasPriority(0);

        // Set CinemachineBrain to instant cut
        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
        }

        if (battleFocusCamera != null)
        {
            battleFocusCamera.Priority = 20;
        }

        // Force immediate camera update
        if (brain != null)
        {
            brain.ManualUpdate();
        }

        yield return new WaitForSeconds(0.1f); // Small delay for instant switch

        if (enerlingPickingCanvas != null)
        {
            Debug.Log("Enabling player selection canvas");
            enerlingPickingCanvas.SetActive(true);
        }
        else
        {
            Debug.LogError("EnerlingPickingCanvas not assigned!");
        }
    }

    public void OnPlayerEnerlingSelected(string playerEnerlingName)
    {
        if (battleStarted) return;

        battleStarted = true;
        StartCoroutine(StartBattleAfterSelection(playerEnerlingName));
    }

    IEnumerator StartBattleAfterSelection(string playerEnerlingName)
    {
        Debug.Log($"Player selected enerling: {playerEnerlingName} - Starting battle sequence");

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

        yield return new WaitForSeconds(0.3f);

        if (playableDirector != null)
        {
            Debug.Log("Disabling Playable Director GameObject to release camera control");
            playableDirector.gameObject.SetActive(false);
        }

        Debug.Log("Switching to BattleFocus camera");
        SetAllCamerasPriority(0);

        if (battleFocusCamera != null)
        {
            battleFocusCamera.Priority = 20;
        }

        yield return new WaitForSeconds(0.5f);

        Debug.Log("Initializing battlefield...");
        InitializeBattleSystems(playerEnerlingName);
    }

    void InitializeBattleSystems(string playerEnerlingName)
    {
        Debug.Log("Initializing battle systems...");

        if (battleManager != null)
        {
            battleManager.InitializeBattlefieldWithEnerling(playerEnerlingName);
        }

        if (aiManager != null && opponentEnerling != null)
        {
            aiManager.InitializeAIEnerling(
                opponentEnerling.ingredientName,
                ingredientDatabase
            );
        }

        if (playerManager != null && battleManager != null)
        {
            var playerEnerling = battleManager.GetBattleEnerling();
            if (playerEnerling != null)
            {
                playerManager.InitializePlayerEnerling(playerEnerling.ingredientName);
            }
        }

        if (turnSystem != null)
        {
            turnSystem.InitializeBattle(battleManager, aiManager);

            // This will automatically start the battle audio
            turnSystem.StartBattleWithAudio();
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
        if (spawnedPlayerCharacter != null)
            Destroy(spawnedPlayerCharacter);
        if (Instance == this)
            Instance = null;
    }

    public bool IsTimelinePlaying()
    {
        return timelinePlaying;
    }

    public IngredientDatabase.IngredientInfo GetCurrentOpponent()
    {
        return opponentEnerling;
    }

    // ========================================================================
    //  HEART (LIFE) UI
    // ========================================================================

    void BuildHeartImages()
    {
        if (heartContainer == null) return;

        heartImages.Clear();

        // Remove any existing children
        for (int i = heartContainer.childCount - 1; i >= 0; i--)
            Destroy(heartContainer.GetChild(i).gameObject);

        int maxLives = GameDataManager.Instance != null ? GameDataManager.Instance.GetOCRBattleMaxLives() : 5;

        for (int i = 0; i < maxLives; i++)
        {
            GameObject heartGO = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
            heartGO.transform.SetParent(heartContainer, false);

            RectTransform rt = heartGO.GetComponent<RectTransform>();
            rt.sizeDelta = heartSize;

            Image img = heartGO.GetComponent<Image>();
            img.sprite = fullHeartSprite;
            img.preserveAspect = true;

            heartImages.Add(img);
        }
    }

    void UpdateHeartUI()
    {
        if (heartImages == null || heartImages.Count == 0) return;
        if (GameDataManager.Instance == null) return;

        int currentLives = GameDataManager.Instance.GetOCRBattleLives();

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] == null) continue;
            heartImages[i].sprite = (i < currentLives) ? fullHeartSprite : emptyHeartSprite;
        }
    }

    // ========================================================================
    //  ENERGY & REGEN TIMER UI
    // ========================================================================

    void RefreshLifeEnergyUI()
    {
        if (GameDataManager.Instance == null) return;

        UpdateHeartUI();

        int curEnergy = GameDataManager.Instance.GetOCRBattleEnergy();
        int maxEnergy = GameDataManager.Instance.GetOCRBattleMaxEnergy();
        int curLives = GameDataManager.Instance.GetOCRBattleLives();
        int maxLives = GameDataManager.Instance.GetOCRBattleMaxLives();
        bool lifeFull = curLives >= maxLives;
        bool energyFull = curEnergy >= maxEnergy;

        if (energyText != null)
            energyText.text = $"{curEnergy}/{maxEnergy}";

        UpdateRegenTimerTexts(lifeFull, energyFull);
    }

    void UpdateRegenTimerTexts(bool lifeFull, bool energyFull)
    {
        if (lifeRegenTimerText != null)
        {
            if (lifeFull)
            {
                lifeRegenTimerText.gameObject.SetActive(false);
            }
            else
            {
                lifeRegenTimerText.gameObject.SetActive(true);
                float remainSec = GameDataManager.Instance.GetOCRLifeRegenRemainingSeconds();
                lifeRegenTimerText.text = $"Regen in: {FormatTime(remainSec)}";
            }
        }

        if (energyRegenTimerText != null)
        {
            if (energyFull)
            {
                energyRegenTimerText.gameObject.SetActive(false);
            }
            else
            {
                energyRegenTimerText.gameObject.SetActive(true);
                float remainSec = GameDataManager.Instance.GetOCREnergyRegenRemainingSeconds();
                energyRegenTimerText.text = $"Regen in: {FormatTime(remainSec)}";
            }
        }
    }

    static string FormatTime(float totalSeconds)
    {
        if (totalSeconds <= 0f) return "00:00";
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    // ========================================================================
    //  PUBLIC API — called by EndingManager
    // ========================================================================

    /// <summary>
    /// Call when the player WINS a battle — catch count increases, life stays.
    /// </summary>
    public void OnBattleWin(string defeatedEnerlingName)
    {
        Debug.Log($"BattlePlayManager: Battle won vs {defeatedEnerlingName}");

        if (PersistentDataManager.Instance != null)
            PersistentDataManager.Instance.IncrementCatchCount(defeatedEnerlingName);

        RefreshLifeEnergyUI();
    }

    /// <summary>
    /// Call when the player LOSES a battle — deduct 1 life.
    /// </summary>
    public void OnBattleLose()
    {
        Debug.Log("BattlePlayManager: Battle lost — deducting 1 life");

        if (GameDataManager.Instance != null)
            GameDataManager.Instance.UseOCRBattleLife();

        RefreshLifeEnergyUI();
    }

    /// <summary>
    /// Consume 1 energy to start a battle. Returns false if not enough energy.
    /// </summary>
    public bool TryUseEnergy()
    {
        if (GameDataManager.Instance == null) return false;
        bool success = GameDataManager.Instance.UseOCRBattleEnergy();
        RefreshLifeEnergyUI();
        return success;
    }

    /// <summary>Returns true if the player has at least 1 life left.</summary>
    public bool HasLivesRemaining()
    {
        return GameDataManager.Instance != null && GameDataManager.Instance.GetOCRBattleLives() > 0;
    }

    /// <summary>Returns true if the player has at least 1 energy.</summary>
    public bool HasEnergyRemaining()
    {
        return GameDataManager.Instance != null && GameDataManager.Instance.GetOCRBattleEnergy() > 0;
    }
}