using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class Player_Data : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI gemsText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI nameText;
    public Slider xpSlider;

    [Header("Profile Icon")]
    public Image profileIconImage; // Reference to the profile icon Image component

    [Header("Frame Display")]
    public Image frameImage; // Reference to the frame Image component in player data

    [Header("Display Settings")]
    public string levelPrefix = "Lv. ";
    public string xpPrefix = "XP: ";
    public string xpSeparator = " / ";

    [Header("Update Settings")]
    public bool autoUpdate = true;
    public float updateInterval = 0.1f;
    public bool updateOnCoinCollect = true;

    [Header("Animation Settings")]
    public bool animateCoinCounter = true;
    public bool animateGemCounter = true;
    public float coinCountSpeed = 10f;
    public float gemCountSpeed = 10f;
    public float xpFillSpeed = 1f;

    [Header("Color Settings")]
    public Color normalTextColor = Color.white;
    public Color highlightTextColor = Color.yellow;
    public float highlightDuration = 0.5f;

    // Private variables
    private int displayedCoins = 0;
    private int displayedGems = 0;
    private int targetCoins = 0;
    private int targetGems = 0;
    private float displayedXP = 0f;
    private float targetXP = 0f;
    private int displayedLevel = 1;
    private int targetLevel = 1;

    private bool isAnimatingCoins = false;
    private bool isAnimatingGems = false;
    private bool isAnimatingXP = false;

    // Cached references
    private GameDataManager gameDataManager;
    private CoinCollectionSystem coinSystem;

    // Events for profile changes
    public System.Action OnProfileIconChanged;
    public System.Action OnFrameChanged;

    private void Start()
    {
        InitializeReferences();
        InitializeUI();
        SetupEventListeners();

        if (autoUpdate)
        {
            StartCoroutine(AutoUpdateUI());
        }
    }

    private void InitializeReferences()
    {
        gameDataManager = GameDataManager.Instance;
        if (gameDataManager == null)
        {
            Debug.LogError("GameDataManager not found!");
        }

        coinSystem = FindFirstObjectByType<CoinCollectionSystem>();
    }

    private void InitializeUI()
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
        {
            Debug.LogWarning("Game data not available yet. UI will update when data is loaded.");
            return;
        }

        // Set initial values
        targetCoins = gameDataManager.CurrentGameData.nutriCoins;
        displayedCoins = targetCoins;

        targetGems = gameDataManager.CurrentGameData.nutriGems;
        displayedGems = targetGems;

        targetLevel = gameDataManager.CurrentGameData.playerLevel;
        displayedLevel = targetLevel;

        targetXP = gameDataManager.CurrentGameData.currentXP;
        displayedXP = targetXP;

        // Update all displays immediately
        UpdateCoinDisplayImmediate();
        UpdateGemDisplayImmediate();
        UpdateLevelDisplayImmediate();
        UpdateXPDisplayImmediate();
        UpdateNameDisplay();
        LoadProfileIcon(); // Load the profile icon
        LoadFrame(); // Load the frame

        // Initialize XP slider
        if (xpSlider != null)
        {
            xpSlider.minValue = 0;
            xpSlider.maxValue = gameDataManager.CurrentGameData.xpToNextLevel;
            xpSlider.value = displayedXP;
        }
    }

    private void SetupEventListeners()
    {
        if (coinSystem != null && updateOnCoinCollect)
        {
            // You might need to modify CoinCollectionSystem to expose events
        }
    }

    private void Update()
    {
        // Smooth animations for coins
        if (isAnimatingCoins && displayedCoins != targetCoins)
        {
            int difference = targetCoins - displayedCoins;
            int change = Mathf.CeilToInt(Mathf.Sign(difference) * coinCountSpeed * Time.deltaTime);

            if (Mathf.Abs(difference) <= Mathf.Abs(change))
            {
                displayedCoins = targetCoins;
                isAnimatingCoins = false;
            }
            else
            {
                displayedCoins += change;
            }

            UpdateCoinDisplay();
        }

        // Smooth animations for gems
        if (isAnimatingGems && displayedGems != targetGems)
        {
            int difference = targetGems - displayedGems;
            int change = Mathf.CeilToInt(Mathf.Sign(difference) * gemCountSpeed * Time.deltaTime);

            if (Mathf.Abs(difference) <= Mathf.Abs(change))
            {
                displayedGems = targetGems;
                isAnimatingGems = false;
            }
            else
            {
                displayedGems += change;
            }

            UpdateGemDisplay();
        }

        // Smooth animations for XP
        if (isAnimatingXP && Mathf.Abs(displayedXP - targetXP) > 0.01f)
        {
            displayedXP = Mathf.Lerp(displayedXP, targetXP, xpFillSpeed * Time.deltaTime);
            UpdateXPDisplay();

            if (Mathf.Abs(displayedXP - targetXP) < 0.01f)
            {
                displayedXP = targetXP;
                isAnimatingXP = false;
            }
        }
    }

    private IEnumerator AutoUpdateUI()
    {
        while (autoUpdate)
        {
            UpdateAllDataFromGameData();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    public void UpdateAllDataFromGameData()
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
            return;

        // Update coins
        int newCoins = gameDataManager.CurrentGameData.nutriCoins;
        if (newCoins != targetCoins)
        {
            targetCoins = newCoins;
            if (animateCoinCounter)
            {
                isAnimatingCoins = true;
            }
            else
            {
                displayedCoins = targetCoins;
                UpdateCoinDisplayImmediate();
            }
        }

        // Update gems
        int newGems = gameDataManager.CurrentGameData.nutriGems;
        if (newGems != targetGems)
        {
            targetGems = newGems;
            if (animateGemCounter)
            {
                isAnimatingGems = true;
            }
            else
            {
                displayedGems = targetGems;
                UpdateGemDisplayImmediate();
            }
        }

        // Update level
        int newLevel = gameDataManager.CurrentGameData.playerLevel;
        if (newLevel != targetLevel)
        {
            targetLevel = newLevel;
            displayedLevel = targetLevel;
            UpdateLevelDisplayImmediate();

            if (newLevel > displayedLevel)
            {
                OnLevelUp();
            }
        }

        // Update XP
        float newXP = gameDataManager.CurrentGameData.currentXP;
        float newXPToNextLevel = gameDataManager.CurrentGameData.xpToNextLevel;

        if (Mathf.Abs(newXP - targetXP) > 0.01f || Mathf.Abs(newXPToNextLevel - xpSlider.maxValue) > 0.01f)
        {
            targetXP = newXP;

            if (xpSlider != null && Mathf.Abs(newXPToNextLevel - xpSlider.maxValue) > 0.01f)
            {
                xpSlider.maxValue = newXPToNextLevel;
            }

            if (animateCoinCounter)
            {
                isAnimatingXP = true;
            }
            else
            {
                displayedXP = targetXP;
                UpdateXPDisplayImmediate();
            }
        }

        // Update name
        UpdateNameDisplay();

        // Update profile icon and frame (in case they changed elsewhere)
        LoadProfileIcon();
        LoadFrame();
    }

    #region Profile Icon Methods

    public void LoadProfileIcon()
    {
        if (profileIconImage == null || gameDataManager?.CurrentGameData == null)
            return;

        // Try to find the icon database through ProfileSettings
        ProfileSettings profileSettings = FindFirstObjectByType<ProfileSettings>();
        if (profileSettings != null && profileSettings.iconDatabase != null)
        {
            string equippedIconId = gameDataManager.CurrentGameData.equippedIconId;
            Sprite iconSprite = profileSettings.iconDatabase.GetIconSprite(equippedIconId);

            if (iconSprite != null)
            {
                profileIconImage.sprite = iconSprite;
            }
        }
    }

    public void UpdateProfileIcon(Sprite newIconSprite)
    {
        if (profileIconImage != null && newIconSprite != null)
        {
            profileIconImage.sprite = newIconSprite;
            OnProfileIconChanged?.Invoke();
        }
    }

    public void RefreshProfileIcon()
    {
        LoadProfileIcon();
    }

    #endregion

    #region Frame Methods

    public void LoadFrame()
    {
        if (frameImage == null || gameDataManager?.CurrentGameData == null)
            return;

        // Try to find the frame database through ProfileSettings
        ProfileSettings profileSettings = FindFirstObjectByType<ProfileSettings>();
        if (profileSettings != null && profileSettings.frameDatabase != null)
        {
            string equippedFrameId = gameDataManager.CurrentGameData.equippedFrameId;
            Sprite frameSprite = profileSettings.frameDatabase.GetFrameSprite(equippedFrameId);

            if (frameSprite != null)
            {
                frameImage.sprite = frameSprite;
            }
        }
    }

    public void UpdateFrame(Sprite newFrameSprite)
    {
        if (frameImage != null && newFrameSprite != null)
        {
            frameImage.sprite = newFrameSprite;
            OnFrameChanged?.Invoke();
        }
    }

    public void RefreshFrame()
    {
        LoadFrame();
    }

    #endregion

    #region Coin Methods

    public void UpdateCoinDisplay()
    {
        if (coinsText != null)
        {
            coinsText.text = displayedCoins.ToString();
        }
    }

    public void UpdateCoinDisplayImmediate()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            displayedCoins = gameDataManager.CurrentGameData.nutriCoins;
            targetCoins = displayedCoins;
        }
        UpdateCoinDisplay();
    }

    public void OnCoinCollected(int amount = 1)
    {
        UpdateCoinDisplayImmediate();

        if (coinsText != null)
        {
            StartCoroutine(HighlightText(coinsText));
        }
    }

    // NEW: Add coins directly and update UI
    public void AddCoins(int amount)
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.nutriCoins += amount;
            gameDataManager.SaveGameData();
            UpdateCoinDisplayImmediate();
        }
    }

    #endregion

    #region Gem Methods

    public void UpdateGemDisplay()
    {
        if (gemsText != null)
        {
            gemsText.text = displayedGems.ToString();
        }
    }

    public void UpdateGemDisplayImmediate()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            displayedGems = gameDataManager.CurrentGameData.nutriGems;
            targetGems = displayedGems;
        }
        UpdateGemDisplay();
    }

    public void OnGemCollected(int amount = 1)
    {
        UpdateGemDisplayImmediate();

        if (gemsText != null)
        {
            StartCoroutine(HighlightText(gemsText));
        }
    }

    // NEW: Add gems directly and update UI
    public void AddGems(int amount)
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.nutriGems += amount;
            gameDataManager.SaveGameData();
            UpdateGemDisplayImmediate();
        }
    }

    #endregion

    #region Level Methods

    public void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            levelText.text = $"{levelPrefix}{displayedLevel}";
        }
    }

    public void UpdateLevelDisplayImmediate()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            displayedLevel = gameDataManager.CurrentGameData.playerLevel;
            targetLevel = displayedLevel;
        }
        UpdateLevelDisplay();
    }

    private void OnLevelUp()
    {
        if (levelText != null)
        {
            StartCoroutine(HighlightText(levelText));
        }

        displayedXP = 0;
        UpdateXPDisplayImmediate();

        Debug.Log($"Level Up! New Level: {displayedLevel}");
    }

    #endregion

    #region XP Methods

    public void UpdateXPDisplay()
    {
        if (xpText != null)
        {
            xpText.text = $"{xpPrefix}{displayedXP:F0}{xpSeparator}{xpSlider.maxValue:F0}";
        }

        if (xpSlider != null)
        {
            xpSlider.value = displayedXP;
        }
    }

    public void UpdateXPDisplayImmediate()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            displayedXP = gameDataManager.CurrentGameData.currentXP;
            targetXP = displayedXP;

            if (xpSlider != null)
            {
                xpSlider.maxValue = gameDataManager.CurrentGameData.xpToNextLevel;
                xpSlider.value = displayedXP;
            }
        }
        UpdateXPDisplay();
    }

    public void AddXP(float amount)
    {
        UpdateXPDisplayImmediate();

        if (xpText != null)
        {
            StartCoroutine(HighlightText(xpText));
        }
    }

    #endregion

    #region Name Methods

    public void UpdateNameDisplay()
    {
        if (nameText != null && gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            string playerName = gameDataManager.CurrentGameData.playerName;
            if (!string.IsNullOrEmpty(playerName) && nameText.text != playerName)
            {
                nameText.text = playerName;
            }
        }
    }

    public void SetPlayerName(string newName)
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.playerName = newName;
            gameDataManager.SaveGameData();
            UpdateNameDisplay();
        }
    }

    #endregion

    #region Utility Methods

    private IEnumerator HighlightText(TextMeshProUGUI text)
    {
        Color originalColor = text.color;
        text.color = highlightTextColor;

        float elapsedTime = 0f;
        while (elapsedTime < highlightDuration)
        {
            text.color = Color.Lerp(highlightTextColor, originalColor, elapsedTime / highlightDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        text.color = originalColor;
    }

    public void ForceUpdateAllUI()
    {
        UpdateCoinDisplayImmediate();
        UpdateGemDisplayImmediate();
        UpdateLevelDisplayImmediate();
        UpdateXPDisplayImmediate();
        UpdateNameDisplay();
        RefreshProfileIcon();
        RefreshFrame();
    }

    #endregion

    #region Public API for Other Scripts

    public void NotifyCoinCollected(int amount = 1)
    {
        OnCoinCollected(amount);
    }

    public void NotifyGemCollected(int amount = 1)
    {
        OnGemCollected(amount);
    }

    public void NotifyXPGained(float amount)
    {
        AddXP(amount);
    }

    public void NotifyLevelChanged()
    {
        UpdateLevelDisplayImmediate();
    }

    public int GetDisplayedCoins() => displayedCoins;
    public int GetDisplayedGems() => displayedGems;
    public int GetDisplayedLevel() => displayedLevel;
    public float GetDisplayedXP() => displayedXP;
    public string GetPlayerName() => nameText != null ? nameText.text : string.Empty;

    #endregion

    #region Event Handlers

    private void OnEnable()
    {
        ForceUpdateAllUI();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    #endregion

    #region Editor Debug Methods

    [ContextMenu("Debug: Add 10 Coins")]
    public void DebugAddCoins()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.nutriCoins += 10;
            gameDataManager.SaveGameData();
            UpdateCoinDisplayImmediate();
        }
    }

    [ContextMenu("Debug: Add 5 Gems")]
    public void DebugAddGems()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.nutriGems += 5;
            gameDataManager.SaveGameData();
            UpdateGemDisplayImmediate();
        }
    }

    [ContextMenu("Debug: Add 50 XP")]
    public void DebugAddXP()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.currentXP += 50;

            if (gameDataManager.CurrentGameData.currentXP >= gameDataManager.CurrentGameData.xpToNextLevel)
            {
                gameDataManager.CurrentGameData.playerLevel++;
                gameDataManager.CurrentGameData.currentXP = 0;
                gameDataManager.CurrentGameData.xpToNextLevel *= 1.5f;
            }

            gameDataManager.SaveGameData();
            ForceUpdateAllUI();
        }
    }

    [ContextMenu("Debug: Force Update UI")]
    public void DebugForceUpdate()
    {
        ForceUpdateAllUI();
    }

    #endregion
}