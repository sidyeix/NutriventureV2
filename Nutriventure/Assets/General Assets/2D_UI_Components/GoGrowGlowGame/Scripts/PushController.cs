using UnityEngine;
using UnityEngine.UI;

public class PushController : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask pushableLayer;
    [SerializeField] private float raycastDistance = 2f;
    [SerializeField] private float raycastHeight = 1f; // Height from ground

    [Header("UI References")]
    [SerializeField] private GameObject pushButtonUI;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("Match this to Sprint button's pressed color for uniformity")]
    [SerializeField] private Color activeColor = new Color(0.78f, 0.78f, 0.78f, 1f);

    [Header("Animation")]
    [SerializeField] private string pushingBoolName = "isPushing";

    // References
    private StarterAssets.StarterAssetsInputs inputs; // Add namespace
    private Animator animator;
    private int pushingBoolHash;

    // State
    private bool isFacingPushable = false;
    private bool isPushing = false;
    private GameObject currentPushableObject;

    private void Start()
    {
        // Get StarterAssetsInputs with namespace
        inputs = GetComponent<StarterAssets.StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        pushingBoolHash = Animator.StringToHash(pushingBoolName);

        // Hide push button initially
        if (pushButtonUI != null)
        {
            pushButtonUI.SetActive(false);
        }

        // Set button color
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
    }

    private void Update()
    {
        // Check for pushable objects in front
        CheckForPushableObjects();

        // Handle push input
        HandlePushInput();

        // Update animator
        UpdateAnimator();

        // Update UI
        UpdateUI();
    }

    private void CheckForPushableObjects()
    {
        // Calculate raycast position (at chest height)
        Vector3 raycastOrigin = transform.position + Vector3.up * raycastHeight;
        Vector3 raycastDirection = transform.forward;

        // Draw debug ray
        Debug.DrawRay(raycastOrigin, raycastDirection * raycastDistance, Color.red);

        // Perform raycast
        RaycastHit hit;
        isFacingPushable = Physics.Raycast(raycastOrigin, raycastDirection, out hit, raycastDistance, pushableLayer);

        if (isFacingPushable)
        {
            currentPushableObject = hit.collider.gameObject;
            Debug.Log($"Facing pushable object: {currentPushableObject.name}");
        }
        else
        {
            currentPushableObject = null;
        }
    }

    private void HandlePushInput()
    {
        // Check if inputs is null
        if (inputs == null)
        {
            Debug.LogWarning("StarterAssetsInputs not found on player!");
            return;
        }

        // Get push input from StarterAssetsInputs
        bool pushInput = inputs.push;

        // Only allow pushing if facing a pushable object
        if (pushInput && isFacingPushable)
        {
            if (!isPushing)
            {
                StartPushing();
            }
        }
        else
        {
            if (isPushing)
            {
                StopPushing();
            }
        }
    }

    private void StartPushing()
    {
        isPushing = true;
        Debug.Log("Started pushing");

        // Here you would add logic to actually push the object
        if (currentPushableObject != null)
        {
            // Optional: Add force to the object
            Rigidbody rb = currentPushableObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 pushDirection = transform.forward;
                rb.AddForce(pushDirection * 5f, ForceMode.Impulse);
            }
        }
    }

    private void StopPushing()
    {
        isPushing = false;
        Debug.Log("Stopped pushing");
    }

    private void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetBool(pushingBoolHash, isPushing);
        }
    }

    private void UpdateUI()
    {
        // Show/hide push button based on whether we're facing a pushable object
        if (pushButtonUI != null)
        {
            pushButtonUI.SetActive(isFacingPushable);
        }

        // Update button color based on push state
        if (buttonImage != null)
        {
            buttonImage.color = isPushing ? activeColor : normalColor;
        }
    }

    // For debugging in editor
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = isFacingPushable ? Color.green : Color.red;
            Vector3 raycastOrigin = transform.position + Vector3.up * raycastHeight;
            Gizmos.DrawRay(raycastOrigin, transform.forward * raycastDistance);

            if (isFacingPushable && currentPushableObject != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(currentPushableObject.transform.position, Vector3.one * 1.2f);
            }
        }
    }

    // Public methods for external access
    public bool IsPushing()
    {
        return isPushing;
    }

    public bool IsFacingPushable()
    {
        return isFacingPushable;
    }
}