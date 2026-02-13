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

    [Header("Kingdom Backgrounds")]
    public KingdomBackground[] kingdomBackgrounds;

    [Header("Main Visuals")]
    public Image kingdomBackground;
    public Image enerlingDisplay;
    public Image iconDisplay;
    public Image rarityIcon;

    [Header("Texts")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    [Header("Life / Stats")]
    public TMP_Text damageText;
    public TMP_Text armorText;
private void Start()
{
    gameObject.SetActive(false);
}

    private void Awake()
    {
        // Validate critical references
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (kingdomBackground == null)
            Debug.LogWarning("Kingdom Background Image is not assigned in IngredientDetailsUI");
        
        if (enerlingDisplay == null)
            Debug.LogWarning("Enerling Display Image is not assigned in IngredientDetailsUI");
        
        if (iconDisplay == null)
            Debug.LogWarning("Icon Display Image is not assigned in IngredientDetailsUI");
        
        if (rarityIcon == null)
            Debug.LogWarning("Rarity Icon Image is not assigned in IngredientDetailsUI");
        
        if (nameText == null)
            Debug.LogWarning("Name Text is not assigned in IngredientDetailsUI");
        
        if (descriptionText == null)
            Debug.LogWarning("Description Text is not assigned in IngredientDetailsUI");
        
        if (damageText == null)
            Debug.LogWarning("Damage Text is not assigned in IngredientDetailsUI");
        
        if (armorText == null)
            Debug.LogWarning("Armor Text is not assigned in IngredientDetailsUI");
    }

    Sprite GetKingdomBG(IngredientDatabase.KingdomOrigin kingdom)
    {
        if (kingdomBackgrounds == null || kingdomBackgrounds.Length == 0) 
        {
            Debug.LogWarning("Kingdom Backgrounds array is not set up in IngredientDetailsUI");
            return null;
        }
        
        foreach (var bg in kingdomBackgrounds)
        {
            if (bg != null && bg.kingdom == kingdom)
                return bg.backgroundSprite;
        }
        
        Debug.LogWarning($"No background sprite found for kingdom: {kingdom}");
        return null;
    }

    public void ShowDetails(IngredientDatabase.IngredientInfo info, IngredientDatabase db) 
    {
        if (info == null)
        {
            Debug.LogError("Cannot show details: IngredientInfo is null");
            return;
        }

        if (db == null)
        {
            Debug.LogError("Cannot show details: IngredientDatabase is null");
            return;
        }

        gameObject.SetActive(true);

        try
        {
            // =========================
            // BASIC INFO
            // =========================
            if (nameText != null)
                nameText.text = info.ingredientName ?? "Unknown";
            
            if (descriptionText != null)
                descriptionText.text = info.enerlingDescription ?? "No description available";

            // =========================
            // VISUALS
            // =========================
            if (enerlingDisplay != null && info.enerlingSprite != null)
                enerlingDisplay.sprite = info.enerlingSprite;
            
            if (iconDisplay != null && info.enerlingSprite != null)
                iconDisplay.sprite = info.enerlingSprite;
            
            if (rarityIcon != null)
            {
                Sprite raritySprite = db.GetRarityIcon(info.rarity);
                if (raritySprite != null)
                    rarityIcon.sprite = raritySprite;
            }

            // KINGDOM BACKGROUND
            if (kingdomBackground != null)
            {
                Sprite bg = GetKingdomBG(info.kingdom);
                if (bg != null)
                    kingdomBackground.sprite = bg;
                else
                    kingdomBackground.sprite = null; // Clear if no background found
            }
            // =========================
// ENERLING ICON OVERRIDE
// =========================
Sprite customIcon = null;

if (frameLibrary != null)
{
    customIcon =
        frameLibrary.GetEnerlingIcon(
            info.ingredientName);
}

Sprite finalIcon =
    customIcon != null
    ? customIcon
    : info.enerlingSprite;

if (enerlingDisplay != null)
    enerlingDisplay.sprite = finalIcon;

if (iconDisplay != null)
    iconDisplay.sprite = finalIcon;

// =========================
// RARITY ICON OVERRIDE
// =========================
Sprite customRarity = null;

if (frameLibrary != null)
{
    customRarity =
        frameLibrary.GetRarityIcon(
            info.rarity);
}

if (rarityIcon != null)
{
    rarityIcon.sprite =
        customRarity != null
        ? customRarity
        : db.GetRarityIcon(info.rarity);
}

            // =========================
            // STATS
            // =========================
            if (damageText != null)
                damageText.text = info.baseDamage.ToString();
            
            if (armorText != null)
                armorText.text = info.armorPercent + "%";

            // =========================
            // SKILLS
            // =========================
            IngredientDatabase.SkillInfo[] skills = new IngredientDatabase.SkillInfo[]
            {
                info.skill1,
                info.skill2,
                info.skill3,
                info.skill4
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error showing ingredient details: {e.Message}");
            ClosePanel();
        }
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}