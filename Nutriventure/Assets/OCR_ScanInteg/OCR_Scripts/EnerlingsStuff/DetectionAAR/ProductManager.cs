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
    
    // Storage for all scanned products
    private static Dictionary<string, ProductScanData> scannedProducts = new Dictionary<string, ProductScanData>();
    private static DateTime lastProductResetTime = DateTime.Now;
    
    public static bool CanScanProduct(string fingerprint)
    {
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
    }
    
    public static int GetProductScanCount(string fingerprint)
    {
        if (!string.IsNullOrEmpty(fingerprint) && scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint].scanCount;
        }
        return 0;
    }

    public static int GetRemainingScans(string fingerprint)
    {
        if (string.IsNullOrEmpty(fingerprint))
            return 0;
            
        if (!scannedProducts.ContainsKey(fingerprint))
            return 3; // New product has all 3 scans available
            
        return Math.Max(0, 3 - scannedProducts[fingerprint].scanCount);
    }

    public static TimeSpan GetProductCooldown(string fingerprint)
    {
        if (!string.IsNullOrEmpty(fingerprint) && scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint].GetRemainingCooldown();
        }
        return TimeSpan.Zero;
    }

    public static bool IsProductOnCooldown(string fingerprint)
    {
        if (!string.IsNullOrEmpty(fingerprint) && scannedProducts.ContainsKey(fingerprint))
        {
            return scannedProducts[fingerprint].IsCooldownActive();
        }
        return false;
    }

    public static string GetProductStatus(string fingerprint)
    {
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
        CleanOldProducts();
        return scannedProducts.Count;
    }

    public static bool AnyProductsOnCooldown()
    {
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
        CleanOldProducts();
    }
    
    public static void ResetAllData()
    {
        scannedProducts.Clear();
        lastProductResetTime = DateTime.Now;
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
        }
    }
}