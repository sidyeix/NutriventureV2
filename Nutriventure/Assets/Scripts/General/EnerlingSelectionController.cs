using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class EnerlingSelectionController : MonoBehaviour
{
    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase;

    [Header("UI References")]
    public GameObject selectionCanvas;
    public Transform contentParent;
    public GameObject oddRowPrefab; // 3 buttons
    public GameObject evenRowPrefab; // 4 buttons
    public GameObject enerlingButtonPrefab;
    public Button closeButton;

    [Header("Side Info Panel")]
    public GameObject infoPanel;
    public Image rarityIconImage;
    public TextMeshProUGUI enerlingNameText;
    public TextMeshProUGUI enerlingDescriptionText;
    public RawImage enerlingPreviewImage;
    public Transform organsContainer;
    public GameObject organIconPrefab;

    [Header("Power-Up Display")]
    public Image powerUpIconImage;           // Direct reference for power-up icon
    public TextMeshProUGUI powerUpAmountText; // Direct reference for amount text
    public TextMeshProUGUI powerUpDescriptionText; // Direct reference for description text

    [Header("Preview Spawn")]
    public Transform previewSpawnPoint;
    public Camera previewCamera;
    public RenderTexture previewRenderTexture;
    public float previewRotationSpeed = 30f;

    [Header("Filters")]
    public Button allFilterButton;
    public Button commonFilterButton;
    public Button rareFilterButton;
    public Button ultraRareFilterButton;
    public Button allKingdomButton;
    public Button nutriKingdomButton;
    public Button alerthiaButton;
    public Button sugariaButton;
    public Button preserviaButton;

    [Header("Action Buttons")]
    public Button equipButton;
    public TextMeshProUGUI equipButtonText;
    public Button removeButton; // New remove button
    public TextMeshProUGUI removeButtonText;

    [Header("Button Colors")]
    public Color normalButtonColor = Color.white;
    public Color selectedButtonColor = new Color(0.52f, 0.52f, 0.52f);
    public Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Color for disabled buttons

    [Header("Audio")]
    public AudioSource sfxAudioSource;
    public AudioClip buttonClickSound;

    // Current selection
    private int currentSlotIndex = -1;
    private EnerlingSlotButton currentSlotButton;
    private IngredientDatabase.IngredientInfo selectedEnerling;
    private GameObject currentPreviewModel;
    private List<GameObject> currentRows = new List<GameObject>();
    private Dictionary<string, GameObject> enerlingButtons = new Dictionary<string, GameObject>();
    private List<IngredientDatabase.IngredientInfo> currentFilteredEnerlings = new List<IngredientDatabase.IngredientInfo>();

    // Track which slot's pet is equipped in the other slot
    private string otherSlotEquippedPet = "";

    // Filter states
    private IngredientDatabase.Rarity currentRarityFilter = IngredientDatabase.Rarity.Common;
    private IngredientDatabase.KingdomOrigin currentKingdomFilter = IngredientDatabase.KingdomOrigin.NutriKingdom;
    private bool useRarityFilter = false;
    private bool useKingdomFilter = false;

    void Start()
    {
        selectionCanvas.SetActive(false);
        infoPanel.SetActive(false);
        SetupFilterButtons();
        SetupKingdomButtons();

        equipButton.onClick.AddListener(OnEquipButtonClicked);

        // Setup remove button
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
            removeButton.gameObject.SetActive(false); // Start hidden
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSelection);
        }

        // Initialize power-up display to hidden/empty state
        ClearPowerUpDisplay();
    }

    void SetupFilterButtons()
    {
        allFilterButton.onClick.AddListener(() => SetRarityFilter(false, IngredientDatabase.Rarity.Common));
        commonFilterButton.onClick.AddListener(() => SetRarityFilter(true, IngredientDatabase.Rarity.Common));
        rareFilterButton.onClick.AddListener(() => SetRarityFilter(true, IngredientDatabase.Rarity.Rare));
        ultraRareFilterButton.onClick.AddListener(() => SetRarityFilter(true, IngredientDatabase.Rarity.UltraRare));
    }

    void SetupKingdomButtons()
    {
        allKingdomButton.onClick.AddListener(() => SetKingdomFilter(false, IngredientDatabase.KingdomOrigin.NutriKingdom));
        nutriKingdomButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.NutriKingdom));
        alerthiaButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.Alerthia));
        sugariaButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.Sugaria));
        preserviaButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.Preservia));
    }

    void SetRarityFilter(bool useFilter, IngredientDatabase.Rarity rarity)
    {
        // Play button click sound
        PlayButtonClickSound();

        useRarityFilter = useFilter;
        currentRarityFilter = rarity;
        UpdateFilterButtonColors();
        RefreshDisplay();
    }

    void SetKingdomFilter(bool useFilter, IngredientDatabase.KingdomOrigin kingdom)
    {
        // Play button click sound
        PlayButtonClickSound();

        useKingdomFilter = useFilter;
        currentKingdomFilter = kingdom;
        UpdateKingdomButtonColors();
        RefreshDisplay();
    }

    void UpdateFilterButtonColors()
    {
        allFilterButton.image.color = normalButtonColor;
        commonFilterButton.image.color = normalButtonColor;
        rareFilterButton.image.color = normalButtonColor;
        ultraRareFilterButton.image.color = normalButtonColor;

        if (!useRarityFilter)
        {
            allFilterButton.image.color = selectedButtonColor;
        }
        else
        {
            switch (currentRarityFilter)
            {
                case IngredientDatabase.Rarity.Common:
                    commonFilterButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.Rarity.Rare:
                    rareFilterButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.Rarity.UltraRare:
                    ultraRareFilterButton.image.color = selectedButtonColor;
                    break;
            }
        }
    }

    void UpdateKingdomButtonColors()
    {
        allKingdomButton.image.color = normalButtonColor;
        nutriKingdomButton.image.color = normalButtonColor;
        alerthiaButton.image.color = normalButtonColor;
        sugariaButton.image.color = normalButtonColor;
        preserviaButton.image.color = normalButtonColor;

        if (!useKingdomFilter)
        {
            allKingdomButton.image.color = selectedButtonColor;
        }
        else
        {
            switch (currentKingdomFilter)
            {
                case IngredientDatabase.KingdomOrigin.NutriKingdom:
                    nutriKingdomButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.KingdomOrigin.Alerthia:
                    alerthiaButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.KingdomOrigin.Sugaria:
                    sugariaButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.KingdomOrigin.Preservia:
                    preserviaButton.image.color = selectedButtonColor;
                    break;
            }
        }
    }

    public void OpenSelectionForSlot(int slotIndex, EnerlingSlotButton slotButton)
    {
        // Play button click sound
        PlayButtonClickSound();

        currentSlotIndex = slotIndex;
        currentSlotButton = slotButton;
        selectionCanvas.SetActive(true);

        // Get the pet equipped in the other slot
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            if (slotIndex == 0)
                otherSlotEquippedPet = GameDataManager.Instance.CurrentGameData.equippedPetSlot2;
            else
                otherSlotEquippedPet = GameDataManager.Instance.CurrentGameData.equippedPetSlot1;
        }

        RefreshDisplay();

        // Get the currently equipped pet in this slot
        string equippedPetName = slotButton.GetEquippedPetName();

        if (!string.IsNullOrEmpty(equippedPetName))
        {
            // If there's an equipped pet, find and select it in the grid
            selectedEnerling = ingredientDatabase.GetIngredientInfo(equippedPetName);
            if (selectedEnerling != null)
            {
                // Update info panel with this pet
                UpdateInfoPanel(selectedEnerling);
                SpawnPreviewModel(selectedEnerling);

                // Show remove button (since it's equipped)
                if (removeButton != null)
                    removeButton.gameObject.SetActive(true);

                // Hide equip button
                if (equipButton != null)
                    equipButton.gameObject.SetActive(false);

                // Show the info panel
                infoPanel.SetActive(true);
            }
        }
        else
        {
            // Hide both buttons initially for empty slot
            if (removeButton != null)
                removeButton.gameObject.SetActive(false);
            if (equipButton != null)
                equipButton.gameObject.SetActive(false);
        }
    }

    // Show information for an equipped pet without opening selection
    public void ShowPetInfo(IngredientDatabase.IngredientInfo enerling, int slotIndex)
    {
        // Store the current slot info
        currentSlotIndex = slotIndex;
        currentSlotButton = null; // No slot button reference for viewing mode
        selectedEnerling = enerling;

        // Update the info panel
        UpdateInfoPanel(enerling);

        // Spawn preview model
        SpawnPreviewModel(enerling);

        // Hide equip button in view mode
        if (equipButton != null)
            equipButton.gameObject.SetActive(false);

        // Show remove button if this pet is equipped
        if (removeButton != null)
        {
            bool isEquipped = IsEnerlingEquipped(enerling.ingredientName);
            removeButton.gameObject.SetActive(isEquipped);
        }

        // Show the info panel
        infoPanel.SetActive(true);
    }

    void RefreshDisplay()
    {
        ClearCurrentDisplay();

        var unlockedEnerlings = ingredientDatabase.GetUnlockedIngredients();
        currentFilteredEnerlings = ingredientDatabase.GetIngredientsByFilter(
            currentRarityFilter,
            currentKingdomFilter,
            useRarityFilter,
            useKingdomFilter
        );

        DisplayEnerlings(currentFilteredEnerlings);
    }

    void DisplayEnerlings(List<IngredientDatabase.IngredientInfo> enerlings)
    {
        if (enerlings == null || enerlings.Count == 0)
        {
            infoPanel.SetActive(false);
            return;
        }

        enerlings.Sort((a, b) =>
        {
            int rarityCompare = a.rarity.CompareTo(b.rarity);
            if (rarityCompare != 0) return rarityCompare;
            return a.ingredientName.CompareTo(b.ingredientName);
        });

        int enerlingIndex = 0;
        int rowIndex = 0;

        while (enerlingIndex < enerlings.Count)
        {
            // Row 0: even (4 buttons), Row 1: odd (3 buttons), alternating
            GameObject rowPrefab = (rowIndex % 2 == 0) ? evenRowPrefab : oddRowPrefab;
            GameObject row = Instantiate(rowPrefab, contentParent);
            currentRows.Add(row);

            // 4 buttons for even rows, 3 buttons for odd rows
            int maxButtons = (rowIndex % 2 == 0) ? 4 : 3;

            for (int i = 0; i < maxButtons && enerlingIndex < enerlings.Count; i++)
            {
                var enerling = enerlings[enerlingIndex];
                CreateEnerlingButton(enerling, row.transform);
                enerlingIndex++;
            }
            rowIndex++;
        }
    }

    void CreateEnerlingButton(IngredientDatabase.IngredientInfo enerling, Transform parent)
    {
        GameObject buttonObj = Instantiate(enerlingButtonPrefab, parent);
        EnerlingButtonController buttonController = buttonObj.GetComponent<EnerlingButtonController>();

        if (buttonController != null)
        {
            buttonController.Initialize(
                enerling.ingredientName,
                GetDisplayedSprite(enerling),
                enerling.rarity,
                ingredientDatabase
            );
        }

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnEnerlingButtonClicked(enerling.ingredientName));
        }

        enerlingButtons[enerling.ingredientName] = buttonObj;

        // Check if this enerling is equipped in the other slot
        bool isEquippedInOtherSlot = enerling.ingredientName == otherSlotEquippedPet;

        // Check if this enerling is equipped in the current slot
        bool isEquippedInCurrentSlot = currentSlotButton != null && currentSlotButton.GetEquippedPetName() == enerling.ingredientName;

        // Disable the button if it's equipped in the other slot (can't unequip from this slot)
        if (isEquippedInOtherSlot)
        {
            button.interactable = false;
            // Visual indication that it's disabled (equipped in other slot)
            Image img = buttonObj.GetComponent<Image>();
            if (img != null)
                img.color = disabledButtonColor;
        }
        else if (isEquippedInCurrentSlot)
        {
            button.interactable = true;
            // Visual indication that it's equipped in this slot
            Image img = buttonObj.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        }
    }

    bool IsEnerlingEquipped(string enerlingName)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
            return false;

        return GameDataManager.Instance.CurrentGameData.equippedPetSlot1 == enerlingName ||
               GameDataManager.Instance.CurrentGameData.equippedPetSlot2 == enerlingName;
    }

    public void OnEnerlingButtonClicked(string enerlingName)
    {
        // Play button click sound
        PlayButtonClickSound();

        selectedEnerling = ingredientDatabase.GetIngredientInfo(enerlingName);
        if (selectedEnerling == null) return;

        UpdateInfoPanel(selectedEnerling);
        SpawnPreviewModel(selectedEnerling);

        // Check if this enerling is already equipped in the current slot
        bool isEquippedInCurrentSlot = currentSlotButton != null && currentSlotButton.GetEquippedPetName() == enerlingName;

        // Check if this enerling is equipped in the other slot
        bool isEquippedInOtherSlot = enerlingName == otherSlotEquippedPet;

        // Update button visibility based on equipped status
        if (removeButton != null)
            removeButton.gameObject.SetActive(isEquippedInCurrentSlot); // Only show remove for current slot's pet

        if (equipButton != null)
        {
            // Only show equip button if:
            // 1. The enerling is NOT equipped in current slot
            // 2. The enerling is NOT equipped in other slot
            // 3. We have a valid slot button (meaning we're in selection mode, not view mode)
            equipButton.gameObject.SetActive(!isEquippedInCurrentSlot && !isEquippedInOtherSlot && currentSlotButton != null);
        }
    }

    void UpdateInfoPanel(IngredientDatabase.IngredientInfo enerling)
    {
        infoPanel.SetActive(true);

        enerlingNameText.text = enerling.ingredientName;
        enerlingDescriptionText.text = enerling.enerlingDescription;
        rarityIconImage.sprite = ingredientDatabase.GetRarityIcon(enerling.rarity);

        // Clear organs
        foreach (Transform child in organsContainer)
            Destroy(child.gameObject);

        // Show organs
        List<string> allOrgans = new List<string>();
        if (enerling.beneficialOrgans != null)
            allOrgans.AddRange(enerling.beneficialOrgans);
        if (enerling.targetOrgans != null)
            allOrgans.AddRange(enerling.targetOrgans);

        foreach (string organ in allOrgans)
        {
            GameObject organIcon = Instantiate(organIconPrefab, organsContainer);
            Image iconImage = organIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                Sprite organSprite = ingredientDatabase.GetOrganSprite(organ);
                if (organSprite != null)
                    iconImage.sprite = organSprite;
            }

            TextMeshProUGUI organText = organIcon.GetComponentInChildren<TextMeshProUGUI>();
            if (organText != null)
                organText.text = organ;
        }

        // Update power-up display using direct references
        UpdatePowerUpDisplay(enerling);
    }

    // New method to update power-up display using direct references
    void UpdatePowerUpDisplay(IngredientDatabase.IngredientInfo enerling)
    {
        if (enerling.powerUps != null && enerling.powerUps.Count > 0)
        {
            var powerUp = enerling.powerUps[0]; // First power-up only

            // Set icon
            if (powerUpIconImage != null && powerUp.powerUpIcon != null)
                powerUpIconImage.sprite = powerUp.powerUpIcon;

            // Set amount text with appropriate prefix
            if (powerUpAmountText != null)
            {
                string prefix = GetPowerUpPrefix(powerUp.powerUpType);
                powerUpAmountText.text = $"{prefix}{powerUp.amount}";
            }

            // Set description text
            if (powerUpDescriptionText != null)
                powerUpDescriptionText.text = powerUp.description;

            // Make sure power-up display is visible
            if (powerUpIconImage != null) powerUpIconImage.gameObject.SetActive(true);
            if (powerUpAmountText != null) powerUpAmountText.gameObject.SetActive(true);
            if (powerUpDescriptionText != null) powerUpDescriptionText.gameObject.SetActive(true);
        }
        else
        {
            // No power-up available - hide or show empty state
            ClearPowerUpDisplay();
        }
    }

    // Helper method to get the correct prefix based on power-up type
    private string GetPowerUpPrefix(IngredientDatabase.PowerUpInfo.PowerUpType type)
    {
        switch (type)
        {
            case IngredientDatabase.PowerUpInfo.PowerUpType.Time:
                return "-"; // Time is deducted/reduced
            case IngredientDatabase.PowerUpInfo.PowerUpType.Heart:
            case IngredientDatabase.PowerUpInfo.PowerUpType.Speed:
            case IngredientDatabase.PowerUpInfo.PowerUpType.Coins:
            case IngredientDatabase.PowerUpInfo.PowerUpType.Exp:
            case IngredientDatabase.PowerUpInfo.PowerUpType.Gems:
            default:
                return "+"; // All others are added/increased
        }
    }

    // Clear power-up display when no power-up exists
    void ClearPowerUpDisplay()
    {
        if (powerUpIconImage != null)
        {
            powerUpIconImage.sprite = null;
            powerUpIconImage.gameObject.SetActive(false);
        }

        if (powerUpAmountText != null)
        {
            powerUpAmountText.text = "";
            powerUpAmountText.gameObject.SetActive(false);
        }

        if (powerUpDescriptionText != null)
        {
            powerUpDescriptionText.text = "No power-up available";
            powerUpDescriptionText.gameObject.SetActive(true);
        }
    }

    void SpawnPreviewModel(IngredientDatabase.IngredientInfo enerling)
    {
        if (currentPreviewModel != null)
            Destroy(currentPreviewModel);

        if (enerling.modelPrefab != null && previewSpawnPoint != null)
        {
            currentPreviewModel = Instantiate(enerling.modelPrefab, previewSpawnPoint);
            currentPreviewModel.transform.localPosition = Vector3.zero;
            currentPreviewModel.transform.localRotation = Quaternion.identity;
            currentPreviewModel.transform.localScale = Vector3.one;

            ApplyEquippedSkinToVisuals(currentPreviewModel, enerling);

            SetLayerRecursively(currentPreviewModel, LayerMask.NameToLayer("UI"));

            StartCoroutine(RotatePreviewModel());
        }
    }

    private Sprite GetDisplayedSprite(IngredientDatabase.IngredientInfo enerling)
    {
        if (enerling == null)
            return null;

        if (enerling.isSkinEquipped && enerling.skinSprite != null)
            return enerling.skinSprite;

        return enerling.enerlingSprite;
    }

    private void ApplyEquippedSkinToVisuals(GameObject spawnedRoot, IngredientDatabase.IngredientInfo enerling)
    {
        if (spawnedRoot == null || enerling == null)
            return;
        if (!enerling.isSkinEquipped || enerling.skinPrefab == null)
            return;

        Transform visuals = spawnedRoot.transform.Find("Visuals");
        if (visuals == null)
            return;

        for (int i = visuals.childCount - 1; i >= 0; i--)
        {
            Destroy(visuals.GetChild(i).gameObject);
        }

        GameObject skinInstance = Instantiate(enerling.skinPrefab, visuals);
        skinInstance.name = enerling.skinPrefab.name;
        skinInstance.transform.localPosition = Vector3.zero;
        skinInstance.transform.localRotation = Quaternion.identity;
        skinInstance.transform.localScale = Vector3.one;

        RefreshAnimatorBindings(spawnedRoot);
    }

    private void RefreshAnimatorBindings(GameObject root)
    {
        if (root == null)
            return;

        bool wasActive = root.activeSelf;
        if (wasActive)
        {
            root.SetActive(false);
            root.SetActive(true);
        }

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator == null)
                continue;

            bool wasEnabled = animator.enabled;
            if (!wasEnabled)
                animator.enabled = true;

            animator.Rebind();
            animator.Update(0f);

            if (!wasEnabled)
                animator.enabled = false;
        }
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    IEnumerator RotatePreviewModel()
    {
        while (currentPreviewModel != null)
        {
            currentPreviewModel.transform.Rotate(Vector3.up, previewRotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    void OnEquipButtonClicked()
    {
        if (selectedEnerling == null || currentSlotButton == null) return;

        // Play button click sound
        PlayButtonClickSound();

        currentSlotButton.EquipPet(selectedEnerling.ingredientName, GetDisplayedSprite(selectedEnerling));
        CloseSelection();
    }

    void OnRemoveButtonClicked()
    {
        if (selectedEnerling == null) return;

        // Play button click sound
        PlayButtonClickSound();

        // Only remove if this pet is equipped in the current slot
        if (currentSlotButton != null && currentSlotButton.GetEquippedPetName() == selectedEnerling.ingredientName)
        {
            currentSlotButton.ClearSlot();

            // Also remove from pet manager
            EnerlingPetManager petManager = FindObjectOfType<EnerlingPetManager>();
            if (petManager != null)
            {
                petManager.RemovePet(currentSlotIndex);
            }
        }

        // Hide the info panel
        infoPanel.SetActive(false);
    }

    public void CloseSelection()
    {
        // Play button click sound
        PlayButtonClickSound();

        selectionCanvas.SetActive(false);
        infoPanel.SetActive(false);
        if (currentPreviewModel != null)
            Destroy(currentPreviewModel);

        // Refresh the slot button state if we have one
        if (currentSlotButton != null)
        {
            currentSlotButton.RefreshSlotState();
        }

        currentSlotIndex = -1;
        currentSlotButton = null;
        selectedEnerling = null;
        otherSlotEquippedPet = "";

        // Hide both buttons
        if (removeButton != null)
            removeButton.gameObject.SetActive(false);
        if (equipButton != null)
            equipButton.gameObject.SetActive(false);

        // Clear power-up display when closing
        ClearPowerUpDisplay();
    }

    void ClearCurrentDisplay()
    {
        foreach (GameObject row in currentRows)
            Destroy(row);
        currentRows.Clear();
        enerlingButtons.Clear();
    }

    // Helper method to play button click sound
    private void PlayButtonClickSound()
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
}