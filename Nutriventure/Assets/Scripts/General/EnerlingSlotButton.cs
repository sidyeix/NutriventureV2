using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnerlingSlotButton : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private int slotIndex = 0; // 0 for first pet, 1 for second pet
    [SerializeField] private Image petIconImage;
    [SerializeField] private Button slotButton;
    [SerializeField] private Button changeButton; // Change button that appears when slot is empty or clicked
    [SerializeField] private Image changeButtonImage; // Reference to the change button's image component
    [SerializeField] private Sprite addSprite; // Sprite to show when slot is empty
    [SerializeField] private Sprite changeSprite; // Sprite to show when slot has a pet

    [Header("Name Display")]
    [SerializeField] private TextMeshProUGUI enerlingNameText; // Text to display enerling name or "Add Enerling"

    [Header("Spawn Points")]
    [SerializeField] private Transform[] walkingSpawnPoints;
    [SerializeField] private Transform[] flyingSpawnPoints;

    private EnerlingSelectionController selectionController;
    private string equippedPetName = "";
    private IngredientDatabase ingredientDatabase;
    private bool isChangeButtonVisible = false;

    void Start()
    {
        if (slotButton == null)
            slotButton = GetComponent<Button>();

        slotButton.onClick.AddListener(OnSlotButtonClicked);

        // Setup change button
        if (changeButton != null)
        {
            changeButton.onClick.AddListener(OnChangeButtonClicked);
        }

        selectionController = FindObjectOfType<EnerlingSelectionController>();

        if (selectionController != null)
            ingredientDatabase = selectionController.ingredientDatabase;

        // Load equipped pet from GameData
        LoadEquippedPet();
    }

    void LoadEquippedPet()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
            return;

        string savedPet = slotIndex == 0 ?
            GameDataManager.Instance.CurrentGameData.equippedPetSlot1 :
            GameDataManager.Instance.CurrentGameData.equippedPetSlot2;

        if (!string.IsNullOrEmpty(savedPet))
        {
            equippedPetName = savedPet;
            UpdateButtonIcon(savedPet);
            UpdateNameDisplay(savedPet);
            UpdateChangeButtonForEquippedPet();
        }
        else
        {
            ShowEmptySlot();
        }
    }

    // NEW: Refresh the slot state (called when selection canvas closes)
    public void RefreshSlotState()
    {
        LoadEquippedPet();
    }

    void OnSlotButtonClicked()
    {
        // Play button sound
        if (AudioHandler.Instance != null)
            AudioHandler.Instance.PlayButtonClick();

        // If there's an equipped pet, show its info
        if (!string.IsNullOrEmpty(equippedPetName))
        {
            ShowEquippedPetInfo();
        }

        // For empty slots, clicking the slot does nothing (change button is always visible and doesn't toggle)
        // For equipped slots, toggle the change button visibility
        if (!string.IsNullOrEmpty(equippedPetName))
        {
            isChangeButtonVisible = !isChangeButtonVisible;
            if (changeButton != null)
                changeButton.gameObject.SetActive(isChangeButtonVisible);
        }
    }

    // Show information of the equipped pet
    void ShowEquippedPetInfo()
    {
        if (selectionController != null && ingredientDatabase != null)
        {
            var ingredient = ingredientDatabase.GetIngredientInfo(equippedPetName);
            if (ingredient != null)
            {
                selectionController.ShowPetInfo(ingredient, slotIndex);
            }
        }
    }

    void OnChangeButtonClicked()
    {
        // Play button sound
        if (AudioHandler.Instance != null)
            AudioHandler.Instance.PlayButtonClick();

        // Hide the change button when clicked (it will be shown again when the selection closes based on slot state)
        if (changeButton != null)
        {
            changeButton.gameObject.SetActive(false);
            isChangeButtonVisible = false;
        }

        // Open selection canvas
        if (selectionController != null)
        {
            selectionController.OpenSelectionForSlot(slotIndex, this);
        }
    }

    public void EquipPet(string petName, Sprite petIcon)
    {
        equippedPetName = petName;
        UpdateButtonIcon(petName);
        UpdateNameDisplay(petName);
        UpdateChangeButtonForEquippedPet();

        // Save to GameData
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            if (slotIndex == 0)
                GameDataManager.Instance.CurrentGameData.equippedPetSlot1 = petName;
            else
                GameDataManager.Instance.CurrentGameData.equippedPetSlot2 = petName;

            GameDataManager.Instance.SaveGameData();
        }

        // Register power-ups for this pet
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.RegisterPetPowerUps(petName);
        }

        // Spawn the pet
        SpawnPet(petName);
    }

    void UpdateButtonIcon(string petName)
    {
        if (petIconImage != null && ingredientDatabase != null)
        {
            var ingredient = ingredientDatabase.GetIngredientInfo(petName);
            if (ingredient != null && ingredient.enerlingSprite != null)
            {
                petIconImage.sprite = ingredient.enerlingSprite;
                // Set color to white with full alpha (normal)
                SetImageColor(petIconImage, Color.white, 1f);
                petIconImage.gameObject.SetActive(true);
            }
        }
    }

    void UpdateNameDisplay(string petName)
    {
        if (enerlingNameText != null && ingredientDatabase != null)
        {
            var ingredient = ingredientDatabase.GetIngredientInfo(petName);
            if (ingredient != null)
            {
                enerlingNameText.text = ingredient.ingredientName;
            }
        }
    }

    void UpdateChangeButtonForEquippedPet()
    {
        if (changeButton != null)
        {
            // Update the change button sprite to changeSprite
            if (changeButtonImage != null && changeSprite != null)
                changeButtonImage.sprite = changeSprite;

            // Initially hide the change button (only shown when slot is clicked)
            changeButton.gameObject.SetActive(false);
            isChangeButtonVisible = false;
        }
    }

    void ShowEmptySlot()
    {
        equippedPetName = "";

        // Make pet icon white with 0 alpha (completely transparent)
        if (petIconImage != null)
        {
            // Keep the sprite but make it completely transparent
            SetImageColor(petIconImage, Color.white, 0f);
            petIconImage.gameObject.SetActive(true);
        }

        // Update name display to "Add Enerling"
        if (enerlingNameText != null)
        {
            enerlingNameText.text = "Add Enerling";
        }

        // Update change button for empty slot - always visible with add sprite
        if (changeButton != null)
        {
            // Set the add sprite
            if (changeButtonImage != null && addSprite != null)
                changeButtonImage.sprite = addSprite;

            // Always show the change button when slot is empty
            changeButton.gameObject.SetActive(true);
            isChangeButtonVisible = true;
        }
    }

    // Helper method to set image color and alpha
    private void SetImageColor(Image image, Color color, float alpha)
    {
        if (image != null)
        {
            color.a = alpha;
            image.color = color;
        }
    }

    void SpawnPet(string petName)
    {
        EnerlingPetManager petManager = FindObjectOfType<EnerlingPetManager>();
        if (petManager != null)
        {
            petManager.SpawnPet(petName, slotIndex);
        }
    }

    public void ClearSlot()
    {
        string oldPetName = equippedPetName;

        equippedPetName = "";
        ShowEmptySlot();

        // Clear from GameData
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            if (slotIndex == 0)
                GameDataManager.Instance.CurrentGameData.equippedPetSlot1 = "";
            else
                GameDataManager.Instance.CurrentGameData.equippedPetSlot2 = "";

            GameDataManager.Instance.SaveGameData();
        }

        // Unregister power-ups for this pet
        if (!string.IsNullOrEmpty(oldPetName) && PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.UnregisterPetPowerUps(oldPetName);
        }
    }

    public string GetEquippedPetName() => equippedPetName;
}