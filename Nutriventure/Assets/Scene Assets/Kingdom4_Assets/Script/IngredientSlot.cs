using UnityEngine;
using UnityEngine.UI;

public class IngredientSlot : MonoBehaviour
{
    public Image ingredientIcon;
    public Button ingredientButton;
    
    private BookUIManager.IngredientData ingredientData;
    private BookUIManager bookManager;
    
    public void Initialize(BookUIManager.IngredientData data, BookUIManager manager)
    {
        ingredientData = data;
        bookManager = manager;
        
        if (ingredientIcon != null && data.icon != null)
        {
            ingredientIcon.sprite = data.icon;
        }
            
        if (ingredientButton != null)
        {
            ingredientButton.onClick.AddListener(OnIngredientClicked);
        }
    }
    
    private void OnIngredientClicked()
    {
        bookManager.ShowIngredientInfo(ingredientData);
    }
}