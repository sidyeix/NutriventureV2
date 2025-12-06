using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image ingredientIcon;
    public Button ingredientButton;
    public TextMeshProUGUI ingredientNameText; // Optional: if you have text
    
    [Header("Silhouette Settings")]
    public Sprite silhouetteSprite; // The silhouette/unknown icon
    public Color silhouetteColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Darkened color for silhouettes
    public Color collectedColor = Color.white; // Normal color for collected ingredients
    
    [Header("Visual Feedback")]
    public GameObject collectedOverlay; // Optional: checkmark or glow effect
    public GameObject lockedOverlay; // Optional: lock icon for unavailable
    
    private BookInteractable.IngredientData ingredientData;
    private BookUIManager bookManager;
    private bool isCollected = false;
    
    public void Initialize(BookInteractable.IngredientData data, BookUIManager manager, bool collected = false)
    {
        ingredientData = data;
        bookManager = manager;
        isCollected = collected;
        
        UpdateAppearance();
            
        if (ingredientButton != null)
        {
            ingredientButton.onClick.AddListener(OnIngredientClicked);
            // Only make button interactable if collected
            ingredientButton.interactable = isCollected;
        }
    }
    
    private void UpdateAppearance()
    {
        if (ingredientIcon != null)
        {
            if (isCollected && ingredientData.ingredientIcon != null)
            {
                // Show actual icon for collected ingredients
                ingredientIcon.sprite = ingredientData.ingredientIcon;
                ingredientIcon.color = collectedColor;
                ingredientIcon.preserveAspect = true;
            }
            else
            {
                // Show silhouette for uncollected ingredients
                ingredientIcon.sprite = silhouetteSprite != null ? silhouetteSprite : ingredientData.ingredientIcon;
                ingredientIcon.color = silhouetteColor;
                ingredientIcon.preserveAspect = true;
                
                // Log silhouette info for debugging
                if (silhouetteSprite != null)
                {
                    Debug.Log($"Using silhouette: {silhouetteSprite.name} with size: {silhouetteSprite.rect.size}");
                }
            }
        }
        
        // Update ingredient name text
        if (ingredientNameText != null)
        {
            ingredientNameText.text = ingredientData.ingredientName;
            // Optional: Change text color based on collection status
            ingredientNameText.color = isCollected ? Color.black : Color.gray;
        }
        
        // Update overlay states
        if (collectedOverlay != null)
        {
            collectedOverlay.SetActive(isCollected);
        }
        
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isCollected);
        }
    }
    
    public void SetCollected(bool collected)
    {
        isCollected = collected;
        UpdateAppearance();
        
        // Update button interactivity
        if (ingredientButton != null)
        {
            ingredientButton.interactable = isCollected;
        }
    }
    
    private void OnIngredientClicked()
    {
        if (isCollected && bookManager != null)
        {
            bookManager.ShowIngredientInfo(ingredientData);
        }
        else
        {
            // Optional: Play a sound or show a message that this ingredient isn't collected yet
            Debug.Log($"Ingredient {ingredientData.ingredientName} not collected yet!");
        }
    }
    
    // Public method to check if this ingredient is collected
    public bool IsCollected()
    {
        return isCollected;
    }
    
    // Public method to get the ingredient data
    public BookInteractable.IngredientData GetIngredientData()
    {
        return ingredientData;
    }
    
    // Public method to set a specific silhouette sprite
    public void SetSilhouetteSprite(Sprite silhouette)
    {
        silhouetteSprite = silhouette;
        UpdateAppearance();
    }
}