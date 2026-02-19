using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileIconButton : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public GameObject lockedOverlay; // GameObject to show when locked (e.g., lock icon)
    public GameObject selectedIndicator; // GameObject to show when selected (e.g., highlight border)
    public Button button;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray color for locked icons

    private ProfileIconDatabase.ProfileIcon iconData;
    private System.Action<ProfileIconDatabase.ProfileIcon> onClickCallback;
    private bool isSelected = false;
    private bool isLocked = false;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(OnButtonClick);
    }

    public void Initialize(ProfileIconDatabase.ProfileIcon data, System.Action<ProfileIconDatabase.ProfileIcon> callback, bool locked, bool selected)
    {
        iconData = data;
        onClickCallback = callback;
        isLocked = locked;
        isSelected = selected;

        // Set visuals
        if (iconImage != null && iconData.iconSprite != null)
            iconImage.sprite = iconData.iconSprite;

        if (nameText != null)
            nameText.text = iconData.iconName;

        // Show/hide locked overlay based on lock state
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(isLocked);
        }

        // Show/hide selected indicator based on selected state
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(isSelected && !isLocked);
        }

        // Update appearance based on state
        UpdateAppearance();

        // Disable button if locked
        button.interactable = !isLocked;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        // Update selected indicator
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(isSelected && !isLocked);
        }

        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (isLocked)
        {
            // When locked, make the icon image gray
            iconImage.color = lockedColor;
            if (nameText != null) nameText.color = lockedColor;
            // Locked overlay remains its original color
        }
        else if (isSelected)
        {
            iconImage.color = selectedColor;
            if (nameText != null) nameText.color = selectedColor;
        }
        else
        {
            iconImage.color = normalColor;
            if (nameText != null) nameText.color = normalColor;
        }
    }

    private void OnButtonClick()
    {
        if (!isLocked)
        {
            onClickCallback?.Invoke(iconData);

            // Play click sound
            if (AudioHandler.Instance != null)
            {
                AudioHandler.Instance.PlayButtonClick();
            }
        }
    }

    public string GetIconId()
    {
        return iconData != null ? iconData.id : "";
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public bool IsLocked()
    {
        return isLocked;
    }
}