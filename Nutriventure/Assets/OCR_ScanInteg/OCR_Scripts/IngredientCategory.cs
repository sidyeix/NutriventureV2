using UnityEngine;
using System.Collections.Generic;

public static class IngredientCategory
{
    // Define categories for each canonical ingredient
    private static Dictionary<string, string> ingredientCategoryMap = new Dictionary<string, string>
    {
        // PRESERVATIVES
        {"Sodium nitrite", "PRESERVATIVE"},
        {"Sodium benzoate", "PRESERVATIVE"},
        
        // NUTRIFICANTS
        {"Calcium", "NUTRIFICANT"},
        {"Vitamin C", "NUTRIFICANT"},
        
        // ALLERGENS
        {"Shrimp", "ALLERGEN"},
        {"Peanuts", "ALLERGEN"},
        {"Eggs", "ALLERGEN"},
        {"Corn", "ALLERGEN"},
        
        // SWEETENERS
        {"Sugar", "SWEETENER"},
        {"Sorbitol", "SWEETENER"}
    };

    // Map synonyms to their canonical names (same as Java)
    private static Dictionary<string, string> ingredientSynonyms = new Dictionary<string, string>
    {
        // Calcium synonyms
        {"calcium carbonate", "Calcium"},
        {"calcium phosphate", "Calcium"},
        {"calcium lactate", "Calcium"},

        // Vitamin C synonyms
        {"ascorbic acid", "Vitamin C"},
        {"sodium ascorbate", "Vitamin C"},

        // Shrimp / shellfish
        {"crustacean shellfish", "Shrimp"},

        // Peanuts / tree nuts
        {"almonds", "Peanuts"},
        {"cashews", "Peanuts"},
        {"tree nuts", "Peanuts"},

        // Eggs
        {"albumin", "Eggs"},

        // Corn derivatives
        {"corn syrup", "Corn"},
        {"corn starch", "Corn"},
        {"corn oil", "Corn"},

        // Sugar variants
        {"cane sugar", "Sugar"},
        {"brown sugar", "Sugar"},
        {"glucose", "Sugar"},
        {"fructose", "Sugar"}
    };

    public static string GetCategory(string ingredientName)
    {
        if (string.IsNullOrEmpty(ingredientName))
            return "OTHER";

        string lowerName = ingredientName.ToLower().Trim();
        
        // First, check if it's a synonym and get canonical name
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
            lowerName.Contains("sorbate") || lowerName.Contains("preservative"))
            return "PRESERVATIVE";
            
        // Check for sweeteners
        if (lowerName.Contains("sugar") || lowerName.Contains("sorbitol") || 
            lowerName.Contains("fructose") || lowerName.Contains("glucose") ||
            lowerName.Contains("syrup") || lowerName.Contains("sweetener"))
            return "SWEETENER";
            
        // Check for nutrificants
        if (lowerName.Contains("calcium") || lowerName.Contains("vitamin") ||
            lowerName.Contains("ascorbic") || lowerName.Contains("mineral"))
            return "NUTRIFICANT";
            
        // Check for allergens
        if (lowerName.Contains("shrimp") || lowerName.Contains("peanut") ||
            lowerName.Contains("almond") || lowerName.Contains("cashew") ||
            lowerName.Contains("egg") || lowerName.Contains("corn") ||
            lowerName.Contains("shellfish") || lowerName.Contains("nut"))
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
    
    // Helper method to get canonical name (optional, for debugging)
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
}