using StarterAssets;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGroupManager : MonoBehaviour
{
    [Header("Group Settings")]
    [SerializeField] private Transform[] optionObjects;
    [SerializeField] private Transform groupStartingPoint;

    [Header("Food Assignment")]
    [SerializeField] private GameObject[] growFoodPrefabs;
    [SerializeField] private GameObject[] junkFoodPrefabs;
    [SerializeField] private float foodScale = 1.0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip dizzyAudioClip;
    [SerializeField] private AudioClip[] correctAudioClips;
    [SerializeField] private AudioClip[] incorrectAudioClips;
    [SerializeField] private GameObject dizzyEffect;

    [Header("Spawn Settings")]
    [SerializeField] private bool spawnOnGroupEntry = true;
    [SerializeField] private bool hideOnGroupExit = true;

    [Header("Delay Settings")]
    [SerializeField] private float beforeMoveDelay = 0.5f;
    [SerializeField] private float afterSmashDelay = 0.5f;
    [SerializeField] private float objectAnimationResetDelay = 1.3f;

    // Audio components
    private AudioSource audioSource;
    private AudioSource dizzyAudioSource;
    private Animator groupAnimator;

    // State
    private bool isActiveGroup = false;
    private int assignedGrowFoodIndex = -1;
    private ThirdPersonController playerController;
    private List<GameObject> spawnedFoods = new List<GameObject>();

    // Track food assignments
    private Dictionary<Transform, GameObject> assignedFoodPrefabs = new Dictionary<Transform, GameObject>();

    private void Start()
    {
        groupAnimator = GetComponent<Animator>();
        if (groupAnimator == null)
        {
            #if UNITY_EDITOR
            Debug.LogError($"No Animator component found on ObjectGroupManager: {gameObject.name}");
            #endif
        }

        // Hide all options initially
        SetGroupObjectsActive(false);

        // Initialize audio sources
        InitializeAudioSources();

        // Find player controller
        playerController = FindObjectOfType<ThirdPersonController>();

        // Initialize dizzy effect
        if (dizzyEffect != null)
        {
            dizzyEffect.SetActive(false);
        }

        // Auto-register with GrowAssessmentManager
        RegisterWithAssessmentManager();
    }

    private void RegisterWithAssessmentManager()
    {
        if (GrowAssessmentManager.Instance != null)
        {
            GrowAssessmentManager.Instance.RegisterGroupManager(this);
            #if UNITY_EDITOR
            Debug.Log($"Registered {gameObject.name} with GrowAssessmentManager");
            #endif
        }
    }

    private void InitializeAudioSources()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (playerController != null)
        {
            dizzyAudioSource = playerController.gameObject.AddComponent<AudioSource>();
            dizzyAudioSource.playOnAwake = false;
            dizzyAudioSource.loop = true;

            if (dizzyAudioClip != null)
            {
                dizzyAudioSource.clip = dizzyAudioClip;
            }
        }
    }

    public void ActivateGroup()
    {
        if (isActiveGroup) return;

        isActiveGroup = true;

        // Clear any existing spawned food
        DestroyAllSpawnedFood();

        // Clear previous assignments
        assignedFoodPrefabs.Clear();

        // Assign random grow food and junk foods
        AssignFoodTypes();

        // Show all objects
        SetGroupObjectsActive(true);

        // Spawn food for all objects
        SpawnFoodForAllObjects();

        #if UNITY_EDITOR
        Debug.Log($"Group {gameObject.name} activated. Grow food at index: {assignedGrowFoodIndex}");
        #endif
    }

    private void SpawnFoodForAllObjects()
    {
        for (int i = 0; i < optionObjects.Length; i++)
        {
            Transform option = optionObjects[i];
            if (option == null) continue;

            InteractiveObject interactiveObj = option.GetComponent<InteractiveObject>();
            if (interactiveObj != null && assignedFoodPrefabs.ContainsKey(option))
            {
                GameObject prefab = assignedFoodPrefabs[option];
                interactiveObj.SpawnAssignedFood(prefab);

                // Track the spawned food
                if (interactiveObj.GetSpawnedFood() != null)
                {
                    spawnedFoods.Add(interactiveObj.GetSpawnedFood());
                }
            }
        }

        #if UNITY_EDITOR
        Debug.Log($"Spawned food for {optionObjects.Length} objects in group: {gameObject.name}");
        #endif
    }

    public void DeactivateGroup()
    {
        if (!isActiveGroup) return;

        isActiveGroup = false;

        if (hideOnGroupExit)
        {
            SetGroupObjectsActive(false);
        }

        // Destroy all spawned food when deactivating
        DestroyAllSpawnedFood();

        #if UNITY_EDITOR
        Debug.Log($"Group {gameObject.name} deactivated");
        #endif
    }

    // Properly destroy all spawned food
    private void DestroyAllSpawnedFood()
    {
        // Destroy tracked spawned foods
        foreach (GameObject food in spawnedFoods)
        {
            if (food != null)
            {
                Destroy(food);
            }
        }
        spawnedFoods.Clear();

        // Also destroy food from interactive objects
        foreach (Transform option in optionObjects)
        {
            if (option != null)
            {
                InteractiveObject interactiveObj = option.GetComponent<InteractiveObject>();
                if (interactiveObj != null)
                {
                    interactiveObj.DestroySpawnedFood();
                }
            }
        }

        #if UNITY_EDITOR
        Debug.Log($"Destroyed all spawned food for group: {gameObject.name}");
        #endif
    }

    private void AssignFoodTypes()
    {
        if (optionObjects.Length != 3)
        {
            #if UNITY_EDITOR
            Debug.LogError("Group must have exactly 3 options!");
            #endif
            return;
        }

        assignedGrowFoodIndex = Random.Range(0, 3);

        for (int i = 0; i < optionObjects.Length; i++)
        {
            Transform option = optionObjects[i];
            if (option == null) continue;

            InteractiveObject interactiveObj = option.GetComponent<InteractiveObject>();
            if (interactiveObj != null)
            {
                if (i == assignedGrowFoodIndex)
                {
                    // This is the correct (grow) food
                    interactiveObj.SetIsGrowFood(true);
                    if (growFoodPrefabs.Length > 0)
                    {
                        GameObject selectedPrefab = growFoodPrefabs[Random.Range(0, growFoodPrefabs.Length)];
                        assignedFoodPrefabs[option] = selectedPrefab;
                        #if UNITY_EDITOR
                        Debug.Log($"Assigned grow food to option {i}: {selectedPrefab.name}");
                        #endif
                    }
                }
                else
                {
                    // These are wrong (junk) foods
                    interactiveObj.SetIsGrowFood(false);
                    if (junkFoodPrefabs.Length > 0)
                    {
                        GameObject selectedPrefab = junkFoodPrefabs[Random.Range(0, junkFoodPrefabs.Length)];
                        assignedFoodPrefabs[option] = selectedPrefab;
                        #if UNITY_EDITOR
                        Debug.Log($"Assigned junk food to option {i}: {selectedPrefab.name}");
                        #endif
                    }
                }

                // Set delay settings and animation times
                interactiveObj.SetDelaySettings(0f, beforeMoveDelay, afterSmashDelay);
                interactiveObj.SetAnimationExitTime(objectAnimationResetDelay);
                interactiveObj.SetGroupManager(this);
            }
        }
    }

    private void SetGroupObjectsActive(bool active)
    {
        foreach (Transform option in optionObjects)
        {
            if (option != null)
            {
                option.gameObject.SetActive(active);

                // Make sure interactive objects are set to interactable when activated
                if (active)
                {
                    InteractiveObject interactiveObj = option.GetComponent<InteractiveObject>();
                    if (interactiveObj != null)
                    {
                        interactiveObj.ResetObject();
                        interactiveObj.SetInteractable(true);
                    }
                }
            }
        }
    }

    // Get all interactive objects in this group
    public InteractiveObject[] GetAllInteractiveObjects()
    {
        List<InteractiveObject> objects = new List<InteractiveObject>();
        foreach (Transform option in optionObjects)
        {
            InteractiveObject obj = option.GetComponent<InteractiveObject>();
            if (obj != null)
            {
                objects.Add(obj);
            }
        }
        return objects.ToArray();
    }

    // Reset this group for new game
    public void ResetGroupForNewGame()
    {
        #if UNITY_EDITOR
        Debug.Log($"Resetting group: {gameObject.name}");
        #endif

        // Deactivate group
        DeactivateGroup();

        // Destroy all spawned food
        DestroyAllSpawnedFood();

        // Reset animator
        SetGroupEntryAnimation(false);

        // Reset assigned index
        assignedGrowFoodIndex = -1;

        // Clear food assignments
        assignedFoodPrefabs.Clear();

        #if UNITY_EDITOR
        Debug.Log($"Group {gameObject.name} reset for new game");
        #endif
    }

    public void SetGroupEntryAnimation(bool isEntry)
    {
        if (groupAnimator != null)
        {
            groupAnimator.SetBool("isEntry", isEntry);
        }
    }

    public void PlayCorrectAnswerAudio()
    {
        if (audioSource == null || correctAudioClips == null || correctAudioClips.Length == 0) return;
        AudioClip selectedClip = correctAudioClips[Random.Range(0, correctAudioClips.Length)];
        audioSource.PlayOneShot(selectedClip);
    }

    public void PlayIncorrectAnswerAudio()
    {
        if (audioSource == null || incorrectAudioClips == null || incorrectAudioClips.Length == 0) return;
        AudioClip selectedClip = incorrectAudioClips[Random.Range(0, incorrectAudioClips.Length)];
        audioSource.PlayOneShot(selectedClip);
    }

    public void PlayDizzyAudio()
    {
        if (dizzyAudioSource != null && dizzyAudioClip != null)
        {
            dizzyAudioSource.Play();
        }
    }

    public void StopDizzyAudio()
    {
        if (dizzyAudioSource != null && dizzyAudioSource.isPlaying)
        {
            dizzyAudioSource.Stop();
        }
    }

    public void EnableDizzyEffect()
    {
        if (dizzyEffect != null && !dizzyEffect.activeSelf)
        {
            dizzyEffect.SetActive(true);
        }
    }

    public void DisableDizzyEffect()
    {
        if (dizzyEffect != null && dizzyEffect.activeSelf)
        {
            dizzyEffect.SetActive(false);
        }
    }

    public Transform GetGroupStartingPoint()
    {
        return groupStartingPoint;
    }

    public bool IsActiveGroup()
    {
        return isActiveGroup;
    }

    public void OnPlayerEnterGroup()
    {
        if (spawnOnGroupEntry)
        {
            ActivateGroup();
        }
    }

    public void SetFoodScale(float scale)
    {
        foodScale = Mathf.Max(0.1f, scale);
    }

    public float GetFoodScale()
    {
        return foodScale;
    }

    public Transform GetFoodSpawnPoint(InteractiveObject interactiveObj)
    {
        return interactiveObj.GetFoodSpawnPoint();
    }

    // Helper method to get spawned food reference
    public GameObject GetSpawnedFoodForOption(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= optionObjects.Length)
            return null;

        Transform option = optionObjects[optionIndex];
        if (option == null)
            return null;

        InteractiveObject interactiveObj = option.GetComponent<InteractiveObject>();
        if (interactiveObj != null)
        {
            return interactiveObj.GetSpawnedFood();
        }

        return null;
    }

    // Method to scale spawned food (if needed)
    public void ScaleSpawnedFood(GameObject foodObject)
    {
        if (foodObject != null && foodScale != 1.0f)
        {
            foodObject.transform.localScale *= foodScale;
        }
    }

    // Get the assigned food prefab for an option
    public GameObject GetAssignedFoodPrefab(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= optionObjects.Length)
            return null;

        Transform option = optionObjects[optionIndex];
        if (option != null && assignedFoodPrefabs.ContainsKey(option))
        {
            return assignedFoodPrefabs[option];
        }

        return null;
    }
}
