using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerArmatureClickHandler : MonoBehaviour, IPointerClickHandler
{
    private CharacterSelectionController characterSelectionController;

    void Start()
    {
        // Find the CharacterSelectionController in the scene
        characterSelectionController = FindObjectOfType<CharacterSelectionController>();

        if (characterSelectionController == null)
        {
            Debug.LogError("CharacterSelectionController not found in scene!");
        }
    }

    // This method is called when the PlayerArmature is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (characterSelectionController != null)
        {
            characterSelectionController.OnPlayerArmatureClicked();
        }
    }
}