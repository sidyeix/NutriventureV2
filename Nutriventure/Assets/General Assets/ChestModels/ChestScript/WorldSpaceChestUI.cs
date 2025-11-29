using UnityEngine;
using TMPro;

public class WorldSpaceChestUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText; // The single Text (TMP) component

    private Chest parentChest;

    // Text content settings
    private string timerFormat = "{0:00}:{1:00}";
    private string claimText = "CLAIM ME!";
    private Color timerColor = Color.white;
    private Color claimColor = Color.yellow;

    void Start()
    {
        // Get reference to parent chest
        parentChest = GetComponentInParent<Chest>();
        if (parentChest == null)
        {
            Debug.LogError("WorldSpaceChestUI: No parent Chest component found!");
            return;
        }

        // Check if timerText is assigned
        if (timerText == null)
        {
            Debug.LogError("WorldSpaceChestUI: Timer Text is not assigned!");
            return;
        }

        // Initialize UI state
        UpdateUIState(false);

        Debug.Log("WorldSpaceChestUI initialized for " + parentChest.ChestName);
    }

    void Update()
    {
        // Update timer if chest is not claimable yet and timerText exists
        if (parentChest != null && timerText != null && !parentChest.isClaimable)
        {
            UpdateTimerDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        if (parentChest == null || timerText == null) return;

        float timeRemaining = parentChest.GetRemainingTime();

        if (timeRemaining > 0)
        {
            // Format time as minutes:seconds
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);

            timerText.text = string.Format(timerFormat, minutes, seconds);
            timerText.color = timerColor;
        }
        else
        {
            // Timer finished, switch to claim text
            UpdateUIState(true);
        }
    }

    public void UpdateUIState(bool isClaimable)
    {
        if (timerText == null) return;

        if (isClaimable)
        {
            timerText.text = claimText;
            timerText.color = claimColor;
            timerText.fontStyle = FontStyles.Bold;
            Debug.Log("WorldSpaceUI switched to CLAIM ME for " + parentChest.ChestName);
        }
        else
        {
            // Timer mode - text will be updated in UpdateTimerDisplay
            timerText.color = timerColor;
            timerText.fontStyle = FontStyles.Normal;
        }
    }

    public void HideUI()
    {
        // Disable the entire Canvas GameObject instead of just the text
        if (gameObject != null)
        {
            gameObject.SetActive(false);
            Debug.Log("WorldSpaceUI hidden for " + (parentChest != null ? parentChest.ChestName : "unknown chest"));
        }
    }

    public void ShowUI()
    {
        // Enable the entire Canvas GameObject
        if (gameObject != null)
        {
            gameObject.SetActive(true);
            Debug.Log("WorldSpaceUI shown for " + (parentChest != null ? parentChest.ChestName : "unknown chest"));
        }
    }

    // Called when chest is opened/claimed
    public void OnChestOpened()
    {
        HideUI();
    }

    // Called when chest is clicked (before opening)
    public void OnChestClicked()
    {
        HideUI();
        if (parentChest != null)
        {
            Debug.Log("WorldSpaceUI hidden due to chest click for " + parentChest.ChestName);
        }
    }
}