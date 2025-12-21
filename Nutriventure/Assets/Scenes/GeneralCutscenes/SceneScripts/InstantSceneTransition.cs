using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using System.Collections;

public class InstantSceneTransition : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private PlayableDirector cutsceneDirector;
    [SerializeField] private string nextSceneName = "Kingdom1";
    [SerializeField] private float fadeOutTime = 1f;

    [Header("UI Elements (Optional)")]
    [SerializeField] private CanvasGroup fadeCanvas;

    void Start()
    {
        if (cutsceneDirector == null)
            cutsceneDirector = GetComponent<PlayableDirector>();

        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped += OnCutsceneFinished;
        }
        else
        {
            // If no timeline, set up manual trigger
            StartCoroutine(ManualTransition());
        }

        // Check if next scene is already loaded
        CheckPreloadedScene();
    }

    void CheckPreloadedScene()
    {
        // Check if Kingdom1 is already loaded in memory
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == nextSceneName)
            {
                Debug.Log($"{nextSceneName} is already preloaded!");
                return;
            }
        }

        Debug.LogWarning($"{nextSceneName} is not preloaded. Will load normally.");
    }

    void OnCutsceneFinished(PlayableDirector director)
    {
        StartCoroutine(TransitionToNextScene());
    }

    IEnumerator ManualTransition()
    {
        // For cutscenes without Timeline
        yield return new WaitForSeconds(10f); // Or your cutscene duration
        StartCoroutine(TransitionToNextScene());
    }

    IEnumerator TransitionToNextScene()
    {
        // Optional: Play fade out effect
        if (fadeCanvas != null)
        {
            yield return StartCoroutine(FadeOut(fadeOutTime));
        }

        // Check if scene is already loaded
        bool isPreloaded = false;
        Scene preloadedScene = default;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == nextSceneName && scene.isLoaded)
            {
                isPreloaded = true;
                preloadedScene = scene;
                break;
            }
        }

        if (isPreloaded)
        {
            // Scene is preloaded - switch instantly!
            Debug.Log($"Switching to preloaded scene: {nextSceneName}");

            // Set the preloaded scene as active
            SceneManager.SetActiveScene(preloadedScene);

            // Unload the cutscene scene
            yield return SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        else
        {
            // Scene wasn't preloaded, load it normally
            Debug.Log($"Loading scene normally: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
    }

    IEnumerator FadeOut(float duration)
    {
        if (fadeCanvas == null) yield break;

        float elapsed = 0f;
        float startAlpha = fadeCanvas.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
            yield return null;
        }

        fadeCanvas.alpha = 1f;
    }

    void OnDestroy()
    {
        if (cutsceneDirector != null)
            cutsceneDirector.stopped -= OnCutsceneFinished;
    }

    // Optional: Manual trigger for testing
    public void SkipToNextScene()
    {
        StartCoroutine(TransitionToNextScene());
    }
}