using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterRotationController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Rotation Settings")]
    public Transform characterToRotate;
    public float sensitivity = 0.2f;
    public float smoothReturnSpeed = 3f;
    public float maxRotationAngle = 45f;
    public bool invertX = false;

    [Header("Target Rotation")]
    public Vector3 targetBaseRotation = new Vector3(0, 43.365f, 0);

    [Header("Animator Control")]
    public Animator characterAnimator;
    public bool applyRootMotion = false;
    public bool disableAnimatorDuringRotation = true;

    [Header("Swipe Area")]
    public RectTransform swipePanel;

    private Vector2 previousPosition;
    private bool pressing = false;
    private float currentRotationOffset = 0f;
    private float targetRotationOffset = 0f;
    private bool isReturningToDefault = false;

    void Start()
    {
        EnsureUIComponents();

        if (characterAnimator == null && characterToRotate != null)
            characterAnimator = characterToRotate.GetComponent<Animator>();

        if (characterAnimator != null)
        {
            characterAnimator.applyRootMotion = applyRootMotion;
        }

        // Force set the initial LOCAL rotation to the target base rotation
        if (characterToRotate != null)
        {
            characterToRotate.localRotation = Quaternion.Euler(targetBaseRotation);
            Debug.Log("Set character local rotation to: " + targetBaseRotation);
        }

        Debug.Log("CharacterRotationController initialized - Character: " + (characterToRotate != null) + " Base Rotation: " + targetBaseRotation);
    }

    void LateUpdate()
    {
        UpdateRotation();
    }

    private void EnsureUIComponents()
    {
        Image image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.01f);
            image.raycastTarget = true;
            Debug.Log("Added Image component for raycasting");
        }
        else
        {
            image.raycastTarget = true;
        }

        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null && swipePanel == null)
        {
            swipePanel = rectTransform;
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            Debug.LogWarning("No EventSystem found in scene! UI input won't work.");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressing = true;
        isReturningToDefault = false;
        previousPosition = eventData.position;

        if (disableAnimatorDuringRotation && characterAnimator != null)
        {
            characterAnimator.enabled = false;
            Debug.Log("Animator disabled for rotation control");
        }

        Debug.Log("Rotation started - Pointer Down");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!pressing) return;

        Vector2 delta = eventData.position - previousPosition;
        previousPosition = eventData.position;

        delta *= sensitivity;

        if (invertX) delta.x = -delta.x;

        targetRotationOffset += delta.x;

        targetRotationOffset = Mathf.Clamp(targetRotationOffset, -maxRotationAngle, maxRotationAngle);

        Debug.Log("Dragging - Delta: " + delta + ", Target Rotation Offset: " + targetRotationOffset.ToString("F2"));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressing = false;
        StartSmoothReturn();

        if (disableAnimatorDuringRotation && characterAnimator != null)
        {
            Invoke("EnableAnimator", 0.1f);
        }

        Debug.Log("Rotation ended - Pointer Up");
    }

    private void EnableAnimator()
    {
        if (characterAnimator != null && !pressing)
        {
            characterAnimator.enabled = true;
            Debug.Log("Animator re-enabled");
        }
    }

    private void UpdateRotation()
    {
        if (characterToRotate == null)
        {
            Debug.LogWarning("CharacterToRotate is not assigned!");
            return;
        }

        if (isReturningToDefault)
        {
            targetRotationOffset = Mathf.Lerp(targetRotationOffset, 0f, smoothReturnSpeed * Time.deltaTime);

            if (Mathf.Abs(targetRotationOffset) < 0.1f)
            {
                targetRotationOffset = 0f;
                isReturningToDefault = false;
            }
        }

        currentRotationOffset = Mathf.Lerp(currentRotationOffset, targetRotationOffset, smoothReturnSpeed * Time.deltaTime);

        // Calculate final rotation: base rotation + current offset
        float finalYRotation = targetBaseRotation.y + currentRotationOffset;

        // Apply rotation as LOCAL rotation to ignore parent rotation
        characterToRotate.localRotation = Quaternion.Euler(targetBaseRotation.x, finalYRotation, targetBaseRotation.z);

        if (pressing && Time.frameCount % 30 == 0)
        {
            Debug.Log("Rotating - Offset: " + currentRotationOffset.ToString("F2") + ", Final Y: " + finalYRotation.ToString("F2") +
                     ", World Y: " + characterToRotate.eulerAngles.y.ToString("F2"));
        }
    }

    private void StartSmoothReturn()
    {
        isReturningToDefault = true;
    }

    public void ResetRotation()
    {
        targetRotationOffset = 0f;
        currentRotationOffset = 0f;
        isReturningToDefault = false;
        pressing = false;

        // Force reset to exact base rotation using LOCAL rotation
        if (characterToRotate != null)
        {
            characterToRotate.localRotation = Quaternion.Euler(targetBaseRotation);
        }

        if (characterAnimator != null && !characterAnimator.enabled)
        {
            characterAnimator.enabled = true;
        }

        Debug.Log("Rotation instantly reset to base rotation");
    }

    public void SmoothResetRotation()
    {
        StartSmoothReturn();
        Debug.Log("Smooth rotation reset started");
    }

    public void OnCharacterSelected()
    {
        SmoothResetRotation();
    }

    public float GetCurrentRotationOffset()
    {
        return currentRotationOffset;
    }

    public bool IsRotating()
    {
        return pressing || isReturningToDefault || Mathf.Abs(currentRotationOffset) > 0.1f;
    }

    public void PrintDebugInfo()
    {
        if (characterToRotate != null)
        {
            Debug.Log("CharacterRotationController Debug:" +
                      " Pressing: " + pressing +
                      " Returning to Default: " + isReturningToDefault +
                      " Target Rotation Offset: " + targetRotationOffset.ToString("F2") +
                      " Current Rotation Offset: " + currentRotationOffset.ToString("F2") +
                      " Base Rotation: " + targetBaseRotation +
                      " Local Rotation: " + characterToRotate.localEulerAngles +
                      " World Rotation: " + characterToRotate.eulerAngles +
                      " Character Assigned: " + (characterToRotate != null) +
                      " Animator Enabled: " + (characterAnimator != null && characterAnimator.enabled));
        }
    }
}