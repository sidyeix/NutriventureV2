using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video; // Add this for VideoClip support

[CreateAssetMenu(fileName = "IngredientDatabase", menuName = "NutriVenture/Ingredient Database")]
public class IngredientDatabase : ScriptableObject
{
    [System.Serializable]
    public enum KingdomOrigin
    {
        NutriKingdom,
        Alerthia,
        Sugaria,
        Preservia
    }

    [System.Serializable]
    public enum Rarity
    {
        Common,
        Rare,
        UltraRare
    }

    [System.Serializable]
    public class SkillInfo
    {
        public enum SkillType
        {
            Heal,
            Damage,
            Defend
        }

        [Header("Skill Info")]
        public string skillName = "Skill Name";
        public SkillType type = SkillType.Damage;

        [Header("Base Values")]
        [Tooltip("Base value for damage/heal")]
        public int baseValue;

        [Tooltip("Minimum value for random range")]
        public int minValue;

        [Tooltip("Maximum value for random range")]
        public int maxValue;

        [Tooltip("Use random range instead of base value")]
        public bool useRandomRange = false;

        [Header("Organ Effects")]
        [Tooltip("Additional damage/heal per beneficial/target organ")]
        public int additionalEffectPerOrgan = 5;

        [Header("Cooldown")]
        public int cooldownTurns = 0;

        [Header("Visual")]
        public Sprite skillSprite;
        public Sprite skillCircleIcon;

        [Header("Description")]
        [TextArea(2, 4)]
        public string skillDescription;

        // Get actual value (either base or random)
        public int GetValue()
        {
            if (useRandomRange)
            {
                return Random.Range(minValue, maxValue + 1);
            }
            return baseValue;
        }

        // Calculate total effect with organ bonuses
        public int CalculateTotalEffect(int organCount)
        {
            int baseEffect = GetValue();

            // Apply organ bonus: 5% per organ
            float organBonus = 1f + (organCount * 0.05f);

            return Mathf.RoundToInt(baseEffect * organBonus);
        }
    }

    [System.Serializable]
    public class IngredientInfo
    {
        [Header("Basic Info")]
        public string ingredientName;
        public Rarity rarity = Rarity.Common;
        public KingdomOrigin kingdom = KingdomOrigin.NutriKingdom;
        public bool isUnlocked = false; // This will be set by PersistentDataManager at runtime

        [Header("Visuals")]
        public Sprite enerlingSprite;
        public GameObject modelPrefab;

       [Header("Catch Mechanics")]
       [Tooltip("Maximum times this enerling can be caught")]
       public int maxCatch = 20;
       
       [Tooltip("Current number of times caught")]
       public int currentCatchCount = 0;

        [Header("Animation")]
        public RuntimeAnimatorController animatorController;

        [Header("Life Stats")]
        [Tooltip("Base life points")]
        public int baseLife = 100;

        [Tooltip("Current life points")]
        public int currentLife = 100;

        [Header("Defense")]
        [Tooltip("Armor as percentage (0-100)")]
        [Range(0, 100)]
        public int armorPercent = 0;

        [Header("Base Attack")]
        [Tooltip("Base damage for normal attacks")]
        public int baseDamage = 10;

        [Header("Immunities")]
        [Tooltip("Immune to organ damage effects")]
        public bool immuneToOrganDamage = false;

        [Header("Organs")]
        [Tooltip("Organs that this ingredient benefits (for healing calculations)")]
        public List<string> beneficialOrgans = new List<string>();

        [Tooltip("Organs that this ingredient targets (for damage calculations)")]
        public List<string> targetOrgans = new List<string>();

        [Header("Skills (4 Skills)")]
        public SkillInfo skill1;
        public SkillInfo skill2;
        public SkillInfo skill3;
        public SkillInfo skill4;

        [Header("Description")]
        [TextArea(3, 5)]
        public string enerlingDescription;

        [Header("Enerling Story")]
        [TextArea(10, 20)]
        public string enerlingStory;

        [Header("Audio")]
        public AudioClip audioClip;

        [Header("Ending Cutscene")]
        [Tooltip("Video file for the ending cutscene when this enerling wins")]
        public VideoClip endingCutscene; // Add this field for video files

        // Battle status (runtime only)
        [System.NonSerialized]
        public int skill1Cooldown = 0;
        [System.NonSerialized]
        public int skill2Cooldown = 0;
        [System.NonSerialized]
        public int skill3Cooldown = 0;
        [System.NonSerialized]
        public int skill4Cooldown = 0;

        // Properties for UI
        public string LifeText
        {
            get { return $"{currentLife}/{baseLife}"; }
        }

        public float LifePercentage
        {
            get { return (float)currentLife / baseLife; }
        }

        public Color LifeTextColor
        {
            get
            {
                float percentage = LifePercentage;
                if (percentage <= 0.33f) return Color.red;
                if (percentage <= 0.66f) return new Color(1f, 0.5f, 0f); // Orange
                return Color.white;
            }
        }

        public string OrgansLabel
        {
            get
            {
                if (beneficialOrgans.Count > 0) return "Beneficial Organs";
                if (targetOrgans.Count > 0) return "Target Organs";
                return "No Special Organs";
            }
        }

        public int OrganCount
        {
            get { return Mathf.Max(beneficialOrgans.Count, targetOrgans.Count); }
        }

        public string AddedAbilityText
        {
            get
            {
                if (OrganCount == 0) return "No additional abilities";

                string organType = beneficialOrgans.Count > 0 ? "beneficial" : "target";
                int bonusPercent = CalculateOrganBonusPercent();

                if (beneficialOrgans.Count > 0)
                {
                    return $"Since the Enerling has {OrganCount} {organType} organs, plus {bonusPercent}% healing every {skill2.cooldownTurns} turns";
                }
                else if (targetOrgans.Count > 0)
                {
                    return $"Since the Enerling has {OrganCount} {organType} organs, plus {bonusPercent}% damage every {skill1.cooldownTurns} turns";
                }

                return "No additional abilities";
            }
        }

        // Calculate organ bonus percentage based on your distribution logic
        private int CalculateOrganBonusPercent()
        {
            int organCount = OrganCount;

            // Your logic: 2 organs = 5%, 3 organs = 10%, 4 organs = 15%
            switch (organCount)
            {
                case 1: return 0;
                case 2: return 5;
                case 3: return 10;
                case 4: return 15;
                case 5: return 20;
                default: return 0;
            }
        }

        // Get effective max life after armor
        public int GetEffectiveMaxLife()
        {
            float armorMultiplier = (100f - armorPercent) / 100f;
            return Mathf.RoundToInt(baseLife * armorMultiplier);
        }

        // Reset battle state
        public void ResetBattleState()
        {
            currentLife = baseLife;
            skill1Cooldown = 0;
            skill2Cooldown = 0;
            skill3Cooldown = 0;
            skill4Cooldown = 0;
        }

        // Reduce cooldowns
        public void ReduceCooldowns()
        {
            if (skill1Cooldown > 0) skill1Cooldown--;
            if (skill2Cooldown > 0) skill2Cooldown--;
            if (skill3Cooldown > 0) skill3Cooldown--;
            if (skill4Cooldown > 0) skill4Cooldown--;
        }

        // Check if skill is ready
        public bool IsSkillReady(int skillNumber)
        {
            switch (skillNumber)
            {
                case 1: return skill1Cooldown == 0;
                case 2: return skill2Cooldown == 0;
                case 3: return skill3Cooldown == 0;
                case 4: return skill4Cooldown == 0;
                default: return false;
            }
        }

        // Set skill cooldown
        public void SetSkillCooldown(int skillNumber)
        {
            switch (skillNumber)
            {
                case 1:
                    skill1Cooldown = skill1.cooldownTurns;
                    break;
                case 2:
                    skill2Cooldown = skill2.cooldownTurns;
                    break;
                case 3:
                    skill3Cooldown = skill3.cooldownTurns;
                    break;
                case 4:
                    skill4Cooldown = skill4.cooldownTurns;
                    break;
            }
        }

        // Heal the enerling
        public void Heal(int amount)
        {
            currentLife = Mathf.Min(baseLife, currentLife + amount);
        }

        // Take damage
        public void TakeDamage(int amount)
        {
            currentLife = Mathf.Max(0, currentLife - amount);
        }
    }

    [Header("Organ Sprites (For UI)")]
    public Sprite heartSprite;
    public Sprite liverSprite;
    public Sprite kidneySprite;
    public Sprite pancreasSprite;
    public Sprite brainSprite;

    [Header("Frame Sprites (For UI)")]
    public Sprite commonFrameSprite;
    public Sprite rareFrameSprite;
    public Sprite ultraRareFrameSprite;

    [Header("Rarity Icons (For UI)")]
    public Sprite commonRarityIcon;
    public Sprite rareRarityIcon;
    public Sprite ultraRareRarityIcon;

    [Header("Ingredients List")]
    [SerializeField]
    public List<IngredientInfo> ingredients = new List<IngredientInfo>();

    // Get ingredient by name
    public IngredientInfo GetIngredientInfo(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return ingredients.Find(i => i.ingredientName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    // Get ingredient by index
    public IngredientInfo GetIngredientInfo(int index)
    {
        if (index >= 0 && index < ingredients.Count)
            return ingredients[index];
        return null;
    }

    // Get all unlocked ingredients
    public List<IngredientInfo> GetUnlockedIngredients()
    {
        return ingredients.FindAll(i => i.isUnlocked);
    }

    // Get all locked ingredients
    public List<IngredientInfo> GetLockedIngredients()
    {
        return ingredients.FindAll(i => !i.isUnlocked);
    }

    // Get ingredients by rarity
    public List<IngredientInfo> GetIngredientsByRarity(Rarity rarity)
    {
        return ingredients.FindAll(i => i.rarity == rarity && i.isUnlocked);
    }

    // Get ingredients by kingdom
    public List<IngredientInfo> GetIngredientsByKingdom(KingdomOrigin kingdom)
    {
        return ingredients.FindAll(i => i.kingdom == kingdom && i.isUnlocked);
    }

    // Get ingredients by both rarity and kingdom
    public List<IngredientInfo> GetIngredientsByFilter(Rarity rarityFilter, KingdomOrigin kingdomFilter, bool useRarityFilter, bool useKingdomFilter)
    {
        List<IngredientInfo> filtered = new List<IngredientInfo>();

        foreach (var ingredient in ingredients)
        {
            if (!ingredient.isUnlocked) continue;

            bool rarityMatch = !useRarityFilter || ingredient.rarity == rarityFilter;
            bool kingdomMatch = !useKingdomFilter || ingredient.kingdom == kingdomFilter;

            if (rarityMatch && kingdomMatch)
            {
                filtered.Add(ingredient);
            }
        }

        return filtered;
    }

    // Get organ sprite by name
    public Sprite GetOrganSprite(string organName)
    {
        switch (organName.ToLower())
        {
            case "heart": return heartSprite;
            case "liver": return liverSprite;
            case "kidney":
            case "kidneys": return kidneySprite;
            case "pancreas": return pancreasSprite;
            case "brain": return brainSprite;
            default: return null;
        }
    }

    // Get frame sprite by rarity
    public Sprite GetFrameSprite(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return commonFrameSprite;
            case Rarity.Rare: return rareFrameSprite;
            case Rarity.UltraRare: return ultraRareFrameSprite;
            default: return commonFrameSprite;
        }
    }

    // Get rarity icon by rarity
    public Sprite GetRarityIcon(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return commonRarityIcon;
            case Rarity.Rare: return rareRarityIcon;
            case Rarity.UltraRare: return ultraRareRarityIcon;
            default: return commonRarityIcon;
        }
    }

    // Unlock an ingredient (runtime only)
    public void UnlockIngredient(string name)
    {
        var ingredient = GetIngredientInfo(name);
        if (ingredient != null && !ingredient.isUnlocked)
        {
            ingredient.isUnlocked = true;
            Debug.Log($"Database: Unlocked {name}");
        }
    }

    // Lock an ingredient (runtime only)
    public void LockIngredient(string name)
    {
        var ingredient = GetIngredientInfo(name);
        if (ingredient != null)
        {
            ingredient.isUnlocked = false;
        }
    }

    // Unlock by index
    public void UnlockIngredient(int index)
    {
        if (index >= 0 && index < ingredients.Count)
        {
            ingredients[index].isUnlocked = true;
        }
    }

    // Reset all unlocks
    public void ResetAllUnlocks()
    {
        foreach (var ingredient in ingredients)
        {
            ingredient.isUnlocked = false;
        }
    }

    // Reset all battle states
    public void ResetAllBattleStates()
    {
        foreach (var ingredient in ingredients)
        {
            ingredient.ResetBattleState();
        }
    }

    // Get total count
    public int GetTotalIngredients()
    {
        return ingredients.Count;
    }

    // Get unlocked count
    public int GetUnlockedCount()
    {
        return GetUnlockedIngredients().Count;
    }

    // Get ingredient index by name
    public int GetIngredientIndex(string name)
    {
        return ingredients.FindIndex(i => i.ingredientName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    // Create a battle-ready copy of an ingredient
    public IngredientInfo CreateBattleCopy(string name)
    {
        var original = GetIngredientInfo(name);
        if (original == null) return null;

        // Create a copy with battle state reset
        IngredientInfo copy = new IngredientInfo
        {
            ingredientName = original.ingredientName,
            rarity = original.rarity,
            kingdom = original.kingdom,
            isUnlocked = original.isUnlocked,
            enerlingSprite = original.enerlingSprite,
            modelPrefab = original.modelPrefab,
            animatorController = original.animatorController,
            baseLife = original.baseLife,
            currentLife = original.baseLife, // Start with full life
            armorPercent = original.armorPercent,
            baseDamage = original.baseDamage,
            immuneToOrganDamage = original.immuneToOrganDamage,
            beneficialOrgans = new List<string>(original.beneficialOrgans),
            targetOrgans = new List<string>(original.targetOrgans),
            skill1 = original.skill1,
            skill2 = original.skill2,
            skill3 = original.skill3,
            skill4 = original.skill4,
            enerlingDescription = original.enerlingDescription,
            enerlingStory = original.enerlingStory,
            audioClip = original.audioClip,
            endingCutscene = original.endingCutscene // Copy the video reference
        };

        return copy;
    }
}