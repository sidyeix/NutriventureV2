using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerRaycastDetector : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactionDistance = 3f;
    public LayerMask interactionLayerMask = -1;
    
    [Header("Input Settings")]
    public bool useMobileTouch = true;
    
    [Header("UI Reference")]
    public GameObject interactionPromptUI;
    
    [Header("Events")]
    public UnityEvent<GameObject> OnObjectDetected;
    public UnityEvent OnObjectLost;
    public UnityEvent<GameObject> OnObjectInteracted;
    
    private Camera playerCamera;
    private GameObject currentDetectedObject;
    private bool isInteractableInRange = false;
    private PlayerInput playerInput;
    private InputAction interactAction;
    
    void Start()
    {
        playerCamera = Camera.main;
        
        // Initialize Input System
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = FindAnyObjectByType<PlayerInput>();
        }
        
        // Create interact action if not already defined
        if (playerInput != null)
        {
            interactAction = playerInput.actions.FindAction("Interact");
            if (interactAction == null)
            {
                // Create a fallback action
                interactAction = new InputAction("Interact");
                interactAction.AddBinding("<Keyboard>/e");
                interactAction.AddBinding("<Touchscreen>/primaryTouch/tap");
                interactAction.Enable();
            }
        }
        
        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(false);
    }
    
    void Update()
    {
        PerformRaycast();
        CheckForInteractionInput();
    }
    
    private void PerformRaycast()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            return;
        }
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        // DEBUG: Visualize the ray
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, isInteractableInRange ? Color.green : Color.red);
        
        if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayerMask))
        {
            if (hit.collider.CompareTag("Interactable"))
            {
                if (currentDetectedObject != hit.collider.gameObject)
                {
                    currentDetectedObject = hit.collider.gameObject;
                    isInteractableInRange = true;
                    OnObjectDetected?.Invoke(currentDetectedObject);
                    ShowInteractionPrompt(true);
                    Debug.Log($"Detected: {currentDetectedObject.name}");
                }
                return;
            }
        }
        
        if (isInteractableInRange)
        {
            ClearDetection();
        }
    }
    
    private void CheckForInteractionInput()
    {
        bool interactionInput = false;
        
        if (useMobileTouch)
        {
            // New Input System touch detection
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                interactionInput = true;
            }
        }
        else
        {
            // New Input System keyboard detection
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactionInput = true;
            }
        }
        
        // Fallback: Use the interact action
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            interactionInput = true;
        }
        
        if (interactionInput)
        {
            TryInteract();
        }
    }
    
    private void TryInteract()
    {
        if (isInteractableInRange && currentDetectedObject != null)
        {
            Interactable interactable = currentDetectedObject.GetComponent<Interactable>();
            if (interactable != null)
            {
                interactable.Pickup();
                Debug.Log($"Picked up: {currentDetectedObject.name}");
            }
            else
            {
                Debug.LogWarning("No Interactable component found on " + currentDetectedObject.name);
            }
            
            OnObjectInteracted?.Invoke(currentDetectedObject);
            ClearDetection();
        }
    }
    
    private void ClearDetection()
    {
        isInteractableInRange = false;
        currentDetectedObject = null;
        OnObjectLost?.Invoke();
        ShowInteractionPrompt(false);
    }
    
    private void ShowInteractionPrompt(bool show)
    {
        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(show);
    }
    
    // Input System event handler (optional)
    public void OnInteract(InputValue value)
    {
        if (value.isPressed && isInteractableInRange)
        {
            TryInteract();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = isInteractableInRange ? Color.green : Color.white;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance);
        }
    }
}