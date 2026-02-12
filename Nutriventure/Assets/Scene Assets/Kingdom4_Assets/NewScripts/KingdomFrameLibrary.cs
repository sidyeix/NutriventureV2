using UnityEngine;

public class KingdomFrameLibrary : MonoBehaviour
{
    // =========================
    // KINGDOM FRAME SYSTEM
    // =========================
    [System.Serializable]
    public struct KingdomFrame
    {
        public IngredientDatabase.KingdomOrigin kingdom;
        public Sprite frameSprite;
    }

    public KingdomFrame[] frames;

    public Sprite GetFrame(IngredientDatabase.KingdomOrigin kingdom)
    {
        foreach (var f in frames)
        {
            if (f.kingdom == kingdom)
                return f.frameSprite;
        }

        return null;
    }

    // =========================
    // ENERLING ICON SYSTEM
    // =========================
    [System.Serializable]
    public struct EnerlingIcon
    {
        public string ingredientName;
        public Sprite iconSprite;
    }

    public EnerlingIcon[] enerlingIcons;

    public Sprite GetEnerlingIcon(string ingredientName)
    {
        foreach (var icon in enerlingIcons)
        {
            if (icon.ingredientName == ingredientName)
                return icon.iconSprite;
        }

        return null;
    }
    [System.Serializable]
public struct RarityIcon
{
    public IngredientDatabase.Rarity rarity;
    public Sprite iconSprite;
}

public RarityIcon[] rarityIcons;

public Sprite GetRarityIcon(IngredientDatabase.Rarity rarity)
{
    foreach (var icon in rarityIcons)
    {
        if (icon.rarity == rarity)
            return icon.iconSprite;
    }

    return null; // fallback handled in UI
}
}
