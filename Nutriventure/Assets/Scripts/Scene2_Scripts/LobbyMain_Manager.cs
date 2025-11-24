using UnityEngine;
using System.Collections;

public class LobbyMain_Manager : MonoBehaviour
{
    [Header("Character System")]
    public Transform characterSpawnPointinLobby; // Keep for fallback
    public CharacterDatabase characterDatabase;

    [Header("Visual Swapping")]
    public CharacterVisualSwapper playerVisualSwapper; // Assign this in inspector

    private GameObject currentSpawnedCharacter; // Keep for fallback

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

        // Use visual swapper if available, otherwise fallback to old system
        if (playerVisualSwapper != null)
        {
            ApplyCharacterVisuals(selectedCharacterID);
        }
        else
        {
            SpawnCharacterInLobby(selectedCharacterID);
        }

        ApplyCharacterAttributes(selectedCharacterID);
    }

    private void ApplyCharacterVisuals(int characterID)
    {
        CharacterDatabase.CharacterData characterData = characterDatabase.GetCharacterByID(characterID);
        if (characterData == null)
        {
            Debug.LogError($"Character data not found for ID: {characterID}. Using default character.");
            characterData = characterDatabase.GetCharacterByID(0); // Use default
        }

        playerVisualSwapper.ApplyCharacterVisuals(characterData);
    }

    // Keep old method as fallback
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

        // Apply to your ThirdPersonController if needed
        // ApplyToPlayerController(characterData);
    }

    // Optional: Apply attributes to ThirdPersonController
    private void ApplyToPlayerController(CharacterDatabase.CharacterData characterData)
    {
        // Example: Get your ThirdPersonController and apply multipliers
        /*
        ThirdPersonController controller = FindObjectOfType<ThirdPersonController>();
        if (controller != null)
        {
            // Apply your speed, jump force multipliers here
            // Make sure to reset to base values when switching characters
        }
        */
    }

    // Optional: Get the current character data
    public CharacterDatabase.CharacterData GetCurrentCharacterData()
    {
        return characterDatabase.GetCharacterByID(GameDataManager.Instance.CurrentGameData.selectedCharacterID);
    }

    // Updated method to use visual swapper
    public void UpdateCharacterInLobby(int newCharacterID)
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.selectedCharacterID = newCharacterID;
            GameDataManager.Instance.SaveGameData();

            if (playerVisualSwapper != null)
            {
                ApplyCharacterVisuals(newCharacterID);
            }
            else
            {
                SpawnCharacterInLobby(newCharacterID);
            }

            ApplyCharacterAttributes(newCharacterID);
        }
    }
}