using UnityEngine;
using TMPro;
using System.Collections;

public class RockNPCInteraction : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float typingSpeed = 0.05f;
    
    [Header("NPC Settings")]
    [SerializeField] private string[] npcMaleNames;
    [SerializeField] private string[] npcFemaleNames;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float interactionDistance = 5f;
    [SerializeField] private bool randomizeAllergenOnStart = false; // CHANGED: false so big rock controls it
    
    private string npcAllergen;
    private Transform player;
    private bool isInteracting = false;
    private bool dialogueShown = false;
    private string npcName;
    private bool isMale = true;
    private bool hasSpoken = false;
    private BigRockInteraction parentRock; // Added reference
    
    // The nine major allergens
    private readonly string[] nineAllergens = new string[]
    {
        "Milk", "Eggs", "Fish", "Crustacean Shellfish", 
        "Tree Nuts", "Peanuts", "Wheat", "Soybeans", "Sesame"
    };
    
    private void Awake()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        
        isMale = Random.Range(0, 2) == 0;
        
        // Find parent rock
        parentRock = GetComponentInParent<BigRockInteraction>();
    }
    
    private void Start()
    {
        if (isMale && npcMaleNames != null && npcMaleNames.Length > 0)
        {
            npcName = npcMaleNames[Random.Range(0, npcMaleNames.Length)];
        }
        else if (!isMale && npcFemaleNames != null && npcFemaleNames.Length > 0)
        {
            npcName = npcFemaleNames[Random.Range(0, npcFemaleNames.Length)];
        }
        else
        {
            npcName = isMale ? "Allerthrian man" : "Allerthrian woman";
        }
        
        if (randomizeAllergenOnStart)
        {
            RandomizeAllergen();
        }
        
        EnsureTriggerCollider();
        
        Debug.Log($"NPC {gameObject.name} - Ready. Name: {npcName}, Allergen: {npcAllergen}");
    }
    
    public void RandomizeAllergen()
    {
        int randomIndex = Random.Range(0, nineAllergens.Length);
        npcAllergen = nineAllergens[randomIndex];
        Debug.Log($"<color=green>✅ NPC {npcName} is allergic to: {npcAllergen}</color>");
    }
    
    public void SetAllergen(string allergen)
    {
        npcAllergen = allergen;
        Debug.Log($"<color=cyan>✅ NPC {npcName} allergen set to: {npcAllergen}</color>");
    }
    
    private void EnsureTriggerCollider()
    {
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
                return;
        }
        
        SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
        sphereCol.isTrigger = true;
        sphereCol.radius = interactionDistance;
        Debug.Log($"NPC {gameObject.name} - Added trigger collider with radius {interactionDistance}");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"<color=cyan>NPC {npcName} - Trigger entered by: {other.gameObject.name}, Tag: {other.tag}</color>");
        
        if (hasSpoken || dialogueShown || isInteracting) 
        {
            return;
        }
        
        bool isPlayer = false;
        
        if (other.CompareTag("Player"))
        {
            isPlayer = true;
        }
        else if (other.GetComponent<CharacterController>() != null)
        {
            isPlayer = true;
        }
        else if (other.transform.parent != null && other.transform.parent.CompareTag("Player"))
        {
            isPlayer = true;
        }
        else if (other.transform.root.CompareTag("Player"))
        {
            isPlayer = true;
        }
        
        if (isPlayer)
        {
            Debug.Log($"<color=green>NPC {npcName} - PLAYER DETECTED! Starting dialogue...</color>");
            StartDialogue();
        }
    }
    
    private void Update()
    {
        if (hasSpoken || dialogueShown || isInteracting || player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= interactionDistance)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0;
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    private void StartDialogue()
    {
        isInteracting = true;
        dialogueShown = true;
        hasSpoken = true;
        
        if (string.IsNullOrEmpty(npcAllergen))
        {
            Debug.LogWarning($"NPC {npcName} - No allergen set! Randomizing now...");
            RandomizeAllergen();
        }
        
        Debug.Log($"<color=magenta>Starting dialogue - NPC: {npcName}, Allergen: {npcAllergen}</color>");
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            
            string[] dialogueLines = new string[]
            {
                $"Hello, I'm {npcName}...",
                $"I'm allergic to {npcAllergen}.",
                $"Please be careful around me!",
                $"The other allergens don't bother me, just {npcAllergen}."
            };
            
            StartCoroutine(TypeDialogue(dialogueLines));
        }
    }
    
    private IEnumerator TypeDialogue(string[] lines)
    {
        foreach (string line in lines)
        {
            dialogueText.text = "";
            foreach (char c in line.ToCharArray())
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
            yield return new WaitForSeconds(1.5f);
        }
        
        yield return new WaitForSeconds(1f);
        
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        
        isInteracting = false;
        
        // FIXED: Notify the parent rock that dialogue is complete
        if (parentRock != null)
        {
            Debug.Log($"NPC {npcName} - Notifying parent rock that dialogue is complete");
            parentRock.OnNPCDialogueComplete();
        }
        
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.ShowMessage($"Remember: {npcName} is allergic to {npcAllergen}");
        }
    }
    
    public void ResetNPC()
    {
        hasSpoken = false;
        dialogueShown = false;
        isInteracting = false;
        RandomizeAllergen();
        Debug.Log($"NPC {npcName} - Reset. New allergen: {npcAllergen}");
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}