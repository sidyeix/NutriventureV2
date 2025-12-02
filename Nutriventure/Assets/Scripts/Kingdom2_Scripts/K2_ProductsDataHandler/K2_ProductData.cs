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
        
        [Header("Information")]
        [TextArea(3, 5)] public string description;
        [TextArea(2, 4)] public string labelTip;
        [TextArea(2, 4)] public string funFact;
        
        // Removed: productIcon, productMaterial, nutrition info
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
            if (product.productID == productID)
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
}