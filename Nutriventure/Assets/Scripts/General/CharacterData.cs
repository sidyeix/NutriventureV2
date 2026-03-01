using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [System.Serializable]
    public class SkinData
    {
        [Header("Skin Info")]
        public string skinName;
        public int skinID;
        public GameObject skinPrefab; // The actual mesh/prefab for this skin
        public Avatar skinAvatar;
        public TimelineAsset skinTimeline;
        public Sprite skinIcon;

        [TextArea]
        public string skinDescription;

        [Header("Visual Settings")]
        public Color skinColor = Color.white;
        public AudioClip selectionSound;

        [Header("Unlock Requirements")]
        public bool unlock = false;
        public bool isSkinReward = false;
        public int nutrigemsToUnlock = 0;
        public string taskToUnlock = "Complete challenges to unlock";
    }

    [System.Serializable]
    public class CharacterData
    {
        [Header("Basic Info")]
        public string characterName;
        public int characterID;
        public GameObject characterPrefab;
        public Sprite characterIcon;
        public Avatar characterAvatar;

        [Header("Character Logo")]
        public Sprite characterLogo;

        [Header("Character Tagline")]
        public string characterTagline;

        [Header("Skins")]
        public List<SkinData> skins = new List<SkinData>();

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

    // Character Helper methods
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

    // Skin Helper methods
    public SkinData GetSkinByID(int characterID, int skinID)
    {
        CharacterData character = GetCharacterByID(characterID);
        if (character == null) return null;

        return character.skins.Find(s => s.skinID == skinID);
    }

    public SkinData GetDefaultSkin(int characterID)
    {
        CharacterData character = GetCharacterByID(characterID);
        if (character == null || character.skins.Count == 0) return null;

        return character.skins[0];
    }

    public int GetSkinCount(int characterID)
    {
        CharacterData character = GetCharacterByID(characterID);
        return character?.skins.Count ?? 0;
    }

    public bool IsCharacterUnlocked(int characterID, GameData gameData)
    {
        CharacterData character = GetCharacterByID(characterID);
        if (character == null)
        {
            Debug.LogWarning($"Character {characterID} not found in database!");
            return false;
        }

        if (character.unlockedByDefault)
        {
            return true;
        }

        return gameData.unlockedCharacterIDs.Contains(characterID);
    }

    public CharacterData GetCharacterByName(string characterName)
    {
        return characters.Find(c => c.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase));
    }

    public (int characterId, int skinId)? GetSkinByName(string skinName)
    {
        foreach (var character in characters)
        {
            foreach (var skin in character.skins)
            {
                if (skin.skinName.Equals(skinName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return (character.characterID, skin.skinID);
                }
            }
        }
        return null;
    }
}