using UnityEngine;
using System;

[CreateAssetMenu(fileName = "K2_ProductData", menuName = "Sugaría/K2_Product Data")]
public class ProductData : ScriptableObject
{
    [Serializable]
    public class ProductInfo
    {
        [Header("Basic Info")]
        public string productID; // Unique identifier (e.g., "BANANA", "COOKIES")
        public string displayName;
        public ProductType productType;

        [Header("Visuals")]
        public GameObject productPrefab; // The 3D model prefab
        public Sprite productIcon; // New: 2D sprite icon for UI

        [Header("Nutrition Information")]
        public Sprite nutritionLabelImage; // 2D UI Sprite for nutrition facts label
        [Range(0, 100)] public float sugarContentAmount; // Sugar content in grams

        [Header("Information")]
        [TextArea(3, 5)] public string description;
        [TextArea(2, 4)] public string labelTip;
        [TextArea(2, 4)] public string funFact;

        // Removed: productMaterial
    }

    public enum ProductType
    {
        NaturalSugar,
        AddedSugar
    }

    public ProductInfo[] allProducts;

    // Helper methods
    public ProductInfo GetProductInfo(string productID)
    {
        foreach (var product in allProducts)
        {
            if (string.Equals(product.productID, productID, System.StringComparison.OrdinalIgnoreCase))
                return product;
        }
        return null;
    }

    public ProductInfo[] GetProductsByType(ProductType type)
    {
        System.Collections.Generic.List<ProductInfo> result = new System.Collections.Generic.List<ProductInfo>();
        foreach (var product in allProducts)
        {
            if (product.productType == type)
                result.Add(product);
        }
        return result.ToArray();
    }

    public int GetTotalCount()
    {
        return allProducts.Length;
    }

    // New helper methods for accessing nutrition data
    public Sprite GetNutritionLabelImage(string productID)
    {
        ProductInfo product = GetProductInfo(productID);
        return product?.nutritionLabelImage;
    }

    public float GetSugarContentAmount(string productID)
    {
        ProductInfo product = GetProductInfo(productID);
        return product?.sugarContentAmount ?? 0f;
    }

    public string GetFormattedSugarContent(string productID)
    {
        ProductInfo product = GetProductInfo(productID);
        if (product != null)
        {
            return $"{product.sugarContentAmount}g";
        }
        return "0g";
    }

    // New helper method for accessing product icon
    public Sprite GetProductIcon(string productID)
    {
        ProductInfo product = GetProductInfo(productID);
        return product?.productIcon;
    }
}