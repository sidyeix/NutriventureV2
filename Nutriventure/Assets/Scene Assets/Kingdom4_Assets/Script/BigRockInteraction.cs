using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BigRockInteraction : MonoBehaviour
{
    [Header("Rock Settings")]
    [SerializeField] private int rockID; // 1-5 for each big rock
    [SerializeField] private RockNPCInteraction npcScript; // Reference to the NPC for this rock
    [SerializeField] private GameObject npcObject; // The NPC GameObject
    
    [Header("Small Rocks (Columns)")]
    [SerializeField] private GameObject[] leftColumnRocks;
    [SerializeField] private GameObject[] middleColumnRocks;
    [SerializeField] private GameObject[] rightColumnRocks;
    
    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem collisionEffect;
    [SerializeField] private AudioClip collisionSound;
    
    [Header("Spawner Reference")]
    [SerializeField] private AllergenSpawnerFinal allergenSpawner;
    
    private bool hasBeenActivated = false;
    private AudioSource audioSource;
    private string currentNPCAllergen;
    private int dangerousColumn;
    
    // The nine major allergens
    private readonly string[] nineAllergens = new string[]
    {
        "Milk", "Eggs", "Fish", "Crustacean Shellfish", 
        "Tree Nuts", "Peanuts", "Wheat", "Soybeans", "Sesame"
    };
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        if (allergenSpawner == null)
            allergenSpawner = FindObjectOfType<AllergenSpawnerFinal>();
        
        // Small rocks start with colliders disabled
        SetSmallRocksColliders(false);
        
        // NPC starts hidden — it will appear when the player collides with the big rock
        if (npcObject != null)
            npcObject.SetActive(false);
        
        // Make sure this big rock has a trigger collider
        EnsureTriggerCollider();
    }
    
    private void EnsureTriggerCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            Debug.Log($"Added BoxCollider trigger to Big Rock {rockID}");
        }
        else if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log($"Set existing collider to trigger for Big Rock {rockID}");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        Debug.Log($"<color=green>Big Rock {rockID} - Player entered trigger!</color>");
        
        if (hasBeenActivated)
        {
            Debug.Log($"Big Rock {rockID} - Already activated");
            return;
        }
        
        if (AllerthriaGameManager.Instance != null && 
            !AllerthriaGameManager.Instance.IsCurrentPhase(AllerthriaGameManager.GamePhase.AllergenHunt))
        {
            Debug.Log($"Big Rock {rockID} - Not in Allergen Hunt phase");
            return;
        }
        
        ActivateRock();
    }
    
    private void ActivateRock()
    {
        hasBeenActivated = true;
        
        // Randomly select what THIS SPECIFIC NPC is allergic to
        currentNPCAllergen = nineAllergens[Random.Range(0, nineAllergens.Length)];
        
        // Randomly select which column contains that allergen (0=left, 1=middle, 2=right)
        dangerousColumn = Random.Range(0, 3);
        
        Debug.Log($"<color=green>✅ ROCK {rockID} ACTIVATED - NPC should be allergic to: {currentNPCAllergen}</color>");
        Debug.Log($"<color=green>   Dangerous column: {GetColumnName(dangerousColumn)}</color>");
        
        // Show the NPC now that the player has collided with the big rock
        if (npcObject != null)
            npcObject.SetActive(true);
        
        // Play effects
        if (collisionEffect != null)
            collisionEffect.Play();
            
        if (collisionSound != null)
            audioSource.PlayOneShot(collisionSound);
            
        // Disable big rock collider temporarily
        GetComponent<Collider>().enabled = false;
        
        // Set the allergen on the NPC using SetAllergen method
        if (npcScript != null)
        {
            npcScript.SetAllergen(currentNPCAllergen);
            Debug.Log($"<color=magenta>✅ NPC allergen set for Rock {rockID}: {currentNPCAllergen}</color>");
        }
        else
        {
            Debug.LogError($"NPC Script is NULL for Rock {rockID}!");
        }
        
        // Notify game manager
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.OnRockActivated(rockID, currentNPCAllergen);
        }
    }
    
    // Called by NPC after dialogue
    public void OnNPCDialogueComplete()
    {
        // Randomize the spawner for this rock's columns
        RandomizeRockColumns();
        
        // ENABLE the small rock colliders
        SetSmallRocksColliders(true);
        
        Debug.Log($"Small rocks ENABLED for Rock {rockID}. Find the {currentNPCAllergen} in the {GetColumnName(dangerousColumn)} column!");
    }
    
    private void RandomizeRockColumns()
    {
        if (allergenSpawner == null)
        {
            Debug.LogError("AllergenSpawnerFinal not found!");
            return;
        }
        
        // Build column lists (left=0, middle=1, right=2)
        List<GameObject>[] columns = new List<GameObject>[3];
        columns[0] = new List<GameObject>(leftColumnRocks);
        columns[1] = new List<GameObject>(middleColumnRocks);
        columns[2] = new List<GameObject>(rightColumnRocks);

        // Identify the two safe columns
        List<GameObject> safeColumn1 = null;
        List<GameObject> safeColumn2 = null;
        for (int i = 0; i < 3; i++)
        {
            if (i == dangerousColumn) continue;
            if (safeColumn1 == null) safeColumn1 = columns[i];
            else safeColumn2 = columns[i];
        }

        // Spawn the NPC's allergen on the dangerous column (this is the path to avoid)
        allergenSpawner.SpawnSpecificAllergenOnRocks(columns[dangerousColumn], currentNPCAllergen, 1.5f);

        // Spawn two different safe allergens on the other columns
        if (safeColumn1 != null && safeColumn2 != null)
            allergenSpawner.SpawnSafeAllergensOnRocks(safeColumn1, safeColumn2, currentNPCAllergen, 1.5f);

        // Mark rocks in the dangerous column so the trigger logic can detect them
        MarkDangerousRocks();

        Debug.Log($"Rock {rockID} columns set up — dangerous column: {GetColumnName(dangerousColumn)} ({currentNPCAllergen})");
    }
    
    private void MarkDangerousRocks()
    {
        GameObject[] dangerousRocks = GetColumnRocks(dangerousColumn);
        
        Debug.Log($"Marking rocks in column {GetColumnName(dangerousColumn)} with allergen: {currentNPCAllergen}");
        
        foreach (GameObject rock in dangerousRocks)
        {
            if (rock != null)
            {
                SmallRockTrigger trigger = rock.GetComponent<SmallRockTrigger>();
                if (trigger != null)
                {
                    trigger.SetAsDangerous(currentNPCAllergen);
                    Debug.Log($"   Rock {rock.name} marked as DANGEROUS with {currentNPCAllergen}");
                }
            }
        }
    }
    
    private string GetColumnName(int column)
    {
        switch(column)
        {
            case 0: return "Left";
            case 1: return "Middle";
            case 2: return "Right";
            default: return "Unknown";
        }
    }
    
    private GameObject[] GetColumnRocks(int column)
    {
        switch(column)
        {
            case 0: return leftColumnRocks;
            case 1: return middleColumnRocks;
            case 2: return rightColumnRocks;
            default: return null;
        }
    }
    
    private void SetSmallRocksColliders(bool enabled)
    {
        int count = 0;
        foreach (GameObject rock in leftColumnRocks)
        {
            if (rock == null) continue;
            foreach (Collider col in rock.GetComponents<Collider>())
            {
                col.enabled = enabled;
                count++;
            }
        }
            
        foreach (GameObject rock in middleColumnRocks)
        {
            if (rock == null) continue;
            foreach (Collider col in rock.GetComponents<Collider>())
            {
                col.enabled = enabled;
                count++;
            }
        }
            
        foreach (GameObject rock in rightColumnRocks)
        {
            if (rock == null) continue;
            foreach (Collider col in rock.GetComponents<Collider>())
            {
                col.enabled = enabled;
                count++;
            }
        }
        
        Debug.Log($"Set {count} small rock colliders to {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    public void OnPlayerEnterRockColumn(int column, GameObject rock)
    {
        SmallRockTrigger trigger = rock.GetComponent<SmallRockTrigger>();
        
        if (trigger != null && trigger.IsDangerous())
        {
            HandleAllergenHit(trigger.GetAllergenName());
        }
        else
        {
            HandleSafeChoice(rock);
        }
    }
    
    private void HandleAllergenHit(string allergen)
    {
        Debug.Log($"❌ Player touched {allergen} - NPC is allergic to {currentNPCAllergen}!");
        
        // Deduct one heart from the player's health
        if (PlayerHealthManager.Instance != null)
        {
            PlayerHealthManager.Instance.TakeDamage(1);
        }
        
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.ShowWarningMessage($"Oh no! This NPC is allergic to {currentNPCAllergen}!");
        }
        
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.AddScore(-50);
        }
        
        StartCoroutine(ResetAfterFailure());
    }
    
    private void HandleSafeChoice(GameObject chosenRock)
    {
        SmallRockTrigger trigger = chosenRock.GetComponent<SmallRockTrigger>();
        string touchedAllergen = trigger != null ? trigger.GetAllergenName() : "unknown";
        
        Debug.Log($"✅ Player touched {touchedAllergen} - SAFE! NPC is only allergic to {currentNPCAllergen}");
        
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.ShowSuccessMessage($"Safe! This NPC is only allergic to {currentNPCAllergen}");
            AllerthriaGameManager.Instance.CollectAllergen(currentNPCAllergen);
        }
        
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.AddAllergenFound();
        }
        
        SetSmallRocksColliders(false);
        
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.MarkRockCompleted(rockID);
        }
        
        if (allergenSpawner != null)
        {
            List<GameObject> allRocks = new List<GameObject>();
            allRocks.AddRange(leftColumnRocks);
            allRocks.AddRange(middleColumnRocks);
            allRocks.AddRange(rightColumnRocks);
            allergenSpawner.ClearItemsOnRocks(allRocks);
        }
    }
    
    private void ResetSmallRocks()
    {
        foreach (GameObject rock in leftColumnRocks)
            if (rock != null) { var t = rock.GetComponent<SmallRockTrigger>(); if (t != null) t.ResetRock(); }
        foreach (GameObject rock in middleColumnRocks)
            if (rock != null) { var t = rock.GetComponent<SmallRockTrigger>(); if (t != null) t.ResetRock(); }
        foreach (GameObject rock in rightColumnRocks)
            if (rock != null) { var t = rock.GetComponent<SmallRockTrigger>(); if (t != null) t.ResetRock(); }
    }

    private IEnumerator ResetAfterFailure()
    {
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.ShowWarningMessage($"Try again! This NPC is allergic to {currentNPCAllergen}");
        }
        
        yield return new WaitForSeconds(2f);
        
        // Reset small rock triggers so the player can land on them again
        ResetSmallRocks();
        SetSmallRocksColliders(false);
        
        if (allergenSpawner != null)
        {
            List<GameObject> allRocks = new List<GameObject>();
            allRocks.AddRange(leftColumnRocks);
            allRocks.AddRange(middleColumnRocks);
            allRocks.AddRange(rightColumnRocks);
            allergenSpawner.ClearItemsOnRocks(allRocks);
        }

        // Reset NPC state so dialogue triggers again, then hide it
        if (npcScript != null)
            npcScript.ResetNPC();
        if (npcObject != null)
            npcObject.SetActive(false);

        // Re-enable the big rock collider so the player can collide again
        GetComponent<Collider>().enabled = true;
        hasBeenActivated = false;
    }
}