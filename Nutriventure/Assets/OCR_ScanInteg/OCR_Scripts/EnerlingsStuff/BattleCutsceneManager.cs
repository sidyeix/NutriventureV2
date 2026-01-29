using UnityEngine;
using UnityEngine.Playables;
using Cinemachine;
using TMPro;
using UnityEngine.UI;

public class BattleCutsceneManager : MonoBehaviour
{
    [Header("Database")]
    public IngredientDatabase ingredientDatabase;

    [Header("Playable Directors")]
    public PlayableDirector nutriKingdomTimeline;
    public PlayableDirector suragriaTimeline;
    public PlayableDirector alerthiaTimeline;
    public PlayableDirector preserviaTimeline;

    [Header("Spawning Points")]
    public Transform nutriKingdomSpawnPoint;
    public Transform suragriaSpawnPoint;
    public Transform alerthiaSpawnPoint;
    public Transform preserviaSpawnPoint;

    [Header("Virtual Cameras")]
    public CinemachineVirtualCamera nutriKingdomCamera;
    public CinemachineVirtualCamera suragriaCamera;
    public CinemachineVirtualCamera alerthiaCamera;
    public CinemachineVirtualCamera preserviaCamera;
    public CinemachineVirtualCamera battleFocusCamera;

    [Header("UI Elements")]
    public TextMeshProUGUI enerlingNameText;
    public Image kingdomOriginImage;
    public TextMeshProUGUI kingdomOriginText;
    public GameObject enerlingInfoCanvas;

    [Header("Kingdom Sprites")]
    public Sprite nutriKingdomSprite;
    public Sprite suragriaSprite;
    public Sprite alerthiaSprite;
    public Sprite preserviaSprite;

    // Current AI enerling
    private string aiEnerlingName;
    private GameObject spawnedAIEnerling;

    void Start()
    {
        InitializeCutscene();
    }

    void InitializeCutscene()
    {
        // Get the scanned enerling from PersistentDataManager
        if (PersistentDataManager.Instance == null)
        {
            Debug.LogError("PersistentDataManager not found!");
            return;
        }

        aiEnerlingName = PersistentDataManager.Instance.GetSelectedEnerlingName();

        if (string.IsNullOrEmpty(aiEnerlingName))
        {
            Debug.LogWarning("No enerling scanned, using random one");
            // Fallback to random enerling
            if (ingredientDatabase != null && ingredientDatabase.ingredients.Count > 0)
            {
                int randomIndex = Random.Range(0, ingredientDatabase.ingredients.Count);
                aiEnerlingName = ingredientDatabase.ingredients[randomIndex].ingredientName;
                PersistentDataManager.Instance.SaveSelectedEnerling(aiEnerlingName);
            }
        }

        Debug.Log($"AI Enerling for battle: {aiEnerlingName}");

        // Setup and play cutscene
        SetupCutscene();
    }

    void SetupCutscene()
    {
        // Get enerling info
        var aiEnerling = ingredientDatabase.GetIngredientInfo(aiEnerlingName);
        if (aiEnerling == null)
        {
            Debug.LogError($"Enerling not found in database: {aiEnerlingName}");
            return;
        }

        // Update UI
        if (enerlingNameText != null)
            enerlingNameText.text = aiEnerling.ingredientName;

        if (kingdomOriginText != null)
            kingdomOriginText.text = aiEnerling.kingdom.ToString();

        // Set kingdom sprite
        if (kingdomOriginImage != null)
        {
            switch (aiEnerling.kingdom)
            {
                case IngredientDatabase.KingdomOrigin.NutriKingdom:
                    kingdomOriginImage.sprite = nutriKingdomSprite;
                    break;
                case IngredientDatabase.KingdomOrigin.Suragria:
                    kingdomOriginImage.sprite = suragriaSprite;
                    break;
                case IngredientDatabase.KingdomOrigin.Alerthia:
                    kingdomOriginImage.sprite = alerthiaSprite;
                    break;
                case IngredientDatabase.KingdomOrigin.Preservia:
                    kingdomOriginImage.sprite = preserviaSprite;
                    break;
            }
        }

        // Spawn AI enerling
        SpawnAIEnerling(aiEnerling);

        // Play timeline based on kingdom
        PlayTimeline(aiEnerling.kingdom);

        // Setup cameras
        SetupCameras(aiEnerling.kingdom);
    }

    void SpawnAIEnerling(IngredientDatabase.IngredientInfo enerling)
    {
        if (spawnedAIEnerling != null)
            Destroy(spawnedAIEnerling);

        Transform spawnPoint = GetSpawnPoint(enerling.kingdom);
        if (spawnPoint == null || enerling.modelPrefab == null)
            return;

        spawnedAIEnerling = Instantiate(enerling.modelPrefab, spawnPoint);
        spawnedAIEnerling.transform.localPosition = Vector3.zero;
        spawnedAIEnerling.transform.localRotation = Quaternion.identity;
    }

    Transform GetSpawnPoint(IngredientDatabase.KingdomOrigin kingdom)
    {
        switch (kingdom)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom: return nutriKingdomSpawnPoint;
            case IngredientDatabase.KingdomOrigin.Suragria: return suragriaSpawnPoint;
            case IngredientDatabase.KingdomOrigin.Alerthia: return alerthiaSpawnPoint;
            case IngredientDatabase.KingdomOrigin.Preservia: return preserviaSpawnPoint;
            default: return nutriKingdomSpawnPoint;
        }
    }

    void PlayTimeline(IngredientDatabase.KingdomOrigin kingdom)
    {
        // Stop all timelines first
        if (nutriKingdomTimeline != null) nutriKingdomTimeline.Stop();
        if (suragriaTimeline != null) suragriaTimeline.Stop();
        if (alerthiaTimeline != null) alerthiaTimeline.Stop();
        if (preserviaTimeline != null) preserviaTimeline.Stop();

        // Play the correct one
        switch (kingdom)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom:
                if (nutriKingdomTimeline != null) nutriKingdomTimeline.Play();
                break;
            case IngredientDatabase.KingdomOrigin.Suragria:
                if (suragriaTimeline != null) suragriaTimeline.Play();
                break;
            case IngredientDatabase.KingdomOrigin.Alerthia:
                if (alerthiaTimeline != null) alerthiaTimeline.Play();
                break;
            case IngredientDatabase.KingdomOrigin.Preservia:
                if (preserviaTimeline != null) preserviaTimeline.Play();
                break;
        }
    }

    void SetupCameras(IngredientDatabase.KingdomOrigin kingdom)
    {
        // Reset all cameras
        if (nutriKingdomCamera != null) nutriKingdomCamera.Priority = 0;
        if (suragriaCamera != null) suragriaCamera.Priority = 0;
        if (alerthiaCamera != null) alerthiaCamera.Priority = 0;
        if (preserviaCamera != null) preserviaCamera.Priority = 0;
        if (battleFocusCamera != null) battleFocusCamera.Priority = 0;

        // Activate the correct kingdom camera
        switch (kingdom)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom:
                if (nutriKingdomCamera != null) nutriKingdomCamera.Priority = 20;
                break;
            case IngredientDatabase.KingdomOrigin.Suragria:
                if (suragriaCamera != null) suragriaCamera.Priority = 20;
                break;
            case IngredientDatabase.KingdomOrigin.Alerthia:
                if (alerthiaCamera != null) alerthiaCamera.Priority = 20;
                break;
            case IngredientDatabase.KingdomOrigin.Preservia:
                if (preserviaCamera != null) preserviaCamera.Priority = 20;
                break;
        }
    }

    // Called from UI buttons
    public void OnSkipButtonClicked()
    {
        // Stop all timelines
        if (nutriKingdomTimeline != null) nutriKingdomTimeline.Stop();
        if (suragriaTimeline != null) suragriaTimeline.Stop();
        if (alerthiaTimeline != null) alerthiaTimeline.Stop();
        if (preserviaTimeline != null) preserviaTimeline.Stop();

        // Show enerling picking canvas (handled by your existing system)
        // Your existing EnerlingPickingCanvas should handle this
    }

    // Called from Fight/Catch button
    public void OnFightCatchButtonClicked()
    {
        // Stop all kingdom timelines
        if (nutriKingdomTimeline != null) nutriKingdomTimeline.Stop();
        if (suragriaTimeline != null) suragriaTimeline.Stop();
        if (alerthiaTimeline != null) alerthiaTimeline.Stop();
        if (preserviaTimeline != null) preserviaTimeline.Stop();

        // Reset all kingdom cameras
        if (nutriKingdomCamera != null) nutriKingdomCamera.Priority = 0;
        if (suragriaCamera != null) suragriaCamera.Priority = 0;
        if (alerthiaCamera != null) alerthiaCamera.Priority = 0;
        if (preserviaCamera != null) preserviaCamera.Priority = 0;

        // Activate battle focus camera
        if (battleFocusCamera != null) battleFocusCamera.Priority = 20;

        // Your existing EnerlingPickingCanvas should be shown now
        // The canvas should handle player enerling selection
    }

    public string GetAIEnerlingName()
    {
        return aiEnerlingName;
    }

    void OnDestroy()
    {
        if (spawnedAIEnerling != null)
            Destroy(spawnedAIEnerling);
    }
}