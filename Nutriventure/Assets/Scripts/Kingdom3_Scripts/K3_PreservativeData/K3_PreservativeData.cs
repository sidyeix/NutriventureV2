using UnityEngine;
using System;

[CreateAssetMenu(fileName = "K3_PreservativeData", menuName = "Preservative/K3_Preservative Data")]
public class K3_PreservativeData : ScriptableObject
{
    [Serializable]
    public class PreservativeInfo
    {
        [Header("Basic Info")]
        public string preservativeID; // Unique identifier
        public string displayName;
        
        [Header("Visuals")]
        public GameObject preservativePrefab; // The 3D model prefab
        public Sprite preservativeIcon; // 2D sprite icon for UI
        
        [Header("Information")]
        [TextArea(3, 5)] public string preservDesc;
        [TextArea(3, 5)] public string strengthsLimits;
        [TextArea(2, 4)] public string foundIn;
        [TextArea(2, 4)] public string funFact;
    }
    
    public PreservativeInfo[] allPreservatives;
    
    // Helper methods
    public PreservativeInfo GetPreservativeInfo(string preservativeID)
    {
        foreach (var preservative in allPreservatives)
        {
            if (preservative.preservativeID == preservativeID)
                return preservative;
        }
        return null;
    }
    
    public int GetTotalCount()
    {
        return allPreservatives.Length;
    }
    
    // Helper method for accessing preservative icon
    public Sprite GetPreservativeIcon(string preservativeID)
    {
        PreservativeInfo preservative = GetPreservativeInfo(preservativeID);
        return preservative?.preservativeIcon;
    }
}