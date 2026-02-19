using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ProfileIconDatabase", menuName = "Game/Profile Icon Database")]
public class ProfileIconDatabase : ScriptableObject
{
    public List<ProfileIcon> icons = new List<ProfileIcon>();

    [System.Serializable]
    public class ProfileIcon
    {
        public string id;
        public string iconName;
        public Sprite iconSprite;
        public bool unlockedByDefault; // Icons available from start
    }

    // Get icon by ID
    public ProfileIcon GetIcon(string id)
    {
        return icons.Find(i => i.id == id);
    }

    // Get icon sprite by ID
    public Sprite GetIconSprite(string id)
    {
        var icon = GetIcon(id);
        return icon != null ? icon.iconSprite : null;
    }

    // Get default icon (first one)
    public ProfileIcon GetDefaultIcon()
    {
        return icons.Count > 0 ? icons[0] : null;
    }

    // Get all icons that are unlocked for the player
    public List<ProfileIcon> GetUnlockedIcons(List<string> playerUnlockedIds)
    {
        List<ProfileIcon> unlocked = new List<ProfileIcon>();

        foreach (var icon in icons)
        {
            if (icon.unlockedByDefault || (playerUnlockedIds != null && playerUnlockedIds.Contains(icon.id)))
            {
                unlocked.Add(icon);
            }
        }

        return unlocked;
    }

    // Check if icon is unlocked
    public bool IsIconUnlocked(string id, List<string> playerUnlockedIds)
    {
        var icon = GetIcon(id);
        if (icon == null) return false;

        return icon.unlockedByDefault || (playerUnlockedIds != null && playerUnlockedIds.Contains(id));
    }
}