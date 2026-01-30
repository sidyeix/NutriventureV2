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

    [Header("Scene Names")]
    public string scanOCRSceneName = "ScanOCR";

    [Header("Settings")]
    public bool stopAllAudioImmediately = true;

    private IngredientDatabase.IngredientInfo opponentEnerling;
    private GameObject spawnedOpponent;
    private PlayableAsset currentTimeline;
    private bool timelinePlaying = false;
    private bool isUnlocked = false;
    private bool battleStarted = false;
    private CinemachineBlendDefinition originalBattleFocusBlend;

    void Start()
    {
        if (battleFocusCamera != null)
        {
            CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                originalBattleFocusBlend = brain.m_DefaultBlend;
            }
        }

        InitializeTimelineControl();

        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(false);

        if (enerlingInfoCanvas != null)
            enerlingInfoCanvas.SetActive(false);

        if (enerlingPickingCanvas != null)
            enerlingPickingCanvas.SetActive(false);

        StartCoroutine(InitializeBattleScene());
    }

    void InitializeTimelineControl()
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

        playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
        playableDirector.Stop();
        playableDirector.time = 0;
        playableDirector.Evaluate();

        Debug.Log("Timeline control initialized");
    }

    void StopAllAudioGameObjects()
    {
        // Deactivate common audio GameObjects
        string[] audioObjectNames = {
            "BattleIntroAudioSource", "Audio Source", "Music", "SFX"
        };

        foreach (string name in audioObjectNames)
        {
            GameObject audioObj = GameObject.Find(name);
            if (audioObj != null)
            {
                audioObj.SetActive(false);
                Debug.Log($"Deactivated audio GameObject: {name}");
            }
        }
    }

    IEnumerator InitializeBattleScene()
    {
        yield return new WaitForSeconds(0.5f);

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

        UpdateEnerlingInfoUI();
        UpdateRarityVisuals();
        UpdateCatchFightButtonText();

        yield return StartCoroutine(PlayIntroductionSequence());
    }

    IEnumerator PlayIntroductionSequence()
    {
        if (catchEnerlingCanvas != null)
            catchEnerlingCanvas.SetActive(false);

        SetAllCamerasPriority(0);
        SetKingdomCameraPriorityByOrigin(opponentEnerling.kingdom, 20);
        SpawnOpponentModel();

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(PlayKingdomTimeline());

        yield return new WaitForSeconds(1f);

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

    IEnumerator PlayKingdomTimeline()
    {
        if (playableDirector == null)
            yield break;

        StopCurrentTimeline();
        currentTimeline = GetKingdomTimelineAsset(opponentEnerling.kingdom);

        if (currentTimeline != null)
        {
            playableDirector.playableAsset = currentTimeline;
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

    void StopCurrentTimeline()
    {
        if (playableDirector == null) return;

        playableDirector.stopped -= OnTimelineFinished;
        playableDirector.Stop();
        playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
        playableDirector.time = 0;
        playableDirector.Evaluate();

        timelinePlaying = false;
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
        if (groceryCamera != null) groceryCamera.Priority = priority;
        if (battleFocusCamera != null) battleFocusCamera.Priority = priority;
        if (nutriKingdomCam != null) nutriKingdomCam.Priority = priority;
        if (alerthiaCam != null) alerthiaCam.Priority = priority;
        if (sugariaCam != null) sugariaCam.Priority = priority;
        if (preserviaCam != null) preserviaCam.Priority = priority;

        Debug.Log($"Set all cameras priority to: {priority}");
    }

    void SetKingdomCameraPriorityByOrigin(IngredientDatabase.KingdomOrigin kingdom, int priority)
    {
        if (nutriKingdomCam != null) nutriKingdomCam.Priority = 0;
        if (alerthiaCam != null) alerthiaCam.Priority = 0;
        if (sugariaCam != null) sugariaCam.Priority = 0;
        if (preserviaCam != null) preserviaCam.Priority = 0;
        if (groceryCamera != null) groceryCamera.Priority = 0;
        if (battleFocusCamera != null) battleFocusCamera.Priority = 0;

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
        StartCoroutine(ShowPlayerSelectionScreen());
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

        SetAllCamerasPriority(0);
        if (battleFocusCamera != null)
        {
            battleFocusCamera.Priority = 20;
        }

        yield return new WaitForSeconds(0.5f);

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
            Debug.Log($"BattleFocus camera priority set to: {battleFocusCamera.Priority}");
        }
        else
        {
            Debug.LogError("BattleFocus camera not assigned!");
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
        else
        {
            Debug.LogWarning("BattleEnerlingManager not assigned!");
        }

        if (aiManager != null && opponentEnerling != null)
        {
            aiManager.InitializeAIEnerling(
                opponentEnerling.ingredientName,
                ingredientDatabase
            );

            Debug.Log($"AI opponent initialized: {opponentEnerling.ingredientName}");
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

        StopCurrentTimeline();
    }

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
}