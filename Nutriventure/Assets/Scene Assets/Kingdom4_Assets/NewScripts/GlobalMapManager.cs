using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GlobalMapManager : MonoBehaviour
{
    [Header("Loading Panel")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Buttons")]
    [SerializeField] private Button kingdom1Button;
    [SerializeField] private Button kingdom2Button;
    [SerializeField] private Button kingdom3Button;
    [SerializeField] private Button kingdom4Button;

    [Header("Scene Names")]
    [SerializeField] private string kingdom1Scene = "3_kingdom1";
    [SerializeField] private string kingdom2Scene = "4_kingdom2";
    [SerializeField] private string kingdom3Scene = "5_kingdom3";
    [SerializeField] private string kingdom4Scene = "6_kingdom4";

    private void Start()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        SetupButtons();
        UpdateKingdomButtons();
    }

    void SetupButtons()
    {
        kingdom1Button.onClick.AddListener(() => TryLoad(kingdom1Scene, true));
        kingdom2Button.onClick.AddListener(() => TryLoad(kingdom2Scene, GameDataManager.Instance.HasSugariaKey()));
        kingdom3Button.onClick.AddListener(() => TryLoad(kingdom3Scene, GameDataManager.Instance.HasPreserviaKey()));
        kingdom4Button.onClick.AddListener(() => TryLoad(kingdom4Scene, GameDataManager.Instance.HasAllerthiaKey()));
    }

    void UpdateKingdomButtons()
    {
        kingdom1Button.interactable = true;
        kingdom2Button.interactable = GameDataManager.Instance.HasSugariaKey();
        kingdom3Button.interactable = GameDataManager.Instance.HasPreserviaKey();
        kingdom4Button.interactable = GameDataManager.Instance.HasAllerthiaKey();
    }

    void TryLoad(string sceneName, bool canLoad)
    {
        if (!canLoad)
        {
            Debug.Log("Kingdom Locked!");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        loadingPanel.SetActive(true);

        if (loadingText != null)
            loadingText.text = "Loading...";

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(sceneName);
    }
}