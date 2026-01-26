using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "IngredientDatabase", menuName = "NutriVenture/Ingredient Database")]
public class IngredientDatabase : ScriptableObject
{
    [System.Serializable]
    public enum KingdomOrigin
    {
        NutriKingdom,
        Alerthia,
        Suragria,
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

        [Header("Skill Type")]
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

        [Header("Cooldown")]
        public int cooldownTurns = 0;

        [Header("Visual")]
        public Sprite skillSprite;

        [Header("Description")]
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
    }

    [System.Serializable]
    public class IngredientInfo
    {
        [Header("Basic Info")]
        public string ingredientName;
        public Rarity rarity = Rarity.Common;
        public KingdomOrigin kingdom = KingdomOrigin.NutriKingdom;
        public bool isUnlocked = false;

        [Header("Visuals")]
        public Sprite enerlingSprite;
        public GameObject modelPrefab;

        [Header("Animation")]
        public RuntimeAnimatorController animatorController;

        [Header("Core Stats")]
        [Tooltip("Base life points")]
        public int baseLife = 100;

        [Tooltip("Armor as percentage (0-100)")]
        [Range(0, 100)]
        public int armorPercent = 0;

        [Tooltip("Base damage for normal attacks")]
        public int baseDamage = 10;

        [Header("Immunities")]
        [Tooltip("Immune to organ damage effects")]
        public bool immuneToOrganDamage = false;

        [Header("Beneficial Organs")]
        [Tooltip("Organs that this ingredient benefits (for healing calculations)")]
        public List<string> beneficialOrgans = new List<string>();

        [Header("Target Organs")]
        [Tooltip("Organs that this ingredient targets (for damage calculations)")]
        public List<string> targetOrgans = new List<string>();

        [Header("Skills")]
        public SkillInfo skill1;
        public SkillInfo skill2;
        public SkillInfo skill3;

        [Header("Description")]
        [TextArea(3, 5)]
        public string ingredientDescription;

        // Battle status (runtime only)
        [System.NonSerialized]
        public int currentLife = 100;

        [System.NonSerialized]
        public int skill1Cooldown = 0;

        [System.NonSerialized]
        public int skill2Cooldown = 0;

        [System.NonSerialized]
        public int skill3Cooldown = 0;

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
        }

        // Reduce cooldowns
        public void ReduceCooldowns()
        {
            if (skill1Cooldown > 0) skill1Cooldown--;
            if (skill2Cooldown > 0) skill2Cooldown--;
            if (skill3Cooldown > 0) skill3Cooldown--;
        }

        // Check if skill is ready
        public bool IsSkillReady(int skillNumber)
        {
            switch (skillNumber)
            {
                case 1: return skill1Cooldown == 0;
                case 2: return skill2Cooldown == 0;
                case 3: return skill3Cooldown == 0;
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
            }
        }
    }

    [Header("Ingredients List")]
    [SerializeField]
    public List<IngredientInfo> ingredients = new List<IngredientInfo>();

    // Get ingredient by name
    public IngredientInfo GetIngredientInfo(string name)
    {
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
        return ingredients.FindAll(i => i.rarity == rarity);
    }

    // Get ingredients by kingdom
    public List<IngredientInfo> GetIngredientsByKingdom(KingdomOrigin kingdom)
    {
        return ingredients.FindAll(i => i.kingdom == kingdom);
    }

    // Unlock an ingredient
    public void UnlockIngredient(string name)
    {
        var ingredient = GetIngredientInfo(name);
        if (ingredient != null)
        {
            ingredient.isUnlocked = true;
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
            currentLife = original.baseLife,
            armorPercent = original.armorPercent,
            baseDamage = original.baseDamage,
            immuneToOrganDamage = original.immuneToOrganDamage,
            beneficialOrgans = new List<string>(original.beneficialOrgans),
            targetOrgans = new List<string>(original.targetOrgans),
            skill1 = original.skill1,
            skill2 = original.skill2,
            skill3 = original.skill3,
            ingredientDescription = original.ingredientDescription
        };

        return copy;
    }
}