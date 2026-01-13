using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroScreen : MonoBehaviour
{
    public string loadingScene = "LoadingScene";
    public float delay = 3f;
    private AsyncOperation loadingSceneLoad;

    private void Start()
    {
        // Start loading the loading scene in the background
        loadingSceneLoad = SceneManager.LoadSceneAsync(loadingScene);
        loadingSceneLoad.allowSceneActivation = false;

        // Wait for delay, then activate the loading scene
        StartCoroutine(WaitAndSwitch());
    }

    IEnumerator WaitAndSwitch()
    {
        // Wait for the delay
        yield return new WaitForSeconds(delay);

        // Wait until the loading scene is at least 90% loaded
        while (loadingSceneLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Activate the loading scene
        loadingSceneLoad.allowSceneActivation = true;
    }
}