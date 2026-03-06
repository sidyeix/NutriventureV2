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

    [Header("Rotation Settings")]
    public float rotationSpeed = 8f;

    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public float lookAtSpeed = 5f;

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
    private float nextIdleThreshold = 0f;
    private Test_EnerlingController followingTarget = null;
    private IngredientDatabase.IngredientInfo ingredientInfo;

    // Cached animation parameter hash — only isWalking is needed
    private static readonly int isWalkingHash = Animator.StringToHash("isWalking");
    private bool wasMoving = false;

    // Interaction states
    private bool isInteracting = false;
    private CinemachineVirtualCamera currentVirtualCamera;
    private Quaternion originalRotation;
    private Coroutine roamingCoroutine;
    private Coroutine socialCoroutine;
    private Coroutine interactionAnimationCoroutine;
    private Coroutine avoidanceCoroutine;

    // Virtual Camera reference — created lazily on first interaction
    private CinemachineVirtualCamera virtualCamera;
    private bool virtualCameraCreated = false;

    // Static reference to all enerlings — use HashSet for O(1) add/remove/contains
    private static HashSet<Test_EnerlingController> allEnerlingsSet = new HashSet<Test_EnerlingController>();
    // Also keep a cached list for iteration (updated when set changes)
    private static List<Test_EnerlingController> allEnerlingsList = new List<Test_EnerlingController>();
    private static bool listDirty = true;

    // Pre-squared distances to avoid per-frame sqrt
    private float minDistSqr;
    private float socialDistSqr;

    // Track if behavior coroutines are running
    private bool isBehaviorRunning = false;

    // Stagger offset for coroutine starts (avoid all ticking same frame)
    private static int spawnIndex = 0;

    void Awake()
    {
        if (allEnerlingsSet.Add(this))
        {
            listDirty = true;
        }
    }

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;
        originalRotation = transform.rotation;

        // Pre-compute squared distances
        minDistSqr = minDistanceBetweenEnerlings * minDistanceBetweenEnerlings;
        socialDistSqr = socialDistance * socialDistance;

        // Disable NavMeshAgent rotation — we handle facing direction manually
        if (navAgent != null)
        {
            navAgent.updateRotation = false;
            navAgent.updateUpAxis = true;
        }

        // Disable root motion so the walking animation doesn't override our manual rotation
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // DO NOT create virtual camera on start — defer until interaction (saves memory + CPU)

        // Stagger coroutine starts so enerlings don't all tick on the same frame
        float stagger = (spawnIndex++) * 0.05f;
        StartCoroutine(StartBehaviorCoroutinesStaggered(stagger));

        // Ensure animator starts in idle state
        if (animator != null)
        {
            animator.SetBool(isWalkingHash, false);
        }

        // Pre-calculate first idle threshold
        nextIdleThreshold = Random.Range(minIdleTime, maxIdleTime);
    }

    private IEnumerator StartBehaviorCoroutinesStaggered(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        StartBehaviorCoroutines();
    }

    void Update()
    {
        // Only look at camera if interacting with an external camera
        if (isInteracting && currentVirtualCamera != null && currentVirtualCamera != virtualCamera)
        {
            LookAtCamera(currentVirtualCamera.transform.position);
            return; // Skip roaming logic while interacting
        }

        if (navAgent == null || !navAgent.isOnNavMesh) return;

        // Determine if the enerling is moving
        bool isMoving = navAgent.velocity.sqrMagnitude > 0.01f; // 0.01 = 0.1^2

        // Rotate to face the direction the agent needs to go (steeringTarget),
        // NOT navAgent.velocity which includes sideways avoidance corrections.
        if (isMoving && navAgent.hasPath)
        {
            Vector3 lookTarget = navAgent.steeringTarget - transform.position;
            lookTarget.y = 0f;
            if (lookTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookTarget.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        // Only update isWalking when movement state changes
        if (isMoving != wasMoving)
        {
            wasMoving = isMoving;
            if (animator != null)
            {
                animator.SetBool(isWalkingHash, isMoving);
            }
        }

        // Handle idle behavior (only when stopped)
        if (!isMoving && !isInteracting && !navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            currentIdleTime += Time.deltaTime;

            if (canIdleAnimate && currentIdleTime > nextIdleThreshold)
            {
                TriggerIdleAnimation();
                currentIdleTime = 0f;
                nextIdleThreshold = Random.Range(minIdleTime, maxIdleTime);
            }
        }
    }

    private void EnsureVirtualCamera()
    {
        if (virtualCameraCreated) return;
        virtualCameraCreated = true;

        GameObject vcamGO = new GameObject("EnerlingVirtualCamera");
        vcamGO.transform.SetParent(transform);
        vcamGO.transform.localPosition = new Vector3(-1.32f, 1.03f, 3.34f);
        vcamGO.transform.localRotation = Quaternion.Euler(0, 190.004f, 0);
        vcamGO.transform.localScale = new Vector3(1.886793f, 1.886793f, 1.886793f);

        virtualCamera = vcamGO.AddComponent<CinemachineVirtualCamera>();
        virtualCamera.Priority = 0;
        virtualCamera.m_Lens.FieldOfView = 60f;
        virtualCamera.LookAt = null;
        virtualCamera.Follow = null;
    }

    private void LookAtCamera(Vector3 cameraPosition)
    {
        Vector3 direction = (cameraPosition - transform.position);
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
        }
    }

    private static List<Test_EnerlingController> GetEnerlingsList()
    {
        if (listDirty)
        {
            allEnerlingsList.Clear();
            allEnerlingsList.AddRange(allEnerlingsSet);
            listDirty = false;
        }
        return allEnerlingsList;
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
                Vector3 randomDirection = Random.insideUnitSphere * Random.Range(minRoamDistance, maxRoamDistance);
                randomDirection += spawnPosition;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, maxRoamDistance, NavMesh.AllAreas))
                {
                    if (!IsTooCloseToOtherEnerlings(hit.position))
                    {
                        navAgent.speed = Random.value > 0.7f ? runSpeed : walkSpeed;
                        navAgent.SetDestination(hit.position);

                        yield return new WaitUntil(() =>
                            !navAgent.pathPending &&
                            navAgent.remainingDistance <= navAgent.stoppingDistance);

                        yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
                    }
                    else
                    {
                        yield return new WaitForSeconds(1f);
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
            else if (!isInteracting && followingTarget != null)
            {
                float distSqr = (transform.position - followingTarget.transform.position).sqrMagnitude;
                if (distSqr > socialDistSqr)
                {
                    navAgent.SetDestination(followingTarget.transform.position);
                }
                yield return new WaitForSeconds(1f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private bool IsTooCloseToOtherEnerlings(Vector3 position)
    {
        var list = GetEnerlingsList();
        for (int i = 0; i < list.Count; i++)
        {
            var enerling = list[i];
            if (enerling != null && enerling != this && enerling.gameObject.activeInHierarchy && !enerling.isInteracting)
            {
                float distSqr = (position - enerling.transform.position).sqrMagnitude;
                if (distSqr < minDistSqr)
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
                Vector3 avoidanceVector = Vector3.zero;
                int nearbyCount = 0;

                var list = GetEnerlingsList();
                for (int i = 0; i < list.Count; i++)
                {
                    var enerling = list[i];
                    if (enerling != null && enerling != this && enerling.gameObject.activeInHierarchy && !enerling.isInteracting)
                    {
                        Vector3 diff = transform.position - enerling.transform.position;
                        float distSqr = diff.sqrMagnitude;
                        if (distSqr < minDistSqr && distSqr > 0.001f)
                        {
                            float dist = Mathf.Sqrt(distSqr);
                            avoidanceVector += (diff / dist) * (1f - (dist / minDistanceBetweenEnerlings));
                            nearbyCount++;
                        }
                    }
                }

                if (nearbyCount > 0)
                {
                    avoidanceVector /= nearbyCount;
                    Vector3 newDestination = navAgent.destination + avoidanceVector * avoidanceForce;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(newDestination, out hit, 2f, NavMesh.AllAreas))
                    {
                        navAgent.SetDestination(hit.position);
                    }
                }
            }
        }
    }

    // Optimized: iterate static list instead of Physics.OverlapSphere
    private IEnumerator SocialBehaviorCheck()
    {
        while (isBehaviorRunning)
        {
            yield return new WaitForSeconds(socialCheckInterval);

            if (!isInteracting && followingTarget == null)
            {
                float socialDistSqr2x = (socialDistance * 2f) * (socialDistance * 2f);
                var list = GetEnerlingsList();
                for (int i = 0; i < list.Count; i++)
                {
                    var other = list[i];
                    if (other != null && other != this && !other.isInteracting)
                    {
                        float distSqr = (transform.position - other.transform.position).sqrMagnitude;
                        if (distSqr < socialDistSqr2x)
                        {
                            if (Random.value < followChance)
                            {
                                followingTarget = other;
                                StartCoroutine(StopFollowingAfterTime(Random.Range(10f, 30f)));
                                break;
                            }
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
            }
        }
    }

    public void StartInteraction(CinemachineVirtualCamera vcam = null)
    {
        if (isInteracting) return;

        isInteracting = true;

        // Lazily create virtual camera only when needed
        EnsureVirtualCamera();
        currentVirtualCamera = vcam != null ? vcam : virtualCamera;

        originalRotation = transform.rotation;

        navAgent.isStopped = true;
        navAgent.ResetPath();

        if (animator != null)
        {
            animator.SetBool(isWalkingHash, false);
        }
        wasMoving = false;

        StopBehaviorCoroutines();

        if (virtualCamera != null)
        {
            virtualCamera.Priority = 20;
        }

        if (interactionAnimationCoroutine != null)
        {
            StopCoroutine(interactionAnimationCoroutine);
        }
        interactionAnimationCoroutine = StartCoroutine(InteractionAnimationRoutine());
    }

    public void EndInteraction()
    {
        if (!isInteracting) return;

        isInteracting = false;
        currentVirtualCamera = null;

        if (virtualCamera != null)
        {
            virtualCamera.Priority = 0;
        }

        if (interactionAnimationCoroutine != null)
        {
            StopCoroutine(interactionAnimationCoroutine);
            interactionAnimationCoroutine = null;
        }

        navAgent.isStopped = false;
        StartBehaviorCoroutines();
        StartCoroutine(ResetRotation());
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
                    socialDistSqr = socialDistance * socialDistance;
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
        EnsureVirtualCamera();
        return virtualCamera;
    }

    public static void PauseAllEnerlings()
    {
        var list = GetEnerlingsList();
        for (int i = 0; i < list.Count; i++)
        {
            var enerling = list[i];
            if (enerling != null && !enerling.isInteracting)
            {
                enerling.navAgent.isStopped = true;
                enerling.navAgent.ResetPath();

                if (enerling.animator != null)
                {
                    enerling.animator.SetBool(isWalkingHash, false);
                }
                enerling.wasMoving = false;

                enerling.StopBehaviorCoroutines();
            }
        }
    }

    public static void ResumeAllEnerlings()
    {
        var list = GetEnerlingsList();
        for (int i = 0; i < list.Count; i++)
        {
            var enerling = list[i];
            if (enerling != null && !enerling.isInteracting)
            {
                enerling.navAgent.isStopped = false;
                enerling.StartBehaviorCoroutines();
            }
        }
    }

    void OnDestroy()
    {
        if (allEnerlingsSet.Remove(this))
        {
            listDirty = true;
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