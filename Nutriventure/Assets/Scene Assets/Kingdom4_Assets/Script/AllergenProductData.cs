using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Allergen_ProductData", menuName = "Allerthria/Allergen Product Data")]
public class AllergenProductData : ScriptableObject
{
    [Serializable]
    public class ProductInfo
    {
        [Header("Basic Info")]
        public string productID;              // e.g. "MILK", "BREAD", "BANANA"
        public string displayName;
        public FoodCategory foodCategory;

        [Header("Visuals")]
        public GameObject productPrefab;      // 3D model
        public Sprite productIcon;             // UI icon

        [Header("Allergen Information")]
        public AllergenType allergenType;      // Big Nine classification
        public bool containsAllergen;          // True if dangerous
        [TextArea(2, 4)]
        public string allergenWarning;         // e.g. "Contains milk protein"

        [Header("Educational Info")]
        [TextArea(3, 5)]
        public string description;
        [TextArea(2, 4)]
        public string labelTip;
        [TextArea(2, 4)]
        public string funFact;
    }

    // 🌾 Optional categorization
    public enum FoodCategory
    {
        Fruit,
        Vegetable,
        Grain,
        Dairy,
        Protein,
        Snack,
        Other
    }

    // 🧾 Big Nine Allergens
    public enum AllergenType
    {
        None,
        Milk,
        Eggs,
        Fish,
        Shellfish,
        TreeNuts,
        Peanuts,
        Wheat,
        Soy,
        Sesame
    }

    public ProductInfo[] allProducts;

    // ================= HELPER METHODS =================

    public ProductInfo GetProductInfo(string productID)
    {
        foreach (var product in allProducts)
        {
            if (product.productID == productID)
                return product;
        }
        return null;
    }

    public int GetAllergenCount()
{
    int count = 0;
    foreach (var product in allProducts)
    {
        if (product.containsAllergen)
            count++;
    }
    return count;
}


    public ProductInfo[] GetProductsWithAllergens()
    {
        System.Collections.Generic.List<ProductInfo> result = new System.Collections.Generic.List<ProductInfo>();
        foreach (var product in allProducts)
        {
            if (product.containsAllergen)
                result.Add(product);
        }
        return result.ToArray();
    }

    public ProductInfo[] GetProductsByAllergen(AllergenType type)
    {
        System.Collections.Generic.List<ProductInfo> result = new System.Collections.Generic.List<ProductInfo>();
        foreach (var product in allProducts)
        {
            if (product.allergenType == type)
                result.Add(product);
        }
        return result.ToArray();
    }

    public Sprite GetProductIcon(string productID)
    {
        return GetProductInfo(productID)?.productIcon;
    }

    public string GetAllergenWarning(string productID)
    {
        return GetProductInfo(productID)?.allergenWarning;
    }

    public bool ContainsAllergen(string productID)
    {
        return GetProductInfo(productID)?.containsAllergen ?? false;
    }
}
