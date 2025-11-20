using UnityEngine;
using System.Collections;

public class LobbyMain_Manager : MonoBehaviour
{
    public Transform characterSpawnPointinLobby;
    public GameObject[] characterPrefabs; // Assign your character prefabs in Inspector
    
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
        
        int selectedCharacterIndex = GameDataManager.Instance.CurrentGameData.selectedCharacterIndex;
        Debug.Log($"Loading character index: {selectedCharacterIndex} in lobby");
        
        SpawnCharacterInLobby(selectedCharacterIndex);
    }
    
    private void SpawnCharacterInLobby(int characterIndex)
    {
        // Validate the index
        if (characterIndex < 0 || characterIndex >= characterPrefabs.Length)
        {
            Debug.LogError($"Invalid character index: {characterIndex}. Using default character 0.");
            characterIndex = 0;
        }
        
        // Destroy existing character if any
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
        }
        
        // Spawn the selected character
        currentSpawnedCharacter = Instantiate(
            characterPrefabs[characterIndex], 
            characterSpawnPointinLobby.position, 
            characterSpawnPointinLobby.rotation
        );
        
        Debug.Log($"Spawned character in lobby: {characterPrefabs[characterIndex].name}");
    }
    
    // Optional: Method to update character if needed during runtime
    public void UpdateCharacterInLobby(int newCharacterIndex)
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.selectedCharacterIndex = newCharacterIndex;
            GameDataManager.Instance.SaveGameData();
            SpawnCharacterInLobby(newCharacterIndex);
        }
    }
    
    // Optional: Get the currently spawned character
    public GameObject GetCurrentSpawnedCharacter()
    {
        return currentSpawnedCharacter;
    }
}