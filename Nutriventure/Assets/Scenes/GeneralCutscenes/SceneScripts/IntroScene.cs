using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroScreen : MonoBehaviour
{
    public string nextScene = "LoadingScene"; // or your desired next scene
    public float delay = 3f; // duration before moving to next scene

    private void Start()
    {
        Invoke(nameof(GoNext), delay);
    }

    void GoNext()
    {
        SceneManager.LoadScene(nextScene);
    }
}
