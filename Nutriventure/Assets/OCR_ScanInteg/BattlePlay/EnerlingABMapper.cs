using UnityEngine;
using UnityEngine.UI;

public class AttackButtonMapper : MonoBehaviour
{
    [System.Serializable]
    public class AttackButton
    {
        public string buttonName;
        public Button uiButton;
        public int attackType;
    }
    
    [Header("Animator Reference")]
    [SerializeField] private Animator animator;
    
    [Header("Animation Parameters")]
    [SerializeField] private string triggerName = "attackTrigger";
    [SerializeField] private string typeParameter = "attackType";
    
    [Header("Attack Buttons")]
    [SerializeField] private AttackButton[] attackButtons;
    
    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        SetupButtons();
    }
    
    void SetupButtons()
    {
        foreach (AttackButton attackButton in attackButtons)
        {
            if (attackButton.uiButton != null)
            {
                // Store attack type in a local variable to avoid closure issues
                int attackType = attackButton.attackType;
                attackButton.uiButton.onClick.AddListener(() => PlayAttack(attackType));
            }
        }
    }
    
    public void PlayAttack(int attackType)
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator not assigned!");
            return;
        }
        
        // Set the attack type
        animator.SetInteger(typeParameter, attackType);
        
        // Trigger the attack animation
        animator.SetTrigger(triggerName);
        
        Debug.Log($"Playing attack type: {attackType}");
    }
}