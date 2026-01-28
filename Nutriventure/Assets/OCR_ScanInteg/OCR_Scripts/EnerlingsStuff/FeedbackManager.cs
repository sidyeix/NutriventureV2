using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class FeedbackManager : MonoBehaviour
{
    [Header("References")]
    public GameObject canvasFeedbackPrefab;
    public Transform playerFeedbackSpawnPoint;
    public Transform aiFeedbackSpawnPoint;

    [Header("Organ Sprites")]
    public Sprite heartSprite;
    public Sprite liverSprite;
    public Sprite kidneySprite;
    public Sprite pancreasSprite;
    public Sprite brainSprite;
    public Sprite shieldSprite;

    [Header("Randomization Settings")]
    public Vector3 boundsMin = new Vector3(0f, -0.23f, 0f);
    public Vector3 boundsMax = new Vector3(1.21f, 0.40f, 0.78f);

    [Header("Timing Settings")]
    public float feedbackInterval = 0.5f;
    public float feedbackDuration = 1.5f;
    public float fadeDuration = 0.5f;

    // Singleton instance
    private static FeedbackManager instance;
    public static FeedbackManager Instance => instance;

    // Feedback queue
    private Queue<FeedbackRequest> feedbackQueue = new Queue<FeedbackRequest>();
    private bool isProcessing = false;

    // Animation coroutines
    private Dictionary<GameObject, Coroutine> activeFeedbacks = new Dictionary<GameObject, Coroutine>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Process feedback queue if not already processing
        if (feedbackQueue.Count > 0 && !isProcessing)
        {
            StartCoroutine(ProcessFeedbackQueue());
        }
    }

    // Main method to request feedback
    public void ShowFeedback(int amount, bool isHeal, Transform spawnPoint, string feedbackType, bool isOrganBonus = false, string organName = "", string source = "")
    {
        FeedbackRequest request = new FeedbackRequest
        {
            amount = amount,
            isHeal = isHeal,
            spawnPoint = spawnPoint,
            type = feedbackType,
            isOrganBonus = isOrganBonus,
            organName = organName,
            source = string.IsNullOrEmpty(source) ? "Unknown" : source
        };

        feedbackQueue.Enqueue(request);
        Debug.Log($"Feedback queued: {amount}, Type: {feedbackType}, OrganBonus: {isOrganBonus}, Organ: {organName}, From: {source}");
    }

    // Convenience methods
    public void ShowPlayerDamage(int amount, bool isOrganBonus = false, string organName = "", string source = "")
    {
        ShowFeedback(amount, false, playerFeedbackSpawnPoint, "Damage", isOrganBonus, organName, source);
    }

    public void ShowPlayerHeal(int amount, bool isOrganBonus = false, string organName = "", string source = "")
    {
        ShowFeedback(amount, true, playerFeedbackSpawnPoint, "Heal", isOrganBonus, organName, source);
    }

    public void ShowAIDamage(int amount, bool isOrganBonus = false, string organName = "", string source = "")
    {
        ShowFeedback(amount, false, aiFeedbackSpawnPoint, "Damage", isOrganBonus, organName, source);
    }

    public void ShowAIHeal(int amount, bool isOrganBonus = false, string organName = "", string source = "")
    {
        ShowFeedback(amount, true, aiFeedbackSpawnPoint, "Heal", isOrganBonus, organName, source);
    }

    public void ShowDefend(Transform spawnPoint, int amount, bool isActivation = false, string source = "")
    {
        string type = isActivation ? "Defend Active" : "Defend";
        ShowFeedback(amount, false, spawnPoint, type, false, "", source);
    }

    public void ShowArmorDamage(Transform spawnPoint, int amount, string source = "")
    {
        ShowFeedback(amount, false, spawnPoint, "Armor", false, "", source);
    }

    public void ShowOrganBonus(Transform spawnPoint, int amount, bool isHeal, string organName, string source = "")
    {
        ShowFeedback(amount, isHeal, spawnPoint, "Organ", true, organName, source);
    }

    public void ShowTotalDamageWithOrganBreakdown(Transform targetSpawnPoint, int baseDamage, List<OrganBonus> organBonuses, bool isPlayerTarget, string source)
    {
        // Show base damage
        if (baseDamage > 0)
        {
            ShowFeedback(baseDamage, false, targetSpawnPoint, "Damage", false, "", source + " (Base)");
        }

        // Show each organ bonus separately
        if (organBonuses != null && organBonuses.Count > 0)
        {
            foreach (var organBonus in organBonuses)
            {
                ShowFeedback(organBonus.bonusAmount, false, targetSpawnPoint, "Organ", true, organBonus.organName, source + $" ({organBonus.organName} Bonus)");
            }
        }
    }

    public void ShowTotalHealWithOrganBreakdown(Transform targetSpawnPoint, int baseHeal, List<OrganBonus> organBonuses, bool isPlayerTarget, string source)
    {
        // Show base heal
        if (baseHeal > 0)
        {
            ShowFeedback(baseHeal, true, targetSpawnPoint, "Heal", false, "", source + " (Base)");
        }

        // Show each organ bonus separately
        if (organBonuses != null && organBonuses.Count > 0)
        {
            foreach (var organBonus in organBonuses)
            {
                ShowFeedback(organBonus.bonusAmount, true, targetSpawnPoint, "Organ", true, organBonus.organName, source + $" ({organBonus.organName} Bonus)");
            }
        }
    }

    IEnumerator ProcessFeedbackQueue()
    {
        isProcessing = true;

        while (feedbackQueue.Count > 0)
        {
            FeedbackRequest request = feedbackQueue.Dequeue();

            if (request.spawnPoint != null)
            {
                CreateFeedback(request);
            }

            // Wait for the specified interval before processing next feedback
            yield return new WaitForSeconds(feedbackInterval);
        }

        isProcessing = false;
    }

    void CreateFeedback(FeedbackRequest request)
    {
        if (canvasFeedbackPrefab == null || request.spawnPoint == null) return;

        // Calculate random position within bounds
        Vector3 randomPosition = new Vector3(
            Random.Range(boundsMin.x, boundsMax.x),
            Random.Range(boundsMin.y, boundsMax.y),
            Random.Range(boundsMin.z, boundsMax.z)
        );

        // Instantiate feedback
        GameObject feedback = Instantiate(canvasFeedbackPrefab, request.spawnPoint);
        feedback.transform.localPosition = randomPosition;

        // Set up feedback visuals
        SetupFeedbackVisuals(feedback, request);

        // Animate
        Coroutine animationCoroutine = StartCoroutine(AnimateFeedback(feedback));
        activeFeedbacks[feedback] = animationCoroutine;
    }

    void SetupFeedbackVisuals(GameObject feedback, FeedbackRequest request)
    {
        // Set damage/heal text - look for "Value" child
        Transform valueTransform = feedback.transform.Find("Value");
        if (valueTransform != null)
        {
            TextMeshProUGUI valueText = valueTransform.GetComponent<TextMeshProUGUI>();
            if (valueText != null)
            {
                // For Defend type, show only the number
                if (request.type == "Defend" || request.type == "Defend Active")
                {
                    valueText.text = $"{request.amount}";
                    valueText.color = Color.yellow;
                }
                else
                {
                    valueText.text = request.isHeal ? $"+{request.amount}" : $"-{request.amount}";

                    if (request.isHeal)
                        valueText.color = Color.green;
                    else if (request.isOrganBonus)
                        valueText.color = new Color(1f, 0.5f, 0f); // Orange
                    else
                        valueText.color = Color.red;
                }
            }
        }

        // Set organ/skill icon - look for "Organ" child
        Transform organTransform = feedback.transform.Find("Organ");
        if (organTransform != null)
        {
            Image organImage = organTransform.GetComponent<Image>();

            // Show organ sprite for organ bonuses
            if (request.isOrganBonus && !string.IsNullOrEmpty(request.organName))
            {
                if (organImage != null)
                {
                    Sprite organSprite = GetOrganSprite(request.organName);
                    if (organSprite != null)
                    {
                        organImage.sprite = organSprite;
                        organImage.preserveAspect = true;
                        organImage.gameObject.SetActive(true);
                    }
                }
            }
            // Show shield for defend skills
            else if (request.type == "Defend" || request.type == "Defend Active")
            {
                if (organImage != null && shieldSprite != null)
                {
                    organImage.sprite = shieldSprite;
                    organImage.preserveAspect = true;
                    organImage.gameObject.SetActive(true);
                }
            }
            // Hide for everything else
            else
            {
                if (organImage != null)
                    organImage.gameObject.SetActive(false);
            }
        }
    }

    Sprite GetOrganSprite(string organName)
    {
        if (string.IsNullOrEmpty(organName)) return null;

        switch (organName.ToLower())
        {
            case "heart":
                return heartSprite;
            case "liver":
                return liverSprite;
            case "kidney":
            case "kidneys":
                return kidneySprite;
            case "pancreas":
                return pancreasSprite;
            case "brain":
                return brainSprite;
            default:
                return null;
        }
    }

    IEnumerator AnimateFeedback(GameObject feedback)
    {
        if (feedback == null) yield break;

        Transform feedbackTransform = feedback.transform;
        Vector3 startPos = feedbackTransform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, 0.33f, 0);

        float elapsed = 0f;

        // Move upward
        while (elapsed < feedbackDuration && feedbackTransform != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / feedbackDuration;
            feedbackTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Fade out
        CanvasGroup canvasGroup = feedback.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = feedback.AddComponent<CanvasGroup>();

        elapsed = 0f;
        while (elapsed < fadeDuration && canvasGroup != null && feedback != null)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        // Cleanup
        if (feedback != null)
        {
            if (activeFeedbacks.ContainsKey(feedback))
                activeFeedbacks.Remove(feedback);

            Destroy(feedback);
        }
    }

    // Clean up all active feedbacks
    public void Cleanup()
    {
        // Stop all animation coroutines
        foreach (var coroutine in activeFeedbacks.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        activeFeedbacks.Clear();

        // Clear queue
        feedbackQueue.Clear();
        isProcessing = false;
    }

    // Structs
    private struct FeedbackRequest
    {
        public int amount;
        public bool isHeal;
        public Transform spawnPoint;
        public string type;
        public bool isOrganBonus;
        public string organName;
        public string source;
    }

    public struct OrganBonus
    {
        public string organName;
        public int bonusAmount;

        public OrganBonus(string organName, int bonusAmount)
        {
            this.organName = organName;
            this.bonusAmount = bonusAmount;
        }
    }

    // Generic animation waiting utility
    public IEnumerator WaitForCurrentStateToFinish(Animator animator, int layer = 0)
    {
        if (animator == null) yield break;

        // Wait until we are fully inside a state (not transitioning)
        while (animator.IsInTransition(layer))
            yield return null;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);

        // Wait until this state finishes playing
        while (state.normalizedTime < 1f || animator.IsInTransition(layer))
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(layer);
        }
    }
}