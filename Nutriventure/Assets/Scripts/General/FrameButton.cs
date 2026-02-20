using UnityEngine;
using UnityEngine.UI;

public class FrameButton : MonoBehaviour
{
    [Header("UI References")]
    public Image frameImage; // The frame image
    public GameObject lockedOverlay; // GameObject to show when locked (e.g., lock icon)
    public GameObject selectedIndicator; // GameObject to show when selected (e.g., highlight border)
    public Button button;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray color for locked frames

    private FrameDatabase.FrameData frameData;
    private System.Action<FrameDatabase.FrameData> onClickCallback;
    private bool isSelected = false;
    private bool isLocked = false;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(OnButtonClick);
    }

    public void Initialize(FrameDatabase.FrameData data, System.Action<FrameDatabase.FrameData> callback, bool locked, bool selected)
    {
        frameData = data;
        onClickCallback = callback;
        isLocked = locked;
        isSelected = selected;

        Debug.Log($"FrameButton Initialize: {data.frameName}, Locked: {isLocked}, Selected: {isSelected}");

        // Set visuals
        if (frameImage != null && frameData.frameSprite != null)
            frameImage.sprite = frameData.frameSprite;

        // Show/hide locked overlay based on lock state
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(isLocked);
            Debug.Log($"Frame {data.frameName} - LockedOverlay active: {isLocked}");
        }

        // Show/hide selected indicator based on selected state
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(isSelected && !isLocked);
            Debug.Log($"Frame {data.frameName} - SelectedIndicator active: {isSelected && !isLocked}");
        }

        // Update appearance based on state
        UpdateAppearance();

        // Disable button if locked
        button.interactable = !isLocked;
        Debug.Log($"Frame {data.frameName} - Button interactable: {!isLocked}");
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        Debug.Log($"Frame {frameData?.frameName} - SetSelected: {selected}");

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
            // When locked, make the frame image gray
            frameImage.color = lockedColor;
            // Locked overlay remains its original color
            Debug.Log($"Frame {frameData?.frameName} - Appearance: Locked (gray)");
        }
        else if (isSelected)
        {
            frameImage.color = selectedColor;
            Debug.Log($"Frame {frameData?.frameName} - Appearance: Selected (yellow)");
        }
        else
        {
            frameImage.color = normalColor;
            Debug.Log($"Frame {frameData?.frameName} - Appearance: Normal (white)");
        }
    }

    private void OnButtonClick()
    {
        if (!isLocked)
        {
            Debug.Log($"FrameButton Clicked: {frameData?.frameName} (Unlocked)");
            onClickCallback?.Invoke(frameData);

            // Play click sound
            if (AudioHandler.Instance != null)
            {
                AudioHandler.Instance.PlayButtonClick();
            }
        }
        else
        {
            Debug.Log($"FrameButton Clicked: {frameData?.frameName} (Locked - ignoring)");
        }
    }

    public string GetFrameId()
    {
        return frameData != null ? frameData.id : "";
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