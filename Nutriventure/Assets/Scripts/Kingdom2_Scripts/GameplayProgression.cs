using UnityEngine;
using TMPro;

public class GameplayProgression : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    
    [Header("Update Settings")]
    public bool autoUpdate = true;
    public float updateInterval = 0.5f;
    
    private void Start()
    {
        InitializeUI();
        
        if (autoUpdate)
        {
            // Start periodic updates
            InvokeRepeating(nameof(UpdateAllDisplays), 0f, updateInterval);
        }
    }
    
    private void InitializeUI()
    {
        UpdateCoinDisplay();
        UpdateLevelDisplay();
        UpdateXPDisplay();
    }
    
    public void UpdateCoinDisplay()
    {
        if (coinsText != null)
        {
            int currentCoins = CoinCollectionSystem.GetCurrentCoins();
            coinsText.text = $"{currentCoins}";
        }
    }
    
    public void UpdateLevelDisplay()
    {
        if (levelText != null && GameDataManager.Instance != null)
        {
            levelText.text = $"Level {GameDataManager.Instance.CurrentGameData.playerLevel}";
        }
    }
    
    public void UpdateXPDisplay()
    {
        if (xpText != null && GameDataManager.Instance != null)
        {
            float currentXP = GameDataManager.Instance.CurrentGameData.currentXP;
            float xpToNextLevel = GameDataManager.Instance.CurrentGameData.xpToNextLevel;
            xpText.text = $"{currentXP}/{xpToNextLevel} XP";
        }
    }
    
    public void UpdateAllDisplays()
    {
        UpdateCoinDisplay();
        UpdateLevelDisplay();
        UpdateXPDisplay();
    }
    
    // Called when coin is collected (can be called from other scripts)
    public void OnCoinCollected()
    {
        UpdateCoinDisplay();
    }
    
    // Called when level changes
    public void OnLevelUp()
    {
        UpdateLevelDisplay();
        UpdateXPDisplay();
    }
    
    private void OnEnable()
    {
        // Refresh UI when this component is enabled
        UpdateAllDisplays();
    }
    
    private void OnDestroy()
    {
        // Clean up repeating invokes
        if (autoUpdate)
        {
            CancelInvoke(nameof(UpdateAllDisplays));
        }
    }
}