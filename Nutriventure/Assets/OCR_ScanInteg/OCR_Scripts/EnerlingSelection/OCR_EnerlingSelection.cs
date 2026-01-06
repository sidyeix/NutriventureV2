using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OCR_EnerlingSelection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private IngredientDatabase ingredientDatabase;
    [SerializeField] private Button selectionButton;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private GameObject enSelectPanel;
    
    [Header("New References")]
    [SerializeField] private Button exitPanelButton;
    [SerializeField] private Button exitButtonToDisable;
    
    [Header("Button Components")]
    [SerializeField] private Image visualProfileImage;
    [SerializeField] private TextMeshProUGUI nameText;
    
    private void Start()
    {
        selectionButton.gameObject.SetActive(false);
        exitPanelButton.gameObject.SetActive(false);
        
        selectionButton.onClick.AddListener(OnButtonClick);
        exitPanelButton.onClick.AddListener(OnExitPanelClick);
        
        SetDefaultUnlockedCharacter();
    }
    
    private void Update()
    {
        selectionButton.gameObject.SetActive(resultsPanel.activeSelf);
        exitPanelButton.gameObject.SetActive(enSelectPanel.activeSelf);
        
        if (exitButtonToDisable != null)
            exitButtonToDisable.interactable = !enSelectPanel.activeSelf;
    }
    
    private void SetDefaultUnlockedCharacter()
    {
        var unlocked = ingredientDatabase.GetUnlockedIngredients();
        
        if (unlocked.Count > 0)
        {
            SetButtonVisuals(unlocked[0]);
        }
        else if (ingredientDatabase.ingredients.Count > 0)
        {
            SetButtonVisuals(ingredientDatabase.ingredients[0]);
        }
    }
    
    private void SetButtonVisuals(IngredientDatabase.IngredientInfo ingredient)
    {
        if (visualProfileImage != null && ingredient.enerlingSprite != null)
        {
            visualProfileImage.sprite = ingredient.enerlingSprite;
        }
        
        if (nameText != null)
        {
            nameText.text = ingredient.ingredientName;
        }
    }
    
    private void OnButtonClick()
    {
        enSelectPanel.SetActive(true);
    }
    
    private void OnExitPanelClick()
    {
        enSelectPanel.SetActive(false);
    }
    
    private void OnDestroy()
    {
        selectionButton.onClick.RemoveListener(OnButtonClick);
        exitPanelButton.onClick.RemoveListener(OnExitPanelClick);
    }
}