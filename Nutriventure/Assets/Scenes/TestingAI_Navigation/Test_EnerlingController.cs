using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Test_EnerlingController : MonoBehaviour
{
    [Header("Roaming Settings")]
    public float minRoamDistance = 5f;
    public float maxRoamDistance = 15f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 6f;
    public float walkSpeed = 1.5f;
    public float runSpeed = 3f;
    
    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    
    [Header("Idle Behavior")]
    public bool canIdleAnimate = true;
    public float idleAnimationChance = 0.3f;
    public float minIdleTime = 5f;
    public float maxIdleTime = 15f;
    
    [Header("Social Behavior")]
    public float socialDistance = 3f;
    public float socialCheckInterval = 2f;
    public float followChance = 0.2f;
    
    private NavMeshAgent navAgent;
    private Animator animator;
    private Vector3 spawnPosition;
    private bool isRoaming = true;
    private float currentIdleTime = 0f;
    private Test_EnerlingController followingTarget = null;
    private IngredientDatabase.IngredientInfo ingredientInfo;
    
    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;
        
        // Start roaming behavior
        StartCoroutine(RoamingBehavior());
        
        // Start social behavior checking
        StartCoroutine(SocialBehaviorCheck());
    }
    
    void Update()
    {
        // Update animation parameters
        if (animator != null)
        {
            animator.SetFloat("Speed", navAgent.velocity.magnitude);
            animator.SetBool("IsMoving", navAgent.velocity.magnitude > 0.1f);
        }
        
        // Handle idle behavior
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            currentIdleTime += Time.deltaTime;
            
            // Random idle animation
            if (canIdleAnimate && currentIdleTime > Random.Range(minIdleTime, maxIdleTime))
            {
                TriggerIdleAnimation();
                currentIdleTime = 0f;
            }
        }
    }
    
    private IEnumerator RoamingBehavior()
    {
        while (isRoaming)
        {
            if (followingTarget == null)
            {
                // Choose a random destination
                Vector3 randomDirection = Random.insideUnitSphere * Random.Range(minRoamDistance, maxRoamDistance);
                randomDirection += spawnPosition;
                
                // Try to find a valid position on NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, maxRoamDistance, NavMesh.AllAreas))
                {
                    navAgent.speed = Random.value > 0.7f ? runSpeed : walkSpeed;
                    navAgent.SetDestination(hit.position);
                    
                    // Wait until reaching destination
                    yield return new WaitUntil(() => 
                        !navAgent.pathPending && 
                        navAgent.remainingDistance <= navAgent.stoppingDistance);
                    
                    // Wait for random time before next roam
                    yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
                }
            }
            else
            {
                // Following behavior
                float distance = Vector3.Distance(transform.position, followingTarget.transform.position);
                if (distance > socialDistance)
                {
                    navAgent.SetDestination(followingTarget.transform.position);
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
    }
    
    private IEnumerator SocialBehaviorCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(socialCheckInterval);
            
            // Check for nearby Enerlings
            Collider[] nearbyEnerlings = Physics.OverlapSphere(transform.position, socialDistance * 2);
            
            foreach (Collider collider in nearbyEnerlings)
            {
                Test_EnerlingController otherEnerling = collider.GetComponent<Test_EnerlingController>();
                if (otherEnerling != null && otherEnerling != this && followingTarget == null)
                {
                    if (Random.value < followChance)
                    {
                        followingTarget = otherEnerling;
                        StartCoroutine(StopFollowingAfterTime(Random.Range(10f, 30f)));
                        break;
                    }
                }
            }
        }
    }
    
    private IEnumerator StopFollowingAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        followingTarget = null;
    }
    
    private void TriggerIdleAnimation()
    {
        if (animator != null)
        {
            string[] idleTriggers = { "Idle1", "Idle2", "LookAround", "Stretch" };
            string randomTrigger = idleTriggers[Random.Range(0, idleTriggers.Length)];
            animator.SetTrigger(randomTrigger);
        }
    }
    
    public void SetIngredientInfo(IngredientDatabase.IngredientInfo info)
    {
        ingredientInfo = info;
        
        if (ingredientInfo != null)
        {
            switch (ingredientInfo.rarity)
            {
                case IngredientDatabase.Rarity.UltraRare:
                    walkSpeed *= 1.2f;
                    socialDistance *= 1.5f;
                    break;
                case IngredientDatabase.Rarity.Rare:
                    walkSpeed *= 1.1f;
                    break;
            }
            
            switch (ingredientInfo.kingdom)
            {
                case IngredientDatabase.KingdomOrigin.NutriKingdom:
                    followChance = 0.3f;
                    break;
                case IngredientDatabase.KingdomOrigin.Alerthia:
                    minWaitTime = 1f;
                    maxWaitTime = 4f;
                    break;
            }
        }
    }
    
    // ADD THIS METHOD - Important for UI manager to get info!
    public IngredientDatabase.IngredientInfo GetIngredientInfo()
    {
        return ingredientInfo;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, socialDistance);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, socialDistance * 2);
    }
}