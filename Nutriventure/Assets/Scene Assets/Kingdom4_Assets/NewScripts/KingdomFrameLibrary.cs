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

    // =========================
    // RARITY ICON SYSTEM
    // =========================
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

    // =========================
    // ORGAN SPRITE SYSTEM
    // =========================
    [System.Serializable]
    public struct OrganSprite
    {
        public string organName;      // e.g., "Heart", "Liver", "Kidney", "Pancreas", "Brain"
        public Sprite organSprite;
    }

    [Header("Organ Sprites")]
    public OrganSprite[] organSprites;

    public Sprite GetOrganSprite(string organName)
    {
        if (string.IsNullOrEmpty(organName)) return null;

        foreach (var organ in organSprites)
        {
            // Case-insensitive comparison
            if (organ.organName.Equals(organName, System.StringComparison.OrdinalIgnoreCase))
                return organ.organSprite;
        }

        Debug.LogWarning($"No sprite found for organ: {organName}");
        return null;
    }

    // Optional: Get organ sprite by index
    public Sprite GetOrganSprite(int index)
    {
        if (index >= 0 && index < organSprites.Length)
            return organSprites[index].organSprite;
        
        return null;
    }

    // Optional: Get all organ names
    public string[] GetAllOrganNames()
    {
        string[] names = new string[organSprites.Length];
        for (int i = 0; i < organSprites.Length; i++)
        {
            names[i] = organSprites[i].organName;
        }
        return names;
    }
}