using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;

public class EnerlingPetManager : MonoBehaviour
{
    [System.Serializable]
    public class PetData
    {
        [Header("Pet Reference")]
        public Transform petTransform; // Reference to the actual pet model
        public string petName; // Store the pet name for identification
        public bool isFlying; // Whether this is a flying pet
        public int spawnPointIndex; // Which spawn point index this pet uses
    }

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform; // Reference to the player

    [Header("Spawn Points")]
    [SerializeField] private Transform[] walkingSpawnPoints; // Spawn points for walking pets
    [SerializeField] private Transform[] flyingSpawnPoints; // Spawn points for flying pets

    [Header("Pets")]
    [SerializeField] public List<PetData> pets = new List<PetData>(); // Dynamic list of pets

    [Header("Pet Settings (Shared)")]
    [SerializeField] private float followDistance = 8.3f;
    [SerializeField] private float minDistance = 0f;
    [SerializeField] private float idealDistance = 0.92f;
    [SerializeField] private float maxDistance = 10.65f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float stoppingSpeed = 3f;
    [SerializeField] private float rotationSpeed = 11.43f;
    [SerializeField] private float delayFactor = 0.3f;
    [SerializeField] private float arrivalThreshold = 0.5f;

    [Header("Idle Positioning")]
    [SerializeField] private float idleSideOffset = 1.2f; // How far to the side pets stay when idle
    [SerializeField] private float idleForwardOffset = -0.7f; // How far behind the player (negative = behind)

    [Header("Jump Settings")]
    [SerializeField] private bool syncJumpWithPlayer = true;
    [SerializeField] private float jumpForce = 8f;

    [Header("Idle Movement")]
    [SerializeField] private bool enableIdleMovement = true;
    [SerializeField] private float idleAmplitude = 0.05f;
    [SerializeField] private float idleSpeed = 0.56f;

    [Header("Flying Settings")]
    [SerializeField] private float flyingHeightOffset = 1.5f;
    [SerializeField] private float flyingBobAmount = 0.2f;
    [SerializeField] private float flyingBobSpeed = 1.5f;
    [SerializeField] private float flyingSmoothTime = 0.3f;

    [Header("Collider Settings")]
    [SerializeField] private float colliderRadius = 0.56f;
    [SerializeField] private float colliderHeight = 0f;
    [SerializeField] private Vector3 colliderCenter = Vector3.zero;

    [Header("Animation")]
    [SerializeField] private string walkAnimationParameter = "isWalking";

    [Header("Platform Mode")]
    [SerializeField] private bool isInPlatformMode = false;

    // Components for each pet
    private List<Rigidbody> petRigidbodies;
    private List<CapsuleCollider> petCapsuleColliders;
    private List<Animator> petAnimators;
    private List<Vector3> originalSpawnPositions;

    // Pet state tracking
    private List<Vector3> targetPositions;
    private List<Vector3> lastPlayerPositions;
    private List<Vector3> playerVelocities;
    private List<Vector3> idleOffsets;
    private List<float> idleTimers;
    private List<bool> isJumping;
    private List<float> playerSpeeds;
    private List<Vector3> petVelocities;
    private List<Vector3> lastPetPositions;
    private List<float> flyingBobTimers;

    // Smoothing for flying pets - using separate arrays for ref parameters
    private List<Vector3> flyingCurrentVelocities;

    // Movement state
    private List<bool> wasMoving;
    private List<float> distanceToTargets;

    // References to player components
    private ThirdPersonController playerController;
    private CharacterController playerCharacterController;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private float playerSpeed;
    private bool wasPlayerGrounded;

    // Layer for pets
    private int enerlingOnlyLayer;

    // Flying pet names list
    private static readonly HashSet<string> FlyingPetNames = new HashSet<string>
    {
        "Aspartame", "Sodium Benzoate", "Sorbitol", "Folic Acid"
    };

    void Awake()
    {
        enerlingOnlyLayer = LayerMask.NameToLayer("EnerlingOnly");
        if (enerlingOnlyLayer == -1)
        {
            Debug.LogWarning("Layer 'EnerlingOnly' not found! Using default layer.");
            enerlingOnlyLayer = 0;
        }

        // Initialize lists
        pets = new List<PetData>();
        petRigidbodies = new List<Rigidbody>();
        petCapsuleColliders = new List<CapsuleCollider>();
        petAnimators = new List<Animator>();
        originalSpawnPositions = new List<Vector3>();
        targetPositions = new List<Vector3>();
        lastPlayerPositions = new List<Vector3>();
        playerVelocities = new List<Vector3>();
        idleOffsets = new List<Vector3>();
        idleTimers = new List<float>();
        isJumping = new List<bool>();
        playerSpeeds = new List<float>();
        petVelocities = new List<Vector3>();
        lastPetPositions = new List<Vector3>();
        flyingBobTimers = new List<float>();
        flyingCurrentVelocities = new List<Vector3>();
        wasMoving = new List<bool>();
        distanceToTargets = new List<float>();
    }

    void Start()
    {
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

        playerController = playerTransform.GetComponent<ThirdPersonController>();
        playerCharacterController = playerTransform.GetComponent<CharacterController>();

        lastPlayerPosition = playerTransform.position;

        LoadEquippedPets();

        Debug.Log($"EnerlingPetManager initialized with {pets.Count} pets");
    }

    public void LoadEquippedPets()
    {
        ClearAllPets();

        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
            return;

        string pet1 = GameDataManager.Instance.CurrentGameData.equippedPetSlot1;
        string pet2 = GameDataManager.Instance.CurrentGameData.equippedPetSlot2;

        if (!string.IsNullOrEmpty(pet1))
        {
            SpawnPet(pet1, 0);
        }

        if (!string.IsNullOrEmpty(pet2))
        {
            SpawnPet(pet2, 1);
        }
    }

    public void SpawnPet(string petName, int slotIndex)
    {
        IngredientDatabase db = FindObjectOfType<EnerlingSelectionController>()?.ingredientDatabase;
        if (db == null)
        {
            Debug.LogError("Cannot spawn pet: IngredientDatabase not found");
            return;
        }

        var ingredient = db.GetIngredientInfo(petName);
        if (ingredient == null || ingredient.modelPrefab == null)
        {
            Debug.LogError($"Cannot spawn pet: {petName} not found or has no prefab");
            return;
        }

        // Remove existing pet in this slot
        for (int i = 0; i < pets.Count; i++)
        {
            if (i == slotIndex && pets[i] != null)
            {
                if (pets[i].petTransform != null)
                    Destroy(pets[i].petTransform.gameObject);
                pets.RemoveAt(i);
                RemovePetComponents(i);
                break;
            }
        }

        bool isFlying = FlyingPetNames.Contains(petName);
        Transform[] spawnPoints = isFlying ? flyingSpawnPoints : walkingSpawnPoints;

        if (spawnPoints == null || spawnPoints.Length == 0 || slotIndex >= spawnPoints.Length)
        {
            Debug.LogError($"No spawn point available for slot {slotIndex}");
            return;
        }

        Transform spawnPoint = spawnPoints[slotIndex];

        // Clean up existing pet at this spawn point
        foreach (var child in spawnPoint.GetComponentsInChildren<Transform>())
        {
            if (child != spawnPoint)
            {
                Destroy(child.gameObject);
            }
        }

        PetData newPet = new PetData();
        newPet.petName = petName;
        newPet.isFlying = isFlying;
        newPet.spawnPointIndex = slotIndex;

        GameObject petObj = Instantiate(ingredient.modelPrefab, spawnPoint);
        petObj.transform.localPosition = Vector3.zero;
        petObj.transform.localRotation = Quaternion.identity;

        SetLayerRecursively(petObj, enerlingOnlyLayer);
        newPet.petTransform = petObj.transform;

        // Add to lists
        if (slotIndex < pets.Count)
        {
            pets[slotIndex] = newPet;
            UpdatePetComponents(slotIndex);
        }
        else
        {
            while (pets.Count < slotIndex)
            {
                pets.Add(null);
                AddEmptyComponents();
            }
            pets.Add(newPet);

            petRigidbodies.Add(null);
            petCapsuleColliders.Add(null);
            petAnimators.Add(null);
            originalSpawnPositions.Add(spawnPoint.position);
            targetPositions.Add(playerTransform.position);
            lastPlayerPositions.Add(playerTransform.position);
            playerVelocities.Add(Vector3.zero);
            idleOffsets.Add(Vector3.zero);
            idleTimers.Add(Random.Range(0f, Mathf.PI * 2f));
            isJumping.Add(false);
            playerSpeeds.Add(0);
            petVelocities.Add(Vector3.zero);
            lastPetPositions.Add(petObj.transform.position);
            flyingBobTimers.Add(Random.Range(0f, Mathf.PI * 2f));
            flyingCurrentVelocities.Add(Vector3.zero);
            wasMoving.Add(false);
            distanceToTargets.Add(0f);

            SetupPetComponents(pets.Count - 1);
        }

        Debug.Log($"Spawned pet {petName} in slot {slotIndex} at {spawnPoint.name} ({(isFlying ? "Flying" : "Walking")})");
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void AddEmptyComponents()
    {
        petRigidbodies.Add(null);
        petCapsuleColliders.Add(null);
        petAnimators.Add(null);
        originalSpawnPositions.Add(Vector3.zero);
        targetPositions.Add(Vector3.zero);
        lastPlayerPositions.Add(Vector3.zero);
        playerVelocities.Add(Vector3.zero);
        idleOffsets.Add(Vector3.zero);
        idleTimers.Add(0);
        isJumping.Add(false);
        playerSpeeds.Add(0);
        petVelocities.Add(Vector3.zero);
        lastPetPositions.Add(Vector3.zero);
        flyingBobTimers.Add(0);
        flyingCurrentVelocities.Add(Vector3.zero);
        wasMoving.Add(false);
        distanceToTargets.Add(0f);
    }

    private void RemovePetComponents(int index)
    {
        if (index < petRigidbodies.Count)
        {
            petRigidbodies.RemoveAt(index);
            petCapsuleColliders.RemoveAt(index);
            petAnimators.RemoveAt(index);
            originalSpawnPositions.RemoveAt(index);
            targetPositions.RemoveAt(index);
            lastPlayerPositions.RemoveAt(index);
            playerVelocities.RemoveAt(index);
            idleOffsets.RemoveAt(index);
            idleTimers.RemoveAt(index);
            isJumping.RemoveAt(index);
            playerSpeeds.RemoveAt(index);
            petVelocities.RemoveAt(index);
            lastPetPositions.RemoveAt(index);
            flyingBobTimers.RemoveAt(index);
            flyingCurrentVelocities.RemoveAt(index);
            wasMoving.RemoveAt(index);
            distanceToTargets.RemoveAt(index);
        }
    }

    private void SetupPetComponents(int petIndex)
    {
        PetData pet = pets[petIndex];

        SetupPetCollider(petIndex);
        SetupPetRigidbody(petIndex);
        petAnimators[petIndex] = pet.petTransform.GetComponent<Animator>();

        // Initialize animation state
        if (petAnimators[petIndex] != null)
        {
            petAnimators[petIndex].SetBool(walkAnimationParameter, false);
        }
    }

    private void UpdatePetComponents(int petIndex)
    {
        PetData pet = pets[petIndex];

        SetupPetCollider(petIndex);
        SetupPetRigidbody(petIndex);
        petAnimators[petIndex] = pet.petTransform.GetComponent<Animator>();
        lastPetPositions[petIndex] = pet.petTransform.position;
        targetPositions[petIndex] = playerTransform.position;
    }

    private void SetupPetCollider(int petIndex)
    {
        PetData pet = pets[petIndex];

        petCapsuleColliders[petIndex] = pet.petTransform.GetComponent<CapsuleCollider>();

        if (petCapsuleColliders[petIndex] == null)
        {
            petCapsuleColliders[petIndex] = pet.petTransform.gameObject.AddComponent<CapsuleCollider>();
        }

        petCapsuleColliders[petIndex].radius = colliderRadius;
        petCapsuleColliders[petIndex].height = colliderHeight > 0 ? colliderHeight : GetModelHeight(pet.petTransform);
        petCapsuleColliders[petIndex].center = colliderCenter;
        petCapsuleColliders[petIndex].isTrigger = false;
    }

    private float GetModelHeight(Transform model)
    {
        Renderer renderer = model.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.y;
        }
        return 1f;
    }

    private void SetupPetRigidbody(int petIndex)
    {
        PetData pet = pets[petIndex];

        petRigidbodies[petIndex] = pet.petTransform.GetComponent<Rigidbody>();

        if (petRigidbodies[petIndex] == null)
        {
            petRigidbodies[petIndex] = pet.petTransform.gameObject.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody based on pet type
        if (pet.isFlying)
        {
            // Flying pets: no gravity, minimal drag
            petRigidbodies[petIndex].useGravity = false;
            petRigidbodies[petIndex].mass = 0.5f;
            petRigidbodies[petIndex].linearDamping = 5f;
            petRigidbodies[petIndex].angularDamping = 5f;
            petRigidbodies[petIndex].constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            // Walking pets: normal physics but with damping to prevent bouncing
            petRigidbodies[petIndex].useGravity = true;
            petRigidbodies[petIndex].mass = 1f;
            petRigidbodies[petIndex].linearDamping = 3f;
            petRigidbodies[petIndex].angularDamping = 3f;
            petRigidbodies[petIndex].constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        petRigidbodies[petIndex].collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void ClearAllPets()
    {
        foreach (var pet in pets)
        {
            if (pet != null && pet.petTransform != null)
                Destroy(pet.petTransform.gameObject);
        }

        pets.Clear();
        petRigidbodies.Clear();
        petCapsuleColliders.Clear();
        petAnimators.Clear();
        originalSpawnPositions.Clear();
        targetPositions.Clear();
        lastPlayerPositions.Clear();
        playerVelocities.Clear();
        idleOffsets.Clear();
        idleTimers.Clear();
        isJumping.Clear();
        playerSpeeds.Clear();
        petVelocities.Clear();
        lastPetPositions.Clear();
        flyingBobTimers.Clear();
        flyingCurrentVelocities.Clear();
        wasMoving.Clear();
        distanceToTargets.Clear();
    }

    public void SetPlatformMode(bool inPlatform)
    {
        isInPlatformMode = inPlatform;

        for (int i = 0; i < pets.Count; i++)
        {
            if (pets[i] == null || pets[i].petTransform == null) continue;

            if (inPlatform)
            {
                originalSpawnPositions[i] = pets[i].petTransform.position;

                Transform spawnPoint = pets[i].isFlying ? flyingSpawnPoints[pets[i].spawnPointIndex] : walkingSpawnPoints[pets[i].spawnPointIndex];
                pets[i].petTransform.SetParent(spawnPoint);
                pets[i].petTransform.localPosition = Vector3.zero;
                pets[i].petTransform.localRotation = Quaternion.identity;

                if (petRigidbodies[i] != null)
                {
                    petRigidbodies[i].isKinematic = true;
                    petRigidbodies[i].linearVelocity = Vector3.zero;
                }

                // Stop walking animation
                if (petAnimators[i] != null)
                {
                    petAnimators[i].SetBool(walkAnimationParameter, false);
                }
            }
            else
            {
                if (petRigidbodies[i] != null)
                {
                    petRigidbodies[i].isKinematic = false;
                }

                pets[i].petTransform.SetParent(transform);
            }
        }
    }

    public void RemovePet(int slotIndex)
    {
        if (slotIndex >= pets.Count || pets[slotIndex] == null) return;

        if (pets[slotIndex].petTransform != null)
            Destroy(pets[slotIndex].petTransform.gameObject);

        pets.RemoveAt(slotIndex);
        RemovePetComponents(slotIndex);

        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            if (slotIndex == 0)
                GameDataManager.Instance.CurrentGameData.equippedPetSlot1 = "";
            else if (slotIndex == 1)
                GameDataManager.Instance.CurrentGameData.equippedPetSlot2 = "";

            GameDataManager.Instance.SaveGameData();
        }

        Debug.Log($"Removed pet from slot {slotIndex}");
    }

    void FixedUpdate()
    {
        if (playerTransform == null || isInPlatformMode) return;

        playerVelocity = (playerTransform.position - lastPlayerPosition) / Time.fixedDeltaTime;
        playerSpeed = playerVelocity.magnitude;
        lastPlayerPosition = playerTransform.position;

        if (syncJumpWithPlayer && playerController != null)
        {
            bool playerIsGrounded = IsPlayerGrounded();

            if (wasPlayerGrounded && !playerIsGrounded)
            {
                for (int i = 0; i < pets.Count; i++)
                {
                    if (pets[i] != null && !pets[i].isFlying) // Only walking pets jump
                        MakePetJump(i);
                }
            }

            wasPlayerGrounded = playerIsGrounded;
        }

        for (int i = 0; i < pets.Count; i++)
        {
            if (pets[i] == null || pets[i].petTransform == null || petRigidbodies[i] == null) continue;

            UpdatePet(i);
        }
    }

    private void UpdatePet(int petIndex)
    {
        PetData pet = pets[petIndex];
        Rigidbody rb = petRigidbodies[petIndex];

        // Calculate target position
        CalculatePetTargetPosition(petIndex);

        // Calculate desired position (with height offset for flying)
        Vector3 desiredPosition = targetPositions[petIndex];

        if (pet.isFlying)
        {
            desiredPosition.y = playerTransform.position.y + flyingHeightOffset;

            // Add bobbing motion
            flyingBobTimers[petIndex] += Time.fixedDeltaTime * flyingBobSpeed;
            float bobOffset = Mathf.Sin(flyingBobTimers[petIndex]) * flyingBobAmount;
            desiredPosition.y += bobOffset;
        }

        // Calculate direction and distance
        Vector3 directionToTarget = desiredPosition - pet.petTransform.position;
        float distanceToTarget = directionToTarget.magnitude;
        distanceToTargets[petIndex] = distanceToTarget;

        // Determine if moving
        bool isMoving = distanceToTarget > arrivalThreshold;

        // Update animation
        if (petAnimators[petIndex] != null)
        {
            petAnimators[petIndex].SetBool(walkAnimationParameter, isMoving);
        }

        if (isMoving)
        {
            Vector3 moveDirection = directionToTarget.normalized;

            // Calculate target velocity
            float currentSpeed = playerSpeed > 0.1f ? moveSpeed : stoppingSpeed;
            Vector3 targetVelocity = moveDirection * currentSpeed;

            if (pet.isFlying)
            {
                // Smooth movement for flying pets - get current velocity from list
                Vector3 currentVelocity = flyingCurrentVelocities[petIndex];

                // SmoothDamp
                rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity,
                    ref currentVelocity, flyingSmoothTime);

                // Store the updated velocity back in the list
                flyingCurrentVelocities[petIndex] = currentVelocity;
            }
            else
            {
                // Walking pets - use forces but with damping
                Vector3 force = moveDirection * currentSpeed * rb.mass * 5f;

                // Scale force based on distance (less force when close)
                float forceScale = Mathf.Clamp01(distanceToTarget / followDistance);
                force *= forceScale;

                rb.AddForce(force, ForceMode.Force);

                // Strong damping to prevent overshooting
                rb.linearVelocity *= 0.95f;

                // Limit maximum speed
                float maxSpeed = currentSpeed;
                if (rb.linearVelocity.magnitude > maxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                }
            }
        }
        else
        {
            // Not moving - strong damping to stop quickly
            rb.linearVelocity *= 0.9f;

            if (rb.linearVelocity.magnitude < 0.1f)
            {
                rb.linearVelocity = Vector3.zero;
            }

            // Add idle movement if enabled and player is still
            if (enableIdleMovement && playerSpeed < 0.1f && !pet.isFlying)
            {
                ApplyPetIdleMovement(petIndex);
            }
        }

        // Update rotation
        UpdatePetRotation(petIndex);

        // Check distance limit
        float distanceToPlayer = Vector3.Distance(pet.petTransform.position, playerTransform.position);
        if (distanceToPlayer > maxDistance)
        {
            TeleportPetToPlayer(petIndex);
        }
    }

    private void CalculatePetTargetPosition(int petIndex)
    {
        PetData pet = pets[petIndex];

        Vector3 playerForward = playerTransform.forward;
        Vector3 playerRight = playerTransform.right;

        float sideMultiplier = pet.spawnPointIndex == 0 ? -1f : 1f;

        // Use idle offsets when player is stopped, otherwise use follow distance
        float currentForwardOffset = playerSpeed > 0.1f ? -followDistance * 0.7f : idleForwardOffset;
        float currentSideOffset = playerSpeed > 0.1f ? 1.2f : idleSideOffset;

        Vector3 offset = playerForward * currentForwardOffset
                        + playerRight * currentSideOffset * sideMultiplier;

        Vector3 desiredPosition = playerTransform.position + offset;

        float smoothFactor = Time.fixedDeltaTime * (1f - delayFactor) * 5f;
        targetPositions[petIndex] = Vector3.Lerp(targetPositions[petIndex], desiredPosition, smoothFactor);
    }

    private void ApplyPetIdleMovement(int petIndex)
    {
        PetData pet = pets[petIndex];

        idleTimers[petIndex] += Time.fixedDeltaTime * idleSpeed;

        float idleX = Mathf.Sin(idleTimers[petIndex]) * idleAmplitude;
        float idleZ = Mathf.Cos(idleTimers[petIndex] * 0.7f) * idleAmplitude;

        idleOffsets[petIndex] = new Vector3(idleX, 0, idleZ);

        if (!pet.isFlying)
        {
            petRigidbodies[petIndex].AddForce(idleOffsets[petIndex] * moveSpeed * 0.1f, ForceMode.Force);
        }
    }

    private void UpdatePetRotation(int petIndex)
    {
        PetData pet = pets[petIndex];
        Rigidbody rb = petRigidbodies[petIndex];

        Vector3 toPlayer = playerTransform.position - pet.petTransform.position;
        toPlayer.y = 0;

        if (toPlayer.magnitude > 0.1f)
        {
            Quaternion targetPlayerRotation = Quaternion.LookRotation(toPlayer.normalized);

            if (!pet.isFlying && rb.linearVelocity.magnitude > 0.2f && playerSpeed > 0.1f)
            {
                Vector3 movementDirection = rb.linearVelocity;
                movementDirection.y = 0;

                if (movementDirection.magnitude > 0.2f)
                {
                    Quaternion targetMovementRotation = Quaternion.LookRotation(movementDirection.normalized);
                    Quaternion blendedRotation = Quaternion.Slerp(targetPlayerRotation, targetMovementRotation, 0.3f);
                    pet.petTransform.rotation = Quaternion.Slerp(pet.petTransform.rotation, blendedRotation, Time.fixedDeltaTime * rotationSpeed);
                }
            }
            else
            {
                pet.petTransform.rotation = Quaternion.Slerp(pet.petTransform.rotation, targetPlayerRotation, Time.fixedDeltaTime * rotationSpeed * 0.5f);
            }
        }
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
        if (pets[petIndex].isFlying) return; // Flying pets don't jump

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
            float sideMultiplier = pet.spawnPointIndex == 0 ? -1f : 1f;
            Vector3 offset = -playerTransform.forward * idealDistance + playerTransform.right * 1.2f * sideMultiplier;

            if (pet.isFlying)
            {
                offset.y += flyingHeightOffset;
            }

            Vector3 teleportPosition = playerTransform.position + offset;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            pet.petTransform.position = teleportPosition;
            targetPositions[petIndex] = teleportPosition;
        }
    }

    public void MakeAllPetsJump()
    {
        for (int i = 0; i < pets.Count; i++)
        {
            if (pets[i] != null && !pets[i].isFlying)
                MakePetJump(i);
        }
    }

    public void TeleportAllPetsToPlayer()
    {
        for (int i = 0; i < pets.Count; i++)
        {
            if (pets[i] != null)
                TeleportPetToPlayer(i);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, minDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerTransform.position, idealDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(playerTransform.position, followDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, maxDistance);

        // Draw idle positions
        Gizmos.color = Color.magenta;
        Vector3 leftIdlePos = playerTransform.position + playerTransform.forward * idleForwardOffset + playerTransform.right * -idleSideOffset;
        Vector3 rightIdlePos = playerTransform.position + playerTransform.forward * idleForwardOffset + playerTransform.right * idleSideOffset;
        Gizmos.DrawWireSphere(leftIdlePos, 0.3f);
        Gizmos.DrawWireSphere(rightIdlePos, 0.3f);

        for (int i = 0; i < pets.Count; i++)
        {
            if (pets[i] != null && pets[i].petTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(playerTransform.position, pets[i].petTransform.position);

                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(pets[i].petTransform.position + pets[i].petTransform.rotation * colliderCenter, colliderRadius);
            }
        }
    }
}