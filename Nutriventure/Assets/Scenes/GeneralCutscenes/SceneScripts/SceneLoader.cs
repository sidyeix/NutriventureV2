using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class SceneLoader : MonoBehaviour
{
    [Header("Progress UI")]
    public Image progressFill;
    public TextMeshProUGUI loadingText;

    [Header("Tips")]
    public TextMeshProUGUI tipText;
    [TextArea]
    public string[] tips;
    public float tipInterval = 3f;

    [Header("Background")]
    public Image backgroundImage;
    public Sprite[] backgroundSprites;

    [Header("Loading Settings")]
    public float minLoadingTime = 5f;

    [Header("Multi-Scene Preloading")]
    [Tooltip("Scene to load immediately (usually Cutscene)")]
    public string immediateScene = "Cutscene";

    [Tooltip("Scene to preload for later (usually Kingdom1)")]
    public string preloadForLater = "Kingdom1";

    [Tooltip("Should we preload the next scene?")]
    public bool enablePreloading = true;

    private int currentTipIndex = 0;
    private float loadingTimer = 0f;
    private AsyncOperation immediateLoad;
    private AsyncOperation preloadedScene;
    private bool isPreloading = false;

    void Start()
    {
        // Setup visuals
        SetupVisuals();

        // Start multi-scene loading
        StartCoroutine(LoadMultipleScenes());
    }

    void SetupVisuals()
    {
        // Random background
        if (backgroundSprites != null && backgroundSprites.Length > 0)
        {
            backgroundImage.sprite = backgroundSprites[Random.Range(0, backgroundSprites.Length)];
        }

        // Random starting tip
        if (tips != null && tips.Length > 0)
        {
            currentTipIndex = Random.Range(0, tips.Length);
            tipText.text = tips[currentTipIndex];
            StartCoroutine(ChangeTips());
        }
    }

    IEnumerator ChangeTips()
    {
        while (true)
        {
            yield return new WaitForSeconds(tipInterval);

            if (tips != null && tips.Length > 0)
            {
                currentTipIndex = (currentTipIndex + 1) % tips.Length;
                tipText.text = tips[currentTipIndex];
            }
        }
    }

    IEnumerator LoadMultipleScenes()
    {
        progressFill.fillAmount = 0f;
        loadingTimer = 0f;

        // 1. Start loading the immediate scene (cutscene)
        immediateLoad = SceneManager.LoadSceneAsync(immediateScene);
        immediateLoad.allowSceneActivation = false;

        // 2. If enabled, start preloading the next scene (kingdom1)
        if (enablePreloading && !string.IsNullOrEmpty(preloadForLater))
        {
            StartCoroutine(PreloadNextScene());
        }

        // Show loading progress for minimum time
        while (loadingTimer < minLoadingTime)
        {
            loadingTimer += Time.deltaTime;

            // Calculate combined progress
            float progress = CalculateCombinedProgress();

            // Update UI
            progressFill.fillAmount = progress;
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";

            yield return null;
        }

        // Minimum time reached, wait for immediate scene to be ready
        while (!IsImmediateSceneReady())
        {
            float progress = CalculateCombinedProgress();
            progressFill.fillAmount = Mathf.Max(0.95f, progress);
            loadingText.text = "Loading... 99%";

            yield return null;
        }

        // Everything ready! Show final state
        progressFill.fillAmount = 1f;
        loadingText.text = "Ready! 100%";

        // Small delay for visual polish
        yield return new WaitForSeconds(0.5f);

        // Activate the immediate scene (cutscene)
        immediateLoad.allowSceneActivation = true;
    }

    IEnumerator PreloadNextScene()
    {
        if (isPreloading) yield break;

        isPreloading = true;

        // Wait a bit before starting preload
        yield return new WaitForSeconds(1f);

        // Load the next scene in background
        preloadedScene = SceneManager.LoadSceneAsync(preloadForLater, LoadSceneMode.Additive);
        preloadedScene.allowSceneActivation = false;
        preloadedScene.priority = 0; // Lower priority than immediate scene

        // Wait for it to load
        while (preloadedScene != null && preloadedScene.progress < 0.9f)
        {
            yield return null;
        }

        // Scene is now preloaded and ready in memory!
        Debug.Log($"{preloadForLater} scene preloaded and ready!");
    }

    float CalculateCombinedProgress()
    {
        float timeProgress = loadingTimer / minLoadingTime;

        // Start with immediate scene progress
        float immediateProgress = immediateLoad != null ? immediateLoad.progress / 0.9f : 0f;

        // Add preloaded scene progress (weighted less)
        float preloadProgress = preloadedScene != null ? preloadedScene.progress / 0.9f : 0f;

        // Weighted average (immediate scene more important)
        float sceneProgress = (immediateProgress * 0.7f) + (preloadProgress * 0.3f);

        // Blend time progress with actual loading progress
        return Mathf.Lerp(sceneProgress * 0.8f, 1f, timeProgress * 0.2f);
    }

    bool IsImmediateSceneReady()
    {
        return immediateLoad != null && immediateLoad.progress >= 0.9f;
    }

    // Input System compatible skip
    void Update()
    {
        if (loadingTimer >= minLoadingTime)
        {
            bool anyInput = false;

            if (Keyboard.current != null)
                anyInput |= Keyboard.current.anyKey.wasPressedThisFrame;

            if (Mouse.current != null)
                anyInput |= Mouse.current.leftButton.wasPressedThisFrame;

            if (Touchscreen.current != null)
                anyInput |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

            if (anyInput && IsImmediateSceneReady())
            {
                immediateLoad.allowSceneActivation = true;
            }
        }
    }
}