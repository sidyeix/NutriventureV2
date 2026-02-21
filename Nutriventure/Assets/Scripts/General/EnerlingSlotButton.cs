using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnerlingSlotButton : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private int slotIndex = 0; // 0 for first pet, 1 for second pet
    [SerializeField] private Image petIconImage;
    [SerializeField] private Button slotButton;
    [SerializeField] private Button removeButton; // New remove button
    [SerializeField] private GameObject emptySlotIndicator;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] walkingSpawnPoints;
    [SerializeField] private Transform[] flyingSpawnPoints;

    private EnerlingSelectionController selectionController;
    private string equippedPetName = "";
    private IngredientDatabase ingredientDatabase;

    void Start()
    {
        if (slotButton == null)
            slotButton = GetComponent<Button>();

        slotButton.onClick.AddListener(OnSlotButtonClicked);

        // Setup remove button
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
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

        if (selectionController != null)
        {
            selectionController.OpenSelectionForSlot(slotIndex, this);
        }
    }

    void OnRemoveButtonClicked()
    {
        // Play button sound
        if (AudioHandler.Instance != null)
            AudioHandler.Instance.PlayButtonClick();

        // Remove the pet
        EnerlingPetManager petManager = FindObjectOfType<EnerlingPetManager>();
        if (petManager != null)
        {
            petManager.RemovePet(slotIndex);
        }

        // Clear the slot
        ClearSlot();
    }

    public void EquipPet(string petName, Sprite petIcon)
    {
        equippedPetName = petName;
        UpdateButtonIcon(petName);

        // Show remove button
        if (removeButton != null)
            removeButton.gameObject.SetActive(true);

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

        if (removeButton != null)
            removeButton.gameObject.SetActive(false);

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
    }

    public string GetEquippedPetName() => equippedPetName;
}