using UnityEngine;
using System.Collections;
using StarterAssets;

public class EnerlingPetManager : MonoBehaviour
{
    [System.Serializable]
    public class PetData
    {
        [Header("Pet Reference")]
        public Transform petTransform; // Reference to the actual pet model

        [Header("Pet Personality")]
        [Range(0, 1)] public float sidePreference = 0.5f; // 0 = left side, 1 = right side
        [Range(0.5f, 1.5f)] public float speedMultiplier = 1f; // Individual speed variation
        [Range(0.5f, 1.5f)] public float distanceMultiplier = 1f; // Individual distance variation
        [HideInInspector] public Vector3 randomOffset; // Random position offset
        [HideInInspector] public float randomPhase; // Random phase for movement
    }

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform; // Reference to the player

    [Header("Pets")]
    [SerializeField] private PetData[] pets = new PetData[2]; // Exactly 2 pets

    [Header("Pet Settings (Shared)")]
    [SerializeField] private float followDistance = 8.3f; // How far the pet stays from player
    [SerializeField] private float minDistance = 0f; // Minimum distance before moving closer
    [SerializeField] private float idealDistance = 0.92f; // Ideal distance when player is still
    [SerializeField] private float maxDistance = 10.65f; // Maximum distance before teleporting
    [SerializeField] private float moveForce = 34.7f; // Force applied to move pet
    [SerializeField] private float stoppingForce = 68.6f; // Extra force when trying to reach ideal distance
    [SerializeField] private float rotationSpeed = 11.43f; // Rotation speed
    [SerializeField] private float delayFactor = 0.3f; // How much delay in following (0-1)

    [Header("Jump Settings")]
    [SerializeField] private bool syncJumpWithPlayer = true; // Whether pet jumps when player jumps
    [SerializeField] private float jumpForce = 8f; // Jump force for pet

    [Header("Idle Movement")]
    [SerializeField] private bool enableIdleMovement = true; // Small idle movements
    [SerializeField] private float idleAmplitude = 0.05f; // How much idle movement
    [SerializeField] private float idleSpeed = 0.56f; // Speed of idle movement

    [Header("Collider Settings")]
    [SerializeField] private float colliderRadius = 0.56f; // Radius of capsule collider
    [SerializeField] private float colliderHeight = 0f; // Height of capsule collider (0 = use model height)
    [SerializeField] private Vector3 colliderCenter = Vector3.zero; // Center of capsule

    [Header("References")]
    [SerializeField] private string walkAnimationParameter = "Speed";
    [SerializeField] private string jumpAnimationParameter = "Jump";
    [SerializeField] private string groundedAnimationParameter = "Grounded";

    // Components for each pet
    private Rigidbody[] petRigidbodies;
    private CapsuleCollider[] petCapsuleColliders;
    private Animator[] petAnimators;

    // Pet state tracking
    private Vector3[] targetPositions;
    private Vector3[] lastPlayerPositions;
    private Vector3[] playerVelocities;
    private Vector3[] idleOffsets;
    private float[] idleTimers;
    private bool[] isJumping;
    private float[] playerSpeeds;
    private Vector3[] petVelocities;
    private Vector3[] lastPetPositions;

    // References to player components
    private ThirdPersonController playerController;
    private CharacterController playerCharacterController;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private float playerSpeed;
    private bool wasPlayerGrounded;

    void Start()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("EnerlingPetManager: No player transform assigned or found with tag 'Player'!");
                enabled = false;
                return;
            }
        }

        // Validate we have exactly 2 pets
        if (pets.Length != 2)
        {
            Debug.LogError("EnerlingPetManager: Exactly 2 pets required!");
            enabled = false;
            return;
        }

        // Initialize arrays
        int petCount = pets.Length;
        petRigidbodies = new Rigidbody[petCount];
        petCapsuleColliders = new CapsuleCollider[petCount];
        petAnimators = new Animator[petCount];
        targetPositions = new Vector3[petCount];
        lastPlayerPositions = new Vector3[petCount];
        playerVelocities = new Vector3[petCount];
        idleOffsets = new Vector3[petCount];
        idleTimers = new float[petCount];
        isJumping = new bool[petCount];
        playerSpeeds = new float[petCount];
        petVelocities = new Vector3[petCount];
        lastPetPositions = new Vector3[petCount];

        // Initialize each pet with random personality
        for (int i = 0; i < petCount; i++)
        {
            if (pets[i].petTransform == null)
            {
                Debug.LogError($"EnerlingPetManager: Pet {i} has no transform assigned!");
                enabled = false;
                return;
            }

            // Generate random offsets for natural variation
            pets[i].randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0,
                Random.Range(-0.5f, 0.5f)
            );
            pets[i].randomPhase = Random.Range(0f, Mathf.PI * 2f);

            // Set up Capsule Collider
            SetupPetCollider(i);

            // Set up Rigidbody
            SetupPetRigidbody(i);

            // Get Animator
            petAnimators[i] = pets[i].petTransform.GetComponent<Animator>();

            // Initialize positions
            lastPlayerPositions[i] = playerTransform.position;
            targetPositions[i] = playerTransform.position;
            lastPetPositions[i] = pets[i].petTransform.position;
            idleTimers[i] = pets[i].randomPhase;
        }

        // Get player components
        playerController = playerTransform.GetComponent<ThirdPersonController>();
        playerCharacterController = playerTransform.GetComponent<CharacterController>();

        // Initialize player tracking
        lastPlayerPosition = playerTransform.position;

        Debug.Log($"EnerlingPetManager initialized with {pets.Length} pets");
    }

    private void SetupPetCollider(int petIndex)
    {
        PetData pet = pets[petIndex];

        // Try to get existing collider
        petCapsuleColliders[petIndex] = pet.petTransform.GetComponent<CapsuleCollider>();

        if (petCapsuleColliders[petIndex] == null)
        {
            // Add new capsule collider
            petCapsuleColliders[petIndex] = pet.petTransform.gameObject.AddComponent<CapsuleCollider>();
        }

        // Configure collider
        petCapsuleColliders[petIndex].radius = colliderRadius;
        petCapsuleColliders[petIndex].height = colliderHeight > 0 ? colliderHeight : GetModelHeight(pet.petTransform);
        petCapsuleColliders[petIndex].center = colliderCenter;
        petCapsuleColliders[petIndex].isTrigger = false;
    }

    private float GetModelHeight(Transform model)
    {
        // Try to get height from renderer bounds
        Renderer renderer = model.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.y;
        }
        return 1f; // Default height
    }

    private void SetupPetRigidbody(int petIndex)
    {
        PetData pet = pets[petIndex];

        petRigidbodies[petIndex] = pet.petTransform.GetComponent<Rigidbody>();

        if (petRigidbodies[petIndex] == null)
        {
            petRigidbodies[petIndex] = pet.petTransform.gameObject.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody
        petRigidbodies[petIndex].mass = 1f;
        petRigidbodies[petIndex].linearDamping = 2f;
        petRigidbodies[petIndex].angularDamping = 2f;
        petRigidbodies[petIndex].constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        petRigidbodies[petIndex].collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        // Calculate player velocity and speed
        playerVelocity = (playerTransform.position - lastPlayerPosition) / Time.fixedDeltaTime;
        playerSpeed = playerVelocity.magnitude;
        lastPlayerPosition = playerTransform.position;

        // Sync jump with player for all pets
        if (syncJumpWithPlayer && playerController != null)
        {
            bool playerIsGrounded = IsPlayerGrounded();

            if (wasPlayerGrounded && !playerIsGrounded)
            {
                // Player just jumped - make all pets jump
                for (int i = 0; i < pets.Length; i++)
                {
                    MakePetJump(i);
                }
            }

            wasPlayerGrounded = playerIsGrounded;
        }

        // Update each pet independently
        for (int i = 0; i < pets.Length; i++)
        {
            if (pets[i].petTransform == null || petRigidbodies[i] == null) continue;

            UpdatePet(i);
        }
    }

    private void UpdatePet(int petIndex)
    {
        PetData pet = pets[petIndex];
        Rigidbody rb = petRigidbodies[petIndex];

        // Calculate pet velocity
        petVelocities[petIndex] = (pet.petTransform.position - lastPetPositions[petIndex]) / Time.fixedDeltaTime;
        lastPetPositions[petIndex] = pet.petTransform.position;

        // Calculate target position with personality
        CalculatePetTargetPosition(petIndex);

        // Calculate direction and distance to target
        Vector3 directionToTarget = targetPositions[petIndex] - pet.petTransform.position;
        float distanceToTarget = directionToTarget.magnitude;

        // Apply personality multipliers
        float personalityMoveForce = moveForce * pet.speedMultiplier;
        float personalityFollowDistance = followDistance * pet.distanceMultiplier;
        float personalityIdealDistance = idealDistance * pet.distanceMultiplier;

        // Determine force based on player movement
        float currentMoveForce = personalityMoveForce;
        float targetDistance = personalityFollowDistance;

        if (playerSpeed < 0.1f)
        {
            // Player is stopped - move to ideal distance
            targetDistance = personalityIdealDistance;
            currentMoveForce = stoppingForce * pet.speedMultiplier;
        }

        // Add random offset for natural positioning
        Vector3 randomPosition = playerTransform.position + pet.randomOffset;
        Vector3 blendedTarget = Vector3.Lerp(targetPositions[petIndex], randomPosition, 0.3f);

        // Adjust target position based on desired distance
        Vector3 directionFromPlayer = (blendedTarget - playerTransform.position).normalized;
        Vector3 adjustedTarget = playerTransform.position + directionFromPlayer * targetDistance;

        // Consider other pet's position to avoid crowding
        int otherPetIndex = petIndex == 0 ? 1 : 0;
        if (pets[otherPetIndex].petTransform != null)
        {
            Vector3 toOtherPet = pets[otherPetIndex].petTransform.position - adjustedTarget;
            if (toOtherPet.magnitude < 1.5f)
            {
                // Move away from other pet
                adjustedTarget -= toOtherPet.normalized * 0.5f;
            }
        }

        // Recalculate direction to adjusted target
        directionToTarget = adjustedTarget - pet.petTransform.position;
        distanceToTarget = directionToTarget.magnitude;

        // Apply force to move toward target
        if (distanceToTarget > 0.3f)
        {
            Vector3 moveDirection = directionToTarget.normalized;

            // Scale force based on distance
            float forceMultiplier = Mathf.Clamp01(distanceToTarget / personalityFollowDistance);
            Vector3 force = moveDirection * currentMoveForce * forceMultiplier;

            // Apply force to Rigidbody
            rb.AddForce(force, ForceMode.Force);

            // Limit velocity
            float maxSpeed = playerSpeed > 0.1f ? personalityMoveForce * 0.5f : stoppingForce * 0.3f * pet.speedMultiplier;
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }
        else if (playerSpeed < 0.1f && distanceToTarget < 0.5f)
        {
            // Player is stopped and pet is close - gentle damping to settle
            rb.linearVelocity *= 0.95f;
        }

        // Add idle movement if enabled and close enough
        if (enableIdleMovement && distanceToTarget < personalityFollowDistance * 0.8f && playerSpeed < 0.1f)
        {
            ApplyPetIdleMovement(petIndex);
        }

        // ALWAYS face the player first, then rotate to movement direction
        UpdatePetRotation(petIndex);

        // Update animator
        UpdatePetAnimator(petIndex);

        // Clamp distance to prevent pet from getting too far
        float distanceToPlayer = Vector3.Distance(pet.petTransform.position, playerTransform.position);
        if (distanceToPlayer > maxDistance)
        {
            TeleportPetToPlayer(petIndex);
        }
    }

    private void CalculatePetTargetPosition(int petIndex)
    {
        PetData pet = pets[petIndex];

        // Calculate base position relative to player
        Vector3 playerForward = playerTransform.forward;
        Vector3 playerRight = playerTransform.right;

        // Determine side based on sidePreference (0 = left, 1 = right)
        float sideMultiplier = Mathf.Lerp(-1f, 1f, pet.sidePreference);

        // Add some randomness to vertical positioning
        float verticalOffset = Mathf.Sin(Time.time * 0.5f + pet.randomPhase) * 0.1f;

        // Position pet behind and to the side with personality
        float personalityFollowDistance = followDistance * pet.distanceMultiplier;
        Vector3 offset = -playerForward * personalityFollowDistance * 0.7f
                        + playerRight * 1.2f * sideMultiplier
                        + Vector3.up * verticalOffset;

        Vector3 desiredPosition = playerTransform.position + offset;

        // Smooth the target position with some delay
        float smoothFactor = Time.fixedDeltaTime * (1f - delayFactor) * 10f;
        targetPositions[petIndex] = Vector3.Lerp(targetPositions[petIndex], desiredPosition, smoothFactor);
    }

    private void ApplyPetIdleMovement(int petIndex)
    {
        PetData pet = pets[petIndex];

        // Small idle movements with personality phase
        idleTimers[petIndex] += Time.fixedDeltaTime * idleSpeed;

        float idleX = Mathf.Sin(idleTimers[petIndex] + pet.randomPhase) * idleAmplitude;
        float idleZ = Mathf.Cos(idleTimers[petIndex] * 0.7f + pet.randomPhase) * idleAmplitude;

        idleOffsets[petIndex] = new Vector3(idleX, 0, idleZ);

        // Apply small force for idle movement
        petRigidbodies[petIndex].AddForce(idleOffsets[petIndex] * moveForce * 0.1f, ForceMode.Force);
    }

    private void UpdatePetRotation(int petIndex)
    {
        PetData pet = pets[petIndex];
        Rigidbody rb = petRigidbodies[petIndex];

        // FIRST: Always face the player
        Vector3 toPlayer = playerTransform.position - pet.petTransform.position;
        toPlayer.y = 0;

        if (toPlayer.magnitude > 0.1f)
        {
            Quaternion targetPlayerRotation = Quaternion.LookRotation(toPlayer.normalized);

            // If moving, blend between facing player and movement direction
            Vector3 movementDirection = rb.linearVelocity;
            movementDirection.y = 0;

            if (movementDirection.magnitude > 0.2f && playerSpeed > 0.1f)
            {
                // Blend between facing player and facing movement direction
                Quaternion targetMovementRotation = Quaternion.LookRotation(movementDirection.normalized);
                Quaternion blendedRotation = Quaternion.Slerp(targetPlayerRotation, targetMovementRotation, 0.3f);
                pet.petTransform.rotation = Quaternion.Slerp(pet.petTransform.rotation, blendedRotation, Time.fixedDeltaTime * rotationSpeed);
            }
            else
            {
                // Just face the player
                pet.petTransform.rotation = Quaternion.Slerp(pet.petTransform.rotation, targetPlayerRotation, Time.fixedDeltaTime * rotationSpeed * 0.5f);
            }
        }
    }

    private void UpdatePetAnimator(int petIndex)
    {
        if (petAnimators[petIndex] == null) return;

        // Calculate speed for walk animation
        float speed = petRigidbodies[petIndex].linearVelocity.magnitude;
        petAnimators[petIndex].SetFloat(walkAnimationParameter, speed);

        // Jump animation
        if (isJumping[petIndex])
        {
            petAnimators[petIndex].SetTrigger(jumpAnimationParameter);
            isJumping[petIndex] = false;
        }

        // Check if grounded for animator
        RaycastHit hit;
        Vector3 rayStart = pets[petIndex].petTransform.position + Vector3.up * 0.1f;
        bool isGrounded = Physics.Raycast(rayStart, Vector3.down, out hit, 0.3f);
        petAnimators[petIndex].SetBool(groundedAnimationParameter, isGrounded);
    }

    private bool IsPlayerGrounded()
    {
        if (playerController != null)
        {
            return playerController.Grounded;
        }
        else if (playerCharacterController != null)
        {
            return playerCharacterController.isGrounded;
        }
        return true;
    }

    private void MakePetJump(int petIndex)
    {
        // Check if pet is grounded
        RaycastHit hit;
        Vector3 rayStart = pets[petIndex].petTransform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 0.3f))
        {
            petRigidbodies[petIndex].AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping[petIndex] = true;
        }
    }

    private void TeleportPetToPlayer(int petIndex)
    {
        PetData pet = pets[petIndex];
        Rigidbody rb = petRigidbodies[petIndex];

        if (pet.petTransform != null && rb != null)
        {
            // Calculate side based on preference
            float sideMultiplier = Mathf.Lerp(-1f, 1f, pet.sidePreference);
            Vector3 offset = -playerTransform.forward * idealDistance + playerTransform.right * 1.2f * sideMultiplier;
            Vector3 teleportPosition = playerTransform.position + offset;

            // Reset velocity
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            pet.petTransform.position = teleportPosition;
            targetPositions[petIndex] = teleportPosition;
        }
    }

    // Public method to make all pets jump
    public void MakeAllPetsJump()
    {
        for (int i = 0; i < pets.Length; i++)
        {
            MakePetJump(i);
        }
    }

    // Public method to teleport all pets
    public void TeleportAllPetsToPlayer()
    {
        for (int i = 0; i < pets.Length; i++)
        {
            TeleportPetToPlayer(i);
        }
    }

    // Visualize in editor
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        // Draw follow distances
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, minDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerTransform.position, idealDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(playerTransform.position, followDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, maxDistance);

        // Draw each pet
        for (int i = 0; i < pets.Length; i++)
        {
            if (pets[i] != null && pets[i].petTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(playerTransform.position, pets[i].petTransform.position);

                // Draw capsule collider
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(pets[i].petTransform.position + pets[i].petTransform.rotation * colliderCenter, colliderRadius);
            }
        }
    }
}