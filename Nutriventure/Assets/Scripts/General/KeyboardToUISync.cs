using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class KeyboardToUISync : MonoBehaviour
{
    [Header("Joystick References")]
    public RectTransform joystickHandle;
    public RectTransform joystickContainer;
    public float joystickMaxOffset = 50f;
    
    [Header("Button References")]
    public Image jumpButton;
    public Image sprintButton;
    public Image crawlButton;
    public Image pushButton;
    
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color activeStateColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green for toggled states
    public Color pushingColor = new Color(0.8f, 0.4f, 0.2f, 1f); // Orange for pushing
    
    [Header("Animation Settings")]
    public float buttonPressScale = 0.9f;
    public float animationSpeed = 10f;
    
    // Animation states
    private Vector3[] originalButtonScales;
    private Vector3[] targetButtonScales;
    private Color[] targetButtonColors;
    
    // Input tracking
    private Vector2 currentKeyboardInput = Vector2.zero;
    private bool isJumpPressed = false;
    private bool isSprintPressed = false;
    private bool isCrawlPressed = false;
    private bool isPushPressed = false;
    
    // State tracking
    private bool wasJumpPressed = false;
    private bool wasCrawlPressed = false;
    private bool wasPushPressed = false;
    
    // Toggle states (for C and E keys)
    private bool isCrawlingActive = false;
    private bool isPushingActive = false;
    
    // Cached components
    private Image[] buttonImages;
    private RectTransform[] buttonRects;
    
    // Input System references
    private Keyboard currentKeyboard;
    
    // Direction indicator
    [Header("Direction Indicator (Optional)")]
    public Image directionIndicator;
    public float indicatorRotationSpeed = 5f;
    public float indicatorMaxAlpha = 0.5f;
    
    // Input actions for reading
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crawlAction;
    private InputAction pushAction;
    
    private void Start()
    {
        InitializeButtonArrays();
        StoreOriginalScales();
        SetupInputSystem();
        
        // Initialize direction indicator
        if (directionIndicator != null)
        {
            Color color = directionIndicator.color;
            color.a = 0;
            directionIndicator.color = color;
        }
    }
    
    private void SetupInputSystem()
    {
        // Create input actions for reading
        moveAction = new InputAction("Move", binding: "<Keyboard>/wasd");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        
        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        sprintAction = new InputAction("Sprint", binding: "<Keyboard>/leftShift");
        crawlAction = new InputAction("Crawl", binding: "<Keyboard>/c");
        pushAction = new InputAction("Push", binding: "<Keyboard>/e");
        
        // Add alternate bindings for arrow keys
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        
        // Enable all actions
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        crawlAction.Enable();
        pushAction.Enable();
        
        // Find the current keyboard
        FindKeyboard();
    }
    
    private void FindKeyboard()
    {
        // Get all input devices
        var devices = InputSystem.devices;
        
        // Look for a keyboard
        foreach (var device in devices)
        {
            if (device is Keyboard keyboard)
            {
                currentKeyboard = keyboard;
                break;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Clean up input actions
        if (moveAction != null) moveAction.Disable();
        if (jumpAction != null) jumpAction.Disable();
        if (sprintAction != null) sprintAction.Disable();
        if (crawlAction != null) crawlAction.Disable();
        if (pushAction != null) pushAction.Disable();
    }
    
    private void InitializeButtonArrays()
    {
        // Create arrays for all buttons
        buttonImages = new Image[4];
        buttonRects = new RectTransform[4];
        originalButtonScales = new Vector3[4];
        targetButtonScales = new Vector3[4];
        targetButtonColors = new Color[4];
        
        // Assign button 0: Jump
        if (jumpButton != null)
        {
            buttonImages[0] = jumpButton;
            buttonRects[0] = jumpButton.rectTransform;
        }
        
        // Assign button 1: Sprint
        if (sprintButton != null)
        {
            buttonImages[1] = sprintButton;
            buttonRects[1] = sprintButton.rectTransform;
        }
        
        // Assign button 2: Crawl
        if (crawlButton != null)
        {
            buttonImages[2] = crawlButton;
            buttonRects[2] = crawlButton.rectTransform;
        }
        
        // Assign button 3: Push
        if (pushButton != null)
        {
            buttonImages[3] = pushButton;
            buttonRects[3] = pushButton.rectTransform;
        }
    }
    
    private void StoreOriginalScales()
    {
        for (int i = 0; i < 4; i++)
        {
            if (buttonRects[i] != null)
            {
                originalButtonScales[i] = buttonRects[i].localScale;
                targetButtonScales[i] = originalButtonScales[i];
                targetButtonColors[i] = normalColor;
            }
        }
    }
    
    private void Update()
    {
        CaptureKeyboardInput();
        UpdateJoystickVisual();
        UpdateButtonVisuals();
        AnimateButtons();
        UpdateDirectionIndicator();
    }
    
    private void CaptureKeyboardInput()
    {
        // Read input using Input System actions
        currentKeyboardInput = moveAction.ReadValue<Vector2>();
        
        // Read button states
        bool newJumpPressed = jumpAction.ReadValue<float>() > 0.1f;
        bool newSprintPressed = sprintAction.ReadValue<float>() > 0.1f;
        bool newCrawlPressed = crawlAction.ReadValue<float>() > 0.1f;
        bool newPushPressed = pushAction.ReadValue<float>() > 0.1f;
        
        // Handle toggle for crawl (C key)
        if (newCrawlPressed && !wasCrawlPressed)
        {
            isCrawlingActive = !isCrawlingActive;
            TriggerButtonPressAnimation(2); // Crawl button index
        }
        
        // Handle toggle for push (E key) 
        if (newPushPressed && !wasPushPressed)
        {
            isPushingActive = !isPushingActive;
            TriggerButtonPressAnimation(3); // Push button index
        }
        
        // Check for state changes for animation triggers
        if (newJumpPressed && !wasJumpPressed)
        {
            TriggerButtonPressAnimation(0); // Jump button index
        }
        
        // Update states
        isJumpPressed = newJumpPressed;
        isSprintPressed = newSprintPressed;
        isCrawlPressed = newCrawlPressed;
        isPushPressed = newPushPressed;
        
        // Store for next frame
        wasJumpPressed = isJumpPressed;
        wasCrawlPressed = isCrawlPressed;
        wasPushPressed = isPushPressed;
    }
    
    private void TriggerButtonPressAnimation(int buttonIndex)
    {
        if (buttonIndex >= 0 && buttonIndex < 4 && buttonRects[buttonIndex] != null)
        {
            targetButtonScales[buttonIndex] = originalButtonScales[buttonIndex] * buttonPressScale;
        }
    }
    
    private void UpdateJoystickVisual()
    {
        if (joystickHandle != null && joystickContainer != null)
        {
            // Calculate joystick position based on keyboard input
            Vector2 inputDirection = currentKeyboardInput.normalized;
            float inputMagnitude = Mathf.Clamp01(currentKeyboardInput.magnitude);
            
            // Calculate target position
            Vector2 targetPosition = inputDirection * joystickMaxOffset * inputMagnitude;
            
            // Smoothly move the joystick handle
            joystickHandle.anchoredPosition = Vector2.Lerp(
                joystickHandle.anchoredPosition, 
                targetPosition, 
                Time.deltaTime * 10f
            );
            
            // Reset to center if no input
            if (currentKeyboardInput.magnitude < 0.1f)
            {
                joystickHandle.anchoredPosition = Vector2.Lerp(
                    joystickHandle.anchoredPosition, 
                    Vector2.zero, 
                    Time.deltaTime * 15f
                );
            }
        }
    }
    
    private void UpdateButtonVisuals()
    {
        // Update button 0: Jump - momentary press
        UpdateButtonColor(0, isJumpPressed, false);
        
        // Update button 1: Sprint - momentary press
        UpdateButtonColor(1, isSprintPressed, false);
        
        // Update button 2: Crawl - toggle state
        if (isCrawlingActive)
        {
            targetButtonColors[2] = activeStateColor;
        }
        else
        {
            UpdateButtonColor(2, isCrawlPressed, false);
        }
        
        // Update button 3: Push - toggle state
        if (isPushingActive)
        {
            targetButtonColors[3] = pushingColor;
        }
        else
        {
            UpdateButtonColor(3, isPushPressed, false);
        }
    }
    
    private void UpdateButtonColor(int index, bool isPressed, bool isActiveState)
    {
        if (index >= 0 && index < 4)
        {
            targetButtonColors[index] = isPressed ? pressedColor : normalColor;
        }
    }
    
    private void AnimateButtons()
    {
        for (int i = 0; i < 4; i++)
        {
            if (buttonRects[i] != null)
            {
                // Animate scale
                buttonRects[i].localScale = Vector3.Lerp(
                    buttonRects[i].localScale, 
                    targetButtonScales[i], 
                    Time.deltaTime * animationSpeed
                );
                
                // Return to normal scale
                if (Vector3.Distance(buttonRects[i].localScale, targetButtonScales[i]) < 0.01f)
                {
                    targetButtonScales[i] = originalButtonScales[i];
                }
            }
            
            if (buttonImages[i] != null)
            {
                // Animate color
                buttonImages[i].color = Color.Lerp(
                    buttonImages[i].color, 
                    targetButtonColors[i], 
                    Time.deltaTime * animationSpeed
                );
            }
        }
    }
    
    private void UpdateDirectionIndicator()
    {
        if (directionIndicator != null)
        {
            if (currentKeyboardInput.magnitude > 0.1f)
            {
                // Calculate angle from input
                float angle = Mathf.Atan2(currentKeyboardInput.x, currentKeyboardInput.y) * Mathf.Rad2Deg;
                
                // Smoothly rotate the indicator
                Quaternion targetRotation = Quaternion.Euler(0, 0, -angle);
                directionIndicator.rectTransform.rotation = Quaternion.Slerp(
                    directionIndicator.rectTransform.rotation,
                    targetRotation,
                    Time.deltaTime * indicatorRotationSpeed
                );
                
                // Fade in
                Color color = directionIndicator.color;
                color.a = Mathf.Lerp(color.a, indicatorMaxAlpha, Time.deltaTime * 5f);
                directionIndicator.color = color;
            }
            else
            {
                // Fade out
                Color color = directionIndicator.color;
                color.a = Mathf.Lerp(color.a, 0, Time.deltaTime * 10f);
                directionIndicator.color = color;
            }
        }
    }
    
    // Public methods for external control
    public void SetJoystickPosition(Vector2 input)
    {
        currentKeyboardInput = Vector2.ClampMagnitude(input, 1f);
    }
    
    public void SetButtonState(string buttonName, bool isPressed)
    {
        switch (buttonName.ToLower())
        {
            case "jump":
                isJumpPressed = isPressed;
                if (isPressed) TriggerButtonPressAnimation(0);
                break;
            case "sprint":
                isSprintPressed = isPressed;
                break;
            case "crawl":
                isCrawlPressed = isPressed;
                if (isPressed) TriggerButtonPressAnimation(2);
                break;
            case "push":
                isPushPressed = isPressed;
                if (isPressed) TriggerButtonPressAnimation(3);
                break;
        }
    }
    
    // Methods to manually set toggle states (if you want to sync with actual game state)
    public void SetCrawlingState(bool isCrawling)
    {
        isCrawlingActive = isCrawling;
    }
    
    public void SetPushingState(bool isPushing)
    {
        isPushingActive = isPushing;
    }
    
    // Method to sync with actual player input from your controller
    public void SyncWithPlayerInput(Vector2 moveInput, bool jump, bool sprint, bool crawl, bool push)
    {
        currentKeyboardInput = moveInput;
        isJumpPressed = jump;
        isSprintPressed = sprint;
        isCrawlPressed = crawl;
        isPushPressed = push;
        
        // Update toggle states based on input
        if (crawl && !wasCrawlPressed)
        {
            isCrawlingActive = !isCrawlingActive;
        }
        if (push && !wasPushPressed)
        {
            isPushingActive = !isPushingActive;
        }
        
        wasCrawlPressed = crawl;
        wasPushPressed = push;
    }
    
    // Method to force toggle crawl state
    public void ToggleCrawl()
    {
        isCrawlingActive = !isCrawlingActive;
        TriggerButtonPressAnimation(2);
    }
    
    // Method to force toggle push state
    public void TogglePush()
    {
        isPushingActive = !isPushingActive;
        TriggerButtonPressAnimation(3);
    }
    
    // Get current input states (useful for debugging)
    public Vector2 GetCurrentMoveInput() => currentKeyboardInput;
    public bool GetJumpPressed() => isJumpPressed;
    public bool GetSprintPressed() => isSprintPressed;
    public bool GetCrawlPressed() => isCrawlPressed;
    public bool GetPushPressed() => isPushPressed;
    public bool GetCrawlingActive() => isCrawlingActive;
    public bool GetPushingActive() => isPushingActive;

}