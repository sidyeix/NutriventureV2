using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Cinemachine;
using System.Collections.Generic;

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
    public float lookAtSpeed = 5f; // Speed for looking at camera
    
    [Header("Idle Behavior")]
    public bool canIdleAnimate = true;
    public float idleAnimationChance = 0.3f;
    public float minIdleTime = 5f;
    public float maxIdleTime = 15f;
    
    [Header("Social Behavior")]
    public float socialDistance = 3f;
    public float socialCheckInterval = 2f;
    public float followChance = 0.2f;
    
    [Header("Collision Avoidance")]
    public float minDistanceBetweenEnerlings = 2f;
    public float avoidanceCheckInterval = 1f;
    public float avoidanceForce = 2f;
    
    [Header("Interaction Animations")]
    public string[] interactionIdleAnimations = { "Idle1", "Idle2", "LookAround", "Stretch" };
    public float interactionAnimationInterval = 3f;
    
    [HideInInspector] public NavMeshAgent navAgent;
    private Animator animator;
    private Vector3 spawnPosition;
    private bool isRoaming = true;
    private float currentIdleTime = 0f;
    private Test_EnerlingController followingTarget = null;
    private IngredientDatabase.IngredientInfo ingredientInfo;
    
    // Animation parameters
    private readonly int isWalkingHash = Animator.StringToHash("isWalking");
    private bool wasMoving = false; // Track previous movement state
    
    // Interaction states
    private bool isInteracting = false;
    private CinemachineVirtualCamera currentVirtualCamera;
    private Quaternion originalRotation;
    private Coroutine roamingCoroutine;
    private Coroutine socialCoroutine;
    private Coroutine interactionAnimationCoroutine;
    private Coroutine avoidanceCoroutine;
    
    // Virtual Camera reference
    private CinemachineVirtualCamera virtualCamera;
    
    // Static reference to all enerlings for coordinated movement
    private static List<Test_EnerlingController> allEnerlings = new List<Test_EnerlingController>();
    
    // Track if behavior coroutines are running
    private bool isBehaviorRunning = false;
    
    void Awake()
    {
        // Register this enerling
        if (!allEnerlings.Contains(this))
        {
            allEnerlings.Add(this);
        }
    }
    
    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Create virtual camera as child
        CreateVirtualCamera();
        
        // Start behavior coroutines
        StartBehaviorCoroutines();
        
        // Ensure animator starts in idle state
        if (animator != null)
        {
            animator.SetBool(isWalkingHash, false);
        }
        
        Debug.Log($"Enerling {gameObject.name} initialized and added to movement coordination");
    }
    
    void Update()
    {
        // Only look at camera if we have a valid camera reference that's not our own
        if (isInteracting && currentVirtualCamera != null && currentVirtualCamera != virtualCamera)
        {
            LookAtCamera(currentVirtualCamera.transform.position);
        }
        
        // Update animation parameters
        if (animator != null)
        {
            // Check if the agent is moving
            bool isMoving = navAgent.velocity.magnitude > 0.1f && navAgent.hasPath;
            
            // Only update if state has changed to avoid unnecessary animator calls
            if (isMoving != wasMoving)
            {
                animator.SetBool(isWalkingHash, isMoving);
                wasMoving = isMoving;
                
                // Optional: Debug to verify animation state changes
                // Debug.Log($"{gameObject.name} isMoving: {isMoving}, velocity: {navAgent.velocity.magnitude}");
            }
            
            // Keep the Speed parameter for blending if needed
            animator.SetFloat("Speed", navAgent.velocity.magnitude);
            animator.SetBool("IsMoving", isMoving);
        }
        
        // Handle idle behavior (only when not interacting)
        if (!isInteracting && !navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
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
    
    private void CreateVirtualCamera()
    {
        // Create a new GameObject for the virtual camera
        GameObject vcamGO = new GameObject("EnerlingVirtualCamera");
        vcamGO.transform.SetParent(transform);
        
        // Set transform properties as specified
        vcamGO.transform.localPosition = new Vector3(-0.695f, 1.24005f, 1.799f);
        vcamGO.transform.localRotation = Quaternion.Euler(0, 190.004f, 0);
        vcamGO.transform.localScale = new Vector3(1.886793f, 1.886793f, 1.886793f);
        
        // Add CinemachineVirtualCamera component
        virtualCamera = vcamGO.AddComponent<CinemachineVirtualCamera>();
        
        // Configure camera settings
        virtualCamera.Priority = 0; // Default priority (inactive)
        virtualCamera.m_Lens.FieldOfView = 60f;
        
        // Don't set LookAt or Follow - these should remain null as per requirements
        virtualCamera.LookAt = null;
        virtualCamera.Follow = null;
        
        Debug.Log($"Created virtual camera for {gameObject.name}");
    }
    
    private void LookAtCamera(Vector3 cameraPosition)
    {
        Vector3 direction = (cameraPosition - transform.position).normalized;
        direction.y = 0; // Keep rotation horizontal only
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
        }
    }
    
    private void StartBehaviorCoroutines()
    {
        isBehaviorRunning = true;
        roamingCoroutine = StartCoroutine(RoamingBehavior());
        socialCoroutine = StartCoroutine(SocialBehaviorCheck());
        avoidanceCoroutine = StartCoroutine(CollisionAvoidanceCheck());
    }
    
    private void StopBehaviorCoroutines()
    {
        isBehaviorRunning = false;
        
        if (roamingCoroutine != null)
        {
            StopCoroutine(roamingCoroutine);
            roamingCoroutine = null;
        }
        
        if (socialCoroutine != null)
        {
            StopCoroutine(socialCoroutine);
            socialCoroutine = null;
        }
        
        if (avoidanceCoroutine != null)
        {
            StopCoroutine(avoidanceCoroutine);
            avoidanceCoroutine = null;
        }
    }
    
    private IEnumerator RoamingBehavior()
    {
        while (isRoaming && isBehaviorRunning)
        {
            if (!isInteracting && followingTarget == null)
            {
                // Choose a random destination
                Vector3 randomDirection = Random.insideUnitSphere * Random.Range(minRoamDistance, maxRoamDistance);
                randomDirection += spawnPosition;
                
                // Try to find a valid position on NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, maxRoamDistance, NavMesh.AllAreas))
                {
                    // Check if destination is too close to other enerlings
                    if (!IsTooCloseToOtherEnerlings(hit.position))
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
                    else
                    {
                        // If too close, wait a bit and try again
                        yield return new WaitForSeconds(1f);
                    }
                }
                else
                {
                    // If can't find valid position, wait and try again
                    yield return new WaitForSeconds(1f);
                }
            }
            else if (!isInteracting && followingTarget != null)
            {
                // Following behavior
                float distance = Vector3.Distance(transform.position, followingTarget.transform.position);
                if (distance > socialDistance)
                {
                    navAgent.SetDestination(followingTarget.transform.position);
                }
                
                yield return new WaitForSeconds(1f);
            }
            else
            {
                // If interacting, just wait
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
    
    private bool IsTooCloseToOtherEnerlings(Vector3 position)
    {
        foreach (var enerling in allEnerlings)
        {
            if (enerling != null && enerling != this && enerling.gameObject.activeInHierarchy && !enerling.isInteracting)
            {
                float distance = Vector3.Distance(position, enerling.transform.position);
                if (distance < minDistanceBetweenEnerlings)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private IEnumerator CollisionAvoidanceCheck()
    {
        while (isBehaviorRunning)
        {
            yield return new WaitForSeconds(avoidanceCheckInterval);
            
            if (!isInteracting && navAgent.hasPath)
            {
                // Check for nearby enerlings and avoid them
                Vector3 avoidanceVector = Vector3.zero;
                int nearbyCount = 0;
                
                foreach (var enerling in allEnerlings)
                {
                    if (enerling != null && enerling != this && enerling.gameObject.activeInHierarchy && !enerling.isInteracting)
                    {
                        float distance = Vector3.Distance(transform.position, enerling.transform.position);
                        if (distance < minDistanceBetweenEnerlings)
                        {
                            Vector3 awayDirection = (transform.position - enerling.transform.position).normalized;
                            avoidanceVector += awayDirection * (1f - (distance / minDistanceBetweenEnerlings));
                            nearbyCount++;
                        }
                    }
                }
                
                if (nearbyCount > 0)
                {
                    // Calculate new destination with avoidance
                    avoidanceVector /= nearbyCount;
                    Vector3 currentDestination = navAgent.destination;
                    Vector3 newDestination = currentDestination + avoidanceVector * avoidanceForce;
                    
                    // Sample position on NavMesh
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(newDestination, out hit, 2f, NavMesh.AllAreas))
                    {
                        navAgent.SetDestination(hit.position);
                    }
                }
            }
        }
    }
    
    private IEnumerator SocialBehaviorCheck()
    {
        while (isBehaviorRunning)
        {
            yield return new WaitForSeconds(socialCheckInterval);
            
            // Only check for social behavior when not interacting
            if (!isInteracting)
            {
                // Check for nearby Enerlings
                Collider[] nearbyEnerlings = Physics.OverlapSphere(transform.position, socialDistance * 2);
                
                foreach (Collider collider in nearbyEnerlings)
                {
                    Test_EnerlingController otherEnerling = collider.GetComponent<Test_EnerlingController>();
                    if (otherEnerling != null && otherEnerling != this && followingTarget == null && !otherEnerling.isInteracting)
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
    }
    
    private IEnumerator StopFollowingAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        followingTarget = null;
    }
    
    private void TriggerIdleAnimation()
    {
        if (animator != null && !isInteracting)
        {
            string[] idleTriggers = { "Idle1", "Idle2", "LookAround", "Stretch" };
            string randomTrigger = idleTriggers[Random.Range(0, idleTriggers.Length)];
            animator.SetTrigger(randomTrigger);
        }
    }
    
    private IEnumerator InteractionAnimationRoutine()
    {
        while (isInteracting)
        {
            yield return new WaitForSeconds(interactionAnimationInterval);
            
            if (animator != null && interactionIdleAnimations.Length > 0)
            {
                string randomAnimation = interactionIdleAnimations[Random.Range(0, interactionIdleAnimations.Length)];
                animator.SetTrigger(randomAnimation);
                Debug.Log($"Playing interaction animation: {randomAnimation}");
            }
        }
    }
    
    // Public method to start interaction
    public void StartInteraction(CinemachineVirtualCamera vcam = null)
    {
        if (isInteracting) return;
        
        isInteracting = true;
        currentVirtualCamera = vcam != null ? vcam : virtualCamera;
        
        // Store original rotation
        originalRotation = transform.rotation;
        
        // Stop current movement
        navAgent.isStopped = true;
        navAgent.ResetPath();
        
        // Update animation to idle
        if (animator != null)
        {
            animator.SetBool(isWalkingHash, false);
            animator.SetBool("IsMoving", false);
            animator.SetFloat("Speed", 0f);
        }
        
        // Stop behavior coroutines
        StopBehaviorCoroutines();
        
        // Activate virtual camera
        if (virtualCamera != null)
        {
            virtualCamera.Priority = 20; // Set to high priority when interacting
        }
        
        // Start interaction animations
        if (interactionAnimationCoroutine != null)
        {
            StopCoroutine(interactionAnimationCoroutine);
        }
        interactionAnimationCoroutine = StartCoroutine(InteractionAnimationRoutine());
        
        Debug.Log($"{gameObject.name} started interaction with virtual camera");
    }
    
    // Public method to end interaction
    public void EndInteraction()
    {
        if (!isInteracting) return;
        
        isInteracting = false;
        currentVirtualCamera = null;
        
        // Deactivate virtual camera
        if (virtualCamera != null)
        {
            virtualCamera.Priority = 0; // Set back to low priority
        }
        
        // Stop interaction animations
        if (interactionAnimationCoroutine != null)
        {
            StopCoroutine(interactionAnimationCoroutine);
            interactionAnimationCoroutine = null;
        }
        
        // Resume movement
        navAgent.isStopped = false;
        
        // Restart behavior coroutines
        StartBehaviorCoroutines();
        
        // Reset rotation to original
        StartCoroutine(ResetRotation());
        
        Debug.Log($"{gameObject.name} ended interaction");
    }
    
    private IEnumerator ResetRotation()
    {
        float elapsedTime = 0f;
        float duration = 1f;
        Quaternion startRotation = transform.rotation;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.rotation = Quaternion.Slerp(startRotation, originalRotation, t);
            yield return null;
        }
        
        transform.rotation = originalRotation;
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
    
    public IngredientDatabase.IngredientInfo GetIngredientInfo()
    {
        return ingredientInfo;
    }
    
    public bool IsInteracting()
    {
        return isInteracting;
    }
    
    public CinemachineVirtualCamera GetVirtualCamera()
    {
        return virtualCamera;
    }
    
    // Static method to pause all enerlings (COMPLETELY stop them)
    public static void PauseAllEnerlings()
    {
        foreach (var enerling in allEnerlings)
        {
            if (enerling != null && !enerling.isInteracting)
            {
                // Stop NavMeshAgent
                enerling.navAgent.isStopped = true;
                enerling.navAgent.ResetPath();
                
                // Update animation to idle
                if (enerling.animator != null)
                {
                    enerling.animator.SetBool(enerling.isWalkingHash, false);
                    enerling.animator.SetBool("IsMoving", false);
                    enerling.animator.SetFloat("Speed", 0f);
                }
                
                // Stop all behavior coroutines
                enerling.StopBehaviorCoroutines();
            }
        }
        Debug.Log($"Paused {allEnerlings.Count} enerlings");
    }
    
    // Static method to resume all enerlings
    public static void ResumeAllEnerlings()
    {
        foreach (var enerling in allEnerlings)
        {
            if (enerling != null && !enerling.isInteracting)
            {
                // Resume NavMeshAgent
                enerling.navAgent.isStopped = false;
                
                // Restart behavior coroutines
                enerling.StartBehaviorCoroutines();
                
                // Note: The walking animation will automatically be set when the agent starts moving
                // as the Update method will detect the velocity change
            }
        }
        Debug.Log($"Resumed {allEnerlings.Count} enerlings");
    }
    
    void OnDestroy()
    {
        // Unregister this enerling
        if (allEnerlings.Contains(this))
        {
            allEnerlings.Remove(this);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, socialDistance);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, socialDistance * 2);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDistanceBetweenEnerlings);
    }
}