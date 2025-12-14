using UnityEngine;

[CreateAssetMenu(fileName = "SpawnableItemData", menuName = "Game/Spawnable Item Data")]
public class SpawnableItemData : ScriptableObject
{
    public enum ItemType
    {
        Coin,
        Peanut,
        Milk,
        Egg,
        Fish,
        Shellfish,
        TreeNut,
        Wheat,
        Soybean,
        Sesame,
        Shield,
        Heart
    }
    
    public enum ItemCategory
    {
        SafePassable,    // Coins
        NotSafe,         // Allergens
        SafePowerup      // Shield, Heart
    }
    
    [Header("Basic Settings")]
    public ItemType itemType;
    public GameObject prefab;
    public ItemCategory category = ItemCategory.NotSafe;
    
    [Header("Spawn Settings")]
    [Range(0f, 1f)] public float spawnChance = 0.5f;
    public int minPerRow = 0;
    public int maxPerRow = 3;
    public bool canSpawnOnRoad = true;
    
    [Header("Visual Settings")]
    public Material material;
    public Color gizmoColor = Color.white;
    
    [Header("Audio")]
    public AudioClip collectSound;
    
    [Header("Effects")]
    public ParticleSystem collectParticles;
    
    public bool IsAllergen()
    {
        return category == ItemCategory.NotSafe;
    }
    
    public bool IsSafePassable()
    {
        return category == ItemCategory.SafePassable;
    }
    
    public bool IsPowerup()
    {
        return category == ItemCategory.SafePowerup;
    }
}