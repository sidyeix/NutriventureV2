using StarterAssets;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGroupManager : MonoBehaviour
{
    [Header("Group Settings")]
    [SerializeField] private Transform[] optionObjects; // The 3 options in this group
    [SerializeField] private Transform groupStartingPoint; // Where player enters this group

    [Header("Food Assignment")]
    [SerializeField] private GameObject[] growFoodPrefabs;
    [SerializeField] private GameObject[] junkFoodPrefabs;
    [SerializeField] private float foodScale = 1.0f; // NEW: Food scale multiplier

    [Header("Audio Settings")]
    [SerializeField] private AudioClip dizzyAudioClip; // Dizzy audio clip
    [SerializeField] private AudioClip[] correctAudioClips; // Audio clips for correct answers
    [SerializeField] private AudioClip[] incorrectAudioClips; // Audio clips for incorrect answers
    [SerializeField] private GameObject dizzyEffect; // Dizzy effect GameObject (enable/disable)

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
    private Animator groupAnimator; // Animator on THIS GameObject

    // State
    private bool isActiveGroup = false;
    private int assignedGrowFoodIndex = -1;
    private ThirdPersonController playerController;

    private void Start()
    {
        // Get the animator on THIS GameObject
        groupAnimator = GetComponent<Animator>();
        if (groupAnimator == null)
        {
            Debug.LogError($"No Animator component found on ObjectGroupManager: {gameObject.name}");
        }

        // Hide all options initially
        SetGroupObjectsActive(false);

        // Initialize audio sources
        InitializeAudioSources();

        // Find player controller
        playerController = FindObjectOfType<ThirdPersonController>();

        // Initialize dizzy effect - disable it at start
        if (dizzyEffect != null)
        {
            dizzyEffect.SetActive(false);
        }
    }

    private void InitializeAudioSources()
    {
        // Create main audio source for this manager
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound for feedback

        // Create dizzy audio source on the player if available
        if (playerController != null)
        {
            dizzyAudioSource = playerController.gameObject.AddComponent<AudioSource>();
            dizzyAudioSource.playOnAwake = false;
            dizzyAudioSource.loop = true; // Loop while dizzy

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

        // Assign random grow food and junk foods
        AssignFoodTypes();

        // Show all objects
        SetGroupObjectsActive(true);

        Debug.Log($"Group {gameObject.name} activated. Grow food at index: {assignedGrowFoodIndex}");
    }

    public void DeactivateGroup()
    {
        if (!isActiveGroup) return;

        isActiveGroup = false;

        if (hideOnGroupExit)
        {
            SetGroupObjectsActive(false);
        }

        Debug.Log($"Group {gameObject.name} deactivated");
    }

    private void AssignFoodTypes()
    {
        if (optionObjects.Length != 3)
        {
            Debug.LogError("Group must have exactly 3 options!");
            return;
        }

        // Randomly select which option gets the grow food (0, 1, or 2)
        assignedGrowFoodIndex = Random.Range(0, 3);

        for (int i = 0; i < optionObjects.Length; i++)
        {
            InteractiveObject interactiveObj = optionObjects[i].GetComponent<InteractiveObject>();
            if (interactiveObj != null)
            {
                if (i == assignedGrowFoodIndex)
                {
                    // Assign as grow food
                    interactiveObj.SetIsGrowFood(true);

                    // Randomly select a grow food prefab
                    if (growFoodPrefabs.Length > 0)
                    {
                        GameObject selectedPrefab = growFoodPrefabs[Random.Range(0, growFoodPrefabs.Length)];
                        interactiveObj.SetFoodPrefab(selectedPrefab);
                        // Spawn food immediately with scale
                        SpawnFoodWithScale(interactiveObj, selectedPrefab);
                    }
                }
                else
                {
                    // Assign as junk food
                    interactiveObj.SetIsGrowFood(false);

                    // Randomly select a junk food prefab
                    if (junkFoodPrefabs.Length > 0)
                    {
                        GameObject selectedPrefab = junkFoodPrefabs[Random.Range(0, junkFoodPrefabs.Length)];
                        interactiveObj.SetFoodPrefab(selectedPrefab);
                        // Spawn food immediately with scale
                        SpawnFoodWithScale(interactiveObj, selectedPrefab);
                    }
                }

                // Pass delay settings
                interactiveObj.SetDelaySettings(0f, beforeMoveDelay, afterSmashDelay);
                interactiveObj.SetAnimationExitTime(objectAnimationResetDelay);

                // Set reference to this manager
                interactiveObj.SetGroupManager(this);
            }
        }
    }

    // NEW: Method to spawn food with custom scale
    private void SpawnFoodWithScale(InteractiveObject interactiveObj, GameObject foodPrefab)
    {
        Transform foodSpawnPoint = interactiveObj.GetFoodSpawnPoint();
        if (foodPrefab != null && foodSpawnPoint != null)
        {
            GameObject spawnedFood = Instantiate(foodPrefab, foodSpawnPoint.position, Quaternion.identity, foodSpawnPoint);

            // Apply custom scale
            if (foodScale != 1.0f)
            {
                spawnedFood.transform.localScale *= foodScale;
                Debug.Log($"Applied scale {foodScale} to {interactiveObj.gameObject.name}'s food");
            }

            // Store reference in interactive object if needed
            interactiveObj.SetSpawnedFood(spawnedFood);
        }
    }

    private void SetGroupObjectsActive(bool active)
    {
        foreach (Transform option in optionObjects)
        {
            if (option != null)
            {
                option.gameObject.SetActive(active);
            }
        }
    }

    // PUBLIC METHOD: Set isEntry on THIS GameObject's animator
    public void SetGroupEntryAnimation(bool isEntry)
    {
        if (groupAnimator != null)
        {
            groupAnimator.SetBool("isEntry", isEntry);
            Debug.Log($"Set isEntry = {isEntry} on ObjectGroupManager: {gameObject.name}");
        }
        else
        {
            Debug.LogError($"Cannot set isEntry: No Animator found on ObjectGroupManager: {gameObject.name}");
        }
    }

    // AUDIO METHODS
    public void PlayCorrectAnswerAudio()
    {
        if (audioSource == null) return;

        if (correctAudioClips != null && correctAudioClips.Length > 0)
        {
            // Randomly select a correct audio clip
            AudioClip selectedClip = correctAudioClips[Random.Range(0, correctAudioClips.Length)];
            audioSource.PlayOneShot(selectedClip);
            Debug.Log("Playing correct answer audio: " + selectedClip.name);
        }
        else
        {
            Debug.LogWarning("No correct audio clips assigned!");
        }
    }

    public void PlayIncorrectAnswerAudio()
    {
        if (audioSource == null) return;

        if (incorrectAudioClips != null && incorrectAudioClips.Length > 0)
        {
            // Randomly select an incorrect audio clip
            AudioClip selectedClip = incorrectAudioClips[Random.Range(0, incorrectAudioClips.Length)];
            audioSource.PlayOneShot(selectedClip);
            Debug.Log("Playing incorrect answer audio: " + selectedClip.name);
        }
        else
        {
            Debug.LogWarning("No incorrect audio clips assigned!");
        }
    }

    // DIZZY EFFECT METHODS - SIMPLE ENABLE/DISABLE
    public void PlayDizzyAudio()
    {
        if (dizzyAudioSource != null && dizzyAudioClip != null)
        {
            dizzyAudioSource.Play();
            Debug.Log("Started dizzy audio");
        }
        else
        {
            Debug.LogWarning("Dizzy audio source or clip not available!");
        }
    }

    public void StopDizzyAudio()
    {
        if (dizzyAudioSource != null && dizzyAudioSource.isPlaying)
        {
            dizzyAudioSource.Stop();
            Debug.Log("Stopped dizzy audio");
        }
    }

    public void EnableDizzyEffect()
    {
        if (dizzyEffect != null && !dizzyEffect.activeSelf)
        {
            dizzyEffect.SetActive(true);
            Debug.Log("Enabled dizzy effect");
        }
    }

    public void DisableDizzyEffect()
    {
        if (dizzyEffect != null && dizzyEffect.activeSelf)
        {
            dizzyEffect.SetActive(false);
            Debug.Log("Disabled dizzy effect");
        }
    }

    // PUBLIC METHODS
    public Transform GetGroupStartingPoint()
    {
        return groupStartingPoint;
    }

    public bool IsActiveGroup()
    {
        return isActiveGroup;
    }

    // Call this when player reaches this group's starting point
    public void OnPlayerEnterGroup()
    {
        if (spawnOnGroupEntry)
        {
            ActivateGroup();
        }
    }

    // NEW: Method to set food scale
    public void SetFoodScale(float scale)
    {
        foodScale = Mathf.Max(0.1f, scale); // Ensure minimum scale
        Debug.Log($"Set food scale to: {foodScale}");
    }

    // NEW: Method to get food scale
    public float GetFoodScale()
    {
        return foodScale;
    }

    // NEW: Helper method for InteractiveObject to get spawn point
    public Transform GetFoodSpawnPoint(InteractiveObject interactiveObj)
    {
        return interactiveObj.GetFoodSpawnPoint();
    }
}
