using UnityEngine;
using System.Collections.Generic;

// Enum for preservative types
public enum PreservativeType
{
    SodiumBenzoate,    // Anti-Microbial
    AscorbicAcid,      // Anti-Oxidant
    PotassiumSorbate   // Anti-Microbial
}

[CreateAssetMenu(fileName = "K3_FoodDatabase", menuName = "Preservative/K3_Food Database")]
public class K3_FoodDatabase : ScriptableObject
{
    [System.Serializable]
    public class FoodProfile
    {
        [Header("Basic Information")]
        public string foodName;
        public string foodType;
        [TextArea(1, 2)]
        public string shelfLife;
        
        [Header("Preservative Settings")]
        [Tooltip("Select the preservative type for this food")]
        public PreservativeType preservativeType;
        [Range(0, 100)] public int minSliderValue = 0;
        [Range(0, 100)] public int maxSliderValue = 100;
        
        [Header("Threat Information")]
        [TextArea(1, 2)]
        public string threats;
        [TextArea(2, 3)]
        public string contents;
        
        [Header("Game Information")]
        [TextArea(3, 4)]
        public string hint;
        
        [Header("Display Settings")]
        public Sprite foodIcon; // Optional: Add food image
        
        // Helper properties (read-only, calculated at runtime)
        public string PreservativeDisplayName
        {
            get
            {
                switch (preservativeType)
                {
                    case PreservativeType.SodiumBenzoate:
                        return "Sodium Benzoate (Anti-Microbial)";
                    case PreservativeType.AscorbicAcid:
                        return "Ascorbic Acid (Anti-Oxidant)";
                    case PreservativeType.PotassiumSorbate:
                        return "Potassium Sorbate (Anti-Microbial)";
                    default:
                        return "Unknown Preservative";
                }
            }
        }
        
        public string PreservativeCategory
        {
            get
            {
                switch (preservativeType)
                {
                    case PreservativeType.SodiumBenzoate:
                    case PreservativeType.PotassiumSorbate:
                        return "Anti-Microbial";
                    case PreservativeType.AscorbicAcid:
                        return "Anti-Oxidant";
                    default:
                        return "Unknown";
                }
            }
        }
        
        public string RangeDescription
        {
            get
            {
                string category = "";
                if (maxSliderValue <= 20) category = "Minimal";
                else if (maxSliderValue <= 50) category = "Moderate";
                else if (maxSliderValue <= 80) category = "High";
                else category = "Very High";
                
                return $"{minSliderValue}-{maxSliderValue} ({category})";
            }
        }
        
        // Method to check if a value is in range
        public bool IsValueInRange(float value)
        {
            return value >= minSliderValue && value <= maxSliderValue;
        }
    }
    
    [Header("Food Profiles Database")]
    [Tooltip("Order MUST match K3_KingAssessment.KAFoods array")]
    public FoodProfile[] foodProfiles = new FoodProfile[8];
    
    [Header("Editor Tools")]
    [SerializeField] private bool autoInitialize = false;
    [SerializeField] private bool showDebugInfo = false;
    
    // Runtime access (Singleton-like pattern for ScriptableObject)
    private static K3_FoodDatabase _instance;
    public static K3_FoodDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // Load from Resources folder
                _instance = Resources.Load<K3_FoodDatabase>("K3_FoodDatabase");
                
                if (_instance == null)
                {
                    Debug.LogError("K3_FoodDatabase not found in Resources folder! Create one via Create->Preservative->K3_Food Database");
                }
            }
            return _instance;
        }
    }
    
    // Initialize with default data in the Editor
    private void OnValidate()
    {
        if (autoInitialize && foodProfiles.Length == 8)
        {
            InitializeDefaultData();
        }
        
        if (showDebugInfo)
        {
            UpdateDebugInfo();
        }
    }
    
    private void InitializeDefaultData()
    {
        // 1. Bread - 10 GAP RANGE: 40-50
        if (foodProfiles[0] == null) foodProfiles[0] = new FoodProfile();
        foodProfiles[0].foodName = "Bread";
        foodProfiles[0].foodType = "Baked Goods";
        foodProfiles[0].shelfLife = "Spoils within days if unpreserved";
        foodProfiles[0].preservativeType = PreservativeType.PotassiumSorbate;
        foodProfiles[0].minSliderValue = 40;
        foodProfiles[0].maxSliderValue = 50; // 10 gap
        foodProfiles[0].threats = "🦠 Mold (Moderate)";
        foodProfiles[0].contents = "Moist, sugar present";
        foodProfiles[0].hint = "Bread molds easily — apply anti-microbial potion to prevent spoilage!";
        
        // 2. Cake - 10 GAP RANGE: 50-60
        if (foodProfiles[1] == null) foodProfiles[1] = new FoodProfile();
        foodProfiles[1].foodName = "Cake";
        foodProfiles[1].foodType = "Baked Goods";
        foodProfiles[1].shelfLife = "Very short if unpreserved";
        foodProfiles[1].preservativeType = PreservativeType.PotassiumSorbate;
        foodProfiles[1].minSliderValue = 50;
        foodProfiles[1].maxSliderValue = 60; // 10 gap
        foodProfiles[1].threats = "🦠 Mold (High)";
        foodProfiles[1].contents = "Moist, sugar-rich";
        foodProfiles[1].hint = "This moist cake is at high risk of mold — use anti-microbial potion!";
        
        // 3. Canned Fruits - 10 GAP RANGE: 40-50
        if (foodProfiles[2] == null) foodProfiles[2] = new FoodProfile();
        foodProfiles[2].foodName = "Canned Fruits";
        foodProfiles[2].foodType = "Preserved Snack";
        foodProfiles[2].shelfLife = "Several months if unpreserved";
        foodProfiles[2].preservativeType = PreservativeType.AscorbicAcid;
        foodProfiles[2].minSliderValue = 40;
        foodProfiles[2].maxSliderValue = 50; // 10 gap
        foodProfiles[2].threats = "🟠 Oxidation (Moderate)";
        foodProfiles[2].contents = "Sealed, but still at risk of color loss";
        foodProfiles[2].hint = "Keep the canned fruits bright — use anti-oxidant potion to prevent browning!";
        
        // 4. Fresh-Cut Fruits - 10 GAP RANGE: 50-60
        if (foodProfiles[3] == null) foodProfiles[3] = new FoodProfile();
        foodProfiles[3].foodName = "Fresh-Cut Fruits";
        foodProfiles[3].foodType = "Snack";
        foodProfiles[3].shelfLife = "Very short if unpreserved";
        foodProfiles[3].preservativeType = PreservativeType.AscorbicAcid;
        foodProfiles[3].minSliderValue = 50;
        foodProfiles[3].maxSliderValue = 60; // 10 gap
        foodProfiles[3].threats = "🟠 Oxidation / Browning (High)";
        foodProfiles[3].contents = "Moist, exposed to air";
        foodProfiles[3].hint = "These fruits oxidize quickly — apply anti-oxidant potion to keep them fresh!";
        
        // 5. Cheese - 10 GAP RANGE: 40-50
        if (foodProfiles[4] == null) foodProfiles[4] = new FoodProfile();
        foodProfiles[4].foodName = "Cheese";
        foodProfiles[4].foodType = "Dairy";
        foodProfiles[4].shelfLife = "Spoils in a few days if unpreserved";
        foodProfiles[4].preservativeType = PreservativeType.PotassiumSorbate;
        foodProfiles[4].minSliderValue = 40;
        foodProfiles[4].maxSliderValue = 50; // 10 gap
        foodProfiles[4].threats = "🦠 Mold (Moderate)";
        foodProfiles[4].contents = "Moist, high fat";
        foodProfiles[4].hint = "Cheese can grow mold fast — apply anti-microbial potion!";
        
        // 6. Sausages - 10 GAP RANGE: 30-40
        if (foodProfiles[5] == null) foodProfiles[5] = new FoodProfile();
        foodProfiles[5].foodName = "Sausages / Hotdogs";
        foodProfiles[5].foodType = "Processed Meat";
        foodProfiles[5].shelfLife = "Spoils within days if unpreserved";
        foodProfiles[5].preservativeType = PreservativeType.AscorbicAcid;
        foodProfiles[5].minSliderValue = 30;
        foodProfiles[5].maxSliderValue = 40; // 10 gap
        foodProfiles[5].threats = "🟠 Oxidation / Rancidity (Moderate)";
        foodProfiles[5].contents = "High fat and protein";
        foodProfiles[5].hint = "Prevent the meat from turning rancid — use anti-oxidant potion!";
        
        // 7. Soft Drink - 10 GAP RANGE: 70-80
        if (foodProfiles[6] == null) foodProfiles[6] = new FoodProfile();
        foodProfiles[6].foodName = "Soft Drink";
        foodProfiles[6].foodType = "Beverage";
        foodProfiles[6].shelfLife = "Spoils fast if unpreserved";
        foodProfiles[6].preservativeType = PreservativeType.SodiumBenzoate;
        foodProfiles[6].minSliderValue = 70;
        foodProfiles[6].maxSliderValue = 80; // 10 gap (from your table: 70-85, adjusted to 70-80)
        foodProfiles[6].threats = "🦠 Microbial Growth (High)";
        foodProfiles[6].contents = "High sugar content, liquid";
        foodProfiles[6].hint = "This sugary drink is very vulnerable to microbes — use anti-microbial potion!";
        
        // 8. Fruit Juice - MULTIPLE PRESERVATIVES
        // For Sodium Benzoate: 50-60 (from your table: 50-70, adjusted to 50-60)
        // For Ascorbic Acid: 40-50 (from your table: 40-60, adjusted to 40-50)
        if (foodProfiles[7] == null) foodProfiles[7] = new FoodProfile();
        foodProfiles[7].foodName = "Fruit Juice";
        foodProfiles[7].foodType = "Beverage";
        foodProfiles[7].shelfLife = "5 days if unpreserved";
        // Fruit Juice can use EITHER Sodium Benzoate OR Ascorbic Acid
        // Defaulting to Sodium Benzoate for the database, but code handles both
        foodProfiles[7].preservativeType = PreservativeType.SodiumBenzoate;
        foodProfiles[7].minSliderValue = 50;
        foodProfiles[7].maxSliderValue = 60; // 10 gap
        foodProfiles[7].threats = "🦠 Microbial Growth (Moderate) & 🟠 Oxidation";
        foodProfiles[7].contents = "Acidic with sugar, natural vitamins";
        foodProfiles[7].hint = "Fruit juice needs protection from both microbes AND oxidation. You may need multiple preservatives!";
    }
    
    private void UpdateDebugInfo()
    {
        Debug.Log("=== K3 Food Database Debug Info ===");
        for (int i = 0; i < foodProfiles.Length; i++)
        {
            if (foodProfiles[i] != null)
            {
                Debug.Log($"Index {i}: {foodProfiles[i].foodName} - {foodProfiles[i].PreservativeDisplayName} - Range: {foodProfiles[i].minSliderValue}-{foodProfiles[i].maxSliderValue}");
            }
            else
            {
                Debug.Log($"Index {i}: [EMPTY SLOT]");
            }
        }
    }
    
    // ========== PUBLIC ACCESS METHODS ==========
    
    // Get food profile by index (matches K3_KingAssessment positioning)
    public FoodProfile GetFoodProfile(int index)
    {
        if (index >= 0 && index < foodProfiles.Length && foodProfiles[index] != null)
        {
            return foodProfiles[index];
        }
        
        Debug.LogWarning($"K3_FoodDatabase: No food profile found at index {index}");
        return null;
    }
    
    // Get slider range as Vector2Int
    public Vector2Int GetSliderRange(int index)
    {
        var profile = GetFoodProfile(index);
        if (profile != null)
        {
            return new Vector2Int(profile.minSliderValue, profile.maxSliderValue);
        }
        return new Vector2Int(0, 100);
    }
    
    // Check if value is in correct range
    public bool IsValueInRange(int index, float value)
    {
        var profile = GetFoodProfile(index);
        if (profile != null)
        {
            return profile.IsValueInRange(value);
        }
        return false;
    }
    
    // Get all food names (for debugging)
    public string[] GetAllFoodNames()
    {
        List<string> names = new List<string>();
        foreach (var profile in foodProfiles)
        {
            if (profile != null)
            {
                names.Add(profile.foodName);
            }
        }
        return names.ToArray();
    }
    
    // Find index by food name (optional helper)
    public int GetFoodIndexByName(string foodName)
    {
        for (int i = 0; i < foodProfiles.Length; i++)
        {
            if (foodProfiles[i] != null && foodProfiles[i].foodName == foodName)
            {
                return i;
            }
        }
        return -1;
    }
    
    // Special method for Fruit Juice dual preservative info
    public string GetFruitJuicePreservativeInfo()
    {
        return "Fruit Juice can use either:\n" +
               "• Sodium Benzoate (50-60) for microbial protection\n" +
               "• Ascorbic Acid (40-50) for oxidation protection";
    }
}