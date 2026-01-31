using UnityEngine;
using System.Collections.Generic;

public static class IngredientCategory
{
    // Define categories for each canonical ingredient - MUST MATCH JAVA LIST
    private static Dictionary<string, string> ingredientCategoryMap = new Dictionary<string, string>
    {
        // PRESERVATIVES (from Java)
        {"Sodium Nitrite", "PRESERVATIVE"},
        {"Sodium Benzoate", "PRESERVATIVE"},
        
        // NUTRIFICANTS (from Java)
        {"Calcium", "NUTRIFICANT"},
        {"Iron", "NUTRIFICANT"},
        {"Vitamin C", "NUTRIFICANT"},
        {"Vitamin A", "NUTRIFICANT"},
        {"Folic Acid", "NUTRIFICANT"},
        
        // ALLERGENS (from Java)
        {"Milk", "ALLERGEN"},
        {"Egg", "ALLERGEN"},
        {"Peanuts", "ALLERGEN"},
        {"Shrimp", "ALLERGEN"},
        
        // SWEETENERS (from Java)
        {"Sugar", "SWEETENER"},
        {"Corn Syrup", "SWEETENER"},
        {"Aspartame", "SWEETENER"},
        {"Stevia Extract", "SWEETENER"},
        {"Sorbitol", "SWEETENER"},
        {"Sucralose", "SWEETENER"}
    };

    // Map synonyms to their canonical names (must match Java)
    private static Dictionary<string, string> ingredientSynonyms = new Dictionary<string, string>
    {
        // Exact matches to Java list - NO EXTRA SYNONYMS
        // The Java only has exact matching, so we should too
        {"calcium", "Calcium"},
        {"iron", "Iron"},
        {"vitamin c", "Vitamin C"},
        {"vitamin a", "Vitamin A"},
        {"milk", "Milk"},
        {"egg", "Egg"},
        {"eggs", "Egg"},
        {"peanut", "Peanuts"},
        {"shrimp", "Shrimp"},
        {"sugar", "Sugar"},
        {"corn syrup", "Corn Syrup"},
        {"sodium nitrite", "Sodium Nitrite"},
        {"aspartame", "Aspartame"},
        {"sodium benzoate", "Sodium Benzoate"},
        {"stevia extract", "Stevia Extract"},
        {"sorbitol", "Sorbitol"},
        {"sucralose", "Sucralose"},
        {"folic acid", "Folic Acid"}
    };

    public static string GetCategory(string ingredientName)
    {
        if (string.IsNullOrEmpty(ingredientName))
            return "OTHER";

        string lowerName = ingredientName.ToLower().Trim();
        
        // First, get canonical name from synonyms (matches Java)
        string canonicalName = ingredientName;
        if (ingredientSynonyms.ContainsKey(lowerName))
        {
            canonicalName = ingredientSynonyms[lowerName];
        }
        else
        {
            // Check if it's already a canonical name (case-insensitive)
            foreach (var entry in ingredientCategoryMap)
            {
                if (entry.Key.ToLower() == lowerName)
                {
                    canonicalName = entry.Key;
                    break;
                }
            }
        }
        
        // Now get the category for the canonical name
        if (ingredientCategoryMap.ContainsKey(canonicalName))
        {
            return ingredientCategoryMap[canonicalName];
        }
        
        // Fallback: Try to determine category from name
        return GetCategoryFromName(ingredientName);
    }
    
    private static string GetCategoryFromName(string ingredientName)
    {
        string lowerName = ingredientName.ToLower();
        
        // Check for preservatives
        if (lowerName.Contains("nitrite") || lowerName.Contains("benzoate") || 
            lowerName.Contains("preservative") || lowerName.Contains("aspartame"))
            return "PRESERVATIVE";
            
        // Check for sweeteners
        if (lowerName.Contains("sugar") || lowerName.Contains("syrup") || 
            lowerName.Contains("sorbitol") || lowerName.Contains("sucralose") ||
            lowerName.Contains("stevia") || lowerName.Contains("sweetener"))
            return "SWEETENER";
            
        // Check for nutrificants
        if (lowerName.Contains("calcium") || lowerName.Contains("iron") ||
            lowerName.Contains("vitamin") || lowerName.Contains("folic") ||
            lowerName.Contains("mineral") || lowerName.Contains("acid"))
            return "NUTRIFICANT";
            
        // Check for allergens
        if (lowerName.Contains("milk") || lowerName.Contains("egg") ||
            lowerName.Contains("peanut") || lowerName.Contains("shrimp") ||
            lowerName.Contains("allergen"))
            return "ALLERGEN";
            
        return "OTHER";
    }

    public static Color GetCategoryColor(string category)
    {
        switch (category)
        {
            case "PRESERVATIVE": return new Color(1f, 0.8f, 0.8f); // Light red
            case "SWEETENER": return new Color(1f, 1f, 0.8f);      // Light yellow
            case "NUTRIFICANT": return new Color(0.8f, 0.9f, 1f);  // Light blue
            case "ALLERGEN": return new Color(1f, 0.9f, 0.8f);     // Light orange
            default: return Color.white;
        }
    }
    
    // Helper method to get canonical name (matches Java logic)
    public static string GetCanonicalName(string ingredientName)
    {
        if (string.IsNullOrEmpty(ingredientName))
            return ingredientName;
            
        string lowerName = ingredientName.ToLower().Trim();
        
        if (ingredientSynonyms.ContainsKey(lowerName))
        {
            return ingredientSynonyms[lowerName];
        }
        
        // Check if it's already a canonical name
        foreach (var entry in ingredientCategoryMap)
        {
            if (entry.Key.ToLower() == lowerName)
            {
                return entry.Key;
            }
        }
        
        return ingredientName;
    }
    
    // NEW: Get all valid ingredients from Java list
    public static string[] GetJavaIngredientList()
    {
        return new string[]
        {
            "Calcium",
            "Milk",
            "Iron",
            "Vitamin C",
            "Vitamin A",
            "Egg",
            "Peanuts",
            "Shrimp",
            "Sugar",
            "Corn Syrup",
            "Sodium Nitrite",
            "Aspartame",
            "Sodium Benzoate",
            "Stevia Extract",
            "Sorbitol",
            "Sucralose",
            "Folic Acid"
        };
    }
    
    // NEW: Check if an ingredient is in the Java list
    public static bool IsInJavaList(string ingredientName)
    {
        if (string.IsNullOrEmpty(ingredientName))
            return false;
            
        string canonical = GetCanonicalName(ingredientName);
        foreach (string javaIngredient in GetJavaIngredientList())
        {
            if (canonical.Equals(javaIngredient, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}