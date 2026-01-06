using UnityEngine;

public class EnerlingAnimationManager : MonoBehaviour
{
    [Header("Database Reference")]
    [SerializeField] private IngredientDatabase ingredientDatabase;
    
    [Header("Animation Config")]
    [SerializeField] private SkillAnimationConfig animationConfig; // Changed from animationData
    
    [Header("Enerling Identity")]
    [SerializeField] private string enerlingName = "Calcium";
    
    [Header("Animator Reference")]
    [SerializeField] private Animator animator;
    
    // Cached references
    private IngredientDatabase.IngredientInfo enerlingInfo;
    private SkillAnimationConfig.EnerlingAnimationMap animationMap; // Changed type
    
    void Awake()
    {
        Initialize();
    }
    
    [ContextMenu("Initialize Enerling")]
    public void Initialize()
    {
        // Get animator if not set
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Load from database
        if (ingredientDatabase != null)
        {
            enerlingInfo = ingredientDatabase.GetIngredientInfo(enerlingName);
            
            if (enerlingInfo != null)
            {
                // Set animator controller from database
                if (enerlingInfo.animatorController != null)
                {
                    animator.runtimeAnimatorController = enerlingInfo.animatorController;
                    Debug.Log($"Set animator controller for {enerlingName}");
                }
            }
            else
            {
                Debug.LogWarning($"Enerling '{enerlingName}' not found in database!");
            }
        }
        
        // Load animation mappings
        if (animationConfig != null)
        {
            animationMap = animationConfig.GetAnimationMap(enerlingName);
            
            if (animationMap != null)
            {
                Debug.Log($"Loaded animation map for {enerlingName}");
            }
            else
            {
                Debug.LogWarning($"No animation map found for {enerlingName}, using defaults");
                CreateDefaultMap();
            }
        }
        else
        {
            Debug.LogWarning("No Animation Config assigned! Creating default map.");
            CreateDefaultMap();
        }
    }
    
    void CreateDefaultMap()
    {
        // Create a default map
        animationMap = new SkillAnimationConfig.EnerlingAnimationMap
        {
            enerlingName = enerlingName,
            skill1Animation = 0,
            skill2Animation = 1,
            skill3Animation = 2,
            defendAnimation = 4,
            getHitAnimation = 3
        };
    }
    
    // ========== PUBLIC ANIMATION METHODS ==========
    
    public void PlaySkill1()
    {
        PlayAnimation(animationMap.skill1Animation, animationMap.skill1AnimationName); // Fixed property name
    }
    
    public void PlaySkill2()
    {
        PlayAnimation(animationMap.skill2Animation, animationMap.skill2AnimationName); // Fixed property name
    }
    
    public void PlaySkill3()
    {
        PlayAnimation(animationMap.skill3Animation, animationMap.skill3AnimationName); // Fixed property name
    }
    
    public void PlayDefend()
    {
        PlayAnimation(animationMap.defendAnimation, animationMap.defendAnimationName); // Fixed property name
    }
    
    public void PlayGetHit()
    {
        PlayAnimation(animationMap.getHitAnimation, animationMap.getHitAnimationName); // Fixed property name
    }
    
    // Universal method to play any skill by index
    public void PlaySkill(int skillIndex)
    {
        switch (skillIndex)
        {
            case 1: PlaySkill1(); break;
            case 2: PlaySkill2(); break;
            case 3: PlaySkill3(); break;
            case 4: PlayDefend(); break;
            default: PlaySkill1(); break;
        }
    }
    
    // Play skill based on SkillType
    public void PlaySkillByType(IngredientDatabase.SkillInfo.SkillType skillType, int skillNumber = 1)
    {
        switch (skillType)
        {
            case IngredientDatabase.SkillInfo.SkillType.Damage:
                // For damage skills, play based on skill number
                switch (skillNumber)
                {
                    case 1: PlaySkill1(); break;
                    case 2: PlaySkill2(); break;
                    case 3: PlaySkill3(); break;
                    default: PlaySkill1(); break;
                }
                break;
                
            case IngredientDatabase.SkillInfo.SkillType.Heal:
                // Use defend animation for heal (or you could add a heal animation)
                PlayDefend();
                break;
                
            case IngredientDatabase.SkillInfo.SkillType.Defend:
                PlayDefend();
                break;
        }
    }
    
    // ========== HELPER METHODS ==========
    
    private void PlayAnimation(int animationType, string animationName = "")
    {
        if (!string.IsNullOrEmpty(animationName))
        {
            // Play by animation name
            animator.Play(animationName, 0, 0f);
        }
        else
        {
            // Use parameter system (attackType + attackTrigger)
            animator.SetInteger("attackType", animationType);
            animator.SetTrigger("attackTrigger");
        }
    }
    
    // ========== GETTER METHODS ==========
    
    public IngredientDatabase.IngredientInfo GetEnerlingInfo()
    {
        return enerlingInfo;
    }
    
    public SkillAnimationConfig.EnerlingAnimationMap GetAnimationMap()
    {
        return animationMap;
    }
    
    public string GetEnerlingName()
    {
        return enerlingName;
    }
    
    // Get specific skill info
    public IngredientDatabase.SkillInfo GetSkillInfo(int skillNumber)
    {
        if (enerlingInfo == null) return null;
        
        switch (skillNumber)
        {
            case 1: return enerlingInfo.skill1;
            case 2: return enerlingInfo.skill2;
            case 3: return enerlingInfo.skill3;
            default: return null;
        }
    }
}