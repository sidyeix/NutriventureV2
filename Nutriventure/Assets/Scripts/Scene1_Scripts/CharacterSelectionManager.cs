using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("References")]
    public Transform characterSpawnPoint;
    public List<GameObject> characterPrefabs;
    public List<Button> characterButtons;
    
    [Header("Settings")]
    public int defaultCharacterIndex = 0;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    
    private GameObject currentSpawnedCharacter;
    private int currentSelectedIndex = -1;
    
    void Start()
    {
        InitializeCharacterSystem();
    }
    
    private void InitializeCharacterSystem()
    {
        // Validate setup
        if (characterPrefabs.Count != characterButtons.Count)
        {
            Debug.LogError("Prefabs and buttons count mismatch!");
            return;
        }
        
        // Set up button listeners
        for (int i = 0; i < characterButtons.Count; i++)
        {
            int index = i; // Important: capture the index for the closure
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }
        
        // Spawn default character
        if (defaultCharacterIndex >= 0 && defaultCharacterIndex < characterPrefabs.Count)
        {
            SelectCharacter(defaultCharacterIndex);
        }
        else
        {
            SelectCharacter(0); // Fallback to first character
        }
    }
    
    public void SelectCharacter(int characterIndex)
    {
        // Don't reselect the same character
        if (characterIndex == currentSelectedIndex) return;
        
        // Validate index
        if (characterIndex < 0 || characterIndex >= characterPrefabs.Count)
        {
            Debug.LogError("Invalid character index: " + characterIndex);
            return;
        }
        
        // Remove current character
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
        }
        
        // Spawn new character
        currentSpawnedCharacter = Instantiate(
            characterPrefabs[characterIndex], 
            characterSpawnPoint.position, 
            characterSpawnPoint.rotation
        );
        
        // Update selection
        currentSelectedIndex = characterIndex;
        UpdateButtonAppearance();
        
        Debug.Log("Character " + characterIndex + " spawned at " + characterSpawnPoint.position);
    }
    
    private void UpdateButtonAppearance()
    {
        for (int i = 0; i < characterButtons.Count; i++)
        {
            Image buttonImage = characterButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = (i == currentSelectedIndex) ? selectedColor : normalColor;
            }
        }
    }
}