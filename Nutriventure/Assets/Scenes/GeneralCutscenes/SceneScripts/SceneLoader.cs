using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem; // Add this

public class SceneLoader : MonoBehaviour
{
    [Header("Progress UI")]
    public Image progressFill;      // Filled image
    public TextMeshProUGUI loadingText; // Percentage text

    [Header("Tips")]
    public TextMeshProUGUI tipText; // Tip display
    [TextArea]
    public string[] tips;           // Add tips in Inspector
    public float tipInterval = 3f;  // Change tip every 3 seconds

    [Header("Background")]
    public Image backgroundImage;   // Background image
    public Sprite[] backgroundSprites; // Random backgrounds

    [Header("Loading Settings")]
    public float minLoadingTime = 5f; // Minimum 5 seconds loading

    private int currentTipIndex = 0;
    private float loadingTimer = 0f;

    void Start()
    {
        // 1. Set random background
        if (backgroundSprites != null && backgroundSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, backgroundSprites.Length);
            backgroundImage.sprite = backgroundSprites[randomIndex];
        }

        // 2. Set random starting tip
        if (tips != null && tips.Length > 0)
        {
            currentTipIndex = Random.Range(0, tips.Length);
            tipText.text = tips[currentTipIndex];

            // Start changing tips
            StartCoroutine(ChangeTips());
        }

        // 3. Start loading
        StartCoroutine(LoadScene());
    }

    IEnumerator ChangeTips()
    {
        while (true)
        {
            yield return new WaitForSeconds(tipInterval);

            if (tips != null && tips.Length > 0)
            {
                // Move to next tip
                currentTipIndex++;
                if (currentTipIndex >= tips.Length)
                    currentTipIndex = 0;

                tipText.text = tips[currentTipIndex];
            }
        }
    }

    IEnumerator LoadScene()
    {
        // Reset progress
        progressFill.fillAmount = 0f;
        loadingTimer = 0f;

        // Get which scene to load
        string sceneToLoad = PlayerPrefs.GetString("NextScene", "PlayerProfile");

        // Start loading in background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false; // Don't switch yet

        // Loading loop for minimum 5 seconds
        while (loadingTimer < minLoadingTime)
        {
            loadingTimer += Time.deltaTime;

            // Calculate progress (0 to 1 over 5 seconds)
            float progress = loadingTimer / minLoadingTime;

            // Update UI
            progressFill.fillAmount = progress;
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";

            yield return null;
        }

        // Wait for async loading to complete
        while (!asyncLoad.isDone)
        {
            // Keep progress at 100%
            progressFill.fillAmount = 1f;
            loadingText.text = "Loading... 100%";

            if (asyncLoad.progress >= 0.9f)
            {
                // Small delay for smooth transition
                yield return new WaitForSeconds(0.5f);

                // Switch to PlayerProfile scene
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // Input System compatible skip
    void Update()
    {
        // Allow skipping after 5 seconds
        if (loadingTimer >= minLoadingTime)
        {
            // Check for ANY input using Input System
            bool anyInput = false;

            // Check keyboard
            if (Keyboard.current != null)
                anyInput |= Keyboard.current.anyKey.wasPressedThisFrame;

            // Check mouse click
            if (Mouse.current != null)
                anyInput |= Mouse.current.leftButton.wasPressedThisFrame;

            // Check touch
            if (Touchscreen.current != null)
                anyInput |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

            // Skip if any input
            if (anyInput)
            {
                string sceneToLoad = PlayerPrefs.GetString("NextScene", "PlayerProfile");
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }
}