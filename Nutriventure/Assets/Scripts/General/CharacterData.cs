using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [System.Serializable]
    public class CharacterData
    {
        [Header("Basic Info")]
        public string characterName;
        public int characterID;
        public GameObject characterPrefab;
        public Sprite characterIcon;
        public Avatar characterAvatar;    // Humanoid avatar

        
        [Header("Gameplay Attributes")]
        public float speedMultiplier = 1f;
        public float jumpForceMultiplier = 1f;
        public float healthMultiplier = 1f;
        public float energyMultiplier = 1f;
        
        [Header("Unlock Requirements")]
        public bool unlockedByDefault = false;
        public int coinsToUnlock = 0;
        public int levelRequirement = 1;
        
        [Header("Visual & Audio")]
        public Color characterColor = Color.white;
        public AudioClip selectionSound;
        
        [TextArea]
        public string characterDescription;
    }

    public List<CharacterData> characters = new List<CharacterData>();
    
    // Helper methods
    public CharacterData GetCharacterByID(int characterID)
    {
        return characters.Find(c => c.characterID == characterID);
    }
    
    public CharacterData GetCharacterByIndex(int index)
    {
        if (index >= 0 && index < characters.Count)
            return characters[index];
        return null;
    }
    
    public GameObject GetCharacterPrefab(int characterID)
    {
        CharacterData character = GetCharacterByID(characterID);
        return character?.characterPrefab;
    }
    
    public int GetCharacterCount()
    {
        return characters.Count;
    }
    
    public bool IsCharacterUnlocked(int characterID, GameData gameData)
    {
        CharacterData character = GetCharacterByID(characterID);
        if (character == null) return false;
        
        if (character.unlockedByDefault) return true;
        
        return gameData.unlockedCharacterIDs.Contains(characterID);
    }
}