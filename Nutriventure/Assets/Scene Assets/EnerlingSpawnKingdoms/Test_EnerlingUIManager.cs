using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Test_EnerlingUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject viewEnerlingButton; // The button that appears when near an Enerling
    public GameObject enerlingInfoPanel;  // The panel that shows Enerling info
    public TextMeshProUGUI enerlingNameText; // Text to display Enerling name
    public Button closeButton; // Button to close the panel
    
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
    
    private Test_EnerlingController currentNearbyEnerling;
    private GameObject player;
    private bool isPanelOpen = false;
    private float timeSinceLastCheck = 0f;
    private Coroutine buttonAnimationCoroutine;
    private Vector3 buttonOriginalPosition;
    private CanvasGroup buttonCanvasGroup;
    private bool isAnimating = false;
    private bool buttonWasVisible = false;
    
    void Start()
    {
        // Find player by tag
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            Debug.LogError("No GameObject with 'Player' tag found!");
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
        if (player == null || isPanelOpen) 
        {
            // Try to find player again if null
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
            
            // If panel is open, make sure button is hidden
            if (isPanelOpen && viewEnerlingButton != null && viewEnerlingButton.activeSelf)
            {
                HideButtonWithAnimation();
            }
            return;
        }
        
        // Check for nearby Enerlings at intervals
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= checkInterval)
        {
            bool foundEnerling = FindNearestEnerling();
            UpdateButtonVisibility(foundEnerling);
            timeSinceLastCheck = 0f;
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
        
        // If we found a different Enerling than before, log it
        if (currentNearbyEnerling != null && currentNearbyEnerling != previousEnerling)
        {
            Debug.Log($"Found nearby Enerling: {currentNearbyEnerling.gameObject.name} (Distance: {closestDistance:F2})");
        }
        
        return currentNearbyEnerling != null;
    }
    
    private void UpdateButtonVisibility(bool shouldShow)
    {
        if (viewEnerlingButton == null) return;
        
        // Check if state changed
        bool stateChanged = (shouldShow != buttonWasVisible);
        
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
        
        // Hide button with animation before opening panel
        HideButtonWithAnimation();
        
        // Get the Enerling's ingredient info
        var ingredientInfo = GetIngredientInfoFromEnerling(currentNearbyEnerling);
        
        if (ingredientInfo != null)
        {
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
            
            enerlingInfoPanel.SetActive(true);
            isPanelOpen = true;
        }
    }
    
    private IngredientDatabase.IngredientInfo GetIngredientInfoFromEnerling(Test_EnerlingController enerling)
    {
        if (enerling == null) return null;
        
        // First try: Use GetIngredientInfo method if available
        var method = enerling.GetType().GetMethod("GetIngredientInfo");
        if (method != null)
        {
            return method.Invoke(enerling, null) as IngredientDatabase.IngredientInfo;
        }
        
        // Second try: Use reflection to get private field
        var field = enerling.GetType().GetField("ingredientInfo", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return field.GetValue(enerling) as IngredientDatabase.IngredientInfo;
        }
        
        // Third try: Get from database using name
        if (ingredientDatabase != null)
        {
            string enerlingName = enerling.gameObject.name;
            string ingredientName = ExtractIngredientName(enerlingName);
            Debug.Log($"Looking for ingredient in database: {ingredientName}");
            return ingredientDatabase.GetIngredientInfo(ingredientName);
        }
        
        return null;
    }
    
    private string ExtractIngredientName(string enerlingName)
    {
        // Remove common suffixes
        string name = enerlingName;
        name = name.Replace("_Enerling", "");
        name = name.Replace("_Enerling_Fallback", "");
        name = name.Replace("(Clone)", "");
        return name.Trim();
    }
    
    private void UpdateEnerlingInfoUI(IngredientDatabase.IngredientInfo info)
    {
        if (enerlingNameText != null && info != null)
        {
            enerlingNameText.text = info.ingredientName;
            Debug.Log($"Updated UI with: {info.ingredientName}");
        }
    }
    
    public void CloseEnerlingInfoPanel()
    {
        if (enerlingInfoPanel != null)
        {
            enerlingInfoPanel.SetActive(false);
            isPanelOpen = false;
            Debug.Log("Closed info panel");
            
            // After closing panel, check if we should show button again
            if (currentNearbyEnerling != null && player != null)
            {
                float distance = Vector3.Distance(player.transform.position, currentNearbyEnerling.transform.position);
                if (distance <= detectionRange)
                {
                    // Player is still near the Enerling, show button again
                    ShowButtonWithAnimation();
                }
            }
        }
    }
    
    // Test method to manually open panel
    public void TestOpenPanel()
    {
        Debug.Log("TestOpenPanel called");
        if (enerlingNameText != null)
        {
            enerlingNameText.text = "Test Enerling";
        }
        if (enerlingInfoPanel != null)
        {
            enerlingInfoPanel.SetActive(true);
            isPanelOpen = true;
            HideButtonWithAnimation(); // Hide button when panel opens
        }
    }
    
    // Test method to toggle button visibility
    public void TestToggleButton()
    {
        if (buttonWasVisible)
        {
            HideButtonWithAnimation();
        }
        else
        {
            ShowButtonWithAnimation();
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
        
        // Stop any running coroutines
        if (buttonAnimationCoroutine != null)
        {
            StopCoroutine(buttonAnimationCoroutine);
        }
    }
    
    // For debugging in the editor
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Enerling UI Manager Debug");
        GUILayout.Label($"Panel Open: {isPanelOpen}");
        GUILayout.Label($"Button Visible: {buttonWasVisible}");
        GUILayout.Label($"Animating: {isAnimating}");
        GUILayout.Label($"Nearby Enerling: {(currentNearbyEnerling != null ? currentNearbyEnerling.gameObject.name : "None")}");
        
        if (GUILayout.Button("Test Open Panel"))
        {
            TestOpenPanel();
        }
        
        if (GUILayout.Button("Test Toggle Button"))
        {
            TestToggleButton();
        }
        
        if (GUILayout.Button("Force Show Button"))
        {
            ShowButtonWithAnimation();
        }
        
        if (GUILayout.Button("Force Hide Button"))
        {
            HideButtonWithAnimation();
        }
        
        GUILayout.EndArea();
    }
    
    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.transform.position, detectionRange);
        }
        else
        {
            // Draw at origin if no player
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Vector3.zero, detectionRange);
        }
    }
}