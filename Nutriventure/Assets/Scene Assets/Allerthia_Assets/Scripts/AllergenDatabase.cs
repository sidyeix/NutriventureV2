using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject database containing the Big 9 allergens and their information.
/// Create via: Assets > Create > Allerthia > Allergen Database
/// </summary>
[CreateAssetMenu(fileName = "AllergenDatabase", menuName = "Allerthia/Allergen Database")]
public class AllergenDatabase : ScriptableObject
{
  public List<AllergenData> allergens = new List<AllergenData>();

  [System.Serializable]
  public class AllergenData
  {
    [Header("Identity")]
    public string allergenID;
    public string allergenName;

    [Header("Display Info")]
    public Sprite allergenImage;
    [TextArea(2, 4)]
    public string description;
    [TextArea(2, 4)]
    public string fact;
    [TextArea(2, 4)]
    public string foodExamples;

    [Header("In-Game Prefab")]
    [Tooltip("The 3D prefab to spawn in the world for this allergen")]
    public GameObject allergenPrefab;
  }

  public AllergenData GetAllergenByID(string id)
  {
    return allergens.Find(a => a.allergenID == id);
  }

  public AllergenData GetAllergenByName(string name)
  {
    return allergens.Find(a => a.allergenName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
  }

  public int TotalAllergenCount => allergens.Count;
}
