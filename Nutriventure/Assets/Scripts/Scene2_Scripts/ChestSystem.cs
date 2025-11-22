using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;

public class ChestSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject chestPanel;
    public Button chestButton;
    public Button claimButton;
    public Button closeButton;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI timerText;
    
    [Header("Chest Images")]
    public RawImage closedChestImage;
    public RawImage openChestImage;
    
    [Header("Coin Popup")]
    public GameObject coinPopup;
    public TextMeshProUGUI coinPopupText;
    public float popupDuration = 2f;
    
    [Header("Audio")]
    public AudioClip chestOpenSound;
    public AudioClip coinsCollectSound;
    
    [Header("Coin Confetti")]
    public ParticleSystem coinConfettiParticles;
    public int confettiParticleCount = 20;
    
    [Header("Chest Settings")]
    public int coinsReward = 50;
    public float cooldownHours = 3f;
    
    [Header("Animation Settings")]
    public float chestOpenDuration = 0.5f;
    
    private bool isAnimating = false;
    
    private void Start()
    {
        InitializeChestSystem();
    }
    
    private void InitializeChestSystem()
    {
        // Setup button listeners
        chestButton.onClick.AddListener(OnChestButtonClicked);
        claimButton.onClick.AddListener(OnClaimButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        
        // Initialize chest images
        InitializeChestImages();
        
        // Update UI state
        UpdateChestUI();
        
        // Start cooldown timer coroutine
        StartCoroutine(UpdateChestTimer());
    }
    
    private void InitializeChestImages()
    {
        // Start with closed chest visible, open chest hidden
        if (closedChestImage != null)
            closedChestImage.gameObject.SetActive(true);
        
        if (openChestImage != null)
            openChestImage.gameObject.SetActive(false);
        
        // Hide coin popup initially
        if (coinPopup != null)
            coinPopup.SetActive(false);
    }
    
    private void OnChestButtonClicked()
    {
        // Play button click sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
        
        // Show chest panel
        chestPanel.SetActive(true);
        
        // Reset chest to closed state when opening panel
        ResetChestToClosed();
        UpdateChestPanelUI();
    }
    
    private void OnClaimButtonClicked()
    {
        // Don't allow multiple clicks during animation
        if (isAnimating) return;
        
        // Play button click sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
        
        // Claim the chest reward
        if (GameDataManager.Instance.CanClaimChest())
        {
            StartCoroutine(ClaimChestRewardWithAnimation());
        }
        else
        {
            Debug.Log("Chest is not available yet!");
        }
    }
    
    private IEnumerator ClaimChestRewardWithAnimation()
    {
        isAnimating = true;
        
        // Disable claim button during animation
        claimButton.interactable = false;
        
        // Store current coins for popup display
        int currentCoins = GameDataManager.Instance.CurrentGameData.nutriCoins;
        
        // Play chest open sound
        if (chestOpenSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(chestOpenSound);
        }
        
        // Animate chest opening
        yield return StartCoroutine(OpenChestAnimation());
        
        // Brief pause before coins effect
        yield return new WaitForSeconds(0.3f);
        
        // Play coins collect sound
        if (coinsCollectSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(coinsCollectSound);
        }
        
        // Show coin confetti
        PlayCoinConfetti();
        
        // Claim the reward
        GameDataManager.Instance.ClaimChestReward();
        
        // Show coin popup
        ShowCoinPopup(coinsReward);
        
        // Update UI
        UpdateChestUI();
        UpdateChestPanelUI();
        
        Debug.Log("Chest claimed successfully!");
        
        // Wait for animations to complete
        yield return new WaitForSeconds(1f);
        
        isAnimating = false;
        
        // Re-enable claim button (it will be disabled by UpdateChestPanelUI if cooldown starts)
        UpdateChestPanelUI();
    }
    
    private IEnumerator OpenChestAnimation()
    {
        // Smooth transition between chest images
        if (closedChestImage != null && openChestImage != null)
        {
            // Start with both images active but open chest at zero scale
            openChestImage.gameObject.SetActive(true);
            openChestImage.transform.localScale = Vector3.zero;
            openChestImage.color = new Color(1, 1, 1, 0);
            
            closedChestImage.gameObject.SetActive(true);
            closedChestImage.color = new Color(1, 1, 1, 1);
            
            float elapsedTime = 0f;
            
            // Scale up and fade in open chest while fading out closed chest
            while (elapsedTime < chestOpenDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / chestOpenDuration;
                
                // Scale open chest
                openChestImage.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, progress);
                
                // Fade transition
                float alpha = Mathf.Clamp01(progress * 2f); // Faster fade in
                openChestImage.color = new Color(1, 1, 1, alpha);
                closedChestImage.color = new Color(1, 1, 1, 1f - alpha);
                
                yield return null;
            }
            
            // Ensure final states
            openChestImage.transform.localScale = Vector3.one;
            openChestImage.color = Color.white;
            closedChestImage.gameObject.SetActive(false);
        }
        else
        {
            // Fallback: simple toggle if something is missing
            if (closedChestImage != null)
                closedChestImage.gameObject.SetActive(false);
            
            if (openChestImage != null)
                openChestImage.gameObject.SetActive(true);
        }
        
        yield return new WaitForSeconds(0.3f); // Brief pause after opening
    }
    
    private void PlayCoinConfetti()
    {
        if (coinConfettiParticles != null)
        {
            // Play the particle system
            coinConfettiParticles.Play();
            
            // Optional: Emit burst of particles
            if (confettiParticleCount > 0)
            {
                coinConfettiParticles.Emit(confettiParticleCount);
            }
        }
        else
        {
            Debug.LogWarning("Coin confetti particles not assigned!");
        }
    }
    
    private void ShowCoinPopup(int coinsCollected)
    {
        if (coinPopup != null && coinPopupText != null)
        {
            coinPopupText.text = $"+{coinsCollected}";
            coinPopup.SetActive(true);
            
            // Start coroutine to hide popup after duration
            StartCoroutine(HideCoinPopupAfterDelay());
        }
    }
    
    private IEnumerator HideCoinPopupAfterDelay()
    {
        yield return new WaitForSeconds(popupDuration);
        
        if (coinPopup != null)
        {
            coinPopup.SetActive(false);
        }
    }
    
    private void ResetChestToClosed()
    {
        // Reset to closed chest state
        if (closedChestImage != null)
        {
            closedChestImage.gameObject.SetActive(true);
            closedChestImage.transform.localScale = Vector3.one;
            closedChestImage.color = Color.white;
        }
        
        if (openChestImage != null)
        {
            openChestImage.gameObject.SetActive(false);
            openChestImage.transform.localScale = Vector3.one;
            openChestImage.color = Color.white;
        }
        
        // Hide coin popup
        if (coinPopup != null)
            coinPopup.SetActive(false);
            
        // Stop any ongoing confetti
        if (coinConfettiParticles != null)
            coinConfettiParticles.Stop();
    }
    
    private void OnCloseButtonClicked()
    {
        // Don't allow closing during animation
        if (isAnimating) return;
        
        // Play button click sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
        
        // Reset chest state before closing
        ResetChestToClosed();
        
        // Close chest panel
        chestPanel.SetActive(false);
    }
    
    private void UpdateChestUI()
    {
        // Update chest button appearance based on availability
        if (GameDataManager.Instance.CanClaimChest())
        {
            chestButton.interactable = true;
        }
        else
        {
            chestButton.interactable = true; // Still interactable to show timer
        }
    }
    
    private void UpdateChestPanelUI()
    {
        // Update coins text - show current coins
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            coinsText.text = $"{GameDataManager.Instance.CurrentGameData.nutriCoins}";
        }
        else
        {
            coinsText.text = "0";
        }
        
        // Update claim button and timer
        if (GameDataManager.Instance.CanClaimChest())
        {
            claimButton.interactable = !isAnimating; // Only enable if not animating
            timerText.text = "Ready to claim!";
            
            // Ensure chest shows as closed when available (unless animating)
            if (!isAnimating && closedChestImage != null && !closedChestImage.gameObject.activeInHierarchy)
            {
                ResetChestToClosed();
            }
        }
        else
        {
            claimButton.interactable = false;
            
            // Update timer
            TimeSpan timeRemaining = GameDataManager.Instance.GetTimeUntilNextChest();
            timerText.text = FormatTimeRemaining(timeRemaining);
            
            // Show open chest if it was recently claimed (and not animating)
            if (!isAnimating && closedChestImage != null && openChestImage != null)
            {
                // Keep chest open during cooldown to show it was claimed
                closedChestImage.gameObject.SetActive(false);
                openChestImage.gameObject.SetActive(true);
            }
        }
    }
    
    private string FormatTimeRemaining(TimeSpan timeRemaining)
    {
        if (timeRemaining.TotalHours >= 1)
        {
            return $"{timeRemaining.Hours}h {timeRemaining.Minutes:00}m {timeRemaining.Seconds:00}s";
        }
        else if (timeRemaining.TotalMinutes >= 1)
        {
            return $"{timeRemaining.Minutes}m {timeRemaining.Seconds:00}s";
        }
        else
        {
            return $"{timeRemaining.Seconds}s";
        }
    }
    
    private IEnumerator UpdateChestTimer()
    {
        while (true)
        {
            // Update the UI every second
            if (chestPanel.activeInHierarchy)
            {
                UpdateChestPanelUI();
            }
            
            // Update chest button appearance
            UpdateChestUI();
            
            yield return new WaitForSeconds(1f);
        }
    }
    
    // Public method to force check chest availability (useful when coming back to game)
    public void RefreshChestState()
    {
        // This will trigger the cooldown check in GameDataManager
        if (GameDataManager.Instance != null)
        {
            // The check happens automatically in LoadGameData()
            // We just need to update the UI
            UpdateChestUI();
            UpdateChestPanelUI();
        }
    }
    
    private void OnEnable()
    {
        // Refresh chest state when this component is enabled
        RefreshChestState();
    }
}