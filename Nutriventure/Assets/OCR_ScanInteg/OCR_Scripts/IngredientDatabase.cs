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

        [Header("Damage Values")]
        [Tooltip("Base damage/heal value")]
        public int baseValue;

        [Tooltip("Minimum damage for random range")]
        public int minValue;

        [Tooltip("Maximum damage for random range")]
        public int maxValue;

        [Tooltip("Use random range instead of base value")]
        public bool useRandomRange = false;

        [Header("Organ Damage Multipliers")]
        [Tooltip("Bonus damage multiplier against heart (percentage)")]
        [Range(0, 200)]
        public int heartDamageMultiplier = 100; // 100% = normal damage

        [Tooltip("Bonus damage multiplier against liver (percentage)")]
        [Range(0, 200)]
        public int liverDamageMultiplier = 100;

        [Tooltip("Bonus damage multiplier against kidneys (percentage)")]
        [Range(0, 200)]
        public int kidneyDamageMultiplier = 100;

        [Tooltip("Bonus damage multiplier against pancreas (percentage)")]
        [Range(0, 200)]
        public int pancreasDamageMultiplier = 100;

        [Tooltip("Bonus damage multiplier against stomach (percentage)")]
        [Range(0, 200)]
        public int stomachDamageMultiplier = 100;

        [Tooltip("Bonus damage multiplier against brain (percentage)")]
        [Range(0, 200)]
        public int brainDamageMultiplier = 100;

        [Header("Defense Properties")]
        [Tooltip("Percentage of damage blocked (0-100)")]
        [Range(0, 100)]
        public int blockPercent = 0;

        [Tooltip("Against which rarities does this block work?")]
        public Rarity[] blockWorksAgainst = new Rarity[] { Rarity.Common };

        [Header("Heal Properties")]
        [Tooltip("Percentage of max life to heal (0-100)")]
        [Range(0, 100)]
        public int healPercent = 0;

        [Header("Cooldown")]
        public int cooldownTurns = 0;

        [Header("Visual")]
        public Sprite skillSprite;

        [Header("Description")]
        public string skillDescription;

        // Get actual damage value (either base or random)
        public int GetDamageValue()
        {
            if (useRandomRange)
            {
                return Random.Range(minValue, maxValue + 1);
            }
            return baseValue;
        }

        // Calculate total damage with organ multiplier
        public int CalculateTotalDamage(IngredientInfo attacker, IngredientInfo target, string organType)
        {
            int baseDamage = GetDamageValue();
            float multiplier = 1f;

            // Apply organ-specific multiplier
            switch (organType.ToLower())
            {
                case "heart":
                    multiplier = heartDamageMultiplier / 100f;
                    break;
                case "liver":
                    multiplier = liverDamageMultiplier / 100f;
                    break;
                case "kidney":
                    multiplier = kidneyDamageMultiplier / 100f;
                    break;
                case "pancreas":
                    multiplier = pancreasDamageMultiplier / 100f;
                    break;
                case "stomach":
                    multiplier = stomachDamageMultiplier / 100f;
                    break;
                case "brain":
                    multiplier = brainDamageMultiplier / 100f;
                    break;
            }

            // If target is immune to organ damage, don't apply multipliers
            if (target.immuneToOrganDamage && multiplier > 1f)
            {
                multiplier = 1f; // Just use base damage
            }

            return Mathf.RoundToInt(baseDamage * multiplier);
        }
    }

    [System.Serializable]
    public class IngredientInfo
    {
        [Header("Basic Info")]
        public string ingredientName; // Keep this name for compatibility
        public Rarity rarity = Rarity.Common;
        public KingdomOrigin kingdom = KingdomOrigin.NutriKingdom;
        public bool isUnlocked = false;

        [Header("Visuals")]
        public Sprite enerlingSprite;
        public GameObject modelPrefab;

        [Header("Animation")]
        public RuntimeAnimatorController animatorController;
        public string idleTrigger = "Idle";
        public string attackTrigger = "Attack";
        public string hitTrigger = "Hit";
        public string blockTrigger = "Block";
        public string deathTrigger = "Death";

        [Header("Core Stats")]
        [Tooltip("Base life points")]
        public int baseLife = 100;

        [Tooltip("Current life (for battle)")]
        public int currentLife = 100;

        [Tooltip("Armor as percentage (0-100)")]
        [Range(0, 100)]
        public int armorPercent = 0;

        [Tooltip("Base damage for normal attacks")]
        public int baseDamage = 10;

        [Header("Immunities")]
        [Tooltip("Immune to all organ damage effects (extra damage from multipliers)")]
        public bool immuneToOrganDamage = false;

        [Header("Organ Weaknesses")]
        [Tooltip("Which organs are weak (for taking extra damage)")]
        public List<string> weakOrgans = new List<string>();

        [Header("Win Chance Modifiers")]
        [Tooltip("Win chance against Common rarity (0-100)")]
        [Range(0, 100)]
        public int winChanceVsCommon = 50;

        [Tooltip("Win chance against Rare rarity (0-100)")]
        [Range(0, 100)]
        public int winChanceVsRare = 50;

        [Tooltip("Win chance against UltraRare rarity (0-100)")]
        [Range(0, 100)]
        public int winChanceVsUltraRare = 50;

        [Header("Skills")]
        public SkillInfo skill1;
        public SkillInfo skill2;
        public SkillInfo skill3;

        [Header("Description")]
        [TextArea(3, 5)]
        public string ingredientDescription;

        // Battle status
        [System.NonSerialized]
        public int skill1Cooldown = 0;

        [System.NonSerialized]
        public int skill2Cooldown = 0;

        [System.NonSerialized]
        public int skill3Cooldown = 0;

        // Helper method to calculate win chance based on opponent rarity
        public int GetWinChanceAgainst(Rarity opponentRarity)
        {
            switch (opponentRarity)
            {
                case Rarity.Common:
                    return winChanceVsCommon;
                case Rarity.Rare:
                    return winChanceVsRare;
                case Rarity.UltraRare:
                    return winChanceVsUltraRare;
                default:
                    return 50;
            }
        }

        // Get effective life after armor
        public int GetEffectiveLife()
        {
            float armorMultiplier = (100f - armorPercent) / 100f;
            return Mathf.RoundToInt(baseLife * armorMultiplier);
        }

        // Check if an organ is weak
        public bool IsOrganWeak(string organ)
        {
            return weakOrgans.Contains(organ.ToLower());
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

    // Get ingredient by name - KEEP THIS METHOD FOR COMPATIBILITY
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

    // Get all unlocked ingredients - KEEP FOR COMPATIBILITY
    public List<IngredientInfo> GetUnlockedIngredients()
    {
        return ingredients.FindAll(i => i.isUnlocked);
    }

    // Get all locked ingredients - KEEP FOR COMPATIBILITY
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

    // Unlock an ingredient - KEEP FOR COMPATIBILITY
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

    // Reset all unlocks - KEEP FOR COMPATIBILITY
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
            idleTrigger = original.idleTrigger,
            attackTrigger = original.attackTrigger,
            hitTrigger = original.hitTrigger,
            blockTrigger = original.blockTrigger,
            deathTrigger = original.deathTrigger,
            baseLife = original.baseLife,
            currentLife = original.baseLife,
            armorPercent = original.armorPercent,
            baseDamage = original.baseDamage,
            immuneToOrganDamage = original.immuneToOrganDamage,
            weakOrgans = new List<string>(original.weakOrgans),
            winChanceVsCommon = original.winChanceVsCommon,
            winChanceVsRare = original.winChanceVsRare,
            winChanceVsUltraRare = original.winChanceVsUltraRare,
            skill1 = original.skill1,
            skill2 = original.skill2,
            skill3 = original.skill3,
            ingredientDescription = original.ingredientDescription
        };

        return copy;
    }
}