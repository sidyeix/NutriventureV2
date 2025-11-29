using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Interactable Settings")]
    public string interactableName = "Object";
    public bool destroyOnPickup = true;
    
    [Header("Events")]
    public UnityEvent OnPickup;
    public UnityEvent OnInteract;
    
    public virtual void Pickup()
    {
        OnPickup?.Invoke();
        OnInteract?.Invoke();
        
        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}