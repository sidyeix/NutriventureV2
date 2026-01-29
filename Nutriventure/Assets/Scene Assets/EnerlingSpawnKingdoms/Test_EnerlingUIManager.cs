using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Test_EnerlingUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject viewEnerlingButton; // The button that appears when near an Enerling
    public GameObject enerlingInfoPanel;  // The panel that shows Enerling info
    
    // Basic Information Header
    [Header("Basic Information")]
    public TextMeshProUGUI enerlingNameText; // Text to display Enerling name
    public Image enerlingSpriteImage; // Image for Enerling sprite
    public TextMeshProUGUI rarityText; // Text for rarity
    public TextMeshProUGUI kingdomText; // Text for kingdom origin
    public TextMeshProUGUI enerlingStoryText; // Text for Enerling story
    
    // Battle Information Header
    [Header("Battle Information")]
    public TextMeshProUGUI baseLifeText; // Text for base life
    public TextMeshProUGUI armorPercentageText; // Text for armor percentage
    public TextMeshProUGUI baseDamageText; // Text for base damage
    
    // Skills Visuals
    [Header("Skill Visuals")]
    public Image skill1Image; // Image for skill 1 sprite
    public Image skill2Image; // Image for skill 2 sprite
    public Image skill3Image; // Image for skill 3 sprite
    public Image skill4Image; // Image for skill 4 sprite
    
    [Header("UI Buttons")]
    public Button closeButton; // Button to close the panel
    public Button textToSpeechButton; // Button for Text-to-Speech
    
    [Header("Camera Reference")]
    public EnerlingCameraController cameraController; // Camera controller
    public Camera viewCamera; // The camera that will view the Enerling
    
    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase; // Assign this in inspector!
    
    [Header("Detection Settings")]
    public float detectionRange = 3f; // How close player needs to be
    public float checkInterval = 0.2f; // How often to check for nearby Enerlings
    
    [Header("Button Animation Settings")]
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float slideDistance = 100f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private bool enableAnimation = true;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource; // AudioSource for playing Enerling audio
    
    private Test_EnerlingController currentNearbyEnerling;
    private GameObject player;
    private bool isPanelOpen = false;
    private float timeSinceLastCheck = 0f;
    private Coroutine buttonAnimationCoroutine;
    private Vector3 buttonOriginalPosition;
    private CanvasGroup buttonCanvasGroup;
    private bool isAnimating = false;
    private bool buttonWasVisible = false;
    private AudioClip currentEnerlingAudioClip;
    
    // Buffer to prevent immediate hiding
    private float lastDetectionTime = 0f;
    private const float DETECTION_BUFFER_TIME = 0.5f;
    
    void Start()
    {
        // Find player by tag
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            Debug.LogError("No GameObject with 'Player' tag found!");
        }
        
        // Get the view camera from camera controller
        if (cameraController != null && viewCamera == null)
        {
            viewCamera = cameraController.GetComponentInChildren<Camera>();
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }
        }
        
        // Initialize button animation components
        InitializeButtonAnimation();
        
        // Setup button listeners
        if (viewEnerlingButton != null)
        {
            Button viewButton = viewEnerlingButton.GetComponent<Button>();
            if (viewButton != null)
            {
                viewButton.onClick.AddListener(OpenEnerlingInfoPanel);
                Debug.Log("View button listener added");
            }
            else
            {
                Debug.LogError("View button has no Button component!");
            }
        }
        else
        {
            Debug.LogError("View Enerling Button is not assigned!");
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseEnerlingInfoPanel);
        }
        
        if (textToSpeechButton != null)
        {
            textToSpeechButton.onClick.AddListener(PlayEnerlingAudio);
        }
        else
        {
            Debug.LogWarning("Text-to-Speech button not assigned!");
        }
        
        // Setup AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                Debug.Log("Added AudioSource component to Enerling UI Manager");
            }
        }
        
        // Hide UI elements initially
        if (viewEnerlingButton != null)
        {
            if (enableAnimation)
            {
                viewEnerlingButton.SetActive(true); // Keep active for animation
                SetButtonInitialState();
            }
            else
            {
                viewEnerlingButton.SetActive(false);
            }
        }
        
        if (enerlingInfoPanel != null)
            enerlingInfoPanel.SetActive(false);
        
        // Log for debugging
        Debug.Log("Enerling UI Manager initialized with animation: " + enableAnimation);
    }
    
    private void InitializeButtonAnimation()
    {
        if (viewEnerlingButton == null || !enableAnimation) return;
        
        // Store original position
        buttonOriginalPosition = viewEnerlingButton.transform.localPosition;
        
        // Add or get CanvasGroup for fade animation
        buttonCanvasGroup = viewEnerlingButton.GetComponent<CanvasGroup>();
        if (buttonCanvasGroup == null)
        {
            buttonCanvasGroup = viewEnerlingButton.AddComponent<CanvasGroup>();
        }
        
        // Set initial state for animation
        SetButtonInitialState();
    }
    
    private void SetButtonInitialState()
    {
        if (viewEnerlingButton == null || !enableAnimation) return;
        
        // Start hidden (slid out and invisible)
        viewEnerlingButton.transform.localPosition = buttonOriginalPosition - new Vector3(slideDistance, 0, 0);
        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;
    }
    
    void Update()
    {
        if (player == null) 
        {
            // Try to find player again if null
            player = GameObject.FindGameObjectWithTag("Player");
            return;
        }
        
        // Check for nearby Enerlings at intervals
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= checkInterval)
        {
            bool foundEnerling = FindNearestEnerling();
            
            // Only handle button visibility if panel is not open
            if (!isPanelOpen)
            {
                UpdateButtonVisibility(foundEnerling);
            }
            
            timeSinceLastCheck = 0f;
        }
        
        // If panel is open, keep button hidden
        if (isPanelOpen && viewEnerlingButton != null && buttonWasVisible)
        {
            HideButtonWithAnimation();
            buttonWasVisible = false;
        }
    }
    
    private bool FindNearestEnerling()
    {
        Test_EnerlingController previousEnerling = currentNearbyEnerling;
        currentNearbyEnerling = null;
        float closestDistance = float.MaxValue;
        
        // Find all Enerlings in the scene
        Test_EnerlingController[] allEnerlings = FindObjectsOfType<Test_EnerlingController>();
        
        if (allEnerlings.Length == 0)
        {
            return false;
        }
        
        foreach (var enerling in allEnerlings)
        {
            if (enerling == null) continue;
            
            float distance = Vector3.Distance(player.transform.position, enerling.transform.position);
            
            if (distance < detectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                currentNearbyEnerling = enerling;
            }
        }
        
        // Update detection time
        if (currentNearbyEnerling != null)
        {
            lastDetectionTime = Time.time;
        }
        
        return currentNearbyEnerling != null;
    }
    
    private void UpdateButtonVisibility(bool shouldShow)
    {
        if (viewEnerlingButton == null) return;
        
        // Check if state changed
        bool stateChanged = (shouldShow != buttonWasVisible);
        
        // Always hide button if no Enerling is detected and enough time has passed
        if (!shouldShow && buttonWasVisible && (Time.time - lastDetectionTime) > DETECTION_BUFFER_TIME)
        {
            stateChanged = true;
        }
        
        if (!stateChanged) return; // No change, no need to animate
        
        buttonWasVisible = shouldShow;
        
        if (shouldShow)
        {
            ShowButtonWithAnimation();
        }
        else
        {
            HideButtonWithAnimation();
        }
    }
    
    private void ShowButtonWithAnimation()
    {
        if (viewEnerlingButton == null || isAnimating || isPanelOpen) return;
        
        if (buttonAnimationCoroutine != null)
            StopCoroutine(buttonAnimationCoroutine);
        
        buttonAnimationCoroutine = StartCoroutine(AnimateButton(true));
    }
    
    private void HideButtonWithAnimation()
    {
        if (viewEnerlingButton == null || isAnimating) return;
        
        if (buttonAnimationCoroutine != null)
            StopCoroutine(buttonAnimationCoroutine);
        
        buttonAnimationCoroutine = StartCoroutine(AnimateButton(false));
    }
    
    private IEnumerator AnimateButton(bool show)
    {
        isAnimating = true;
        
        // Ensure button is active for animation
        if (show && !viewEnerlingButton.activeSelf)
            viewEnerlingButton.SetActive(true);
        
        float elapsedTime = 0f;
        Vector3 startPosition = viewEnerlingButton.transform.localPosition;
        Vector3 targetPosition = show ? buttonOriginalPosition : 
            buttonOriginalPosition - new Vector3(slideDistance, 0, 0);
        
        float startAlpha = buttonCanvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;
        
        // Enable interaction when showing
        if (show)
        {
            buttonCanvasGroup.interactable = true;
            buttonCanvasGroup.blocksRaycasts = true;
        }
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            
            // Use animation curves
            float slideT = slideCurve.Evaluate(t);
            float fadeT = fadeCurve.Evaluate(t);
            
            // Position animation
            viewEnerlingButton.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, slideT);
            
            // Alpha animation
            buttonCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, fadeT);
            
            yield return null;
        }
        
        // Final state
        viewEnerlingButton.transform.localPosition = targetPosition;
        buttonCanvasGroup.alpha = targetAlpha;
        
        // Disable interaction when hiding
        if (!show)
        {
            buttonCanvasGroup.interactable = false;
            buttonCanvasGroup.blocksRaycasts = false;
            viewEnerlingButton.SetActive(false); // Fully deactivate when hidden
        }
        
        isAnimating = false;
    }
    
    public void OpenEnerlingInfoPanel()
    {
        Debug.Log("OpenEnerlingInfoPanel called");
        
        if (currentNearbyEnerling == null)
        {
            Debug.LogWarning("No nearby Enerling to show!");
            return;
        }
        
        if (enerlingInfoPanel == null)
        {
            Debug.LogError("Enerling Info Panel is not assigned!");
            return;
        }
        
        if (viewCamera == null)
        {
            Debug.LogError("View camera is not assigned!");
            return;
        }
        
        // Start interaction with Enerling, passing the camera
        currentNearbyEnerling.StartInteraction(viewCamera);
        
        // Switch camera to view Enerling (first-person from player position)
        if (cameraController != null)
        {
            cameraController.StartViewingEnerling(currentNearbyEnerling);
        }
        
        // Hide button with animation before opening panel
        HideButtonWithAnimation();
        
        // Get the Enerling's ingredient info
        var ingredientInfo = GetIngredientInfoFromEnerling(currentNearbyEnerling);
        
        if (ingredientInfo != null)
        {
            // Store the audio clip for Text-to-Speech
            currentEnerlingAudioClip = ingredientInfo.audioClip;
            
            // Enable/disable Text-to-Speech button based on audio availability
            if (textToSpeechButton != null)
            {
                textToSpeechButton.interactable = (currentEnerlingAudioClip != null);
                
                // Optional: Change button color based on availability
                var buttonImage = textToSpeechButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = (currentEnerlingAudioClip != null) ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
            
            // Update UI with Enerling info
            UpdateEnerlingInfoUI(ingredientInfo);
            
            // Show the panel
            enerlingInfoPanel.SetActive(true);
            isPanelOpen = true;
                
            Debug.Log($"Opened info panel for {ingredientInfo.ingredientName}");
        }
        else
        {
            Debug.LogWarning("Could not get ingredient info for nearby Enerling");
            
            // Fallback: Show panel with just the name
            if (enerlingNameText != null)
            {
                string displayName = currentNearbyEnerling.gameObject.name
                    .Replace("_Enerling", "")
                    .Replace("_Enerling_Fallback", "")
                    .Replace("_Enerling", "")
                    .Replace("(Clone)", "");
                enerlingNameText.text = displayName;
            }
            
            // Disable Text-to-Speech button for fallback
            if (textToSpeechButton != null)
            {
                textToSpeechButton.interactable = false;
            }
            
            enerlingInfoPanel.SetActive(true);
            isPanelOpen = true;
        }
    }
    
    private IngredientDatabase.IngredientInfo GetIngredientInfoFromEnerling(Test_EnerlingController enerling)
    {
        if (enerling == null) return null;
        
        var method = enerling.GetType().GetMethod("GetIngredientInfo");
        if (method != null)
        {
            return method.Invoke(enerling, null) as IngredientDatabase.IngredientInfo;
        }
        
        if (ingredientDatabase != null)
        {
            string enerlingName = enerling.gameObject.name;
            string ingredientName = ExtractIngredientName(enerlingName);
            return ingredientDatabase.GetIngredientInfo(ingredientName);
        }
        
        return null;
    }
    
    private string ExtractIngredientName(string enerlingName)
    {
        string name = enerlingName;
        name = name.Replace("_Enerling", "");
        name = name.Replace("_Enerling_Fallback", "");
        name = name.Replace("(Clone)", "");
        return name.Trim();
    }
    
    private void UpdateEnerlingInfoUI(IngredientDatabase.IngredientInfo info)
    {
        if (info == null) return;
        
        if (enerlingNameText != null)
        {
            enerlingNameText.text = info.ingredientName;
        }
        
        if (enerlingSpriteImage != null && info.enerlingSprite != null)
        {
            enerlingSpriteImage.sprite = info.enerlingSprite;
            enerlingSpriteImage.preserveAspect = true;
        }
        
        if (rarityText != null)
        {
            rarityText.text = info.rarity.ToString();
            switch (info.rarity)
            {
                case IngredientDatabase.Rarity.Common:
                    rarityText.color = Color.white;
                    break;
                case IngredientDatabase.Rarity.Rare:
                    rarityText.color = Color.blue;
                    break;
                case IngredientDatabase.Rarity.UltraRare:
                    rarityText.color = Color.yellow;
                    break;
            }
        }
        
        if (kingdomText != null)
        {
            kingdomText.text = info.kingdom.ToString();
        }
        
        if (enerlingStoryText != null)
        {
            if (string.IsNullOrEmpty(info.enerlingStory) || 
                string.IsNullOrWhiteSpace(info.enerlingStory))
            {
                enerlingStoryText.text = "There's no enerling story available...";
                enerlingStoryText.fontStyle = FontStyles.Italic;
                enerlingStoryText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }
            else
            {
                enerlingStoryText.text = info.enerlingStory;
                enerlingStoryText.fontStyle = FontStyles.Normal;
                enerlingStoryText.color = Color.white;
            }
        }
        
        if (baseLifeText != null)
        {
            baseLifeText.text = $"{info.baseLife}";
        }
        
        if (armorPercentageText != null)
        {
            armorPercentageText.text = $"{info.armorPercent}%";
        }
        
        if (baseDamageText != null)
        {
            baseDamageText.text = $"{info.baseDamage}";
        }
        
        // Update Skill Visuals
        UpdateSkillImage(skill1Image, info.skill1);
        UpdateSkillImage(skill2Image, info.skill2);
        UpdateSkillImage(skill3Image, info.skill3);
        UpdateSkillImage(skill4Image, info.skill4);
        
        Debug.Log($"Updated UI with: {info.ingredientName}");
    }
    
    private void UpdateSkillImage(Image skillImage, IngredientDatabase.SkillInfo skillInfo)
    {
        if (skillImage != null)
        {
            if (skillInfo != null && skillInfo.skillSprite != null)
            {
                skillImage.sprite = skillInfo.skillSprite;
                skillImage.preserveAspect = true;
                skillImage.gameObject.SetActive(true);
            }
            else
            {
                skillImage.gameObject.SetActive(false);
            }
        }
    }
    
    public void PlayEnerlingAudio()
    {
        if (audioSource == null || currentEnerlingAudioClip == null) return;
        
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        audioSource.clip = currentEnerlingAudioClip;
        audioSource.Play();
        
        Debug.Log($"Playing audio for: {currentEnerlingAudioClip.name}");
    }
    
    public void CloseEnerlingInfoPanel()
    {
        if (enerlingInfoPanel == null) return;
        
        enerlingInfoPanel.SetActive(false);
        isPanelOpen = false;
        
        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // End interaction with Enerling
        if (currentNearbyEnerling != null)
        {
            currentNearbyEnerling.EndInteraction();
        }
        
        // Switch camera back to player
        if (cameraController != null)
        {
            cameraController.StopViewingEnerling();
        }
        
        Debug.Log("Closed info panel");
        
        // Check distance and decide whether to show button again
        if (currentNearbyEnerling != null && player != null)
        {
            float distance = Vector3.Distance(player.transform.position, currentNearbyEnerling.transform.position);
            if (distance <= detectionRange)
            {
                ShowButtonWithAnimation();
            }
            else
            {
                currentNearbyEnerling = null;
                buttonWasVisible = false;
            }
        }
        else
        {
            currentNearbyEnerling = null;
            buttonWasVisible = false;
        }
    }
    
    void OnDestroy()
    {
        // Clean up listeners
        if (viewEnerlingButton != null)
        {
            Button viewButton = viewEnerlingButton.GetComponent<Button>();
            if (viewButton != null)
            {
                viewButton.onClick.RemoveListener(OpenEnerlingInfoPanel);
            }
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseEnerlingInfoPanel);
        }
        
        if (textToSpeechButton != null)
        {
            textToSpeechButton.onClick.RemoveListener(PlayEnerlingAudio);
        }
        
        // Stop any running coroutines
        if (buttonAnimationCoroutine != null)
        {
            StopCoroutine(buttonAnimationCoroutine);
        }
        
        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}