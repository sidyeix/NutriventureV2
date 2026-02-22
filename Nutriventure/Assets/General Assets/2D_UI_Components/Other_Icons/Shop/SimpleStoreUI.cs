using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SimpleStoreUI : MonoBehaviour
{
    public static SimpleStoreUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject storePanel;
    public Image itemImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public TextMeshProUGUI itemDescriptionText;
    public Button buyButton;
    public Button backButton;
    public Button exitButton;

    [Header("Player Resource Display")]
    public TextMeshProUGUI coinsText;  // Add this reference
    public TextMeshProUGUI gemsText;   // Add this reference

    [Header("Confirmation Panel")]
    public GameObject confirmPanel;
    public TextMeshProUGUI confirmText;
    public Button yesButton;
    public Button noButton;

    [Header("Error Panel")]
    public GameObject errorPanel;
    public TextMeshProUGUI errorText;
    public float errorShowTime = 2f;

    [Header("Database")]
    public CharacterDatabase characterDatabase;

    [Header("Audio")]
    public AudioSource sfxAudioSource;
    public AudioClip buttonClickSound;
    public AudioClip purchaseSuccessSound;
    public AudioClip errorSound;

    private SimpleStoreItem currentItem;
    private int currentCharacterID = -1;
    private Coroutine errorCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("=== STORE UI STARTING ===");

        // Setup button listeners
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
            SetupButtonSound(buyButton);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
            SetupButtonSound(backButton);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClicked);
            SetupButtonSound(exitButton);
        }

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(ConfirmPurchase);
            SetupButtonSound(yesButton);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(CancelPurchase);
            SetupButtonSound(noButton);
        }

        // Hide panels initially
        storePanel.SetActive(false);
        confirmPanel.SetActive(false);
        errorPanel.SetActive(false);

        // Initial button states
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(true);

        // Initial resource display
        UpdateResourceDisplay();

        Debug.Log("Store UI initialized");
    }

    void SetupButtonSound(Button button)
    {
        if (button != null)
        {
            button.onClick.AddListener(PlayButtonClickSound);
        }
    }

    void PlayButtonClickSound()
    {
        if (sfxAudioSource != null && buttonClickSound != null)
        {
            sfxAudioSource.PlayOneShot(buttonClickSound);
        }
        else if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    void PlayPurchaseSuccessSound()
    {
        if (sfxAudioSource != null && purchaseSuccessSound != null)
        {
            sfxAudioSource.PlayOneShot(purchaseSuccessSound);
        }
        else if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayClaimSound();
        }
    }

    void PlayErrorSound()
    {
        if (sfxAudioSource != null && errorSound != null)
        {
            sfxAudioSource.PlayOneShot(errorSound);
        }
    }

    public void ShowItemDetails(SimpleStoreItem item)
    {
        if (item == null)
        {
            Debug.LogError("Item is null!");
            return;
        }

        currentItem = item;
        currentCharacterID = item.characterID;

        CharacterDatabase.CharacterData charData = characterDatabase.GetCharacterByID(item.characterID);

        if (charData == null)
        {
            ShowError("Character not found!");
            return;
        }

        Debug.Log($"Showing: {charData.characterName}");

        // Update UI
        if (itemImage != null && charData.characterIcon != null)
        {
            itemImage.sprite = charData.characterIcon;
            itemImage.gameObject.SetActive(true);
        }

        if (itemNameText != null)
            itemNameText.text = charData.characterName;

        if (itemPriceText != null)
            itemPriceText.text = $"{charData.coinsToUnlock} N";

        if (itemDescriptionText != null)
            itemDescriptionText.text = charData.characterDescription;

        // Show store panel
        storePanel.SetActive(true);

        // Show back button, hide exit button
        if (backButton != null) backButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(false);

        // Update resource display
        UpdateResourceDisplay();

        // Play character selection sound
        if (charData.selectionSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(charData.selectionSound);
        }

        // Update buy button
        UpdateBuyButton();
    }

    void UpdateBuyButton()
    {
        if (buyButton == null || currentCharacterID == -1) return;

        CharacterDatabase.CharacterData charData = characterDatabase.GetCharacterByID(currentCharacterID);
        if (charData == null) return;

        buyButton.interactable = true;

        TextMeshProUGUI buttonText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = "BUY";
        }
    }

    void OnBuyClicked()
    {
        if (currentCharacterID == -1)
        {
            Debug.LogError("No item selected!");
            return;
        }

        CharacterDatabase.CharacterData charData = characterDatabase.GetCharacterByID(currentCharacterID);
        if (charData == null) return;

        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            int currentCoins = GameDataManager.Instance.CurrentGameData.nutriCoins;

            if (currentCoins >= charData.coinsToUnlock)
            {
                if (confirmPanel != null && confirmText != null)
                {
                    confirmText.text = $"Buy {charData.characterName} for {charData.coinsToUnlock} N?";
                    confirmPanel.SetActive(true);
                }
            }
            else
            {
                ShowError($"Not enough coins! Need {charData.coinsToUnlock} N. You have {currentCoins} coins.");
            }
        }
    }

    void ConfirmPurchase()
    {
        if (currentCharacterID == -1)
        {
            Debug.LogError("No item to purchase!");
            return;
        }

        CharacterDatabase.CharacterData charData = characterDatabase.GetCharacterByID(currentCharacterID);
        if (charData == null) return;

        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
        {
            ShowError("Game data error!");
            CancelPurchase();
            return;
        }

        int currentCoins = GameDataManager.Instance.CurrentGameData.nutriCoins;
        int itemCost = charData.coinsToUnlock;

        if (currentCoins >= itemCost)
        {
            // Deduct coins
            GameDataManager.Instance.CurrentGameData.nutriCoins -= itemCost;

            // Unlock character
            if (!GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs.Contains(currentCharacterID))
            {
                GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs.Add(currentCharacterID);
            }

            // Save game
            GameDataManager.Instance.SaveGameData();

            // Update resource display immediately
            UpdateResourceDisplay();

            // Also update Player_Data if available
            UpdatePlayerDataDisplay();

            // Play purchase success sound
            PlayPurchaseSuccessSound();

            // Hide the 3D object
            if (currentItem != null)
            {
                currentItem.HideItem();
            }

            // Refresh all store items
            RefreshAllStoreItems();

            // Close panels
            confirmPanel.SetActive(false);
            HideStore();

            Debug.Log($"Purchased character {currentCharacterID}!");
        }
        else
        {
            ShowError($"Not enough coins! Need {itemCost} N.");
            CancelPurchase();
        }
    }

    void CancelPurchase()
    {
        confirmPanel.SetActive(false);
    }

    void ShowError(string message)
    {
        Debug.LogError($"Store Error: {message}");

        // Play error sound
        PlayErrorSound();

        if (errorPanel != null && errorText != null)
        {
            errorText.text = message;
            errorPanel.SetActive(true);

            if (errorCoroutine != null)
            {
                StopCoroutine(errorCoroutine);
            }
            errorCoroutine = StartCoroutine(HideError());
        }
    }

    IEnumerator HideError()
    {
        yield return new WaitForSeconds(errorShowTime);
        errorPanel.SetActive(false);
        errorCoroutine = null;
    }

    void OnBackClicked()
    {
        HideStore();
    }

    void OnExitClicked()
    {
        HideStore();
    }

    public void HideStore()
    {
        // Hide store panel
        storePanel.SetActive(false);
        confirmPanel.SetActive(false);

        // Hide error panel if showing
        if (errorPanel != null && errorPanel.activeSelf)
        {
            errorPanel.SetActive(false);
            if (errorCoroutine != null)
            {
                StopCoroutine(errorCoroutine);
                errorCoroutine = null;
            }
        }

        // Reset current item
        currentItem = null;
        currentCharacterID = -1;

        // Hide back button, show exit button
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(true);
    }

    public void ShowStoreUI()
    {
        // When shop opens, ensure exit button is visible and update resources
        if (exitButton != null) exitButton.gameObject.SetActive(true);
        if (backButton != null) backButton.gameObject.SetActive(false);

        // Update resource display when shop opens
        UpdateResourceDisplay();
    }

    public void RefreshAllStoreItems()
    {
        SimpleStoreItem[] allItems = FindObjectsOfType<SimpleStoreItem>(true);
        foreach (var item in allItems)
        {
            if (item != null)
            {
                item.RefreshVisibility();
            }
        }
    }

    // NEW METHOD: Update coin and gems display
    public void UpdateResourceDisplay()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
        {
            Debug.LogWarning("GameData not available for resource display");
            return;
        }

        // Update coins text
        if (coinsText != null)
        {
            coinsText.text = GameDataManager.Instance.CurrentGameData.nutriCoins.ToString();
        }

        // Update gems text (if you have gems in GameData)
        // Note: Your current GameData doesn't have gems, so I'm adding a placeholder
        // If you add gems later, update this line
        if (gemsText != null)
        {
            // If you have gems in GameData, use:
            // gemsText.text = GameDataManager.Instance.CurrentGameData.gems.ToString();

            // For now, set to 0 or your placeholder
            gemsText.text = "0"; // Change this when you add gems to GameData
        }
    }

    // NEW METHOD: Update Player_Data display as well
    private void UpdatePlayerDataDisplay()
    {
        Player_Data playerData = FindObjectOfType<Player_Data>();
        if (playerData != null)
        {
            playerData.UpdateCoinDisplayImmediate();
        }
    }

    void Update()
    {
        // Update button in real-time
        if (storePanel.activeSelf && currentCharacterID != -1)
        {
            UpdateBuyButton();
        }
    }

    // Called when shop opens
    public void OnShopOpened()
    {
        RefreshAllStoreItems();
        ShowStoreUI();
    }

    // Called when shop closes
    public void OnShopClosed()
    {
        HideStore();
    }
}