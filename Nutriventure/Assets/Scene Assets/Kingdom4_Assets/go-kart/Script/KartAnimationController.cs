using UnityEngine;

public class KartAnimationController : MonoBehaviour
{
    [Header("Animation Parameters")]
    public string driveAnimationBool = "IsDriving";
    
    [Header("References")]
    public KartController kartController;
    
    private Animator playerAnimator;
    private bool wasDriving = false;
    private int driveAnimID;

    void Start()
    {
        // Cache animation ID for better performance
        driveAnimID = Animator.StringToHash(driveAnimationBool);
        
        // Auto-find player by tag
        FindPlayerAnimator();
        
        // Ensure we start in non-driving state
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(driveAnimID, false);
        }
    }

    void Update()
    {
        // Try to find player animator if it's null
        if (playerAnimator == null)
        {
            FindPlayerAnimator();
            return;
        }

        if (kartController == null) return;

        bool isDriving = kartController.enabled;
        
        // Only update when driving state changes
        if (isDriving != wasDriving)
        {
            playerAnimator.SetBool(driveAnimID, isDriving);
            wasDriving = isDriving;
        }
    }

    private void FindPlayerAnimator()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                Debug.Log("✅ Player animator found automatically!");
                // Reset driving state when finding new animator
                playerAnimator.SetBool(driveAnimID, false);
                wasDriving = false;
            }
            else
            {
                Debug.LogWarning("⚠️ Player found but no Animator component found!");
            }
        }
    }

    // Optional: Manual override if needed
    public void SetPlayerAnimator(Animator animator)
    {
        playerAnimator = animator;
        wasDriving = false;
        
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(driveAnimID, false);
        }
    }
}