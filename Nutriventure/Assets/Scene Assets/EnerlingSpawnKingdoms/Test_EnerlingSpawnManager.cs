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
    
    [Header("Unlock Filtering")]
    [Tooltip("Check to spawn only unlocked Enerlings. Uncheck to spawn all including locked ones")]
    public bool spawnOnlyUnlocked = true;
    
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
        
        Debug.Log($"=== SPAWN DEBUG ===");
        Debug.Log($"Kingdom Filter Mode: {kingdomFilterMode}");
        Debug.Log($"Spawn Only Unlocked: {spawnOnlyUnlocked}");
        Debug.Log($"Max Enerlings: {maxEnerlingsInScene}");
        // Get filtered ingredients based on kingdom filter
        List<IngredientDatabase.IngredientInfo> ingredientsToSpawn = GetFilteredIngredients();
        Debug.Log($"Ingredients to spawn after all filters: {ingredientsToSpawn.Count}");

        if (ingredientsToSpawn.Count == 0)
        {
            Debug.LogError("❌ No ingredients match the current filter settings!");
            
            // List possible causes
            Debug.LogError("Possible causes:");
            Debug.LogError("1. Database has no ingredients");
            Debug.LogError($"2. Kingdom filter ({kingdomFilterMode}) filters out all ingredients");
            Debug.LogError($"3. Unlock filter (spawnOnlyUnlocked={spawnOnlyUnlocked}) filters out all ingredients");
            Debug.LogError($"4. Specific kingdom: {specificKingdom} doesn't match any ingredients");
            
            yield break;
        }
        
        Debug.Log($"Found {ingredientsToSpawn.Count} ingredients matching filter: {kingdomFilterMode}");
        Debug.Log($"Spawn only unlocked: {spawnOnlyUnlocked}");
        
        // Limit the number of Enerlings if needed
        if (maxEnerlingsInScene > 0 && ingredientsToSpawn.Count > maxEnerlingsInScene)
        {
            ingredientsToSpawn = GetRandomSubset(ingredientsToSpawn, maxEnerlingsInScene);
        }
        
        foreach (var ingredient in ingredientsToSpawn)
        {
            if (ingredient.modelPrefab != null)
            {
                // Check if ingredient is unlocked if we're filtering by unlocked status
                if (!spawnOnlyUnlocked || ingredient.isUnlocked)
                {
                    SpawnEnerling(ingredient);
                    yield return new WaitForSeconds(spawnDelay);
                }
                else
                {
                    Debug.Log($"Skipping locked ingredient: {ingredient.ingredientName}");
                }
            }
        }
        
        Debug.Log($"Spawned {spawnedEnerlings.Count} Enerlings");
        
        // Report statistics
        int unlockedCount = 0;
        int lockedCount = 0;
        foreach (var enerling in spawnedEnerlings)
        {
            if (enerling != null && enerling.GetIngredientInfo() != null)
            {
                if (enerling.GetIngredientInfo().isUnlocked)
                    unlockedCount++;
                else
                    lockedCount++;
            }
        }
        Debug.Log($"Spawned Enerlings - Unlocked: {unlockedCount}, Locked: {lockedCount}");
    }
    
    private List<IngredientDatabase.IngredientInfo> GetFilteredIngredients()
    {
        List<IngredientDatabase.IngredientInfo> filteredIngredients;
        
        // Get all ingredients (both unlocked and locked) from the database
        filteredIngredients = new List<IngredientDatabase.IngredientInfo>(ingredientDatabase.ingredients);
        
        // === DEBUG: Check what's in the database ===
        Debug.Log($"=== DATABASE DEBUG ===");
        Debug.Log($"Database reference: {ingredientDatabase.name}");
        Debug.Log($"Total ingredients in database: {filteredIngredients.Count}");
        
        if (filteredIngredients.Count == 0)
        {
            Debug.LogError("❌ Database has NO ingredients!");
            return new List<IngredientDatabase.IngredientInfo>();
        }
        
        // Log all ingredients with their kingdom and unlock status
        foreach (var ingredient in filteredIngredients)
        {
            string unlocked = ingredient.isUnlocked ? "✓" : "🔒";
            Debug.Log($"{unlocked} {ingredient.ingredientName} - Kingdom: {ingredient.kingdom}, Prefab: {ingredient.modelPrefab != null}");
        }
        Debug.Log($"=== END DATABASE DEBUG ===");
        
        // Apply kingdom filter
        var kingdomFiltered = FilterByKingdom(filteredIngredients);
        
        // === DEBUG: Check kingdom filtering ===
        Debug.Log($"After kingdom filter ({kingdomFilterMode}): {kingdomFiltered.Count} ingredients");
        
        // Apply unlock filter if needed
        if (spawnOnlyUnlocked)
        {
            var unlockedOnly = new List<IngredientDatabase.IngredientInfo>();
            foreach (var ingredient in kingdomFiltered)
            {
                if (ingredient.isUnlocked)
                {
                    unlockedOnly.Add(ingredient);
                }
            }
            Debug.Log($"After unlock filter (spawnOnlyUnlocked={spawnOnlyUnlocked}): {unlockedOnly.Count} ingredients");
            return unlockedOnly;
        }
        
        return kingdomFiltered;
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
            
            // Name the GameObject with locked status indicator
            string lockedStatus = ingredient.isUnlocked ? "Unlocked" : "LOCKED";
            enerlingGO.name = $"{ingredient.ingredientName}_Enerling [{lockedStatus}]";
            
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
            
            // Visual indicator for locked Enerlings (optional)
            if (!ingredient.isUnlocked)
            {
                ApplyLockedVisualEffect(enerlingGO);
            }
            
            spawnedEnerlings.Add(controller);
            
            // Track spawn counts
            if (!spawnedCounts.ContainsKey(ingredient.ingredientName))
            {
                spawnedCounts[ingredient.ingredientName] = 0;
            }
            spawnedCounts[ingredient.ingredientName]++;
            
            string statusMsg = ingredient.isUnlocked ? "✓" : "🔒";
            Debug.Log($"{statusMsg} Spawned {ingredient.ingredientName} ({ingredient.kingdom}) at {spawnPosition} - Locked: {!ingredient.isUnlocked}");
        }
        else
        {
            Debug.LogWarning($"Failed to find valid spawn position for {ingredient.ingredientName}");
        }
    }
    
    private void ApplyLockedVisualEffect(GameObject enerlingGO)
    {
        // Option 1: Add a visual indicator (like a lock icon or different color)
        // You can modify this based on your visual requirements
        
        // Example: Add a semi-transparent grey material to indicate locked status
        Renderer[] renderers = enerlingGO.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // You can either change the material or add an overlay
            // For now, we'll just log it for debugging
        }
        
        // Option 2: Add a lock icon as a child object
        // GameObject lockIcon = new GameObject("LockedIndicator");
        // lockIcon.transform.SetParent(enerlingGO.transform);
        // lockIcon.transform.localPosition = Vector3.up * 2f;
        // Add sprite renderer or mesh to show lock icon
        
        Debug.Log($"Applied locked visual effect to {enerlingGO.name}");
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
    
    public void SetUnlockFilter(bool onlyUnlocked)
    {
        spawnOnlyUnlocked = onlyUnlocked;
        Debug.Log($"Spawn only unlocked set to: {onlyUnlocked}");
    }
    
    public void SpawnWithFilter(KingdomFilterMode mode, bool onlyUnlocked = true)
    {
        kingdomFilterMode = mode;
        spawnOnlyUnlocked = onlyUnlocked;
        RespawnAllEnerlings();
    }
    
    public void SpawnAllKingdoms(bool includeLocked = false)
    {
        spawnOnlyUnlocked = !includeLocked;
        SpawnWithFilter(KingdomFilterMode.All, spawnOnlyUnlocked);
    }
    
    public void SpawnNutriKingdomOnly(bool includeLocked = false)
    {
        spawnOnlyUnlocked = !includeLocked;
        SpawnWithFilter(KingdomFilterMode.NutriKingdomOnly, spawnOnlyUnlocked);
    }
    
    public void SpawnAlerthiaOnly(bool includeLocked = false)
    {
        spawnOnlyUnlocked = !includeLocked;
        SpawnWithFilter(KingdomFilterMode.AlerthiaOnly, spawnOnlyUnlocked);
    }
    
    public void SpawnSuragriaOnly(bool includeLocked = false)
    {
        spawnOnlyUnlocked = !includeLocked;
        SpawnWithFilter(KingdomFilterMode.SuragriaOnly, spawnOnlyUnlocked);
    }
    
    public void SpawnPreserviaOnly(bool includeLocked = false)
    {
        spawnOnlyUnlocked = !includeLocked;
        SpawnWithFilter(KingdomFilterMode.PreserviaOnly, spawnOnlyUnlocked);
    }
    
    public void SpawnSpecificKingdomOnly(IngredientDatabase.KingdomOrigin kingdom, bool includeLocked = false)
    {
        specificKingdom = kingdom;
        spawnOnlyUnlocked = !includeLocked;
        SpawnWithFilter(KingdomFilterMode.SpecificKingdom, spawnOnlyUnlocked);
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
            // Check unlock status if spawnOnlyUnlocked is true
            if (spawnOnlyUnlocked && !ingredient.isUnlocked)
            {
                Debug.LogWarning($"Cannot spawn {ingredientName} because it's locked and spawnOnlyUnlocked is enabled");
                return;
            }
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
    
    // New methods for unlock status
    public int GetUnlockedSpawnedCount()
    {
        int count = 0;
        foreach (var enerling in spawnedEnerlings)
        {
            if (enerling != null && enerling.GetIngredientInfo() != null && enerling.GetIngredientInfo().isUnlocked)
            {
                count++;
            }
        }
        return count;
    }
    
    public int GetLockedSpawnedCount()
    {
        int count = 0;
        foreach (var enerling in spawnedEnerlings)
        {
            if (enerling != null && enerling.GetIngredientInfo() != null && !enerling.GetIngredientInfo().isUnlocked)
            {
                count++;
            }
        }
        return count;
    }
    
    public void ToggleUnlockFilter()
    {
        spawnOnlyUnlocked = !spawnOnlyUnlocked;
        Debug.Log($"Toggled spawnOnlyUnlocked to: {spawnOnlyUnlocked}");
        RespawnAllEnerlings();
    }
    
    public void SpawnOnlyUnlocked()
    {
        spawnOnlyUnlocked = true;
        RespawnAllEnerlings();
    }
    
    public void SpawnAllIncludingLocked()
    {
        spawnOnlyUnlocked = false;
        RespawnAllEnerlings();
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