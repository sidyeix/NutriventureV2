using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Test_EnerlingUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject viewEnerlingButton; // The button that appears when near an Enerling
    public GameObject enerlingInfoPanel;  // The panel that shows Enerling info
    public TextMeshProUGUI enerlingNameText; // Text to display Enerling name
    public Button closeButton; // Button to close the panel
    
    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase; // Assign this in inspector!
    
    [Header("Settings")]
    public float detectionRange = 3f; // How close player needs to be
    public float checkInterval = 0.3f; // How often to check for nearby Enerlings
    
    private Test_EnerlingController currentNearbyEnerling;
    private GameObject player;
    private bool isPanelOpen = false;
    private float timeSinceLastCheck = 0f;
    
    void Start()
    {
        // Find player by tag
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            Debug.LogError("No GameObject with 'Player' tag found!");
        }
        
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
            viewEnerlingButton.SetActive(false);
        
        if (enerlingInfoPanel != null)
            enerlingInfoPanel.SetActive(false);
        
        // Log for debugging
        Debug.Log("Enerling UI Manager initialized");
    }
    
    void Update()
    {
        if (player == null || isPanelOpen) 
        {
            // Try to find player again if null
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
            return;
        }
        
        // Check for nearby Enerlings at intervals
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= checkInterval)
        {
            FindNearestEnerling();
            UpdateButtonVisibility();
            timeSinceLastCheck = 0f;
        }
    }
    
    private void FindNearestEnerling()
    {
        currentNearbyEnerling = null;
        float closestDistance = float.MaxValue;
        
        // Find all Enerlings in the scene
        Test_EnerlingController[] allEnerlings = FindObjectsOfType<Test_EnerlingController>();
        
        if (allEnerlings.Length == 0)
        {
            // No Enerlings in scene
            return;
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
        
        // Debug logging
        if (currentNearbyEnerling != null)
        {
            Debug.Log($"Found nearby Enerling: {currentNearbyEnerling.gameObject.name} (Distance: {closestDistance:F2})");
        }
    }
    
    private void UpdateButtonVisibility()
    {
        if (viewEnerlingButton == null) return;
        
        bool shouldShow = currentNearbyEnerling != null && !isPanelOpen;
        
        // Only update if state changed
        if (viewEnerlingButton.activeSelf != shouldShow)
        {
            viewEnerlingButton.SetActive(shouldShow);
            Debug.Log($"View button visibility: {shouldShow}");
        }
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
        
        // Get the Enerling's ingredient info
        var ingredientInfo = GetIngredientInfoFromEnerling(currentNearbyEnerling);
        
        if (ingredientInfo != null)
        {
            // Update UI with Enerling info
            UpdateEnerlingInfoUI(ingredientInfo);
            
            // Show the panel
            enerlingInfoPanel.SetActive(true);
            isPanelOpen = true;
            
            // Hide the view button
            if (viewEnerlingButton != null)
                viewEnerlingButton.SetActive(false);
                
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
                    .Replace("_Enerling", "");
                enerlingNameText.text = displayName;
            }
            
            enerlingInfoPanel.SetActive(true);
            isPanelOpen = true;
            if (viewEnerlingButton != null)
                viewEnerlingButton.SetActive(false);
        }
    }
    
    private IngredientDatabase.IngredientInfo GetIngredientInfoFromEnerling(Test_EnerlingController enerling)
    {
        // Try to get info from the controller first
        if (enerling != null)
        {
            // Try using reflection or a public method
            var type = enerling.GetType();
            var method = type.GetMethod("GetIngredientInfo");
            if (method != null)
            {
                return method.Invoke(enerling, null) as IngredientDatabase.IngredientInfo;
            }
        }
        
        // Fallback: Try to get from database using name
        if (ingredientDatabase != null)
        {
            string enerlingName = enerling.gameObject.name;
            string ingredientName = ExtractIngredientName(enerlingName);
            Debug.Log($"Looking for ingredient: {ingredientName}");
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
        name = name.Replace("_Enerling", ""); // Double check
        name = name.Replace("(Clone)", ""); // Remove clone suffix if present
        return name.Trim();
    }
    
    private void UpdateEnerlingInfoUI(IngredientDatabase.IngredientInfo info)
    {
        if (enerlingNameText != null && info != null)
        {
            enerlingNameText.text = info.ingredientName;
            Debug.Log($"Updated UI with: {info.ingredientName}");
        }
        
        // You can add more UI updates here:
        // - Rarity
        // - Kingdom
        // - Stats
        // - Description
        // - Image
    }
    
    public void CloseEnerlingInfoPanel()
    {
        if (enerlingInfoPanel != null)
        {
            enerlingInfoPanel.SetActive(false);
            isPanelOpen = false;
            Debug.Log("Closed info panel");
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