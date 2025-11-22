using UnityEngine;
using System.Collections;

public class LobbyMain_Manager : MonoBehaviour
{
    public Transform characterSpawnPointinLobby;
    public CharacterDatabase characterDatabase;
    
    private GameObject currentSpawnedCharacter;
    
    void Start()
    {
        // Wait a frame to ensure GameDataManager is ready
        StartCoroutine(InitializeAfterFrame());
    }

    private IEnumerator InitializeAfterFrame()
    {
        yield return null; // Wait one frame
        
        // Check if GameDataManager is available
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
        {
            Debug.LogError("GameDataManager not found or data not loaded!");
            yield break;
        }
        
        if (characterDatabase == null)
        {
            Debug.LogError("CharacterDatabase not assigned!");
            yield break;
        }
        
        int selectedCharacterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
        Debug.Log($"Loading character ID: {selectedCharacterID} in lobby");
        
        SpawnCharacterInLobby(selectedCharacterID);
        ApplyCharacterAttributes(selectedCharacterID);
    }
    
    private void SpawnCharacterInLobby(int characterID)
    {
        CharacterDatabase.CharacterData characterData = characterDatabase.GetCharacterByID(characterID);
        if (characterData == null)
        {
            Debug.LogError($"Character data not found for ID: {characterID}. Using default character.");
            characterData = characterDatabase.GetCharacterByID(0); // Use default
        }
        
        // Destroy existing character if any
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
        }
        
        // Spawn the selected character
        currentSpawnedCharacter = Instantiate(
            characterData.characterPrefab, 
            characterSpawnPointinLobby.position, 
            characterSpawnPointinLobby.rotation
        );
        
        Debug.Log($"Spawned character in lobby: {characterData.characterName}");
    }
    
    private void ApplyCharacterAttributes(int characterID)
    {
        CharacterDatabase.CharacterData characterData = characterDatabase.GetCharacterByID(characterID);
        if (characterData == null) return;
        
        // Apply character attributes to your game systems
        Debug.Log($"Applied attributes for {characterData.characterName}: " +
                 $"Speed: {characterData.speedMultiplier}x, " +
                 $"Jump: {characterData.jumpForceMultiplier}x, " +
                 $"Health: {characterData.healthMultiplier}x");
        
        // Example: Apply to player controller (you'll need to implement this)
        // PlayerController player = currentSpawnedCharacter.GetComponent<PlayerController>();
        // if (player != null)
        // {
        //     player.speed *= characterData.speedMultiplier;
        //     player.jumpForce *= characterData.jumpForceMultiplier;
        //     player.maxHealth *= characterData.healthMultiplier;
        // }
    }
    
    // Optional: Get the current character data
    public CharacterDatabase.CharacterData GetCurrentCharacterData()
    {
        return characterDatabase.GetCharacterByID(GameDataManager.Instance.CurrentGameData.selectedCharacterID);
    }
    
    // Optional: Method to update character if needed during runtime
    public void UpdateCharacterInLobby(int newCharacterID)
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.selectedCharacterID = newCharacterID;
            GameDataManager.Instance.SaveGameData();
            SpawnCharacterInLobby(newCharacterID);
            ApplyCharacterAttributes(newCharacterID);
        }
    }
}