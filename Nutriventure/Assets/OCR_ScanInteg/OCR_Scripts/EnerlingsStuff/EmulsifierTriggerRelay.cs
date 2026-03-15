using UnityEngine;

// Attach this to the trigger collider object if the EmulsifierManager is on a different GameObject.
public class EmulsifierTriggerRelay : MonoBehaviour
{
    public EmulsifierManager manager;

    private void Reset()
    {
        // Ensure the collider is a trigger
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
            manager.HandleTriggerEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (manager != null)
        {
            manager.HandleTriggerExit(other);
        }
    }
}
