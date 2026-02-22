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
    public Transform powerUpsContainer;
    public GameObject powerUpIconPrefab;

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

    // Current selection
    private int currentSlotIndex = -1;
    private EnerlingSlotButton currentSlotButton;
    private IngredientDatabase.IngredientInfo selectedEnerling;
    private GameObject currentPreviewModel;
    private List<GameObject> currentRows = new List<GameObject>();
    private Dictionary<string, GameObject> enerlingButtons = new Dictionary<string, GameObject>();
    private List<IngredientDatabase.IngredientInfo> currentFilteredEnerlings = new List<IngredientDatabase.IngredientInfo>();

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
        RefreshDisplay();

        // Hide remove button initially (will show when an enerling is selected)
        if (removeButton != null)
            removeButton.gameObject.SetActive(false);
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
                enerling.enerlingSprite,
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

        // Check if this enerling is already equipped in either slot
        bool isEquipped = IsEnerlingEquipped(enerling.ingredientName);
        if (isEquipped)
        {
            button.interactable = true;
            // Visual indication that it's equipped
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

        // Show remove button when an enerling is selected
        if (removeButton != null)
            removeButton.gameObject.SetActive(true);
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

        // Clear powerups
        foreach (Transform child in powerUpsContainer)
            Destroy(child.gameObject);

        // Show powerups
        if (enerling.powerUps != null)
        {
            foreach (var powerUp in enerling.powerUps)
            {
                GameObject powerUpIcon = Instantiate(powerUpIconPrefab, powerUpsContainer);
                Image iconImage = powerUpIcon.GetComponent<Image>();
                if (iconImage != null && powerUp.powerUpIcon != null)
                    iconImage.sprite = powerUp.powerUpIcon;

                TextMeshProUGUI amountText = powerUpIcon.GetComponentInChildren<TextMeshProUGUI>();
                if (amountText != null)
                    amountText.text = $"+{powerUp.amount}";

                Button btn = powerUpIcon.GetComponent<Button>();
                if (btn != null)
                {
                    string description = powerUp.description;
                    btn.onClick.AddListener(() => ShowTooltip(description));
                }
            }
        }
    }

    void ShowTooltip(string message)
    {
        Debug.Log(message);
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

            SetLayerRecursively(currentPreviewModel, LayerMask.NameToLayer("UI"));

            StartCoroutine(RotatePreviewModel());
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

        currentSlotButton.EquipPet(selectedEnerling.ingredientName, selectedEnerling.enerlingSprite);
        CloseSelection();
    }

    void OnRemoveButtonClicked()
    {
        if (currentSlotButton == null) return;

        // Play button click sound
        PlayButtonClickSound();

        // Clear the slot
        currentSlotButton.ClearSlot();

        // Also remove from pet manager
        EnerlingPetManager petManager = FindObjectOfType<EnerlingPetManager>();
        if (petManager != null)
        {
            petManager.RemovePet(currentSlotIndex);
        }

        CloseSelection();
    }

    public void CloseSelection()
    {
        // Play button click sound
        PlayButtonClickSound();

        selectionCanvas.SetActive(false);
        infoPanel.SetActive(false);
        if (currentPreviewModel != null)
            Destroy(currentPreviewModel);
        currentSlotIndex = -1;
        currentSlotButton = null;
        selectedEnerling = null;

        // Hide remove button
        if (removeButton != null)
            removeButton.gameObject.SetActive(false);
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
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }
}