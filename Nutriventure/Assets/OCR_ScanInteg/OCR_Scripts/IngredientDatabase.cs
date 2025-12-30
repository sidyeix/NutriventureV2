using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "IngredientDatabase", menuName = "NutriVenture/Ingredient Database")]
public class IngredientDatabase : ScriptableObject
{
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
        
        [Header("Damage/Range")]
        [Tooltip("Damage value or range (for damage/heal skills)")]
        public int damageValue;
        
        [Header("Organ Damage")]
        public int liverDamage;
        public int heartDamage;
        public int kidneyDamage;
        
        [Header("Visual")]
        public Sprite skillSprite;
    }
    
    [System.Serializable]
    public class IngredientInfo
    {
        [Header("Basic Info")]
        public string ingredientName;
        public bool isUnlocked = false;
        public Sprite enerlingSprite;
        public GameObject modelPrefab;
        
        [Header("Animation Controller")]
        public RuntimeAnimatorController animatorController;
        
        [Header("Animation Triggers")]
        public string idleTrigger = "Idle";
        public string attackTrigger = "Attack";
        public string hitTrigger = "Hit";
        public string blockTrigger = "Block";
        public string deathTrigger = "Death";
        
        [Header("Health Stats")]
        public int liverHealth;
        public int heartHealth;
        public int kidneyHealth;
        
        [Header("Skills")]
        public SkillInfo skill1;
        public SkillInfo skill2;
        public SkillInfo skill3;
    }
    
    [Header("Ingredients List")]
    [SerializeField]
    public List<IngredientInfo> ingredients = new List<IngredientInfo>();
    
    public IngredientInfo GetIngredientInfo(string name)
    {
        return ingredients.Find(i => i.ingredientName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }
    
    public List<IngredientInfo> GetUnlockedIngredients()
    {
        return ingredients.FindAll(i => i.isUnlocked);
    }
    
    public List<IngredientInfo> GetLockedIngredients()
    {
        return ingredients.FindAll(i => !i.isUnlocked);
    }
    
    public void UnlockIngredient(string name)
    {
        var ingredient = GetIngredientInfo(name);
        if (ingredient != null)
        {
            ingredient.isUnlocked = true;
        }
    }
    
    public void ResetAllUnlocks()
    {
        foreach (var ingredient in ingredients)
        {
            ingredient.isUnlocked = false;
        }
    }
}