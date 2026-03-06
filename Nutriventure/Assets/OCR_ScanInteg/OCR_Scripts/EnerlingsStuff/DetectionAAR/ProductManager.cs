using UnityEngine;
using System;
using System.Collections.Generic;
public static class ProductManager
{
    // Internal data structure - completely self-contained
    private class ProductScanData
    {
        public string fingerprint;
        public DateTime firstScanTime;
        public int scanCount;
        public string lastSelectedIngredient;
        
        public ProductScanData(string fingerprint)
        {
            this.fingerprint = fingerprint;
            this.firstScanTime = DateTime.Now;
            this.scanCount = 1;
            this.lastSelectedIngredient = "";
        }
        
        public ProductScanData(string fingerprint, DateTime firstScan, int count, string lastIngredient)
        {
            this.fingerprint = fingerprint;
            this.firstScanTime = firstScan;
            this.scanCount = count;
            this.lastSelectedIngredient = lastIngredient;
        }
        
        public bool CanScanAgain()
        {
            return scanCount < 3;
        }
        
        public void RecordScan()
        {
            scanCount++;
        }
        
        public TimeSpan GetRemainingCooldown()
        {
            if (scanCount >= 3)
            {
                DateTime cooldownEnd = firstScanTime.AddHours(24);
                TimeSpan remaining = cooldownEnd - DateTime.Now;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
            return TimeSpan.Zero;
        }
        
        public bool IsCooldownActive()
        {
            return scanCount >= 3 && GetRemainingCooldown().TotalSeconds > 0;
        }
    }
    
    // Serializable wrapper for JSON persistence
    [Serializable]
    private class SavedProduct
    {
        public string fingerprint;
        public string firstScanTime; // ISO 8601
        public int scanCount;
        public string lastSelectedIngredient;
    }
    
    [Serializable]
    private class SavedProductList
    {
        public List<SavedProduct> products = new List<SavedProduct>();
    }
    
    private const string SAVE_KEY = "ProductManager_ScanData";
    
    // Storage for all scanned products
    private static Dictionary<string, ProductScanData> scannedProducts = new Dictionary<string, ProductScanData>();
    private static DateTime lastProductResetTime = DateTime.Now;
    private static bool isLoaded = false;
    
    public static bool CanScanProduct(string fingerprint)
    {
        EnsureLoaded();
        CleanOldProducts();
        
        if (string.IsNullOrEmpty(fingerprint))
            return false;
            
        if (scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint].CanScanAgain();
        }
        return true; // New product can always be scanned
    }
    
    public static void RecordProductScan(string fingerprint, string selectedIngredient = "")
    {
        if (string.IsNullOrEmpty(fingerprint))
            return;
            
        EnsureLoaded();
        CleanOldProducts();
        
        if (scannedProducts.ContainsKey(fingerprint))
        {
            ProductScanData data = scannedProducts[fingerprint];
            data.RecordScan();
            if (!string.IsNullOrEmpty(selectedIngredient))
                data.lastSelectedIngredient = selectedIngredient;
        }
        else
        {
            scannedProducts[fingerprint] = new ProductScanData(fingerprint);
            if (!string.IsNullOrEmpty(selectedIngredient))
                scannedProducts[fingerprint].lastSelectedIngredient = selectedIngredient;
        }
        
        Debug.Log($"[ProductManager] Product scanned: {fingerprint}. Scan {scannedProducts[fingerprint].scanCount}/3");
        SaveData();
    }
    
    public static int GetProductScanCount(string fingerprint)
    {
        EnsureLoaded();
        if (!string.IsNullOrEmpty(fingerprint) && scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint].scanCount;
        }
        return 0;
    }

    public static int GetRemainingScans(string fingerprint)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(fingerprint))
            return 0;
            
        if (!scannedProducts.ContainsKey(fingerprint))
            return 3; // New product has all 3 scans available
            
        return Math.Max(0, 3 - scannedProducts[fingerprint].scanCount);
    }

    public static TimeSpan GetProductCooldown(string fingerprint)
    {
        EnsureLoaded();
        if (!string.IsNullOrEmpty(fingerprint) && scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint].GetRemainingCooldown();
        }
        return TimeSpan.Zero;
    }

    public static bool IsProductOnCooldown(string fingerprint)
    {
        EnsureLoaded();
        if (!string.IsNullOrEmpty(fingerprint) && scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint].IsCooldownActive();
        }
        return false;
    }

    public static string GetProductStatus(string fingerprint)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(fingerprint))
            return "Invalid product";
            
        if (!scannedProducts.ContainsKey(fingerprint))
            return "Ready to scan (0/3)";
            
        ProductScanData data = scannedProducts[fingerprint];
        
        if (data.IsCooldownActive())
        {
            TimeSpan remaining = data.GetRemainingCooldown();
            return $"Max scans reached. Reset in: {remaining.Hours}h {remaining.Minutes}m";
        }
        else
        {
            return $"Scanned {data.scanCount}/3 times. {3 - data.scanCount} scans remaining";
        }
    }

    public static int GetTotalScannedProducts()
    {
        EnsureLoaded();
        CleanOldProducts();
        return scannedProducts.Count;
    }

    public static bool AnyProductsOnCooldown()
    {
        EnsureLoaded();
        CleanOldProducts();
        
        foreach (var product in scannedProducts.Values)
        {
            if (product.IsCooldownActive())
            {
                return true;
            }
        }
        return false;
    }
    
    public static Dictionary<string, TimeSpan> GetAllProductsOnCooldown()
    {
        EnsureLoaded();
        CleanOldProducts();
        
        Dictionary<string, TimeSpan> cooldownProducts = new Dictionary<string, TimeSpan>();
        
        foreach (var pair in scannedProducts)
        {
            if (pair.Value.IsCooldownActive())
            {
                cooldownProducts[pair.Key] = pair.Value.GetRemainingCooldown();
            }
        }
        
        return cooldownProducts;
    }
    
    public static void CleanupExpiredProducts()
    {
        EnsureLoaded();
        CleanOldProducts();
    }
    
    public static void ResetAllData()
    {
        scannedProducts.Clear();
        lastProductResetTime = DateTime.Now;
        SaveData();
        Debug.Log("[ProductManager] All product data reset");
    }
    
    // Auto-clean products older than 24 hours
    private static void CleanOldProducts()
    {
        bool removedAny = false;
        List<string> productsToRemove = new List<string>();
        
        foreach (var pair in scannedProducts)
        {
            if ((DateTime.Now - pair.Value.firstScanTime).TotalHours >= 24)
            {
                productsToRemove.Add(pair.Key);
                removedAny = true;
            }
        }
        
        foreach (string fingerprint in productsToRemove)
        {
            scannedProducts.Remove(fingerprint);
            Debug.Log($"[ProductManager] Removed expired product: {fingerprint}");
        }
        
        if (removedAny)
        {
            lastProductResetTime = DateTime.Now;
            SaveData();
        }
    }
    
    // ========================================================================
    //  PERSISTENCE (PlayerPrefs JSON)
    // ========================================================================
    
    private static void EnsureLoaded()
    {
        if (!isLoaded)
        {
            LoadData();
            isLoaded = true;
        }
    }
    
    private static void SaveData()
    {
        SavedProductList list = new SavedProductList();
        foreach (var pair in scannedProducts)
        {
            list.products.Add(new SavedProduct
            {
                fingerprint = pair.Value.fingerprint,
                firstScanTime = pair.Value.firstScanTime.ToString("o"),
                scanCount = pair.Value.scanCount,
                lastSelectedIngredient = pair.Value.lastSelectedIngredient
            });
        }
        string json = JsonUtility.ToJson(list);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }
    
    private static void LoadData()
    {
        scannedProducts.Clear();
        
        if (!PlayerPrefs.HasKey(SAVE_KEY))
            return;
        
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json))
            return;
        
        try
        {
            SavedProductList list = JsonUtility.FromJson<SavedProductList>(json);
            if (list == null || list.products == null)
                return;
            
            foreach (var saved in list.products)
            {
                DateTime firstScan;
                if (!DateTime.TryParse(saved.firstScanTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out firstScan))
                    firstScan = DateTime.Now;
                
                scannedProducts[saved.fingerprint] = new ProductScanData(
                    saved.fingerprint, firstScan, saved.scanCount, saved.lastSelectedIngredient ?? ""
                );
            }
            Debug.Log($"[ProductManager] Loaded {scannedProducts.Count} products from save");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProductManager] Failed to load save data: {e.Message}");
            scannedProducts.Clear();
        }
    }
}