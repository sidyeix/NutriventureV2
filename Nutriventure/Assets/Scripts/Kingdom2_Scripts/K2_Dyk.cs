using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Serialization;

public class K2_Dyk : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel; // The main popup panel
    public TextMeshProUGUI factText; // The TMP text field for displaying facts
    public TextMeshProUGUI titleText; // Optional: "Did You Know?" title
    public Image thoughtBubbleTail; // Optional: Thought bubble tail pointing to player
    public RectTransform thoughtBubble; // The thought bubble rectangle
    
    [Header("Timing Settings")]
    public float initialDelay = 12f; // Time before first popup appears (seconds)
    public float displayDuration = 3f; // How long each fact stays visible (seconds)
    public float timeBetweenFacts = 12f; // Time between popups (seconds)
    
    [Header("Animation Settings")]
    public Animator panelAnimator; // Optional: Animator for show/hide animations
    
    [Header("Thought Bubble Animation")]
    public float floatAmplitude = 10f; // How much the bubble floats up and down
    public float floatSpeed = 1f; // Speed of floating animation
    public float scalePulseAmount = 0.1f; // How much it pulses when appearing
    public float scalePulseSpeed = 2f; // Speed of pulse animation
    public float fadeInDuration = 0.5f; // Time to fade in
    public float fadeOutDuration = 0.3f; // Time to fade out
    
    [Header("Text Animation")]
    public float textRevealSpeed = 0.05f; // Time between characters appearing (0 for instant)
    public float textFadeDuration = 0.5f; // Time for text fade in/out
    public bool typewriterEffect = true; // Whether to use typewriter effect
    
    [Header("Position Settings")]
    public Transform playerTransform; // Reference to player transform
    public Vector2 screenOffset = new Vector2(0, 100f); // Offset from player position
    public float followSmoothness = 5f; // How smoothly the bubble follows player
    
    [Header("Randomization")]
    public bool shuffleFacts = true; // Whether to shuffle the facts
    public bool loopFacts = true; // Whether to loop through facts when all shown
    
    [Header("Content - Did You Know Facts")]
    [TextArea(3, 5)]
    public string[] facts = new string[]
    {
        "4 grams of sugar = 1 teaspoon.",
        "You absorb added sugars faster because they don't come with fiber.",
        "Eating too many added sugars can make you feel tired later.",
        "Some snacks hide sugar behind different ingredient names.",
        "Drinks with sugar are absorbed fastest and can spike your blood sugar.",
        "Sugar can appear 5-7 times in some ingredient lists.",
        "Foods with 0g 'Added Sugar' still contain natural sugars.",
        "'Total Sugars' in Nutrition Facts label includes both natural and added sugars.",
        "'Added Sugars' tells you how much sugar was put in during manufacturing.",
        "Ingredients are listed from most to least.",
        "If sugar is the first 3 ingredients, the product is high in sugar.",
        "Some labels use 'g' (grams), but your brain understands teaspoons better.",
        "Fiber slows down sugar absorption — so foods with fiber are usually healthier.",
        "The less ingredients a product has, the less processed it usually is.",
        "Words ending in '-ose' usually mean sugar.",
        "'Corn syrup' and 'high-fructose corn syrup' are common sweeteners in UPFs.",
        "Added sugars are created or added during production — not naturally found in food."
    };
    
    // Private variables
    private List<string> availableFacts = new List<string>();
    private List<string> usedFacts = new List<string>();
    private Coroutine popupCoroutine;
    private Coroutine animationCoroutine;
    private bool isPopupActive = false;
    private bool isGamePaused = false;
    private bool isInitialized = false;
    private float floatTimer = 0f;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Color originalTextColor;
    private Color originalTitleColor;
    
    // References to other managers
    private ProductInformationManager productInfoManager;
    private GameplayProgression gameplayProgression;
    
    void Start()
    {
        // Get references to other managers
        productInfoManager = FindObjectOfType<ProductInformationManager>();
        gameplayProgression = FindObjectOfType<GameplayProgression>();
        
        // Get CanvasGroup or add one
        canvasGroup = popupPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = popupPanel.AddComponent<CanvasGroup>();
        
        // Store original values
        if (popupPanel != null)
        {
            originalScale = popupPanel.transform.localScale;
            popupPanel.transform.localScale = Vector3.zero; // Start hidden
        }
        
        if (factText != null)
            originalTextColor = factText.color;
        
        if (titleText != null)
            originalTitleColor = titleText.color;
        
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
        
        // Initialize facts list
        InitializeFacts();
        
        // Hide panel at start
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            canvasGroup.alpha = 0f;
        }
        
        // Subscribe to pause events
        if (productInfoManager != null)
        {
            ProductInformationManager.OnProductPanelShown += OnGamePaused;
            ProductInformationManager.OnProductPanelHidden += OnGameResumed;
        }
        
        // Start the popup system after initial delay
        Invoke(nameof(StartPopupSystem), initialDelay);
        
        Debug.Log("K2_Dyk initialized with " + facts.Length + " facts");
    }
    
    void Update()
    {
        // Update floating animation if popup is active
        if (isPopupActive && popupPanel.activeSelf)
        {
            UpdateFloatingAnimation();
            
            // Update position to follow player
            if (playerTransform != null)
                UpdatePosition();
        }
    }
    
    private void UpdateFloatingAnimation()
    {
        // Floating up and down animation
        floatTimer += Time.deltaTime * floatSpeed;
        float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;
        
        // Pulse animation
        float pulseScale = 1f + Mathf.Sin(Time.time * scalePulseSpeed) * scalePulseAmount * 0.1f;
        
        // Apply animations
        if (thoughtBubble != null)
        {
            thoughtBubble.anchoredPosition = new Vector2(0, floatOffset);
            thoughtBubble.localScale = Vector3.one * pulseScale;
        }
    }
    
    private void UpdatePosition()
    {
        if (popupPanel == null || Camera.main == null) return;
        
        // Convert player world position to screen position
        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(playerTransform.position);
        
        // Apply offset
        playerScreenPos += new Vector3(screenOffset.x, screenOffset.y, 0);
        
        // Smoothly move the popup
        RectTransform popupRect = popupPanel.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            Vector2 targetPos = new Vector2(playerScreenPos.x, playerScreenPos.y);
            popupRect.position = Vector2.Lerp(popupRect.position, targetPos, Time.deltaTime * followSmoothness);
            
            // Rotate tail toward player if we have one
            if (thoughtBubbleTail != null)
            {
                // Calculate direction from bubble to player
                Vector3 direction = playerTransform.position - popupRect.position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                thoughtBubbleTail.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
            }
        }
    }
    
    private void InitializeFacts()
    {
        // Clear lists
        availableFacts.Clear();
        usedFacts.Clear();
        
        // Add all facts to available list
        foreach (string fact in facts)
        {
            if (!string.IsNullOrEmpty(fact))
                availableFacts.Add(fact);
        }
        
        // Shuffle if enabled
        if (shuffleFacts)
        {
            ShuffleList(availableFacts);
        }
        
        isInitialized = true;
    }
    
    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    
    private void StartPopupSystem()
    {
        // Start the popup coroutine
        if (popupCoroutine == null)
        {
            popupCoroutine = StartCoroutine(PopupRoutine());
        }
    }
    
    private IEnumerator PopupRoutine()
    {
        while (true)
        {
            // Wait for the specified time between facts
            yield return new WaitForSeconds(timeBetweenFacts);
            
            // Check if we should show a popup
            if (CanShowPopup())
            {
                // Get a random fact
                string fact = GetNextFact();
                if (fact != null)
                {
                    // Show the popup with animations
                    yield return StartCoroutine(ShowPopupWithAnimations(fact));
                    
                    // Wait for the display duration
                    yield return new WaitForSeconds(displayDuration);
                    
                    // Hide the popup with animations
                    yield return StartCoroutine(HidePopupWithAnimations());
                }
            }
        }
    }
    
    private bool CanShowPopup()
    {
        // Don't show popup if:
        // 1. Game is paused (product info panel is showing)
        // 2. No facts available
        // 3. Popup is already active
        // 4. Panel is not assigned
        
        if (isGamePaused)
        {
            return false;
        }
        
        if (availableFacts.Count == 0 && !loopFacts)
        {
            return false;
        }
        
        if (isPopupActive)
        {
            return false;
        }
        
        if (popupPanel == null || factText == null)
        {
            return false;
        }
        
        return true;
    }
    
    private string GetNextFact()
    {
        if (availableFacts.Count == 0)
        {
            if (loopFacts)
            {
                // Reset facts - move all used facts back to available
                availableFacts = new List<string>(usedFacts);
                usedFacts.Clear();
                
                // Reshuffle if enabled
                if (shuffleFacts)
                {
                    ShuffleList(availableFacts);
                }
            }
            else
            {
                return null;
            }
        }
        
        // Get the first fact from available list
        string fact = availableFacts[0];
        availableFacts.RemoveAt(0);
        usedFacts.Add(fact);
        
        return fact;
    }
    
    private IEnumerator ShowPopupWithAnimations(string fact)
    {
        isPopupActive = true;
        
        // Pause timer if available
        if (gameplayProgression != null)
        {
            gameplayProgression.PauseTimer();
        }
        
        // Update text
        factText.text = "";
        
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            
            // Reset timer for floating animation
            floatTimer = 0f;
            
            // Start animations
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);
            
            animationCoroutine = StartCoroutine(ShowPopupAnimations(fact));
        }
        
        Debug.Log("Did You Know popup shown: " + fact);
        
        yield return animationCoroutine;
    }
    
    private IEnumerator ShowPopupAnimations(string fact)
    {
        // Reset alpha
        canvasGroup.alpha = 0f;
        
        // Fade in the panel
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            
            // Scale animation
            float scaleProgress = elapsedTime / fadeInDuration;
            float bounceScale = Mathf.Sin(scaleProgress * Mathf.PI) * 0.2f;
            popupPanel.transform.localScale = originalScale * (1f + bounceScale);
            
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        popupPanel.transform.localScale = originalScale;
        
        // Animate text
        yield return StartCoroutine(AnimateTextReveal(fact));
    }
    
    private IEnumerator AnimateTextReveal(string fact)
    {
        if (typewriterEffect && textRevealSpeed > 0)
        {
            // Typewriter effect
            factText.text = "";
            factText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
            
            if (titleText != null)
                titleText.color = new Color(originalTitleColor.r, originalTitleColor.g, originalTitleColor.b, 0f);
            
            // Fade in title
            float textTimer = 0f;
            while (textTimer < textFadeDuration)
            {
                textTimer += Time.deltaTime;
                if (titleText != null)
                    titleText.color = Color.Lerp(
                        new Color(originalTitleColor.r, originalTitleColor.g, originalTitleColor.b, 0f),
                        originalTitleColor,
                        textTimer / textFadeDuration
                    );
                yield return null;
            }
            
            // Type out the fact character by character
            for (int i = 0; i < fact.Length; i++)
            {
                factText.text += fact[i];
                
                // Add a subtle sound effect here if desired
                // AudioManager.PlaySound("typewriter_click");
                
                yield return new WaitForSeconds(textRevealSpeed);
            }
            
            // Fade in the completed text
            textTimer = 0f;
            while (textTimer < textFadeDuration)
            {
                textTimer += Time.deltaTime;
                factText.color = Color.Lerp(
                    new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f),
                    originalTextColor,
                    textTimer / textFadeDuration
                );
                yield return null;
            }
            
            factText.color = originalTextColor;
        }
        else
        {
            // Instant reveal with fade
            factText.text = fact;
            factText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
            
            if (titleText != null)
                titleText.color = new Color(originalTitleColor.r, originalTitleColor.g, originalTitleColor.b, 0f);
            
            float textTimer = 0f;
            while (textTimer < textFadeDuration)
            {
                textTimer += Time.deltaTime;
                float progress = textTimer / textFadeDuration;
                
                if (titleText != null)
                    titleText.color = Color.Lerp(
                        new Color(originalTitleColor.r, originalTitleColor.g, originalTitleColor.b, 0f),
                        originalTitleColor,
                        progress
                    );
                
                factText.color = Color.Lerp(
                    new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f),
                    originalTextColor,
                    progress
                );
                
                yield return null;
            }
            
            factText.color = originalTextColor;
            if (titleText != null)
                titleText.color = originalTitleColor;
        }
    }
    
    private IEnumerator HidePopupWithAnimations()
    {
        yield return StartCoroutine(HidePopupAnimations());
        
        // Resume timer if available
        if (gameplayProgression != null)
        {
            gameplayProgression.ResumeTimer();
        }
        
        isPopupActive = false;
        
        Debug.Log("Did You Know popup hidden");
    }
    
    private IEnumerator HidePopupAnimations()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        // Fade out the panel
        float elapsedTime = 0f;
        Vector3 startScale = popupPanel.transform.localScale;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;
            
            // Fade out
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            
            // Scale down slightly
            popupPanel.transform.localScale = Vector3.Lerp(startScale, startScale * 0.8f, progress);
            
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        popupPanel.SetActive(false);
        popupPanel.transform.localScale = originalScale;
    }
    
    // Event handlers for game pause/resume
    private void OnGamePaused()
    {
        isGamePaused = true;
        
        // If popup is currently showing, hide it immediately
        if (isPopupActive)
        {
            StopAllCoroutines();
            StartCoroutine(HidePopupImmediately());
        }
    }
    
    private void OnGameResumed()
    {
        isGamePaused = false;
    }
    
    private IEnumerator HidePopupImmediately()
    {
        if (popupPanel != null && popupPanel.activeSelf)
        {
            // Quick fade out
            float elapsedTime = 0f;
            while (elapsedTime < 0.2f)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / 0.2f);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            popupPanel.SetActive(false);
            popupPanel.transform.localScale = originalScale;
            
            // Resume timer if available
            if (gameplayProgression != null)
            {
                gameplayProgression.ResumeTimer();
            }
            
            isPopupActive = false;
        }
    }
    
    // Public methods for external control
    public void ShowRandomFact()
    {
        if (!isPopupActive && !isGamePaused)
        {
            string fact = GetNextFact();
            if (fact != null)
            {
                StopAllCoroutines();
                StartCoroutine(ShowPopupWithAnimations(fact));
                StartCoroutine(AutoHidePopup());
            }
        }
    }
    
    private IEnumerator AutoHidePopup()
    {
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(HidePopupWithAnimations());
    }
    
    public void HideCurrentPopup()
    {
        if (isPopupActive)
        {
            StopAllCoroutines();
            StartCoroutine(HidePopupWithAnimations());
        }
    }
    
    public void PausePopupSystem()
    {
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }
        
        // Also hide current popup
        HideCurrentPopup();
    }
    
    public void ResumePopupSystem()
    {
        if (popupCoroutine == null && !isGamePaused)
        {
            popupCoroutine = StartCoroutine(PopupRoutine());
        }
    }
    
    public void ResetPopupSystem()
    {
        // Stop current coroutine
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }
        
        // Hide any active popup
        if (isPopupActive)
        {
            HideCurrentPopup();
        }
        
        // Reset facts
        InitializeFacts();
        
        // Restart system
        Invoke(nameof(StartPopupSystem), initialDelay);
    }
    
    // Clean up
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (productInfoManager != null)
        {
            ProductInformationManager.OnProductPanelShown -= OnGamePaused;
            ProductInformationManager.OnProductPanelHidden -= OnGameResumed;
        }
        
        // Stop all coroutines
        StopAllCoroutines();
    }
    
    // Context menu for testing
    [ContextMenu("Test Show Random Fact")]
    public void TestShowRandomFact()
    {
        ShowRandomFact();
    }
    
    [ContextMenu("Test Hide Popup")]
    public void TestHidePopup()
    {
        HideCurrentPopup();
    }
    
    [ContextMenu("Test Reset System")]
    public void TestResetSystem()
    {
        ResetPopupSystem();
    }
    
    [ContextMenu("Debug Status")]
    public void DebugStatus()
    {
        Debug.Log($"=== DID YOU KNOW POPUP STATUS ===");
        Debug.Log($"Total Facts: {GetTotalFactsCount()}");
        Debug.Log($"Available Facts: {GetAvailableFactsCount()}");
        Debug.Log($"Used Facts: {GetUsedFactsCount()}");
        Debug.Log($"Popup Active: {IsPopupActive()}");
        Debug.Log($"Game Paused: {IsSystemPaused()}");
        Debug.Log($"Coroutine Running: {popupCoroutine != null}");
        Debug.Log($"Panel Assigned: {popupPanel != null}");
        Debug.Log($"Text Assigned: {factText != null}");
        
        if (productInfoManager != null)
        {
            Debug.Log($"Product Panel Visible: {productInfoManager.IsPanelVisible()}");
        }
    }
    
    // Statistics getters
    public int GetTotalFactsCount() => facts.Length;
    public int GetAvailableFactsCount() => availableFacts.Count;
    public int GetUsedFactsCount() => usedFacts.Count;
    public bool IsPopupActive() => isPopupActive;
    public bool IsSystemPaused() => isGamePaused;
}

// Optional: Add this to a new file called ThoughtBubbleAnimator.cs for additional effects
/*
[RequireComponent(typeof(RectTransform))]
public class ThoughtBubbleAnimator : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float floatAmount = 10f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
    
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        originalScale = transform.localScale;
    }
    
    void Update()
    {
        // Floating animation
        float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        rectTransform.anchoredPosition = originalPosition + new Vector2(0, floatOffset);
        
        // Pulse animation
        float pulseScale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale * pulseScale;
    }
}
*/