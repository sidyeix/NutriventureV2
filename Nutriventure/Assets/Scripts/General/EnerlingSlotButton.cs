using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnerlingSlotButton : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private int slotIndex = 0; // 0 for first pet, 1 for second pet
    [SerializeField] private Image petIconImage;
    [SerializeField] private Button slotButton;
    [SerializeField] private Button changeButton; // Change button that appears when slot is clicked
    [SerializeField] private GameObject emptySlotIndicator;

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
            changeButton.gameObject.SetActive(false); // Start hidden
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
        }
        else
        {
            ShowEmptySlot();
        }
    }

    void OnSlotButtonClicked()
    {
        // Play button sound
        if (AudioHandler.Instance != null)
            AudioHandler.Instance.PlayButtonClick();

        // Show the change button
        if (changeButton != null)
        {
            isChangeButtonVisible = !isChangeButtonVisible;
            changeButton.gameObject.SetActive(isChangeButtonVisible);
        }
    }

    void OnChangeButtonClicked()
    {
        // Play button sound
        if (AudioHandler.Instance != null)
            AudioHandler.Instance.PlayButtonClick();

        // Hide the change button
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

        // Save to GameData
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            if (slotIndex == 0)
                GameDataManager.Instance.CurrentGameData.equippedPetSlot1 = petName;
            else
                GameDataManager.Instance.CurrentGameData.equippedPetSlot2 = petName;

            GameDataManager.Instance.SaveGameData();
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
                petIconImage.gameObject.SetActive(true);
            }
        }

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(false);
    }

    void ShowEmptySlot()
    {
        if (petIconImage != null)
            petIconImage.gameObject.SetActive(false);

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(true);

        equippedPetName = "";
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

        // Hide change button if visible
        if (changeButton != null)
        {
            changeButton.gameObject.SetActive(false);
            isChangeButtonVisible = false;
        }
    }

    public string GetEquippedPetName() => equippedPetName;
}