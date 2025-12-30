using UnityEngine;

public static class IngredientCategory
{
    public static string GetCategory(string ingredient)
    {
        string lowerIngredient = ingredient.ToLower();

        // PRESERVATIVES - Match from Java list
        if (lowerIngredient.Contains("sodium nitrite") || 
            lowerIngredient.Contains("sodium benzoate"))
            return "PRESERVATIVE";

        // NUTRIFICANTS - Match from Java list
        if (lowerIngredient.Contains("calcium") || 
            lowerIngredient.Contains("vitamin c") ||
            lowerIngredient.Contains("ascorbic acid"))
            return "NUTRIFICANT";

        // ALLERGENS - Match from Java list
        if (lowerIngredient.Contains("shrimp") || 
            lowerIngredient.Contains("peanuts") ||
            lowerIngredient.Contains("almonds") ||
            lowerIngredient.Contains("cashews") ||
            lowerIngredient.Contains("eggs") ||
            lowerIngredient.Contains("corn"))
            return "ALLERGEN";

        // SWEETENERS - Match from Java list
        if (lowerIngredient.Contains("sugar") || 
            lowerIngredient.Contains("sorbitol") ||
            lowerIngredient.Contains("fructose") ||
            lowerIngredient.Contains("glucose"))
            return "SWEETENER";

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
}