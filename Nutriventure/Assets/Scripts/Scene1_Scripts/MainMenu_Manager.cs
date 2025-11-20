using UnityEngine;          
using System.Collections;   
using System.Collections.Generic; 
using System;               
using UnityEngine.UI;        
using UnityEngine.SceneManagement; 
using TMPro;                 

public class MainMenu_Manager : MonoBehaviour
{
    public GameObject SettingsPanel;
    
    // Character Selection References
    [Header("Character Selection References")]
    public Transform characterSpawnPoint;
    public List<GameObject> characterPrefabs;
    public List<Button> characterButtons;
    
    [Header("Character Selection Settings")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    
    // Local character data - temporary until saved
    private GameObject currentSpawnedCharacter;
    private int currentSelectedCharacterIndex = 0;
    private List<bool> unlockedCharacters = new List<bool>() { true, true, true, true, true, true };

    void Start()
    {
        SettingsPanel.SetActive(false);
        
        // Wait for GameDataManager to be ready, then initialize
        StartCoroutine(InitializeAfterFrame());
    }

    private IEnumerator InitializeAfterFrame()
    {
        // Wait for GameDataManager to initialize
        yield return null;
        
        // If GameDataManager exists, load the saved character, otherwise use default
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            currentSelectedCharacterIndex = GameDataManager.Instance.CurrentGameData.selectedCharacterIndex;
        }
        
        InitializeCharacterSystem();
    }

    public void ToggleSettings()
    {
        SettingsPanel.SetActive(!SettingsPanel.activeSelf);
    }
    
    // START BUTTON - This is where we save to GameDataManager and transition
    public void OnStartButtonClicked()
    {
        Debug.Log($"Start button clicked - Saving character {currentSelectedCharacterIndex} to GameDataManager");
        
        // Save the currently selected character to GameDataManager
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.selectedCharacterIndex = currentSelectedCharacterIndex;
            GameDataManager.Instance.SaveGameData();
            Debug.Log($"Character {currentSelectedCharacterIndex} saved to GameDataManager");
        }
        else
        {
            Debug.LogWarning("GameDataManager not found - character selection won't be persisted");
        }
        
        // Transition to next scene
        LoadNextScene();
    }
    
    private void LoadNextScene()
    {
        // Replace "GameScene" with your actual game scene name
        SceneManager.LoadScene("2_Lobby");
    }
    
    // Character Selection Methods
    private void InitializeCharacterSystem()
    {
        // Spawn initial character
        SpawnCharacter(currentSelectedCharacterIndex);
        UpdateButtonAppearance();
        
        // Clear existing listeners first to prevent duplicates
        foreach (Button button in characterButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
        
        // Set up button listeners
        for (int i = 0; i < characterButtons.Count; i++)
        {
            int index = i;
            characterButtons[i].onClick.AddListener(() => OnCharacterSelected(index));
        }
        
        Debug.Log($"Character system initialized with character: {currentSelectedCharacterIndex}");
    }
    
    private void OnCharacterSelected(int characterIndex)
    {
        Debug.Log($"Character {characterIndex} selected");
        
        if (characterIndex == currentSelectedCharacterIndex) 
        {
            Debug.Log("Same character selected, ignoring");
            return;
        }
        
        // Check if character is unlocked
        if (!unlockedCharacters[characterIndex])
        {
            Debug.Log("Character is locked!");
            return;
        }
        
        // Update LOCAL data only - not saved to GameDataManager yet
        currentSelectedCharacterIndex = characterIndex;
        
        // Spawn new character
        SpawnCharacter(characterIndex);
        UpdateButtonAppearance();
        
        Debug.Log($"Character preview changed to: {characterIndex} (Not saved yet)");
    }
    
    private void SpawnCharacter(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characterPrefabs.Count)
        {
            Debug.LogError($"Invalid character index: {characterIndex}");
            return;
        }
        
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
        }
        
        currentSpawnedCharacter = Instantiate(
            characterPrefabs[characterIndex], 
            characterSpawnPoint.position, 
            characterSpawnPoint.rotation
        );
        
        Debug.Log($"Spawned character preview: {characterPrefabs[characterIndex].name}");
    }
    
    private void UpdateButtonAppearance()
    {
        for (int i = 0; i < characterButtons.Count; i++)
        {
            Image buttonImage = characterButtons[i].GetComponent<Image>();
            Button button = characterButtons[i];
            
            if (buttonImage != null)
            {
                // Visual feedback for selection
                buttonImage.color = (i == currentSelectedCharacterIndex) ? selectedColor : normalColor;
                
                // Visual feedback for locked characters
                if (!unlockedCharacters[i])
                {
                    buttonImage.color = Color.gray;
                    button.interactable = false;
                }
                else
                {
                    button.interactable = true;
                }
            }
        }
    }
    
    // Method to get the selected character for the next scene
    public GameObject GetCurrentCharacterPrefab()
    {
        return characterPrefabs[currentSelectedCharacterIndex];
    }
    
    public int GetCurrentCharacterIndex()
    {
        return currentSelectedCharacterIndex;
    }
    
    // Helper methods to manage unlocked characters if needed
    public void UnlockCharacter(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < unlockedCharacters.Count)
        {
            unlockedCharacters[characterIndex] = true;
            UpdateButtonAppearance();
        }
    }
    
    public void LockCharacter(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < unlockedCharacters.Count)
        {
            unlockedCharacters[characterIndex] = false;
            UpdateButtonAppearance();
        }
    }
}