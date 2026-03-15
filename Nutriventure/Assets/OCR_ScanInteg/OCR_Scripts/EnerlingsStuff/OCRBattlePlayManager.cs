using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the OCR Battle Play scene:
///  - Spawns the player's equipped character / skin at PlayerSpawnPoint
///  - Displays & manages the life (heart) panel
///  - Displays & manages the energy counter
///  - Handles regen timers that persist across sessions
///  - Updates the EnerlingPanelFight UI (hearts, energy text, regen timers)
/// </summary>
public class OCRBattlePlayManager : MonoBehaviour
{
    public static OCRBattlePlayManager Instance { get; private set; }

    // ───────── Character Spawn ─────────
    [Header("Character Spawn")]
    [Tooltip("The Transform in the scene where the player character will be spawned")]
    public Transform playerSpawnPoint;

    // ───────── Heart / Life Panel ─────────
    [Header("Heart Panel (Life System)")]
    [Tooltip("Parent transform inside the heart panel to hold heart images")]
    public Transform heartContainer;
    [Tooltip("Sprite for a full heart")]
    public Sprite fullHeartSprite;
    [Tooltip("Sprite for an empty heart")]
    public Sprite emptyHeartSprite;
    [Tooltip("Size of each heart image (width x height)")]
    public Vector2 heartSize = new Vector2(64f, 64f);

    // ───────── EnerlingPanelFight UI ─────────
    [Header("EnerlingPanelFight UI")]
    [Tooltip("Heart / life panel inside EnerlingPanelFight (same heartContainer or a second one)")]
    public Transform fightHeartContainer;
    [Tooltip("Energy text with format '15/15'")]
    public TextMeshProUGUI energyText;
    [Tooltip("Text that shows remaining regen time for life (hidden when full)")]
    public TextMeshProUGUI lifeRegenTimerText;
    [Tooltip("Text that shows remaining regen time for energy (hidden when full)")]
    public TextMeshProUGUI energyRegenTimerText;

    // ───────── Runtime State ─────────
    private GameObject spawnedCharacter;
    private List<Image> heartImages = new List<Image>();
    private List<Image> fightHeartImages = new List<Image>();

    // ========================================================================
    //  UNITY LIFECYCLE
    // ========================================================================

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
        // Process any offline regeneration first
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ProcessOCRBattleRegen();
        }

        SpawnPlayerCharacter();
        BuildHeartImages(heartContainer, heartImages);
        BuildHeartImages(fightHeartContainer, fightHeartImages);
        RefreshAllUI();
    }

    void Update()
    {
        // Tick regen timers every frame when not full
        if (GameDataManager.Instance == null) return;

        bool lifeFull = GameDataManager.Instance.GetOCRBattleLives() >= GameDataManager.Instance.GetOCRBattleMaxLives();
        bool energyFull = GameDataManager.Instance.GetOCRBattleEnergy() >= GameDataManager.Instance.GetOCRBattleMaxEnergy();

        // Process regen tick (checks if a full interval has passed)
        GameDataManager.Instance.ProcessOCRBattleRegen();

        // Update UI continuously
        RefreshAllUI();

        // Auto-check regen completion and rebuild hearts if lives changed
        UpdateRegenTimerTexts(lifeFull, energyFull);
    }

    // ========================================================================
    //  CHARACTER SPAWNING
    // ========================================================================

    void SpawnPlayerCharacter()
    {
        if (playerSpawnPoint == null)
        {
            Debug.LogWarning("OCRBattlePlayManager: PlayerSpawnPoint not assigned!");
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("OCRBattlePlayManager: GameDataManager not found!");
            return;
        }

        GameObject prefab = GameDataManager.Instance.GetEquippedCharacterOrSkinPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("OCRBattlePlayManager: No character/skin prefab found!");
            return;
        }

        // Clean up any previously spawned character
        if (spawnedCharacter != null)
            Destroy(spawnedCharacter);

        spawnedCharacter = Instantiate(prefab, playerSpawnPoint.position, playerSpawnPoint.rotation, playerSpawnPoint);
        spawnedCharacter.transform.localPosition = Vector3.zero;
        spawnedCharacter.transform.localRotation = Quaternion.identity;
        spawnedCharacter.name = "PlayerCharacter_Spawned";

        Debug.Log($"OCRBattlePlayManager: Spawned player character '{prefab.name}' at {playerSpawnPoint.name}");
    }

    // ========================================================================
    //  HEART (LIFE) UI
    // ========================================================================

    void BuildHeartImages(Transform container, List<Image> imageList)
    {
        if (container == null) return;

        imageList.Clear();

        // Remove any existing children
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        int maxLives = GameDataManager.Instance != null ? GameDataManager.Instance.GetOCRBattleMaxLives() : 5;

        for (int i = 0; i < maxLives; i++)
        {
            GameObject heartGO = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
            heartGO.transform.SetParent(container, false);

            RectTransform rt = heartGO.GetComponent<RectTransform>();
            rt.sizeDelta = heartSize;

            Image img = heartGO.GetComponent<Image>();
            img.sprite = fullHeartSprite;
            img.preserveAspect = true;

            imageList.Add(img);
        }
    }

    void UpdateHeartUI(List<Image> imageList)
    {
        if (imageList == null || imageList.Count == 0) return;
        if (GameDataManager.Instance == null) return;

        int currentLives = GameDataManager.Instance.GetOCRBattleLives();

        for (int i = 0; i < imageList.Count; i++)
        {
            if (imageList[i] == null) continue;
            imageList[i].sprite = (i < currentLives) ? fullHeartSprite : emptyHeartSprite;
        }
    }

    // ========================================================================
    //  ENERGY & REGEN TIMER UI
    // ========================================================================

    void RefreshAllUI()
    {
        if (GameDataManager.Instance == null) return;

        // Hearts
        UpdateHeartUI(heartImages);
        UpdateHeartUI(fightHeartImages);

        // Energy text
        if (energyText != null)
        {
            int cur = GameDataManager.Instance.GetOCRBattleEnergy();
            int max = GameDataManager.Instance.GetOCRBattleMaxEnergy();
            energyText.text = $"{cur}/{max}";
        }
    }

    void UpdateRegenTimerTexts(bool lifeFull, bool energyFull)
    {
        // Life regen timer
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
                lifeRegenTimerText.text = FormatTime(remainSec);
            }
        }

        // Energy regen timer
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
                energyRegenTimerText.text = FormatTime(remainSec);
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
    //  PUBLIC API — called by EndingManager / BattlePlayManager
    // ========================================================================

    /// <summary>
    /// Call when the player WINS a battle — catch count increases, life stays.
    /// </summary>
    public void OnBattleWin(string defeatedEnerlingName)
    {
        Debug.Log($"OCRBattlePlayManager: Battle won vs {defeatedEnerlingName}");

        // Increment catch count
        if (PersistentDataManager.Instance != null)
            PersistentDataManager.Instance.IncrementCatchCount(defeatedEnerlingName);

        RefreshAllUI();
    }

    /// <summary>
    /// Call when the player LOSES a battle — deduct 1 life.
    /// </summary>
    public void OnBattleLose()
    {
        Debug.Log("OCRBattlePlayManager: Battle lost — deducting 1 life");

        if (GameDataManager.Instance != null)
            GameDataManager.Instance.UseOCRBattleLife();

        RefreshAllUI();
    }

    /// <summary>
    /// Consume 1 energy to start a battle. Returns false if not enough energy.
    /// </summary>
    public bool TryUseEnergy()
    {
        if (GameDataManager.Instance == null) return false;
        bool success = GameDataManager.Instance.UseOCRBattleEnergy();
        RefreshAllUI();
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

    /// <summary>Returns the spawned player character root GameObject.</summary>
    public GameObject GetSpawnedCharacter()
    {
        return spawnedCharacter;
    }

    void OnDestroy()
    {
        if (spawnedCharacter != null)
            Destroy(spawnedCharacter);
        if (Instance == this)
            Instance = null;
    }
}
