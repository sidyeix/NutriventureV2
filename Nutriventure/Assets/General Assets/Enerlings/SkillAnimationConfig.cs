using UnityEngine;

[CreateAssetMenu(fileName = "SkillAnimationConfig", menuName = "NutriVenture/Skill Animation Config")]
public class SkillAnimationConfig : ScriptableObject
{
    [System.Serializable]
    public class EnerlingAnimationMap
    {
        public string enerlingName;
        
        [Header("Animation Types (0=Dash, 1=Jump, 2=Range, 3=GetHit, 4=Defend)")]
        public int skill1Animation = 0; // Calcium: Dash
        public int skill2Animation = 1; // Calcium: Jump & Attack
        public int skill3Animation = 2; // Calcium: Range Attack
        public int defendAnimation = 4; // Defend Skill
        public int getHitAnimation = 3; // Get Hit
        
        [Header("Optional: Custom Animation Names")]
        public string skill1AnimationName;
        public string skill2AnimationName;
        public string skill3AnimationName;
        public string defendAnimationName;
        public string getHitAnimationName;
    }
    
    [Header("Enerling Animation Mappings")]
    public EnerlingAnimationMap[] animationMaps;
    
    public EnerlingAnimationMap GetAnimationMap(string enerlingName)
    {
        if (animationMaps == null) return CreateDefaultMap(enerlingName);
        
        foreach (var map in animationMaps)
        {
            if (map.enerlingName == enerlingName)
                return map;
        }
        
        // Return default map if not found
        return CreateDefaultMap(enerlingName);
    }
    
    private EnerlingAnimationMap CreateDefaultMap(string enerlingName)
    {
        return new EnerlingAnimationMap
        {
            enerlingName = enerlingName,
            skill1Animation = 0,
            skill2Animation = 1,
            skill3Animation = 2,
            defendAnimation = 4,
            getHitAnimation = 3
        };
    }
}