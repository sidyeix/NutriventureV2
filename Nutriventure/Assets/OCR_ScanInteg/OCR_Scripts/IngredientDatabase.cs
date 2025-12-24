using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "IngredientDatabase", menuName = "NutriVenture/Ingredient Database")]
public class IngredientDatabase : ScriptableObject
{
    [System.Serializable]
    public class IngredientInfo
    {
        [Header("Basic Info")]
        public string ingredientName;
        public GameObject modelPrefab;

        [Header("Animation Controller")]
        public RuntimeAnimatorController animatorController; // Each ingredient gets its own animator
        
        [Header("Animation Triggers")]
        public string idleTrigger = "Idle";
        public string attackTrigger = "Attack";
        public string hitTrigger = "Hit";
        public string blockTrigger = "Block";
        public string deathTrigger = "Death";

        [Header("Battle Stats")]
        [Tooltip("Damage to Liver, Heart, and Kidney respectively.")]
        public int liverDamage;
        public int heartDamage;
        public int kidneyDamage;

        [Header("Visual Effects")]
        public GameObject hitParticlePrefab;
        public GameObject blockParticlePrefab;
    }

    [SerializeField]
    public List<IngredientInfo> ingredients = new List<IngredientInfo>();

    public IngredientInfo GetIngredientInfo(string name)
    {
        return ingredients.Find(i => i.ingredientName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }
}