using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementButton : MonoBehaviour
{
    [Header("UI References")]
    public Image achievementIcon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI prizeText;
    public GameObject claimOverlay;
    public Button claimButton;
    public Button mainButton; // Add main button for the entire achievement

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color notCompleteColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color claimedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private AchievementDatabase.AchievementData achievementData;
    private System.Action<AchievementDatabase.AchievementData> onAchievementClickCallback; // For opening info panel
    private System.Action<AchievementDatabase.AchievementData> onClaimCallback; // For claiming
    private AchievementStatus currentStatus;
    private string achievementId;

    private void Awake()
    {
        // Setup main button if it exists
        if (mainButton == null)
            mainButton = GetComponent<Button>();

        if (mainButton != null)
            mainButton.onClick.AddListener(OnMainButtonClick);
    }

    public void Initialize(
        AchievementDatabase.AchievementData data,
        AchievementStatus status,
        System.Action<AchievementDatabase.AchievementData> clickCallback, // For opening info panel
        System.Action<AchievementDatabase.AchievementData> claimCallback = null) // Optional claim callback
    {
        achievementData = data;
        achievementId = data.id;
        currentStatus = status;
        onAchievementClickCallback = clickCallback;
        onClaimCallback = claimCallback;

        // Set visuals
        if (achievementIcon != null && data.achievementIcon != null)
            achievementIcon.sprite = data.achievementIcon;

        if (nameText != null)
            nameText.text = data.achievementName;

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (prizeText != null)
            prizeText.text = data.prizeGems.ToString();

        // Setup claim button if it exists
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimButtonClick);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        switch (currentStatus)
        {
            case AchievementStatus.NotComplete:
                achievementIcon.color = notCompleteColor;
                if (nameText != null) nameText.color = notCompleteColor;
                if (descriptionText != null) descriptionText.color = notCompleteColor;
                if (prizeText != null) prizeText.color = notCompleteColor;
                if (claimOverlay != null) claimOverlay.SetActive(false);
                break;

            case AchievementStatus.Completed:
                achievementIcon.color = normalColor;
                if (nameText != null) nameText.color = normalColor;
                if (descriptionText != null) descriptionText.color = normalColor;
                if (prizeText != null) prizeText.color = normalColor;
                if (claimOverlay != null) claimOverlay.SetActive(true);
                break;

            case AchievementStatus.Claimed:
                achievementIcon.color = claimedColor;
                if (nameText != null) nameText.color = claimedColor;
                if (descriptionText != null) descriptionText.color = claimedColor;
                if (prizeText != null) prizeText.color = claimedColor;
                if (claimOverlay != null) claimOverlay.SetActive(false);
                break;
        }
    }

    private void OnMainButtonClick()
    {
        // Always open info panel regardless of status
        onAchievementClickCallback?.Invoke(achievementData);

        // Play click sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    private void OnClaimButtonClick()
    {
        if (currentStatus == AchievementStatus.Completed)
        {
            onClaimCallback?.Invoke(achievementData);

            // Play claim sound
            if (AudioHandler.Instance != null)
            {
                AudioHandler.Instance.PlayClaimSound();
            }
        }
    }

    public void UpdateStatus(AchievementStatus newStatus)
    {
        currentStatus = newStatus;
        UpdateUI();
    }

    public string GetAchievementId()
    {
        return achievementId;
    }

    public AchievementStatus GetCurrentStatus()
    {
        return currentStatus;
    }

    public AchievementDatabase.AchievementData GetAchievementData()
    {
        return achievementData;
    }
}