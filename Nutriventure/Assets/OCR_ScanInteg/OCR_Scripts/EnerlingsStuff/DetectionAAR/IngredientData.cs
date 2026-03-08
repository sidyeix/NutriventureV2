using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class IngredientData
{
    public string ingredient; // Name of the scanned ingredient
    public string status;     // Success or error status
    
    // Product identification
    public string fingerprint;      // Unique product identifier
    public int total_detected;      // How many ingredients were detected
    public string mode;             // automatic or manual mode
    public string[] all_ingredients; // All detected ingredients array
    
    // TIMING METRICS - NEW
    public int ocr_time_ms;         // ML Kit OCR processing time
    public int decode_time_ms;       // Base64 decode time
    public int bitmap_time_ms;       // Bitmap conversion time
    public int match_time_ms;        // Ingredient matching time
    public int total_time_ms;        // Total processing time in plugin
    
    // Check if the data is valid and usable
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(ingredient) && !string.IsNullOrEmpty(status);
    }
    
    // Check if this is a duplicate product scan
    public bool IsDuplicateProduct()
    {
        if (string.IsNullOrEmpty(fingerprint))
            return false;
            
        // A product is a "duplicate" (already maxed out) if it CANNOT be scanned again
        // CanScanProduct returns false when product has been scanned 3 times
        return !ProductManager.CanScanProduct(fingerprint);
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
    
    // Helper method to get remaining scans for this product
    public int GetRemainingScans()
    {
        if (string.IsNullOrEmpty(fingerprint))
            return 3; // New product has 3 scans available
        return ProductManager.GetRemainingScans(fingerprint);
    }
    
    // Helper method to get cooldown time if product is maxed out
    public System.TimeSpan GetCooldownTime()
    {
        if (string.IsNullOrEmpty(fingerprint))
            return System.TimeSpan.Zero;
        return ProductManager.GetProductCooldown(fingerprint);
    }
    
    // Helper method to get formatted status for UI
    public string GetProductStatus()
    {
        if (string.IsNullOrEmpty(fingerprint))
            return "New product - 3 scans available";
        return ProductManager.GetProductStatus(fingerprint);
    }
    
    // Get formatted timing string for debugging
    public string GetTimingSummary()
    {
        return $"OCR: {ocr_time_ms}ms, Decode: {decode_time_ms}ms, Bitmap: {bitmap_time_ms}ms, Match: {match_time_ms}ms, Total: {total_time_ms}ms";
    }
}