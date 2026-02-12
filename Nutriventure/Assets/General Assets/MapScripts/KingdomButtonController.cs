using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class KingdomButtonController : MonoBehaviour
{
    [Header("Kingdom Configuration")]
    [SerializeField] private string sceneToLoad;

    [Header("UI References")]
    [SerializeField] private Button kingdomButton;
    [SerializeField] private CanvasGroup loadingIndicator; // 🔥 CanvasGroup instead

    [Header("Audio")]
    [SerializeField] private AudioClip clickSound;
    private AudioSource audioSource;

    private void Start()
    {
        if (kingdomButton == null)
            kingdomButton = GetComponent<Button>();

        if (kingdomButton != null)
            kingdomButton.onClick.AddListener(OnKingdomButtonClick);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && clickSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Hide loading indicator visually (NOT SetActive)
        if (loadingIndicator != null)
        {
            loadingIndicator.alpha = 0;
            loadingIndicator.blocksRaycasts = false;
            loadingIndicator.interactable = false;
        }
    }

    private void OnKingdomButtonClick()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);

        ShowLoading();

        // Optional: prevent double click
        kingdomButton.interactable = false;

        StartCoroutine(LoadKingdomSceneAsync());
    }

    private void ShowLoading()
    {
        if (loadingIndicator == null) return;

        loadingIndicator.alpha = 1;
        loadingIndicator.blocksRaycasts = true;
        loadingIndicator.interactable = true;
    }

    private void HideLoading()
    {
        if (loadingIndicator == null) return;

        loadingIndicator.alpha = 0;
        loadingIndicator.blocksRaycasts = false;
        loadingIndicator.interactable = false;
    }

    private IEnumerator LoadKingdomSceneAsync()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError($"Scene name not set for {gameObject.name}!");
            HideLoading();
            yield break;
        }

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneToLoad);
        loadOp.allowSceneActivation = false;

        // Optional: small delay so animation is visible
        yield return new WaitForSeconds(0.2f);

        while (loadOp.progress < 0.9f)
        {
            yield return null;
        }

        loadOp.allowSceneActivation = true;
    }
}
