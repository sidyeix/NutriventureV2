using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnerlingButtonController : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI enerlingNameText;
    public Image frameImage; // Add this to reference the Frame image

    private string enerlingName;
    private EnerlingSelectionManager manager;

    public void Initialize(string name, Sprite icon, IngredientDatabase.Rarity rarity, IngredientDatabase database)
    {
        enerlingName = name;
        manager = GetComponentInParent<EnerlingSelectionManager>();

        // Set name text and initially hide it
        if (enerlingNameText != null)
        {
            enerlingNameText.text = name;
            enerlingNameText.gameObject.SetActive(false);
        }

        // Set icon if needed
        Image iconImage = GetComponent<Image>();
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
        }

        // Set frame based on rarity
        if (frameImage != null && database != null)
        {
            Sprite frameSprite = database.GetFrameSprite(rarity);
            if (frameSprite != null)
            {
                frameImage.sprite = frameSprite;
                Debug.Log($"Set frame for {name} to {rarity} frame");
            }
        }
        else
        {
            Debug.LogWarning($"FrameImage or Database is null for {name}");
        }
    }

    // Highlight this button and activate/deactivate name text
    public void SetHighlight(bool highlight)
    {
        // Get the button component
        Button button = GetComponent<Button>();
        if (button != null)
        {
            // Set button color based on highlight state
            button.image.color = highlight ?
                new Color(0.52f, 0.52f, 0.52f, 1f) : // Selected color
                Color.white; // Normal color
        }

        // Activate/deactivate the name text
        if (enerlingNameText != null)
        {
            enerlingNameText.gameObject.SetActive(highlight);
        }
    }
}