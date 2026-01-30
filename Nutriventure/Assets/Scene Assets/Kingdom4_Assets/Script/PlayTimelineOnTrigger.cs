using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class PlayTimelineOnTrigger : MonoBehaviour
{
    public PlayableDirector playableDirector;
    public bool playOnlyOnce = true;

    [Header("Phase Trigger Settings")]
    public bool triggerOnPlatformPhase = true;
    public bool triggerOnCastlePhase = true;
    public bool triggerOnEndGame = true;
    
    [Header("Auto-play Timeline")]
    public bool autoPlayTimeline = true;
    public float timelineDelay = 0.5f;

    [Header("Phase Actions")]
    [Tooltip("What to do when Platform Phase is triggered")]
    public bool completePlatformPhase = true;
    [Tooltip("What to do when Castle Phase is triggered")]
    public bool reachQueen = true;
    [Tooltip("What to do when End Game is triggered")]
    public bool completeGame = true;

    [Header("Key Activation (For Castle Phase)")]
    public GameObject keyGameObject; // Drag your 3D key here
    public bool activateKeyAfterTimeline = true;
    public float keyActivationDelay = 0f;
    public bool makeKeyCollectible = true; // If using CollectibleKey script

    private bool hasPlayed = false;

    void Start()
    {
        // Ensure playableDirector is found if not assigned
        if (playableDirector == null)
        {
            playableDirector = GetComponent<PlayableDirector>();
            if (playableDirector == null)
            {
                Debug.LogWarning($"No PlayableDirector found on {gameObject.name}");
            }
        }

        // Initialize key state
        InitializeKey();
    }

    void InitializeKey()
    {
        if (keyGameObject != null && activateKeyAfterTimeline)
        {
            // Start with key inactive
            keyGameObject.SetActive(false);
            
            // Also disable the CollectibleKey script to prevent Update() running
            CollectibleKey collectibleKey = keyGameObject.GetComponent<CollectibleKey>();
            if (collectibleKey != null)
            {
                collectibleKey.isCollectible = false;
                collectibleKey.enabled = false; // Disable script until activated
            }
            
            Debug.Log("Key initialized as inactive (GameObject and script disabled)");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered())
        {
            // Check current phase and trigger appropriate actions
            if (triggerOnPlatformPhase && 
                AllerthriaGameManager.Instance.currentPhase == AllerthriaGameManager.GamePhase.PlatformPhase)
            {
                Debug.Log("Trigger: Platform Phase - Player completed platform section");
                
                // Trigger platform phase completion
                if (completePlatformPhase)
                {
                    AllerthriaGameManager.Instance.CompletePlatformPhase();
                }
                
                // Play timeline if assigned
                if (autoPlayTimeline && playableDirector != null)
                {
                    StartCoroutine(PlayTimelineWithDelay());
                }
                
                MarkAsPlayed();
            }
            else if (triggerOnCastlePhase && 
                     AllerthriaGameManager.Instance.currentPhase == AllerthriaGameManager.GamePhase.CastlePhase)
            {
                Debug.Log("Trigger: Castle Phase - Player reached queen area");
                
                // Trigger castle phase actions
                if (reachQueen)
                {
                    AllerthriaGameManager.Instance.ReachQueen();
                }
                
                // Play timeline if assigned
                if (autoPlayTimeline && playableDirector != null)
                {
                    StartCoroutine(PlayTimelineWithDelay());
                }
                else
                {
                    // If no timeline, just activate the key
                    ActivateKey();
                }
                
                MarkAsPlayed();
            }
            else if (triggerOnEndGame && 
                     AllerthriaGameManager.Instance.currentPhase == AllerthriaGameManager.GamePhase.EndGame)
            {
                Debug.Log("Trigger: End Game - Player returned with key");
                
                // Trigger end game actions
                if (completeGame)
                {
                    AllerthriaGameManager.Instance.CompleteGame();
                }
                
                // Play timeline if assigned
                if (autoPlayTimeline && playableDirector != null)
                {
                    StartCoroutine(PlayTimelineWithDelay());
                }
                
                MarkAsPlayed();
            }
        }
    }

    private IEnumerator PlayTimelineWithDelay()
    {
        yield return new WaitForSeconds(timelineDelay);
        
        if (playableDirector != null)
        {
            Debug.Log($"Playing timeline: {playableDirector.name}");
            
            // Subscribe to timeline completion
            playableDirector.stopped += OnTimelineStopped;
            
            playableDirector.Play();
            
            // Optional: Wait for timeline to complete
            yield return new WaitForSeconds((float)playableDirector.duration);
            Debug.Log("Timeline completed");
        }
        else
        {
            // If no timeline, just activate the key
            ActivateKey();
        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        // Unsubscribe to prevent multiple calls
        director.stopped -= OnTimelineStopped;
        
        // Activate key after timeline completes
        if (activateKeyAfterTimeline)
        {
            StartCoroutine(ActivateKeyWithDelay());
        }
    }

    private IEnumerator ActivateKeyWithDelay()
    {
        yield return new WaitForSeconds(keyActivationDelay);
        ActivateKey();
    }

    private void ActivateKey()
    {
        if (keyGameObject != null)
        {
            // Activate the GameObject
            keyGameObject.SetActive(true);
            Debug.Log($"Key GameObject activated: {keyGameObject.name}");
            
            // Enable the CollectibleKey script
            CollectibleKey collectibleKey = keyGameObject.GetComponent<CollectibleKey>();
            if (collectibleKey != null)
            {
                collectibleKey.enabled = true; // Enable the script
                
                // If using CollectibleKey script, make it collectible
                if (makeKeyCollectible)
                {
                    // Wait one frame to ensure script is fully enabled
                    StartCoroutine(MakeKeyCollectibleAfterDelay(collectibleKey));
                }
                else
                {
                    // Just enable it but don't make collectible yet
                    collectibleKey.isCollectible = true;
                    Debug.Log("Key script enabled but not glowing yet");
                }
            }
            else
            {
                Debug.LogWarning("No CollectibleKey script found on key GameObject!");
            }
            
            // Optional: Trigger event for UI or other systems
            OnKeyActivated();
        }
    }

    private IEnumerator MakeKeyCollectibleAfterDelay(CollectibleKey keyScript)
    {
        yield return null; // Wait one frame for script to initialize
        keyScript.MakeCollectible();
        Debug.Log("Key is now collectible!");
    }

    protected virtual void OnKeyActivated()
    {
        // Override this method for custom behavior
        Debug.Log("Key activation event fired");
    }

    private bool hasTriggered()
    {
        return playOnlyOnce && hasPlayed;
    }

    private void MarkAsPlayed()
    {
        if (playOnlyOnce)
        {
            hasPlayed = true;
            Debug.Log($"Trigger marked as played. Will not trigger again.");
            
            // Optional: Disable the trigger collider
            Collider collider = GetComponent<Collider>();
            if (collider != null && collider.isTrigger)
            {
                collider.enabled = false;
            }
        }
    }

    // Method to manually trigger the timeline (for testing)
    [ContextMenu("Test Play Timeline")]
    public void TestPlayTimeline()
    {
        if (playableDirector != null)
        {
            playableDirector.Play();
            Debug.Log("Manually playing timeline");
        }
        else
        {
            Debug.LogError("No PlayableDirector assigned!");
        }
    }

    [ContextMenu("Test Activate Key")]
    public void TestActivateKey()
    {
        ActivateKey();
    }

    // Method to reset the trigger
    [ContextMenu("Reset Trigger")]
    public void ResetTrigger()
    {
        hasPlayed = false;
        Debug.Log("Trigger reset - will fire again");
        
        // Re-enable collider if disabled
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        
        // Reset key state
        if (keyGameObject != null)
        {
            CollectibleKey collectibleKey = keyGameObject.GetComponent<CollectibleKey>();
            if (collectibleKey != null)
            {
                collectibleKey.ResetKey();
                collectibleKey.enabled = false; // Disable script
            }
            
            keyGameObject.SetActive(false);
        }
    }

    // Optional: Draw gizmos for visibility
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        if (playableDirector != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playableDirector.transform.position);
        }
        
        if (keyGameObject != null)
        {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f); // Gold color
            Gizmos.DrawLine(transform.position, keyGameObject.transform.position);
            Gizmos.DrawWireCube(keyGameObject.transform.position, Vector3.one * 0.5f);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, 1.5f);
        
        // Draw the phase it triggers on
        string triggerInfo = $"Triggers on:\n- Platform Phase: {triggerOnPlatformPhase}\n- Castle Phase: {triggerOnCastlePhase}\n- End Game: {triggerOnEndGame}";
        
        if (keyGameObject != null && activateKeyAfterTimeline)
        {
            triggerInfo += $"\n\nActivates Key: Yes\nDelay: {keyActivationDelay}s";
        }
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, triggerInfo);
    }
#endif
}