using UnityEngine;

/// <summary>
/// Attach this to each allergen prefab. It stores the allergen ID and notifies
/// AllergenGameManager when the player enters/exits the trigger zone.
/// The prefab needs a Collider set to IsTrigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AllergenPickup : MonoBehaviour
{
  [Header("Allergen Identity")]
  [Tooltip("Set automatically by AllergenGameManager.SpawnAllergens, or manually if pre-placed")]
  [SerializeField] private string allergenID;

  public string AllergenID => allergenID;

  /// <summary>Called by AllergenGameManager after spawning to set the ID.</summary>
  public void Initialize(string id)
  {
    allergenID = id;
  }

  void OnTriggerEnter(Collider other)
  {
    if (!other.CompareTag("Player")) return;

    if (AllergenGameManager.Instance != null)
      AllergenGameManager.Instance.OnPlayerNearAllergen(this);
  }

  void OnTriggerExit(Collider other)
  {
    if (!other.CompareTag("Player")) return;

    if (AllergenGameManager.Instance != null)
      AllergenGameManager.Instance.OnPlayerLeftAllergen(this);
  }
}
