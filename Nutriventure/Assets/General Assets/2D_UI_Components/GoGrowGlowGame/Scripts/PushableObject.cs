using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class PushableObject : MonoBehaviour
{
    [Header("Push Settings")]
    public float mass = 10f;
    public float drag = 5f;
    public float angularDrag = 2f;
    public bool useGravity = true;

    [Header("UI Settings")]
    public GameObject pushButtonUI;
    public float uiActivationRange = 2f;

    private Rigidbody rb;
    private bool playerInRange = false;
    private ThirdPersonController playerController;
    private bool isBeingPushed = false;

    void Start()
    {
        // Set the tag (IMPORTANT!)
        gameObject.tag = "Pushable";

        // Setup Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        ConfigureRigidbody();

        // Find player
        playerController = FindObjectOfType<ThirdPersonController>();

        // Hide push button initially
        if (pushButtonUI != null)
        {
            pushButtonUI.SetActive(false);
        }

        // Ensure collider exists
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        }
    }

    void ConfigureRigidbody()
    {
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.useGravity = useGravity;
        rb.isKinematic = true; // Start as kinematic - won't move from collisions
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
    }

    void Update()
    {
        if (playerController != null)
        {
            UpdateUIState();
        }
    }

    void UpdateUIState()
    {
        float distance = Vector3.Distance(transform.position, playerController.transform.position);
        bool isFacingObject = IsPlayerFacingObject();
        bool canShowUI = distance <= uiActivationRange && isFacingObject && !playerController.IsCrawling();

        if (canShowUI != playerInRange)
        {
            playerInRange = canShowUI;
            ShowPushButton(playerInRange);
        }
    }

    bool IsPlayerFacingObject()
    {
        if (playerController == null) return false;

        Vector3 toObject = transform.position - playerController.transform.position;
        toObject.y = 0;

        if (toObject.magnitude < 0.1f) return false;

        Vector3 playerForward = playerController.transform.forward;
        playerForward.y = 0;

        float angle = Vector3.Angle(playerForward, toObject.normalized);
        return angle <= 60f; // 60 degree viewing angle
    }

    public void ShowPushButton(bool show)
    {
        if (pushButtonUI != null)
        {
            pushButtonUI.SetActive(show);
        }
    }

    // Called when player starts pushing
    public void OnStartPushing()
    {
        isBeingPushed = true;
        if (rb != null)
        {
            rb.isKinematic = false; // Allow physics
            rb.linearVelocity = Vector3.zero; // Reset velocity
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Called when player stops pushing
    public void OnStopPushing()
    {
        isBeingPushed = false;
        if (rb != null)
        {
            rb.isKinematic = true; // Stop physics
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // When player walks into object without pushing
        if (collision.gameObject.CompareTag("Player") && rb.isKinematic && !isBeingPushed)
        {
            // Object is kinematic, so it won't move
            // You can add a slight push-back effect here if needed
        }
    }

    public bool IsPlayerInRange()
    {
        return playerInRange;
    }

    public bool IsBeingPushed()
    {
        return isBeingPushed;
    }

    void OnDrawGizmosSelected()
    {
        // Draw activation range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, uiActivationRange);
    }
}