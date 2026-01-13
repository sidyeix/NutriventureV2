using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class LoadingManager : MonoBehaviour
{
    [Header("UI References")]
    public Image progressFill;
    public TextMeshProUGUI percentageText;
    public TextMeshProUGUI tipText;
    public GameObject skipHint;

    [Header("Loading Settings")]
    public string immediateScene = "PlayerProfile";
    public string preloadScene = "3_Kingdom1";
    public float minLoadingTime = 3f;
    public float skipTime = 1f; // Time after 100% before skip is allowed

    [Header("Tips")]
    public string[] tips;
    public float tipInterval = 3f;

    private float loadingTimer = 0f;
    private float skipTimer = 0f;
    private int currentTipIndex = 0;
    private AsyncOperation immediateLoad;
    private AsyncOperation preloadedScene;
    private bool isLoadingComplete = false;
    private bool canSkip = false;
    private bool sceneLoaded = false;

    void Start()
    {
        progressFill.fillAmount = 0f;
        percentageText.text = "0%";

        if (skipHint != null)
            skipHint.SetActive(false);

        StartCoroutine(LoadingSequence());
    }

    IEnumerator LoadingSequence()
    {
        if (tips != null && tips.Length > 0)
        {
            currentTipIndex = Random.Range(0, tips.Length);
            tipText.text = tips[currentTipIndex];
            StartCoroutine(RotateTips());
        }

        immediateLoad = SceneManager.LoadSceneAsync(immediateScene);
        immediateLoad.allowSceneActivation = false;

        if (!string.IsNullOrEmpty(preloadScene))
        {
            preloadedScene = SceneManager.LoadSceneAsync(preloadScene, LoadSceneMode.Additive);
            preloadedScene.allowSceneActivation = false;
        }

        // PHASE 1: Wait for scene to load (actual loading progress)
        while (!IsSceneLoaded())
        {
            loadingTimer += Time.deltaTime;

            float progress = GetLoadingProgress();
            UpdateProgressUI(progress);

            yield return null;
        }

        // Scene is 100% loaded
        sceneLoaded = true;
        UpdateProgressUI(1f);

        // PHASE 2: Start skip timer AFTER loading is 100%
        while (skipTimer < skipTime)
        {
            skipTimer += Time.deltaTime;
            loadingTimer += Time.deltaTime;
            yield return null;
        }

        // Skip time passed, show skip hint
        ShowSkipHint();
        canSkip = true;

        // PHASE 3: Wait for min loading time OR player skip
        while (loadingTimer < minLoadingTime)
        {
            loadingTimer += Time.deltaTime;

            if (CheckForSkipInput())
            {
                immediateLoad.allowSceneActivation = true;
                yield break;
            }

            yield return null;
        }

        // Min loading time reached, auto-proceed
        immediateLoad.allowSceneActivation = true;
    }

    bool IsSceneLoaded()
    {
        return immediateLoad != null && immediateLoad.progress >= 0.9f;
    }

    float GetLoadingProgress()
    {
        return immediateLoad != null ?
            Mathf.Clamp01(immediateLoad.progress / 0.9f) : 0f;
    }

    void UpdateProgressUI(float progress)
    {
        progress = Mathf.Clamp01(progress);
        progressFill.fillAmount = progress;
        percentageText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }

    void ShowSkipHint()
    {
        if (skipHint != null)
            skipHint.SetActive(true);
    }

    IEnumerator RotateTips()
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

    bool CheckForSkipInput()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        return false;
    }

    void Update()
    {
        if (canSkip && CheckForSkipInput())
        {
            immediateLoad.allowSceneActivation = true;
        }
    }

    void OnDestroy()
    {
        if (preloadedScene != null && SceneManager.GetSceneByName(preloadScene).isLoaded)
        {
            SceneManager.UnloadSceneAsync(preloadScene);
        }
    }
}