using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Collections;

public class Test_EnerlingSpawnManager : MonoBehaviour
{
    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase;
    
    [Header("Spawn Settings")]
    public Transform spawnAreaCenter;
    public float spawnRadius = 20f;
    public int maxSpawnAttempts = 30;
    public float spawnHeight = 0.5f;
    
    [Header("Kingdom Filtering")]
    public KingdomFilterMode kingdomFilterMode = KingdomFilterMode.All;
    public IngredientDatabase.KingdomOrigin specificKingdom = IngredientDatabase.KingdomOrigin.NutriKingdom;
    
    [Header("Enerling Scale Settings")]
    [Range(0.1f, 3f)]
    public float globalScaleMultiplier = 1f;
    [Range(0.1f, 3f)]
    public float minRandomScale = 0.8f;
    [Range(0.1f, 3f)]
    public float maxRandomScale = 1.2f;
    public bool useRandomScale = false;
    public bool preserveOriginalScale = true;
    
    [Header("Collider Settings")]
    public bool addCapsuleCollider = true;
    public float colliderHeight = 2f;
    public float colliderRadius = 0.5f;
    public Vector3 colliderCenter = Vector3.up * 1f;
    public PhysicsMaterial colliderMaterial = null;
    public bool isTrigger = false;
    
    [Header("Density Settings")]
    public float minDistanceBetweenEnerlings = 2f;
    public int maxEnerlingsInScene = 20;
    
    [Header("Spawning Behavior")]
    public bool spawnOnlyUnlocked = true;
    public bool spawnOnStart = true;
    public float spawnDelay = 0.5f;
    
    [Header("Parenting")]
    public Transform enerlingsParent;
    
    private List<Test_EnerlingController> spawnedEnerlings = new List<Test_EnerlingController>();
    private Dictionary<string, int> spawnedCounts = new Dictionary<string, int>();
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    
    // Enum for kingdom filtering
    public enum KingdomFilterMode
    {
        All,
        NutriKingdomOnly,
        AlerthiaOnly,
        SuragriaOnly,
        PreserviaOnly,
        SpecificKingdom
    }
    
    void Start()
    {
        if (spawnOnStart)
        {
            StartCoroutine(SpawnAllEnerlingsWithDelay());
        }
    }
    
    private IEnumerator SpawnAllEnerlingsWithDelay()
    {
        yield return new WaitForSeconds(1f); // Initial delay
        
        // Get filtered ingredients based on kingdom filter
        List<IngredientDatabase.IngredientInfo> ingredientsToSpawn = GetFilteredIngredients();
        
        if (ingredientsToSpawn.Count == 0)
        {
            Debug.LogWarning("No ingredients match the current filter settings!");
            yield break;
        }
        
        Debug.Log($"Found {ingredientsToSpawn.Count} ingredients matching filter: {kingdomFilterMode}");
        
        // Limit the number of Enerlings if needed
        if (maxEnerlingsInScene > 0 && ingredientsToSpawn.Count > maxEnerlingsInScene)
        {
            ingredientsToSpawn = GetRandomSubset(ingredientsToSpawn, maxEnerlingsInScene);
        }
        
        foreach (var ingredient in ingredientsToSpawn)
        {
            if (ingredient.modelPrefab != null)
            {
                SpawnEnerling(ingredient);
                yield return new WaitForSeconds(spawnDelay);
            }
        }
        
        Debug.Log($"Spawned {spawnedEnerlings.Count} Enerlings");
    }
    
    private List<IngredientDatabase.IngredientInfo> GetFilteredIngredients()
    {
        List<IngredientDatabase.IngredientInfo> filteredIngredients;
        
        if (spawnOnlyUnlocked)
        {
            filteredIngredients = ingredientDatabase.GetUnlockedIngredients();
        }
        else
        {
            filteredIngredients = new List<IngredientDatabase.IngredientInfo>(ingredientDatabase.ingredients);
        }
        
        // Apply kingdom filter
        return FilterByKingdom(filteredIngredients);
    }
    
    private List<IngredientDatabase.IngredientInfo> FilterByKingdom(List<IngredientDatabase.IngredientInfo> ingredients)
    {
        if (kingdomFilterMode == KingdomFilterMode.All)
        {
            return ingredients;
        }
        
        List<IngredientDatabase.IngredientInfo> filtered = new List<IngredientDatabase.IngredientInfo>();
        IngredientDatabase.KingdomOrigin targetKingdom = GetTargetKingdom();
        
        foreach (var ingredient in ingredients)
        {
            if (ingredient.kingdom == targetKingdom)
            {
                filtered.Add(ingredient);
            }
        }
        
        return filtered;
    }
    
    private IngredientDatabase.KingdomOrigin GetTargetKingdom()
    {
        switch (kingdomFilterMode)
        {
            case KingdomFilterMode.NutriKingdomOnly:
                return IngredientDatabase.KingdomOrigin.NutriKingdom;
            case KingdomFilterMode.AlerthiaOnly:
                return IngredientDatabase.KingdomOrigin.Alerthia;
            case KingdomFilterMode.SuragriaOnly:
                return IngredientDatabase.KingdomOrigin.Sugaria;
            case KingdomFilterMode.PreserviaOnly:
                return IngredientDatabase.KingdomOrigin.Preservia;
            case KingdomFilterMode.SpecificKingdom:
                return specificKingdom;
            default:
                return IngredientDatabase.KingdomOrigin.NutriKingdom;
        }
    }
    
    private List<IngredientDatabase.IngredientInfo> GetRandomSubset(List<IngredientDatabase.IngredientInfo> list, int count)
    {
        List<IngredientDatabase.IngredientInfo> shuffled = new List<IngredientDatabase.IngredientInfo>(list);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }
        
        return shuffled.GetRange(0, Mathf.Min(count, shuffled.Count));
    }
    
    public void SpawnEnerling(IngredientDatabase.IngredientInfo ingredient)
    {
        Vector3 spawnPosition = FindValidSpawnPosition();
        
        if (spawnPosition != Vector3.zero)
        {
            GameObject enerlingGO = Instantiate(
                ingredient.modelPrefab,
                spawnPosition,
                Quaternion.Euler(0, Random.Range(0f, 360f), 0)
            );
            
            // Set parent if specified
            if (enerlingsParent != null)
            {
                enerlingGO.transform.SetParent(enerlingsParent);
            }
            
            // Name the GameObject
            enerlingGO.name = $"{ingredient.ingredientName}_Enerling";
            
            // Store original scale before modification
            if (preserveOriginalScale)
            {
                originalScales[enerlingGO] = ingredient.modelPrefab.transform.localScale;
            }
            
            // Apply scale modifications
            ApplyScaleToEnerling(enerlingGO);
            
            // Add collider to prevent player from going through
            AddCapsuleCollider(enerlingGO);
            
            // Add NavMeshAgent if not present
            NavMeshAgent agent = SetupNavMeshAgent(enerlingGO);
            
            // Add Test_EnerlingController if not present
            Test_EnerlingController controller = enerlingGO.GetComponent<Test_EnerlingController>();
            if (controller == null)
            {
                controller = enerlingGO.AddComponent<Test_EnerlingController>();
            }
            
            // Pass ingredient info to controller
            controller.SetIngredientInfo(ingredient);
            
            // Set animator controller if specified
            if (ingredient.animatorController != null)
            {
                Animator animator = enerlingGO.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = ingredient.animatorController;
                }
            }
            
            spawnedEnerlings.Add(controller);
            
            // Track spawn counts
            if (!spawnedCounts.ContainsKey(ingredient.ingredientName))
            {
                spawnedCounts[ingredient.ingredientName] = 0;
            }
            spawnedCounts[ingredient.ingredientName]++;
            
            Debug.Log($"✓ Spawned {ingredient.ingredientName} ({ingredient.kingdom}) at {spawnPosition}");
        }
        else
        {
            Debug.LogWarning($"Failed to find valid spawn position for {ingredient.ingredientName}");
        }
    }
    
    private Vector3 FindValidSpawnPosition()
    {
        Vector3 center = spawnAreaCenter != null ? spawnAreaCenter.position : Vector3.zero;
        
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // Generate random position within spawn area
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 potentialPosition = center + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);
            
            // Simple ground check using raycast
            RaycastHit hit;
            if (Physics.Raycast(potentialPosition + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                potentialPosition = hit.point + Vector3.up * 0.1f; // Small offset above ground
                
                // Check if position is on NavMesh
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(potentialPosition, out navHit, 2f, NavMesh.AllAreas))
                {
                    potentialPosition = navHit.position;
                    
                    // Check if too close to other Enerlings
                    bool tooClose = false;
                    foreach (var enerling in spawnedEnerlings)
                    {
                        if (enerling != null && Vector3.Distance(potentialPosition, enerling.transform.position) < minDistanceBetweenEnerlings)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    
                    if (!tooClose)
                    {
                        return potentialPosition;
                    }
                }
            }
        }
        
        return Vector3.zero;
    }
    
    private void AddCapsuleCollider(GameObject enerling)
    {
        if (!addCapsuleCollider) return;
        
        // Check if collider already exists
        CapsuleCollider existingCollider = enerling.GetComponent<CapsuleCollider>();
        if (existingCollider != null)
        {
            // Configure existing collider
            existingCollider.height = colliderHeight;
            existingCollider.radius = colliderRadius;
            existingCollider.center = colliderCenter;
            existingCollider.isTrigger = isTrigger;
            
            if (colliderMaterial != null)
            {
                existingCollider.material = colliderMaterial;
            }
            return;
        }
        
        // Check in children too
        existingCollider = enerling.GetComponentInChildren<CapsuleCollider>();
        if (existingCollider == null)
        {
            // Add new capsule collider
            CapsuleCollider collider = enerling.AddComponent<CapsuleCollider>();
            collider.height = colliderHeight;
            collider.radius = colliderRadius;
            collider.center = colliderCenter;
            collider.isTrigger = isTrigger;
            
            if (colliderMaterial != null)
            {
                collider.material = colliderMaterial;
            }
            
            // Also add a rigidbody if needed for proper collision
            Rigidbody rb = enerling.GetComponent<Rigidbody>();
            if (rb == null && !isTrigger)
            {
                rb = enerling.AddComponent<Rigidbody>();
                rb.isKinematic = true; // Don't let physics move it
                rb.useGravity = false;
            }
        }
    }
    
    private NavMeshAgent SetupNavMeshAgent(GameObject enerling)
    {
        NavMeshAgent agent = enerling.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = enerling.AddComponent<NavMeshAgent>();
        }
        
        // Configure agent based on scale
        float scaleFactor = enerling.transform.localScale.y; // Use Y scale as reference
        
        agent.height = 1f * scaleFactor;
        agent.radius = 0.3f * Mathf.Max(enerling.transform.localScale.x, enerling.transform.localScale.z);
        agent.speed = 1.5f * Mathf.Clamp(scaleFactor, 0.5f, 1.5f);
        agent.acceleration = 8f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 0.1f;
        agent.autoBraking = true;
        
        // Enable obstacle avoidance
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = 50; // Mid-range priority
        
        return agent;
    }
    
    private void ApplyScaleToEnerling(GameObject enerling)
    {
        if (preserveOriginalScale && originalScales.ContainsKey(enerling))
        {
            // Start from original prefab scale
            enerling.transform.localScale = originalScales[enerling];
        }
        
        // Apply global multiplier
        enerling.transform.localScale *= globalScaleMultiplier;
        
        // Apply random scale if enabled
        if (useRandomScale)
        {
            float randomScale = Random.Range(minRandomScale, maxRandomScale);
            enerling.transform.localScale *= randomScale;
        }
    }
    
    // Public methods for UI control
    public void SetKingdomFilter(KingdomFilterMode mode)
    {
        kingdomFilterMode = mode;
        Debug.Log($"Kingdom filter set to: {mode}");
    }
    
    public void SetSpecificKingdom(IngredientDatabase.KingdomOrigin kingdom)
    {
        specificKingdom = kingdom;
        kingdomFilterMode = KingdomFilterMode.SpecificKingdom;
        Debug.Log($"Specific kingdom set to: {kingdom}");
    }
    
    public void SpawnWithFilter(KingdomFilterMode mode)
    {
        kingdomFilterMode = mode;
        RespawnAllEnerlings();
    }
    
    public void SpawnAllKingdoms()
    {
        SpawnWithFilter(KingdomFilterMode.All);
    }
    
    public void SpawnNutriKingdomOnly()
    {
        SpawnWithFilter(KingdomFilterMode.NutriKingdomOnly);
    }
    
    public void SpawnAlerthiaOnly()
    {
        SpawnWithFilter(KingdomFilterMode.AlerthiaOnly);
    }
    
    public void SpawnSuragriaOnly()
    {
        SpawnWithFilter(KingdomFilterMode.SuragriaOnly);
    }
    
    public void SpawnPreserviaOnly()
    {
        SpawnWithFilter(KingdomFilterMode.PreserviaOnly);
    }
    
    public void SpawnSpecificKingdomOnly(IngredientDatabase.KingdomOrigin kingdom)
    {
        specificKingdom = kingdom;
        SpawnWithFilter(KingdomFilterMode.SpecificKingdom);
    }
    
    public void ResizeAllEnerlings(float newScaleMultiplier)
    {
        globalScaleMultiplier = newScaleMultiplier;
        
        foreach (var enerling in spawnedEnerlings)
        {
            if (enerling != null && enerling.gameObject != null)
            {
                GameObject enerlingGO = enerling.gameObject;
                
                // Reset to original scale if preserved
                if (preserveOriginalScale && originalScales.ContainsKey(enerlingGO))
                {
                    enerlingGO.transform.localScale = originalScales[enerlingGO];
                }
                
                // Apply new global multiplier
                enerlingGO.transform.localScale *= globalScaleMultiplier;
                
                // Apply random scale if enabled
                if (useRandomScale)
                {
                    float randomScale = Random.Range(minRandomScale, maxRandomScale);
                    enerlingGO.transform.localScale *= randomScale;
                }
                
                // Update NavMeshAgent size
                NavMeshAgent agent = enerlingGO.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    float scaleFactor = enerlingGO.transform.localScale.y;
                    agent.height = 1f * scaleFactor;
                    agent.radius = 0.3f * Mathf.Max(enerlingGO.transform.localScale.x, enerlingGO.transform.localScale.z);
                }
            }
        }
        
        Debug.Log($"Resized all Enerlings to scale multiplier: {globalScaleMultiplier}");
    }
    
    public void SetScaleByPercentage(float percentage)
    {
        globalScaleMultiplier = percentage / 100f;
        ResizeAllEnerlings(globalScaleMultiplier);
    }
    
    public void SpawnSpecificEnerling(string ingredientName)
    {
        var ingredient = ingredientDatabase.GetIngredientInfo(ingredientName);
        if (ingredient != null)
        {
            SpawnEnerling(ingredient);
        }
        else
        {
            Debug.LogWarning($"Ingredient {ingredientName} not found in database");
        }
    }
    
    public void ClearAllEnerlings()
    {
        foreach (var enerling in spawnedEnerlings)
        {
            if (enerling != null && enerling.gameObject != null)
            {
                Destroy(enerling.gameObject);
            }
        }
        
        spawnedEnerlings.Clear();
        spawnedCounts.Clear();
        originalScales.Clear();
    }
    
    public void RespawnAllEnerlings()
    {
        ClearAllEnerlings();
        StartCoroutine(SpawnAllEnerlingsWithDelay());
    }
    
    public List<Test_EnerlingController> GetAllSpawnedEnerlings()
    {
        return new List<Test_EnerlingController>(spawnedEnerlings);
    }
    
    public Dictionary<string, int> GetSpawnedCounts()
    {
        return new Dictionary<string, int>(spawnedCounts);
    }
    
    public int GetSpawnedCountForKingdom(IngredientDatabase.KingdomOrigin kingdom)
    {
        int count = 0;
        foreach (var enerling in spawnedEnerlings)
        {
            if (enerling != null && enerling.GetComponent<Test_EnerlingController>() != null)
            {
                // Note: You might need to store kingdom info in the controller
                // or access it differently
            }
        }
        return count;
    }
    
    void OnDrawGizmosSelected()
    {
        if (spawnAreaCenter != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(spawnAreaCenter.position, 0.5f);
            
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawWireSphere(spawnAreaCenter.position, spawnRadius);
        }
    }
}