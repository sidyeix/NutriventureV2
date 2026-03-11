using UnityEngine;
using System.Collections;

public class DamagePanelController : MonoBehaviour
{
    private static DamagePanelController instance;
    public static DamagePanelController Instance { get { return instance; } }

    [Header("Settings")]
    public float showDuration = 1f;

    private Coroutine hideCoroutine;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Make sure panel is hidden at start
        gameObject.SetActive(false);
    }

    // Call this method to show the panel
    public void ShowDamagePanel()
    {

        // Stop any existing hide coroutine
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        // Show the panel immediately
        gameObject.SetActive(true);

        // Start coroutine to hide it after duration
        hideCoroutine = StartCoroutine(HidePanelAfterDelay());
    }

    private IEnumerator HidePanelAfterDelay()
    {
        yield return CoroutineYieldCache.WaitForSeconds(showDuration);

        // Hide the panel
        gameObject.SetActive(false);

        hideCoroutine = null;
    }

    // Optional: Force hide the panel
    public void ForceHidePanel()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        gameObject.SetActive(false);
    }
}