using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrowAssessmentManager : MonoBehaviour
{
    public static GrowAssessmentManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject growAssessCanvas;
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private TMP_Text trackerText;
    [SerializeField] private Transform plusOneSpawnPoint;
    [SerializeField] private GameObject plusOnePrefab;

    [Header("Animation Settings")]
    [SerializeField] private float panelSlideDuration = 0.8f;
    [SerializeField] private float panelSlideDistance = 400f;
    [SerializeField] private float panelShowDelay = 0.2f;
    [SerializeField] private float plusOneDuration = 1.5f;
    [SerializeField] private float plusOneFadeDuration = 0.5f;
    [SerializeField] private float plusOneFloatHeight = 50f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip panelSlideInSound;
    [SerializeField] private AudioClip panelSlideOutSound;
    [SerializeField] private AudioClip plusOneSound;
    [SerializeField] private AudioClip completeSound;
    [SerializeField] private float panelSlideSoundDelay = 0.1f;

    [Header("Energy Settings")]
    [SerializeField] private float correctAnswerEnergyGain = 20f; // +20/100 energy
    [SerializeField] private float wrongAnswerEnergyDeduction = 25f; // -25/100 energy

    [Header("Point System")]
    [SerializeField] private int correctAnswerPoints = 1000;
    [SerializeField] private int wrongAnswerPoints = 500;

    [Header("Tracking Settings")]
    [SerializeField] private int totalQuestions = 8;
    [SerializeField] private string trackerFormat = "{0}/{1} Assessments";

    [Header("References")]
    [SerializeField] private List<InteractiveObject> assessmentObjects = new List<InteractiveObject>();

    private int correctAnswersCount = 0;
    private bool isAssessmentActive = false;
    private bool isTrackerVisible = false;
    private Vector3 trackerPanelHiddenPosition;
    private Vector3 trackerPanelVisiblePosition;
    private Coroutine panelSlideCoroutine;
    private AudioSource audioSource;
    private Coroutine checkEnergyCoroutine;
    private bool shouldRespawnAtLatestPoint = false;
    private Vector3 latestRespawnPoint;
    private ThirdPersonController playerController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        playerController = FindObjectOfType<ThirdPersonController>();
    }

    private void Start()
    {
        InitializeTracker();
        DisableAssessment();
    }

    private void InitializeTracker()
    {
        if (trackerPanel != null)
        {
            trackerPanelHiddenPosition = trackerPanel.transform.localPosition - new Vector3(panelSlideDistance, 0, 0);
            trackerPanelVisiblePosition = trackerPanel.transform.localPosition;

            trackerPanel.transform.localPosition = trackerPanelHiddenPosition;
            trackerPanel.SetActive(false);
        }

        UpdateTrackerText();
    }

    public void StartGrowAssessment()
    {
        if (isAssessmentActive) return;

        Debug.Log("Starting Grow Assessment...");

        // Store latest position for respawn
        if (playerController != null)
        {
            latestRespawnPoint = playerController.transform.position;
            shouldRespawnAtLatestPoint = true;
            Debug.Log($"Stored latest respawn point: {latestRespawnPoint}");
        }

        // Enable canvas
        if (growAssessCanvas != null)
        {
            growAssessCanvas.SetActive(true);
        }

        // Show tracker panel
        ShowTrackerPanel();

        // Activate all assessment objects
        foreach (InteractiveObject obj in assessmentObjects)
        {
            if (obj != null)
            {
                obj.SetInteractable(true);
            }
        }

        isAssessmentActive = true;

        // Start energy checking
        StartEnergyCheck();

        // Start One Life check
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.StartOneLifeCheck();
        }
    }

    public void EndGrowAssessment()
    {
        if (!isAssessmentActive) return;

        Debug.Log("Ending Grow Assessment...");

        // Hide tracker panel
        HideTrackerPanel();

        // Disable canvas after delay
        StartCoroutine(DisableCanvasAfterDelay());

        // Deactivate all assessment objects
        foreach (InteractiveObject obj in assessmentObjects)
        {
            if (obj != null)
            {
                obj.SetInteractable(false);
            }
        }

        isAssessmentActive = false;
        shouldRespawnAtLatestPoint = false;

        // Stop energy checking
        StopEnergyCheck();

        // Stop One Life check
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.StopOneLifeCheck();
        }
    }

    private IEnumerator DisableCanvasAfterDelay()
    {
        yield return new WaitForSeconds(panelSlideDuration + 0.3f);

        if (growAssessCanvas != null)
        {
            growAssessCanvas.SetActive(false);
        }
    }

    private void DisableAssessment()
    {
        if (growAssessCanvas != null)
        {
            growAssessCanvas.SetActive(false);
        }

        if (trackerPanel != null)
        {
            trackerPanel.SetActive(false);
        }

        isAssessmentActive = false;
        shouldRespawnAtLatestPoint = false;
    }

    // Called when player selects correct answer
    public void OnCorrectAnswerSelected()
    {
        if (!isAssessmentActive) return;

        correctAnswersCount++;
        Debug.Log($"Correct answer! Total: {correctAnswersCount}/{totalQuestions}");

        // Add points
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.AddPoints(correctAnswerPoints);

            // Add energy (+20/100)
            GoGrowGlowGameManager.Instance.AddEnergy(correctAnswerEnergyGain);
            Debug.Log($"Added {correctAnswerEnergyGain} energy for correct answer");
        }

        // Update UI
        UpdateTrackerText();

        // Show +1 effect
        ShowPlusOneEffect();

        // Play sound
        PlaySound(plusOneSound);

        // Check if assessment is complete
        if (correctAnswersCount >= totalQuestions)
        {
            AssessmentComplete();
        }
    }

    // Called when player selects wrong answer
    public void OnWrongAnswerSelected()
    {
        if (!isAssessmentActive) return;

        Debug.Log("Wrong answer selected!");

        // Deduct points
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.AddPoints(-wrongAnswerPoints);

            // Deduct energy (-25/100)
            GoGrowGlowGameManager.Instance.RemoveEnergy(wrongAnswerEnergyDeduction);
            Debug.Log($"Deducted {wrongAnswerEnergyDeduction} energy for wrong answer");

            // Check if energy reached zero
            if (GoGrowGlowGameManager.Instance.GetCurrentEnergy() <= 0f)
            {
                HandleEnergyZero();
            }
        }

        // Optional: Show negative feedback
        // You can add negative effects here
    }

    private void HandleEnergyZero()
    {
        Debug.Log("Energy reached zero! Respawning at latest point...");

        // Respawn at latest point
        RespawnAtLatestPoint();
    }

    private void RespawnAtLatestPoint()
    {
        if (playerController != null && shouldRespawnAtLatestPoint)
        {
            // Reset player position to latest point
            playerController.transform.position = latestRespawnPoint;

            // Reset energy to 50%
            if (GoGrowGlowGameManager.Instance != null)
            {
                GoGrowGlowGameManager.Instance.SetEnergy(50f);
            }

            Debug.Log($"Respawned at latest point: {latestRespawnPoint}");

            // Show respawn effect if available
            ShowRespawnEffect();
        }
    }

    private void ShowRespawnEffect()
    {
        // You can add a visual effect here
        Debug.Log("Respawn effect triggered");

        // Example: Play a sound
        PlaySound(completeSound);
    }

    private void AssessmentComplete()
    {
        Debug.Log("=== ASSESSMENT COMPLETE! ===");

        // Play complete sound
        PlaySound(completeSound);

        // Update tracker text
        if (trackerText != null)
        {
            trackerText.text = "COMPLETE!";
            StartCoroutine(FlashCompleteText());
        }

        // End assessment after delay
        Invoke(nameof(EndGrowAssessment), 2f);
    }

    private void StartEnergyCheck()
    {
        if (checkEnergyCoroutine != null)
            StopCoroutine(checkEnergyCoroutine);

        checkEnergyCoroutine = StartCoroutine(CheckEnergyRoutine());
    }

    private void StopEnergyCheck()
    {
        if (checkEnergyCoroutine != null)
        {
            StopCoroutine(checkEnergyCoroutine);
            checkEnergyCoroutine = null;
        }
    }

    private IEnumerator CheckEnergyRoutine()
    {
        while (isAssessmentActive)
        {
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds

            if (GoGrowGlowGameManager.Instance != null)
            {
                float currentEnergy = GoGrowGlowGameManager.Instance.GetCurrentEnergy();

                if (currentEnergy <= 0f)
                {
                    HandleEnergyZero();
                    yield break; // Stop checking
                }
            }
        }
    }

    private void ShowPlusOneEffect()
    {
        if (plusOnePrefab == null || plusOneSpawnPoint == null) return;

        GameObject plusOneObj = Instantiate(plusOnePrefab, plusOneSpawnPoint.position, Quaternion.identity, plusOneSpawnPoint);

        Canvas canvas = plusOneObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = plusOneObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }

        StartCoroutine(AnimatePlusOne(plusOneObj));
    }

    private IEnumerator AnimatePlusOne(GameObject plusOneObj)
    {
        TMP_Text textComponent = plusOneObj.GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            Color originalColor = textComponent.color;
            Vector3 originalPosition = plusOneObj.transform.localPosition;
            float elapsedTime = 0f;

            while (elapsedTime < plusOneDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / plusOneDuration;

                float yOffset = Mathf.Lerp(0, plusOneFloatHeight, Mathf.Sin(progress * Mathf.PI * 0.5f));
                plusOneObj.transform.localPosition = originalPosition + new Vector3(0, yOffset, 0);

                if (progress > (1 - (plusOneFadeDuration / plusOneDuration)))
                {
                    float fadeProgress = (progress - (1 - (plusOneFadeDuration / plusOneDuration))) / (plusOneFadeDuration / plusOneDuration);
                    textComponent.color = Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0), fadeProgress);
                }

                float scale = 1 + Mathf.Sin(progress * Mathf.PI) * 0.1f;
                plusOneObj.transform.localScale = Vector3.one * scale;

                yield return null;
            }
        }

        Destroy(plusOneObj);
    }

    private void UpdateTrackerText()
    {
        if (trackerText != null)
        {
            trackerText.text = string.Format(trackerFormat, correctAnswersCount, totalQuestions);
        }
    }

    private IEnumerator FlashCompleteText()
    {
        if (trackerText == null) yield break;

        Color originalColor = trackerText.color;
        float flashDuration = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.PingPong(elapsedTime * 3f, 1f);
            trackerText.color = Color.Lerp(originalColor, Color.yellow, t);

            float scale = 1 + Mathf.Sin(elapsedTime * 5f) * 0.05f;
            trackerText.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        trackerText.color = originalColor;
        trackerText.transform.localScale = Vector3.one;
    }

    public void ShowTrackerPanel()
    {
        if (isTrackerVisible || trackerPanel == null) return;

        isTrackerVisible = true;
        trackerPanel.SetActive(true);

        if (panelSlideCoroutine != null)
            StopCoroutine(panelSlideCoroutine);

        panelSlideCoroutine = StartCoroutine(SlidePanel(true));
    }

    public void HideTrackerPanel()
    {
        if (!isTrackerVisible || trackerPanel == null) return;

        if (panelSlideCoroutine != null)
            StopCoroutine(panelSlideCoroutine);

        panelSlideCoroutine = StartCoroutine(SlidePanel(false));

        StartCoroutine(DisablePanelAfterSlide());
    }

    private IEnumerator SlidePanel(bool slideIn)
    {
        if (trackerPanel == null) yield break;

        Vector3 startPos = trackerPanel.transform.localPosition;
        Vector3 targetPos = slideIn ? trackerPanelVisiblePosition : trackerPanelHiddenPosition;
        float elapsedTime = 0f;

        if (slideIn && panelSlideInSound != null)
        {
            StartCoroutine(PlaySoundDelayed(panelSlideInSound, panelSlideSoundDelay));
        }
        else if (!slideIn && panelSlideOutSound != null)
        {
            StartCoroutine(PlaySoundDelayed(panelSlideOutSound, panelSlideSoundDelay));
        }

        if (slideIn)
        {
            yield return new WaitForSeconds(panelShowDelay);
        }

        while (elapsedTime < panelSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / panelSlideDuration;

            if (slideIn)
            {
                t = 1 - Mathf.Pow(1 - t, 3);
            }
            else
            {
                t = Mathf.Pow(t, 3);
            }

            trackerPanel.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        trackerPanel.transform.localPosition = targetPos;
        panelSlideCoroutine = null;
    }

    private IEnumerator DisablePanelAfterSlide()
    {
        yield return new WaitForSeconds(panelSlideDuration + 0.1f);
        trackerPanel.SetActive(false);
        isTrackerVisible = false;
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySound(clip);
    }

    public void RegisterAssessmentObject(InteractiveObject obj)
    {
        if (!assessmentObjects.Contains(obj))
        {
            assessmentObjects.Add(obj);
            Debug.Log($"Registered assessment object: {obj.gameObject.name}");
        }
    }

    public bool IsAssessmentActive() => isAssessmentActive;
    public int GetCorrectAnswersCount() => correctAnswersCount;
    public int GetTotalQuestions() => totalQuestions;
    public float GetCorrectEnergyGain() => correctAnswerEnergyGain;
    public float GetWrongEnergyDeduction() => wrongAnswerEnergyDeduction;

    // Method to update latest respawn point
    public void UpdateRespawnPoint(Vector3 newPoint)
    {
        latestRespawnPoint = newPoint;
        shouldRespawnAtLatestPoint = true;
        Debug.Log($"Updated respawn point to: {newPoint}");
    }
}