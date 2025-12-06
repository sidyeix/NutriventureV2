using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class K2_IntroCutsceneManager : MonoBehaviour
{
    [Header("Timeline References")]
    [SerializeField] private GameObject timelineParentObject; // Drag your parent object with PlayableDirector here
    [SerializeField] private PlayableDirector timelineDirector;
    
    [Header("UI References")]
    [SerializeField] private Button startButton; // Drag your UI button here
    
    [Header("Camera References")]
    [SerializeField] private GameObject playerFollowCamera; // Drag your PlayerFollowCamera Virtual Camera here
    [SerializeField] private GameObject timelineCamera; // Optional: Your timeline's camera if separate
    
    [Header("UI Canvas")]
    [SerializeField] private GameObject gameUICanvas; // Drag your "UI_Canvas_StarterAssetsInputs_Joysticks" here
    
    [Header("Audio Handler")]
    [SerializeField] private GameObject audioHandler; // Drag your "Audio_Handler" GameObject here
    
    void Start()
    {
        InitializeAllComponents();
        
        // Add click listener to button
        if (startButton != null)
        {
            startButton.onClick.AddListener(PlayCutscene);
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
            // Subscribe to the stopped event
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
        
        // Ensure audio handler is enabled initially (for background audio before cutscene)
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (timelineDirector != null)
        {
            timelineDirector.stopped -= OnTimelineFinished;
        }
    }

    public void PlayCutscene()
    {
        if (timelineParentObject != null && timelineDirector != null)
        {
            // Enable the timeline parent object
            timelineParentObject.SetActive(true);
            
            // Play the timeline
            timelineDirector.Play();
            
            // Disable the start button
            if (startButton != null)
            {
                startButton.interactable = false;
                
                // Optional: Hide the entire button GameObject
                // startButton.gameObject.SetActive(false);
            }
            
            // Disable audio handler during cutscene
            if (audioHandler != null)
            {
                audioHandler.SetActive(false);
            }
            
            Debug.Log("Cutscene started - Audio handler disabled");
        }
        else
        {
            Debug.LogError("Timeline references are not set!");
        }
    }
    
    private void OnTimelineFinished(PlayableDirector director)
    {
        // This method is called when the timeline finishes playing
        
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
        
        Debug.Log("Cutscene finished - UI enabled, audio handler re-enabled");
        
        // Optional: You can also hide the start button completely after cutscene
        // if (startButton != null) startButton.gameObject.SetActive(false);
    }
}