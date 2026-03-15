using UnityEngine;

// Attach this to the collider GameObject if the trigger is not on the AllergenGameManager GameObject.
public class AllergenScrollTriggerRelay : MonoBehaviour
{
  public AllergenGameManager manager;

  private void Reset()
  {
    Collider col = GetComponent<Collider>();
    if (col != null && !col.isTrigger)
    {
      col.isTrigger = true;
    }
  }

  private void OnTriggerEnter(Collider other)
  {
    if (manager != null)
    {
      manager.HandleScrollTriggerEnter(other);
    }
  }

  private void OnTriggerExit(Collider other)
  {
    if (manager != null)
    {
      manager.HandleScrollTriggerExit(other);
    }
  }
}
