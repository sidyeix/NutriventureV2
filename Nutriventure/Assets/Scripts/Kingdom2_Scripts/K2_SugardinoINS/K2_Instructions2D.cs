using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class K2_Instructions2D : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject systemsPanel; // Parent "SystemsPanel"
    [SerializeField] private RawImage instVisuals; // RawImage UI "InstVisuals"

    [Header("Navigation Buttons")]
    [SerializeField] private Button confirmButton; // Button to close panel
    [SerializeField] private Button leftNavBtn; // Previous instruction
    [SerializeField] private Button rightNavBtn; // Next instruction

    [Header("Page Indicator")]
    [SerializeField] private TMP_Text pageText; // Text component for page indicator (e.g., "1/3")

    [Header("Instruction Images")]
    [SerializeField] private List<Texture2D> instructionImages = new List<Texture2D>(); // List of 2D sprites/images

    [Header("External UI Button")]
    [SerializeField] private Button externalOpenButton; // UI Button that can open the panel
    [SerializeField] private bool enableExternalButton = true; // Whether the external button is enabled
    [SerializeField] private bool showButtonAfterTrigger = true; // Show button after collider is triggered

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool disableAfterFirstTrigger = true;

    [Header("Input Settings (Optional)")]
    [SerializeField] private bool enableKeyboardNavigation = false; // Disabled for mobile

    private int currentPage = 0;
    private bool hasBeenTriggered = false;
    private StarterAssets.ThirdPersonController playerController;
    private UnityEngine.InputSystem.PlayerInput playerInput;
    private bool panelOpenedExternally = false; // Track if panel was opened by button

    [Header("Game Start References")]
    [SerializeField] private GameplayProgression gameplayProgression;
    [SerializeField] private K2_IntroCutsceneManager introCutsceneManager;
    private bool hasTriggeredGameStart = false;
    private bool cutscenePlayedOnEntry = false;

    void Start()
    {
        InitializePanel();
        SetupButtonListeners();
        InitializeExternalButton();

        // NOTE: Auto-play of the intro cutscene is handled by K2_ResumeGameCanvas.
        // It checks preserviaKeyCollected and saved-game state after a short delay
        // (to guarantee GameDataManager & K2_GameStateManager are initialised)
        // and calls K2_IntroCutsceneManager.PlayCutscene() when appropriate.
    }

    void InitializePanel()
    {
        // Ensure panel is disabled at start
        if (systemsPanel != null)
        {
            systemsPanel.SetActive(false);
            Debug.Log("SystemsPanel initialized and disabled");
        }
        else
        {
            Debug.LogError("SystemsPanel not assigned!");
        }

        // Disable collider at start if already triggered
        if (disableAfterFirstTrigger && hasBeenTriggered)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    void InitializeExternalButton()
    {
        if (externalOpenButton != null)
        {
            // Initially disable the external button
            externalOpenButton.gameObject.SetActive(showButtonAfterTrigger ? false : true);
            externalOpenButton.interactable = false;

            // Add listener to open panel
            externalOpenButton.onClick.RemoveAllListeners();
            externalOpenButton.onClick.AddListener(OpenPanelFromButton);

            Debug.Log("External button initialized - initially disabled");
        }
        else if (enableExternalButton)
        {
            Debug.LogWarning("ExternalOpenButton not assigned but enableExternalButton is true!");
        }
    }

    void SetupButtonListeners()
    {
        // Confirm button - close panel
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(CloseInstructionPanel);
            Debug.Log("Confirm button listener added");
        }
        else
        {
            Debug.LogError("ConfirmButton not assigned!");
        }

        // Left navigation button - previous page
        if (leftNavBtn != null)
        {
            leftNavBtn.onClick.RemoveAllListeners();
            leftNavBtn.onClick.AddListener(PreviousInstruction);
            Debug.Log("Left navigation button listener added");
        }
        else
        {
            Debug.LogError("LeftNavBtn not assigned!");
        }

        // Right navigation button - next page
        if (rightNavBtn != null)
        {
            rightNavBtn.onClick.RemoveAllListeners();
            rightNavBtn.onClick.AddListener(NextInstruction);
            Debug.Log("Right navigation button listener added");
        }
        else
        {
            Debug.LogError("RightNavBtn not assigned!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if player entered trigger
        if (other.CompareTag(playerTag) && !hasBeenTriggered)
        {
            Debug.Log("Player entered instruction trigger");
            TriggerInstructionPanel();
        }
    }

    // This method handles the initial trigger (from collider)
    public void TriggerInstructionPanel()
    {
        if (!hasBeenTriggered)
        {
            hasBeenTriggered = true;

            // Activate external button if enabled
            if (enableExternalButton && externalOpenButton != null)
            {
                externalOpenButton.gameObject.SetActive(true);
                externalOpenButton.interactable = true;
                Debug.Log("External button activated after collider trigger");
            }

            // Disable trigger collider if needed
            if (disableAfterFirstTrigger)
            {
                GetComponent<Collider>().enabled = false;
            }

            // Open the panel immediately
            OpenInstructionPanel(false); // false = not opened by button
        }
    }

    // Called by external UI button
    public void OpenPanelFromButton()
    {
        if (hasBeenTriggered && enableExternalButton)
        {
            panelOpenedExternally = true;
            OpenInstructionPanel(true); // true = opened by button
        }
        else if (!hasBeenTriggered)
        {
            Debug.LogWarning("Cannot open panel from button: Collider not triggered yet!");
        }
    }

    void OpenInstructionPanel(bool fromButton = false)
    {
        if (systemsPanel == null || instructionImages.Count == 0)
        {
            Debug.LogError("Cannot open panel: Missing references!");
            return;
        }

        // Get player references
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerController = player.GetComponent<StarterAssets.ThirdPersonController>();
            playerInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();

            // Disable player movement and input
            if (playerController != null)
                playerController.enabled = false;

            if (playerInput != null)
                playerInput.enabled = false;
        }

        // Reset to first page
        currentPage = 0;

        // Update UI
        UpdateInstructionDisplay();

        // Activate panel
        systemsPanel.SetActive(true);

        Debug.Log($"Instruction panel opened {(fromButton ? "from button" : "from trigger")}");
    }

    void CloseInstructionPanel()
    {
        if (systemsPanel != null)
        {
            systemsPanel.SetActive(false);
            Debug.Log("Instruction panel closed");
        }

        // Re-enable player movement and input
        if (playerController != null)
            playerController.enabled = true;

        if (playerInput != null)
            playerInput.enabled = true;

        // Start the game when instructions are confirmed (first time only)
        if (!hasTriggeredGameStart)
        {
            hasTriggeredGameStart = true;

            if (gameplayProgression == null)
                gameplayProgression = FindObjectOfType<GameplayProgression>();

            // Cutscene already played on scene entry — just start the game timer
            StartGameTimer();
        }

        // Reset player references
        playerController = null;
        playerInput = null;

        // Reset external button state
        panelOpenedExternally = false;
    }

    private void OnIntroCutsceneFinished()
    {
        // Unsubscribe to avoid duplicate calls
        if (introCutsceneManager != null)
            introCutsceneManager.OnCutsceneFinished -= OnIntroCutsceneFinished;

        StartGameTimer();
    }

    private void StartGameTimer()
    {
        if (gameplayProgression != null)
        {
            gameplayProgression.StartGame();
            Debug.Log("K2: Game timer started");
        }
        else
        {
            Debug.LogWarning("K2: GameplayProgression not found — cannot start game!");
        }
    }

    void PreviousInstruction()
    {
        if (instructionImages.Count == 0) return;

        currentPage--;
        if (currentPage < 0)
            currentPage = instructionImages.Count - 1; // Wrap to last page

        UpdateInstructionDisplay();
        Debug.Log($"Previous instruction: Page {currentPage + 1}/{instructionImages.Count}");
    }

    void NextInstruction()
    {
        if (instructionImages.Count == 0) return;

        currentPage++;
        if (currentPage >= instructionImages.Count)
            currentPage = 0; // Wrap to first page

        UpdateInstructionDisplay();
        Debug.Log($"Next instruction: Page {currentPage + 1}/{instructionImages.Count}");
    }

    void UpdateInstructionDisplay()
    {
        // Update instruction image
        if (instVisuals != null && instructionImages.Count > 0)
        {
            if (currentPage >= 0 && currentPage < instructionImages.Count)
            {
                instVisuals.texture = instructionImages[currentPage];
                Debug.Log($"Displaying instruction image {currentPage + 1}");
            }
        }

        // Update page text indicator
        UpdatePageText();

        // Update navigation button states (optional visual feedback)
        UpdateNavigationButtons();
    }

    void UpdatePageText()
    {
        if (pageText != null)
        {
            if (instructionImages.Count > 0)
            {
                // Display as "1/3", "2/3", etc. (1-based for user readability)
                pageText.text = $"{currentPage + 1}/{instructionImages.Count}";
            }
            else
            {
                pageText.text = "0/0";
            }
            Debug.Log($"Page text updated: {pageText.text}");
        }
        else
        {
            Debug.LogWarning("PageText not assigned!");
        }
    }

    void UpdateNavigationButtons()
    {
        // You can add visual feedback here (like changing button colors)
        // when at first/last page if desired
        /*
        if (leftNavBtn != null)
        {
            leftNavBtn.interactable = !(instructionImages.Count <= 1);
        }
        
        if (rightNavBtn != null)
        {
            rightNavBtn.interactable = !(instructionImages.Count <= 1);
        }
        */
    }

    // Public methods for external control

    public void OpenPanelManually()
    {
        if (!hasBeenTriggered)
        {
            // Trigger from manual call (e.g., from another script)
            TriggerInstructionPanel();
        }
        else
        {
            // Panel already triggered, open it
            OpenInstructionPanel(false);
        }
    }

    public void ClosePanelManually()
    {
        CloseInstructionPanel();
    }

    public void GoToPage(int pageIndex)
    {
        if (instructionImages.Count == 0) return;

        if (pageIndex >= 0 && pageIndex < instructionImages.Count)
        {
            currentPage = pageIndex;
            UpdateInstructionDisplay();
        }
    }

    public void AddInstructionImage(Texture2D newImage)
    {
        if (newImage != null)
        {
            instructionImages.Add(newImage);
            UpdateInstructionDisplay(); // Update display to reflect new count
            Debug.Log($"Added new instruction image. Total: {instructionImages.Count}");
        }
    }

    public void RemoveInstructionImage(int index)
    {
        if (index >= 0 && index < instructionImages.Count)
        {
            instructionImages.RemoveAt(index);

            // Adjust current page if necessary
            if (currentPage >= instructionImages.Count)
            {
                currentPage = Mathf.Max(0, instructionImages.Count - 1);
            }

            UpdateInstructionDisplay();
            Debug.Log($"Removed instruction image at index {index}. Total: {instructionImages.Count}");
        }
    }

    // Getter methods

    public int GetCurrentPage()
    {
        return currentPage;
    }

    public int GetTotalPages()
    {
        return instructionImages.Count;
    }

    public bool HasBeenTriggered()
    {
        return hasBeenTriggered;
    }

    public bool IsPanelOpen()
    {
        return systemsPanel != null && systemsPanel.activeInHierarchy;
    }

    public bool IsExternalButtonEnabled()
    {
        return externalOpenButton != null && externalOpenButton.interactable;
    }

    // Reset functionality

    public void ResetTrigger()
    {
        hasBeenTriggered = false;
        hasTriggeredGameStart = false;
        cutscenePlayedOnEntry = false;
        currentPage = 0;
        GetComponent<Collider>().enabled = true;
        panelOpenedExternally = false;

        // Reset external button
        if (externalOpenButton != null)
        {
            externalOpenButton.gameObject.SetActive(showButtonAfterTrigger ? false : true);
            externalOpenButton.interactable = false;
        }

        Debug.Log("Instruction trigger and external button reset");
    }

    // Enable/disable external button programmatically

    public void SetExternalButtonEnabled(bool enabled)
    {
        enableExternalButton = enabled;

        if (externalOpenButton != null)
        {
            if (enabled && hasBeenTriggered)
            {
                externalOpenButton.gameObject.SetActive(true);
                externalOpenButton.interactable = true;
            }
            else
            {
                externalOpenButton.interactable = false;
                if (!showButtonAfterTrigger)
                {
                    externalOpenButton.gameObject.SetActive(false);
                }
            }
        }

        Debug.Log($"External button {(enabled ? "enabled" : "disabled")}");
    }

    public void SetExternalButtonVisible(bool visible)
    {
        if (externalOpenButton != null)
        {
            externalOpenButton.gameObject.SetActive(visible);
            Debug.Log($"External button {(visible ? "made visible" : "hidden")}");
        }
    }

    // Input handling with Input System - Disabled for mobile by default

    void Update()
    {
        if (IsPanelOpen() && enableKeyboardNavigation)
        {
            HandleInputSystemNavigation();
        }
    }

    void HandleInputSystemNavigation()
    {
#if ENABLE_INPUT_SYSTEM
        // Get keyboard input
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            // Left arrow or A key for previous
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
            {
                PreviousInstruction();
            }
            
            // Right arrow or D key for next
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
            {
                NextInstruction();
            }
            
            // Escape or Enter to close
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            {
                CloseInstructionPanel();
            }
        }
#endif
    }

    // Editor helper

#if UNITY_EDITOR
    [ContextMenu("Auto-Find References in Children")]
    void AutoFindReferences()
    {
        // Find SystemsPanel in children
        if (systemsPanel == null)
        {
            systemsPanel = transform.Find("SystemsPanel")?.gameObject;
            if (systemsPanel != null) Debug.Log("Auto-found SystemsPanel");
        }
        
        // Find UI elements within SystemsPanel
        if (systemsPanel != null)
        {
            // Find InstVisuals
            if (instVisuals == null)
            {
                instVisuals = systemsPanel.GetComponentInChildren<RawImage>();
                if (instVisuals != null) Debug.Log("Auto-found InstVisuals");
            }
            
            // Find ConfirmButton
            if (confirmButton == null)
            {
                confirmButton = systemsPanel.GetComponentInChildren<Button>();
                if (confirmButton != null) Debug.Log("Auto-found ConfirmButton");
            }
            
            // Find LeftNavBtn (assume it's named or tagged)
            if (leftNavBtn == null)
            {
                Button[] allButtons = systemsPanel.GetComponentsInChildren<Button>();
                foreach (Button btn in allButtons)
                {
                    if (btn.gameObject.name.Contains("Left") || btn.gameObject.name.Contains("Prev"))
                    {
                        leftNavBtn = btn;
                        Debug.Log("Auto-found LeftNavBtn");
                        break;
                    }
                }
            }
            
            // Find RightNavBtn (assume it's named or tagged)
            if (rightNavBtn == null)
            {
                Button[] allButtons = systemsPanel.GetComponentsInChildren<Button>();
                foreach (Button btn in allButtons)
                {
                    if (btn.gameObject.name.Contains("Right") || btn.gameObject.name.Contains("Next"))
                    {
                        rightNavBtn = btn;
                        Debug.Log("Auto-found RightNavBtn");
                        break;
                    }
                }
            }
            
            // Find PageText (Text component)
            if (pageText == null)
            {
                pageText = systemsPanel.GetComponentInChildren<TMP_Text>();
                if (pageText != null) Debug.Log("Auto-found PageText");
            }
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    // Debug methods

    [ContextMenu("Test Open Panel")]
    void TestOpenPanel()
    {
        if (instructionImages.Count == 0)
        {
            Debug.LogWarning("No instruction images assigned! Add some images first.");
            return;
        }

        TriggerInstructionPanel();
    }

    [ContextMenu("Test Close Panel")]
    void TestClosePanel()
    {
        CloseInstructionPanel();
    }

    [ContextMenu("Test Next Instruction")]
    void TestNextInstruction()
    {
        NextInstruction();
    }

    [ContextMenu("Test Previous Instruction")]
    void TestPreviousInstruction()
    {
        PreviousInstruction();
    }

    [ContextMenu("Test Trigger From Button")]
    void TestTriggerFromButton()
    {
        // Simulate button click
        OpenPanelFromButton();
    }

    [ContextMenu("Test Reset Trigger")]
    void TestResetTrigger()
    {
        ResetTrigger();
    }
}