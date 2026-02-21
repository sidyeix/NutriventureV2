using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class IngredientDetailsUI : MonoBehaviour
{
    [System.Serializable]
    public class KingdomBackground
    {
        public IngredientDatabase.KingdomOrigin kingdom;
        public Sprite backgroundSprite;
    }

    [Header("Frame Library")]
    public KingdomFrameLibrary frameLibrary;

    [Header("3D Viewer")]
    public Enerling3DViewer viewer;

    [Header("Kingdom Backgrounds")]
    public KingdomBackground[] kingdomBackgrounds;

    [Header("Main Visuals")]
    public Image kingdomBackground;
    public Image iconDisplay;
    public Image rarityIcon;

    [Header("Texts")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    [Header("Stats")]
    public TMP_Text damageText;
    public TMP_Text armorText;

    [Header("Organ Info")]
    public TMP_Text organTypeText;           // Shows "Beneficial Organs" or "Target Organs"
    public Transform organIconsContainer;     // Parent container for organ icons
    public Image organIconPrefab;              // Prefab for individual organ icons

    [Header("Skill Icons")]
    public Image skill1Icon;
    public Image skill2Icon;
    public Image skill3Icon;
    public Image skill4Icon;
    [Tooltip("If true, uses skillCircleIcon. If false, uses skillSprite")]
    public bool useCircleIcons = true;

    [Header("Navigation Buttons")]
    public Button nextButton;
    public Button prevButton;
    public Button closeButton;

    [Header("Page Indicator")]
    public TMP_Text pageIndicatorText;

    private IngredientDatabase.IngredientInfo currentInfo;
    private IngredientDatabase database;
    private List<IngredientDatabase.IngredientInfo> currentFilteredList;
    private int currentIndex = -1;
    private List<GameObject> organIconObjects = new List<GameObject>(); // Track created icons

    private void Start()
    {
        gameObject.SetActive(false);

        // Add navigation listeners
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNext);

        if (prevButton != null)
            prevButton.onClick.AddListener(ShowPrevious);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    // =========================
    // GET KINGDOM BG
    // =========================
    Sprite GetKingdomBG(IngredientDatabase.KingdomOrigin kingdom)
    {
        foreach (var bg in kingdomBackgrounds)
        {
            if (bg.kingdom == kingdom)
                return bg.backgroundSprite;
        }
        return null;
    }

    // =========================
    // CLEAR ORGAN ICONS
    // =========================
    private void ClearOrganIcons()
    {
        foreach (var icon in organIconObjects)
        {
            if (icon != null)
                Destroy(icon);
        }
        organIconObjects.Clear();
    }

    // =========================
    // SETUP ORGAN ICONS
    // =========================
    private void SetupOrganIcons(IngredientDatabase.IngredientInfo info)
    {
        if (organIconsContainer == null || organIconPrefab == null) return;

        // Clear existing icons
        ClearOrganIcons();

        // Determine which organ list to use
        List<string> organsToShow = info.beneficialOrgans.Count > 0 
            ? info.beneficialOrgans 
            : info.targetOrgans;

        // Update organ type text
        if (organTypeText != null)
        {
            if (info.beneficialOrgans.Count > 0)
                organTypeText.text = "Beneficial Organs:";
            else if (info.targetOrgans.Count > 0)
                organTypeText.text = "Target Organs:";
            else
                organTypeText.text = "No Special Organs";
        }

        // If no organs to show, hide the container or return
        if (organsToShow.Count == 0)
        {
            if (organIconsContainer.gameObject != null)
                organIconsContainer.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (organIconsContainer.gameObject != null)
                organIconsContainer.gameObject.SetActive(true);
        }

        // Create icon for each organ
        foreach (string organName in organsToShow)
        {
            // Create new icon from prefab
            Image icon = Instantiate(organIconPrefab, organIconsContainer);
            
            // Try to get organ sprite from frame library FIRST
            Sprite organSprite = null;
            
            if (frameLibrary != null)
            {
                organSprite = frameLibrary.GetOrganSprite(organName);
            }
            
            // Fallback to database if not found in frame library
            if (organSprite == null && database != null)
            {
                organSprite = database.GetOrganSprite(organName);
            }
            
            if (organSprite != null)
            {
                icon.sprite = organSprite;
            }
            else
            {
                Debug.LogWarning($"No sprite found for organ: {organName}");
                // Optionally set a default color or hide the icon
                icon.gameObject.SetActive(false);
            }
            
            // Make sure the icon is visible and properly sized
            icon.color = Color.white;
            
            organIconObjects.Add(icon.gameObject);
        }
    }

    // =========================
    // SETUP SKILL ICONS - NEW METHOD
    // =========================
    private void SetupSkillIcons(IngredientDatabase.IngredientInfo info)
    {
        if (info == null) return;

        // Create arrays for skills and their corresponding UI images
        IngredientDatabase.SkillInfo[] skills = new IngredientDatabase.SkillInfo[] 
        { 
            info.skill1, 
            info.skill2, 
            info.skill3, 
            info.skill4 
        };
        
        Image[] skillImages = new Image[] 
        { 
            skill1Icon, 
            skill2Icon, 
            skill3Icon, 
            skill4Icon 
        };

        // Loop through all 4 skills
        for (int i = 0; i < 4; i++)
        {
            if (skillImages[i] != null)
            {
                if (skills[i] != null)
                {
                    // Get the appropriate icon based on shape preference
                    Sprite iconSprite = GetSkillIcon(skills[i]);
                    
                    if (iconSprite != null)
                    {
                        skillImages[i].sprite = iconSprite;
                        skillImages[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        Debug.LogWarning($"No icon found for skill {i+1}: {skills[i].skillName}");
                        skillImages[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    // Hide if skill doesn't exist
                    skillImages[i].gameObject.SetActive(false);
                }
            }
        }
    }

    // =========================
    // GET SKILL ICON FROM DATABASE - NEW HELPER METHOD
    // =========================
    private Sprite GetSkillIcon(IngredientDatabase.SkillInfo skill)
    {
        if (skill == null) return null;
        
        // Return circle icon if available and useCircleIcons is true
        if (useCircleIcons && skill.skillCircleIcon != null)
            return skill.skillCircleIcon;
        
        // Fallback to rectangle icon
        return skill.skillSprite;
    }

    // =========================
    // SHOW DETAILS
    // =========================
    public void ShowDetails(
        IngredientDatabase.IngredientInfo info,
        IngredientDatabase db,
        List<IngredientDatabase.IngredientInfo> filteredList = null,
        int index = -1)
    {
        if (info == null) return;

        currentInfo = info;
        database = db;

        // Store the filtered list and current index for navigation
        if (filteredList != null)
        {
            currentFilteredList = filteredList;
            currentIndex = index;
        }

        gameObject.SetActive(true);

        // =========================
        // TEXT
        // =========================
        if (nameText != null)
            nameText.text = info.ingredientName;
        
        if (descriptionText != null)
            descriptionText.text = info.enerlingDescription;

        // =========================
        // ICON
        // =========================
        if (iconDisplay != null)
        {
            // Try to get custom icon from frame library first
            Sprite customIcon = frameLibrary != null ? frameLibrary.GetEnerlingIcon(info.ingredientName) : null;
            iconDisplay.sprite = customIcon != null ? customIcon : info.enerlingSprite;
        }

        // =========================
        // RARITY
        // =========================
        if (rarityIcon != null)
        {
            // Try to get rarity icon from frame library first
            Sprite raritySprite = frameLibrary != null ? frameLibrary.GetRarityIcon(info.rarity) : null;
            rarityIcon.sprite = raritySprite != null ? raritySprite : db.GetRarityIcon(info.rarity);
        }

        // =========================
        // KINGDOM BG
        // =========================
        if (kingdomBackground != null)
            kingdomBackground.sprite = GetKingdomBG(info.kingdom);

        // =========================
        // STATS
        // =========================
        if (damageText != null)
            damageText.text = info.baseDamage.ToString();
        
        if (armorText != null)
            armorText.text = info.armorPercent + "%";

        // =========================
        // ORGAN INFO
        // =========================
        SetupOrganIcons(info);

        // =========================
        // SKILL ICONS - NEW!
        // =========================
        SetupSkillIcons(info);

        // =========================
        // UPDATE NAVIGATION
        // =========================
        UpdateNavigationButtons();
        UpdatePageIndicator();

        // =========================
        // SPAWN 3D MODEL
        // =========================
        if (viewer != null)
        {
            viewer.ShowEnerling(info);
        }
        else
        {
            Debug.LogWarning("3D Viewer not assigned in Details UI");
        }
    }

    // =========================
    // NAVIGATION METHODS
    // =========================
    public void ShowNext()
    {
        if (currentFilteredList == null || currentFilteredList.Count <= 1)
            return;

        // Move to next index, wrap around
        currentIndex = (currentIndex + 1) % currentFilteredList.Count;
        currentInfo = currentFilteredList[currentIndex];

        // Refresh display with new info
        RefreshDisplay();

        // Update 3D viewer
        if (viewer != null)
            viewer.ShowEnerling(currentInfo);
    }

    public void ShowPrevious()
    {
        if (currentFilteredList == null || currentFilteredList.Count <= 1)
            return;

        // Move to previous index, wrap around
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = currentFilteredList.Count - 1;

        currentInfo = currentFilteredList[currentIndex];

        // Refresh display with new info
        RefreshDisplay();

        // Update 3D viewer
        if (viewer != null)
            viewer.ShowEnerling(currentInfo);
    }

    private void RefreshDisplay()
    {
        if (currentInfo == null || database == null) return;

        // Update text
        if (nameText != null)
            nameText.text = currentInfo.ingredientName;
        
        if (descriptionText != null)
            descriptionText.text = currentInfo.enerlingDescription;

        // Update icon
        if (iconDisplay != null)
        {
            Sprite customIcon = frameLibrary != null ? frameLibrary.GetEnerlingIcon(currentInfo.ingredientName) : null;
            iconDisplay.sprite = customIcon != null ? customIcon : currentInfo.enerlingSprite;
        }

        // Update rarity
        if (rarityIcon != null)
        {
            Sprite raritySprite = frameLibrary != null ? frameLibrary.GetRarityIcon(currentInfo.rarity) : null;
            rarityIcon.sprite = raritySprite != null ? raritySprite : database.GetRarityIcon(currentInfo.rarity);
        }

        // Update kingdom background
        if (kingdomBackground != null)
            kingdomBackground.sprite = GetKingdomBG(currentInfo.kingdom);

        // Update stats
        if (damageText != null)
            damageText.text = currentInfo.baseDamage.ToString();
        
        if (armorText != null)
            armorText.text = currentInfo.armorPercent + "%";

        // Update organ icons
        SetupOrganIcons(currentInfo);

        // Update skill icons
        SetupSkillIcons(currentInfo);

        // Update navigation UI
        UpdateNavigationButtons();
        UpdatePageIndicator();
    }

    private void UpdateNavigationButtons()
    {
        if (currentFilteredList == null || currentFilteredList.Count <= 1)
        {
            // Disable both buttons if there's only one item or no list
            if (nextButton != null) nextButton.interactable = false;
            if (prevButton != null) prevButton.interactable = false;
            return;
        }

        // Enable both buttons (they'll wrap around)
        if (nextButton != null) nextButton.interactable = true;
        if (prevButton != null) prevButton.interactable = true;
    }

    private void UpdatePageIndicator()
    {
        if (pageIndicatorText != null && currentFilteredList != null)
        {
            pageIndicatorText.text = $"{currentIndex + 1} / {currentFilteredList.Count}";
        }
    }

    // =========================
    // CLOSE
    // =========================
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        
        // Clear organ icons
        ClearOrganIcons();
        
        // Clear references
        currentInfo = null;
        currentFilteredList = null;
        currentIndex = -1;
    }
}