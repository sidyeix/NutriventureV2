using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoreDatabase", menuName = "Game/Store Database")]
public class StoreItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class StoreItem
    {
        public string itemName;
        public Sprite itemIcon;
        public int coinCost;
        public string description;
        public int characterID = -1; // For characters only
    }

    public List<StoreItem> allItems = new List<StoreItem>();

    public StoreItem GetItemByID(int id)
    {
        if (id >= 0 && id < allItems.Count)
            return allItems[id];
        return null;
    }

    public StoreItem GetCharacterItemByCharacterID(int characterID)
    {
        return allItems.Find(item => item.characterID == characterID);
    }
}