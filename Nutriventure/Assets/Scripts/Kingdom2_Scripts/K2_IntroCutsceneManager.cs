using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Add this namespace
using System.Collections;

public class K2_IntroCutsceneManager : MonoBehaviour
{
    [Header("Timeline References")]
    [SerializeField] private GameObject timelineParentObject;
    [SerializeField] private PlayableDirector timelineDirector;
    
    [Header("UI References")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button skipButton;
    
    [Header("Camera References")]
    [SerializeField] private GameObject playerFollowCamera;
    [SerializeField] private GameObject timelineCamera;
    
    [Header("UI Canvas")]
    [SerializeField] private GameObject gameUICanvas;
    
    [Header("Audio Handler")]
    [SerializeField] private GameObject audioHandler;
    
    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float skipDelay = 1.0f;
    
    [Header("Input Actions")]
    [SerializeField] private InputAction skipAction; // Define input action for skipping
    
    private bool isCutscenePlaying = false;
    
    void Start()
    {
        InitializeAllComponents();
        
        // Add click listener to buttons
        if (startButton != null)
        {
            startButton.onClick.AddListener(PlayCutscene);
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipCutscene);
        }
        
        // Setup input action for skipping
        if (allowSkip)
        {
            skipAction = new InputAction("SkipCutscene");
            skipAction.AddBinding("<Keyboard>/space");
            skipAction.AddBinding("<Keyboard>/escape");
            skipAction.AddBinding("<Keyboard>/enter");
            skipAction.AddBinding("<Gamepad>/buttonSouth"); // A button on Xbox, Cross on PlayStation
            skipAction.performed += ctx => SkipCutscene();
        }
    }
    
    private void InitializeAllComponents()
    {
        // Ensure timeline parent is disabled initially
        if (timelineParentObject != null)
        {
            timelineParentObject.SetActive(false);
        }
        
        // Ensure PlayableDirector is stopped
        if (timelineDirector != null)
        {
            timelineDirector.Stop();
            timelineDirector.stopped += OnTimelineFinished;
        }
        
        // Ensure player follow camera is disabled initially
        if (playerFollowCamera != null)
        {
            playerFollowCamera.SetActive(false);
        }
        
        // Ensure game UI canvas is disabled initially
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
        }
        
        // Ensure skip button is disabled initially
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
        
        // Ensure audio handler is enabled initially
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
        }
    }
    
    void OnEnable()
    {
        if (allowSkip)
        {
            skipAction.Enable();
        }
    }
    
    void OnDisable()
    {
        if (allowSkip)
        {
            skipAction.Disable();
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (timelineDirector != null)
        {
            timelineDirector.stopped -= OnTimelineFinished;
        }
        
        if (allowSkip)
        {
            skipAction.Dispose();
        }
    }

    public void PlayCutscene()
    {
        if (timelineParentObject != null && timelineDirector != null)
        {
            isCutscenePlaying = true;
            
            // Enable the timeline parent object
            timelineParentObject.SetActive(true);
            
            // Play the timeline
            timelineDirector.Play();
            
            // Disable the start button
            if (startButton != null)
            {
                startButton.interactable = false;
            }
            
            // Disable audio handler during cutscene
            if (audioHandler != null)
            {
                audioHandler.SetActive(false);
            }
            
            // Show skip button after delay (if allowed)
            if (allowSkip && skipButton != null)
            {
                StartCoroutine(ShowSkipButtonAfterDelay());
            }
            
            Debug.Log("Cutscene started - Audio handler disabled");
        }
        else
        {
            Debug.LogError("Timeline references are not set!");
        }
    }
    
    private IEnumerator ShowSkipButtonAfterDelay()
    {
        yield return new WaitForSeconds(skipDelay);
        
        if (skipButton != null && isCutscenePlaying)
        {
            skipButton.gameObject.SetActive(true);
        }
    }
    
    public void SkipCutscene()
    {
        if (!isCutscenePlaying || !allowSkip) return;
        
        Debug.Log("Cutscene skipped");
        
        // Stop the timeline
        if (timelineDirector != null && timelineDirector.state == PlayState.Playing)
        {
            timelineDirector.Stop();
        }
        
        // Manually call the finish method
        FinishCutscene();
    }
    
    private void OnTimelineFinished(PlayableDirector director)
    {
        // This method is called when the timeline finishes playing normally
        FinishCutscene();
    }
    
    private void FinishCutscene()
    {
        isCutscenePlaying = false;
        
        // Disable the timeline parent object
        if (timelineParentObject != null)
        {
            timelineParentObject.SetActive(false);
        }
        
        // Enable the player follow camera
        if (playerFollowCamera != null)
        {
            playerFollowCamera.SetActive(true);
        }
        
        // Enable the game UI canvas
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
        }
        
        // Re-enable audio handler after cutscene
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
        }
        
        // Hide skip button
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
        
        Debug.Log("Cutscene finished/skipped - UI enabled, audio handler re-enabled");
    }
}