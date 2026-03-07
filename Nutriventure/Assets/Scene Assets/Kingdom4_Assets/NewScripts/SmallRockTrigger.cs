using UnityEngine;
using System.Collections;

public class SmallRockTrigger : MonoBehaviour
{
    [Header("Rock Settings")]
    [SerializeField] private int columnID; // 0 = left, 1 = middle, 2 = right
    [SerializeField] private int rockID; // Which big rock this belongs to
    
    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem touchEffect;
    [SerializeField] private AudioClip touchSound;
    
    private BigRockInteraction parentRock;
    private AudioSource audioSource;
    private bool isActivated = false;
    private bool isDangerousForCurrentNPC = false;
    private string allergenOnThisRock;
    
    // Separate colliders
    private Collider platformCollider; // For physical landing
    private Collider triggerCollider;   // For item detection
    
    private void Awake()
    {
        // Setup colliders in Awake to ensure they're ready
        SetupColliders();
    }
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        parentRock = GetComponentInParent<BigRockInteraction>();
    }
    
    private void SetupColliders()
{
    // Remove any existing colliders to start fresh
    Collider[] existingColliders = GetComponents<Collider>();
    foreach (Collider col in existingColliders)
    {
        DestroyImmediate(col);
    }
    
    // Add PLATFORM collider (non-trigger) - this is what the player stands on
    BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
    boxCol.isTrigger = false;
    boxCol.size = new Vector3(1f, 0.2f, 1f); // Flat platform shape
    platformCollider = boxCol;
    
    // Add TRIGGER collider (for item detection) - this detects player for allergen collection
    SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
    sphereCol.isTrigger = true;
    sphereCol.radius = 1.5f;
    sphereCol.center = new Vector3(0, 1f, 0); // Position at item height
    triggerCollider = sphereCol;
    
    Debug.Log($"Setup colliders for {gameObject.name} - Platform+Trigger. " +
              $"Platform enabled: {platformCollider.enabled}, Trigger enabled: {triggerCollider.enabled}");
}
    
    public void SetAsDangerous(string allergen)
    {
        isDangerousForCurrentNPC = true;
        allergenOnThisRock = allergen;
        Debug.Log($"<color=red>⚠️ Rock {gameObject.name} set as DANGEROUS with {allergen}</color>");
    }
    
    public void SetAllergen(string allergen)
    {
        allergenOnThisRock = allergen;
        isDangerousForCurrentNPC = false;
        Debug.Log($"Rock {gameObject.name} set with SAFE allergen: {allergen}");
    }
    
    public bool IsDangerous()
    {
        return isDangerousForCurrentNPC;
    }
    
    public string GetAllergenName()
    {
        return allergenOnThisRock;
    }
    
    // This is for the ITEM trigger (allergen collection)
    // This is for the ITEM trigger (allergen collection)
private void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player") || isActivated) return;
    
    Debug.Log($"<color=yellow>Trigger detected on {gameObject.name} with {allergenOnThisRock}</color>");
    
    isActivated = true;
    
    // Play effects
    if (touchEffect != null)
        touchEffect.Play();
    if (touchSound != null)
        audioSource.PlayOneShot(touchSound);
    
    // Log what happened
    if (isDangerousForCurrentNPC)
    {
        Debug.Log($"<color=red>❌ Player touched DANGEROUS {allergenOnThisRock}!</color>");
    }
    else
    {
        Debug.Log($"<color=green>✅ Player touched SAFE {allergenOnThisRock}</color>");
    }
    
    // Notify parent rock
    if (parentRock != null)
    {
        parentRock.OnPlayerEnterRockColumn(columnID, gameObject);
    }
    
    // IMPORTANT: ONLY disable the trigger collider, NEVER the platform collider!
    if (triggerCollider != null)
    {
        triggerCollider.enabled = false;
        Debug.Log($"Disabled trigger collider, platform collider still active");
    }
    
    // Hide the floating item (the allergen model), but keep the rock solid!
    StartCoroutine(DisableItem());
}

private IEnumerator DisableItem()
{
    yield return new WaitForSeconds(0.3f);
    
    // Find and disable any child items (the floating allergen model)
    // This should NOT affect the rock's platform collider
    foreach (Transform child in transform)
    {
        if (child.GetComponent<ItemCollectible>() != null)
        {
            child.gameObject.SetActive(false);
            Debug.Log($"Disabled floating item on {gameObject.name}, rock platform remains");
            break;
        }
    }
}
    
    // This is for debugging - show colliders in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 0.2f, 1f));
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, 1f, 0), 1.5f);
    }
}