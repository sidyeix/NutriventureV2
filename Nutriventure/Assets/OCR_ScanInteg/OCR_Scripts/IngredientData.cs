using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class IngredientData
{
    public string ingredient; // Name of the scanned ingredient
    public string status;     // Success or error status
    
    // MUST MATCH JAVA JSON FIELD NAMES EXACTLY
    public string fingerprint;      // Unique product identifier
    public int total_detected;      // How many ingredients were detected
    public string mode;             // automatic or manual mode
    public string[] all_ingredients; // All detected ingredients array
    
    // Check if the data is valid and usable
    public bool IsValid()
    {
        // Check if it's an error message
        if (!string.IsNullOrEmpty(status) && status.StartsWith("ERROR"))
        {
            Debug.LogError($"OCR Error: {status}");
            return false;
        }
        
        // Check for required fields
        if (string.IsNullOrEmpty(ingredient) || string.IsNullOrEmpty(status))
        {
            Debug.LogError($"Invalid ingredient data: ingredient='{ingredient}', status='{status}'");
            return false;
        }
        
        // Check if ingredient is in Java list
        if (!IngredientCategory.IsInJavaList(ingredient))
        {
            Debug.LogWarning($"Ingredient '{ingredient}' not found in Java ingredient list");
            // Still valid, but might not have a model
        }
        
        return true;
    }
    
    // Check if this is a duplicate product scan
    public bool IsDuplicateProduct()
    {
        return !string.IsNullOrEmpty(fingerprint) && 
               ProductManager.IsProductAlreadyScanned(fingerprint);
    }
    
    // Get all detected ingredients as list
    public List<string> GetAllIngredients()
    {
        if (all_ingredients == null) 
            return new List<string> { ingredient };
            
        return new List<string>(all_ingredients);
    }
    
    // Get total count of detected ingredients
    public int GetTotalDetected()
    {
        return total_detected > 0 ? total_detected : 1;
    }
    
    // Convenience property for cleaner code access
    public int totalDetected { get { return GetTotalDetected(); } }
    
    // NEW: Check if this is an error response
    public bool IsError()
    {
        return !string.IsNullOrEmpty(status) && status.StartsWith("ERROR");
    }
    
    // NEW: Get error message if any
    public string GetErrorMessage()
    {
        if (IsError())
        {
            return status;
        }
        return null;
    }
}