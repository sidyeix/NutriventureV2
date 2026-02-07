using UnityEngine;
using System;

public static class JsonParser
{
    // Main method to parse plugin response - handles both success and error cases
    public static IngredientData ParseIngredientResponse(string jsonResponse)
    {
        // First check if it's an error message (starts with "ERROR")
        if (jsonResponse.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("Plugin error: " + jsonResponse);
            return CreateErrorIngredient(jsonResponse);
        }
        
        try
        {
            // Parse the JSON using JsonUtility
            SimpleJsonData jsonData = JsonUtility.FromJson<SimpleJsonData>(jsonResponse);
            
            // Create IngredientData from parsed JSON
            IngredientData data = new IngredientData
            {
                ingredient = jsonData.ingredient,
                status = jsonData.status,
                fingerprint = jsonData.fingerprint,
                total_detected = jsonData.total_detected,
                mode = jsonData.mode
            };
            
            // Validate the parsed data
            if (data != null && data.IsValid())
            {
                Debug.Log($"Successfully parsed ingredient: {data.ingredient} " +
                         $"(Total detected: {data.total_detected}, Fingerprint: {data.fingerprint}, " +
                         $"Mode: {data.mode})");
                return data;
            }
            else
            {
                Debug.LogWarning("Invalid JSON data received: " + jsonResponse);
                return CreateErrorIngredient("Invalid ingredient data format");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("JSON parse error: " + e.Message + "\nRaw response: " + jsonResponse);
            return CreateErrorIngredient("Parse error: " + e.Message);
        }
    }
    
    // Helper method to create error ingredient data
    private static IngredientData CreateErrorIngredient(string errorMessage)
    {
        return new IngredientData
        {
            ingredient = "Error",
            status = errorMessage,
            fingerprint = "",
            total_detected = 0,
            mode = "error"
        };
    }
    
    // Simple class for JSON parsing
    [System.Serializable]
    private class SimpleJsonData
    {
        public string ingredient;
        public string status;
        public string fingerprint;
        public int total_detected;
        public string mode;
        public string all_ingredients; // This might be an array string, we'll handle separately if needed
    }
}