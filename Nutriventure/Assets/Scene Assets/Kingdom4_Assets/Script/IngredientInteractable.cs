using UnityEngine;

[RequireComponent(typeof(Collider))] // Makes sure there's a collider for tapping
public class IngredientInteractable : Interactable
{
    [Header("Ingredient Settings")]
    public string ingredientId; // MUST match productID in ScriptableObject
    private bool isCollected = false;

    // Optional hook for specialized ingredient behaviors (e.g., peanut timeline)
    protected virtual void OnCollected() { }
    public void NotifyCollectedToManager()
    {
        if (isCollected)
            return;

        isCollected = true;
        OnCollected();

        if (AllergenGameManager.Instance != null)
        {
            AllergenGameManager.Instance.RegisterCollectedIngredient(ingredientId, gameObject);
        }
    }

    public override void Pickup()
    {
        NotifyCollectedToManager();
    }

    // Visualize the trigger area in the editor
    private void OnDrawGizmosSelected()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = Color.green;

            if (collider is BoxCollider boxCollider)
            {
                Gizmos.DrawWireCube(
                    transform.position + boxCollider.center,
                    boxCollider.size
                );
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Gizmos.DrawWireSphere(
                    transform.position + sphereCollider.center,
                    sphereCollider.radius
                );
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                // Draw capsule outline
                Vector3 top = transform.position + capsuleCollider.center + Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius);
                Vector3 bottom = transform.position + capsuleCollider.center - Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius);

                Gizmos.DrawWireSphere(top, capsuleCollider.radius);
                Gizmos.DrawWireSphere(bottom, capsuleCollider.radius);

                // Draw connecting lines
                Gizmos.DrawLine(top + Vector3.right * capsuleCollider.radius, bottom + Vector3.right * capsuleCollider.radius);
                Gizmos.DrawLine(top - Vector3.right * capsuleCollider.radius, bottom - Vector3.right * capsuleCollider.radius);
                Gizmos.DrawLine(top + Vector3.forward * capsuleCollider.radius, bottom + Vector3.forward * capsuleCollider.radius);
                Gizmos.DrawLine(top - Vector3.forward * capsuleCollider.radius, bottom - Vector3.forward * capsuleCollider.radius);
            }
        }
    }
}