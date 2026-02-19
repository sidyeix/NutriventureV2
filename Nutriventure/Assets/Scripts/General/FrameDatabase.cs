using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FrameDatabase", menuName = "Game/Frame Database")]
public class FrameDatabase : ScriptableObject
{
    public List<FrameData> frames = new List<FrameData>();

    [System.Serializable]
    public class FrameData
    {
        public string id;
        public string frameName;
        public Sprite frameSprite; // The frame image to display
        public bool unlockedByDefault; // Frames available from start
    }

    // Get frame by ID
    public FrameData GetFrame(string id)
    {
        return frames.Find(f => f.id == id);
    }

    // Get frame sprite by ID
    public Sprite GetFrameSprite(string id)
    {
        var frame = GetFrame(id);
        return frame != null ? frame.frameSprite : null;
    }

    // Get default frame (first one)
    public FrameData GetDefaultFrame()
    {
        return frames.Count > 0 ? frames[0] : null;
    }

    // Get all frames that are unlocked for the player
    public List<FrameData> GetUnlockedFrames(List<string> playerUnlockedIds)
    {
        List<FrameData> unlocked = new List<FrameData>();

        foreach (var frame in frames)
        {
            if (frame.unlockedByDefault || (playerUnlockedIds != null && playerUnlockedIds.Contains(frame.id)))
            {
                unlocked.Add(frame);
            }
        }

        return unlocked;
    }

    // Check if frame is unlocked
    public bool IsFrameUnlocked(string id, List<string> playerUnlockedIds)
    {
        var frame = GetFrame(id);
        if (frame == null) return false;

        return frame.unlockedByDefault || (playerUnlockedIds != null && playerUnlockedIds.Contains(id));
    }
}