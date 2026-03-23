using System.Collections;
using System.Collections.Generic;
using TMPro;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class NPCChallengeManager : MonoBehaviour
{
    [System.Serializable]
    public class CategorySpriteEntry
    {
        public NPCAllergenCategory category;
        public Sprite sprite;
    }

    [Header("NPC Trigger Canvas (World Space)")]
    public GameObject npcCanvasRoot;
    public CanvasGroup npcCanvasGroup;
    public Transform allergyGridParent;
    public GameObject allergyImagePrefab;
    public float npcCanvasFadeDuration = 0.25f;
    public float allergySpawnInterval = 0.5f;
    public AudioClip allergyPopSfx;

    [Header("Allergy Setup")]
    public int minAllergies = 1;
    public int maxAllergies = 3;
    public List<CategorySpriteEntry> categorySprites = new List<CategorySpriteEntry>();

    [Header("Answer Result Grid")]
    [Tooltip("Shown in the same grid after the player starts judging foods.")]
    public Sprite correctResultSprite;
    public Sprite wrongResultSprite;

    [Header("Food Spawning")]
    [Tooltip("Pool of food prefabs that have NPCChallengeFoodDefinition attached.")]
    public List<NPCChallengeFoodDefinition> foodPrefabs = new List<NPCChallengeFoodDefinition>();
    public Transform[] foodSpawnPoints;
    public bool clearExistingFoodOnRespawn = true;

    [Header("Raycast Investigation")]
    public Transform rayOrigin;
    public Camera rayCamera;
    public float rayDistance = 5f;
    [Tooltip("Vertical offset added to the ray origin so the investigate ray can be cast higher.")]
    public float rayHeightOffset = 0.5f;
    public LayerMask raycastMask = ~0;
    public bool drawDebugRay = false;

    [Header("Investigate UI")]
    public GameObject investigateButtonRoot;
    public Button investigateButton;

    [Header("Observe Canvas")]
    public GameObject observeCanvasRoot;
    public CanvasGroup observeCanvasGroup;
    public Button safeButton;
    public Button unsafeButton;
    public Button backButton;
    public float observeCanvasFadeDuration = 0.2f;

    [Header("Investigate Camera Transition")]
    [Tooltip("Applies a smooth Cinemachine blend only when Investigate is clicked.")]
    public bool smoothInvestigateTransition = true;
    public CinemachineBlendDefinition.Style investigateBlendStyle = CinemachineBlendDefinition.Style.EaseInOut;
    public float investigateBlendDuration = 0.35f;
    public AudioClip cameraTransitionSfx;

    [Header("Gameplay")]
    public PlayerHealthManager playerHealthManager;
    public float wrongAnswerDamage = 1f;

    [Header("Answer Audio Feedback")]
    public AudioClip correctAnswerSfx;
    public AudioClip incorrectAnswerSfx;

    [Header("Wagon Re-Ride")]
    public bool reenableWagonAfterChallenge = true;
    public KartTrigger kartTriggerToReenable;

    [Header("NPC Animator Result")]
    public Animator npcAnimator;
    public string allergyParameterName = "isAllergy";
    public string happyParameterName = "isHappy";

    private readonly Dictionary<NPCAllergenCategory, Sprite> spriteByCategory = new Dictionary<NPCAllergenCategory, Sprite>();
    private readonly List<NPCAllergenCategory> currentNpcAllergies = new List<NPCAllergenCategory>();
    private readonly List<NPCChallengeFoodDefinition> spawnedFoods = new List<NPCChallengeFoodDefinition>();
    private readonly HashSet<NPCChallengeFoodDefinition> answeredFoods = new HashSet<NPCChallengeFoodDefinition>();
    private readonly List<bool> answerResults = new List<bool>();

    private bool playerInsideTrigger;
    private bool hasTriggeredOnce;
    private NPCChallengeFoodDefinition hoveredFood;
    private NPCChallengeFoodDefinition selectedFood;
    private Coroutine npcCanvasFadeCoroutine;
    private Coroutine observeCanvasFadeCoroutine;
    private Coroutine allergySpawnCoroutine;
    private Coroutine investigateBlendRestoreCoroutine;
    private bool challengeFinished;

    void Awake()
    {
        BuildSpriteLookup();
        WireButtons();
        HideNpcCanvasImmediate();
        HideObserveCanvasImmediate();

        if (investigateButtonRoot != null)
            investigateButtonRoot.SetActive(false);

        if (playerHealthManager == null)
            playerHealthManager = PlayerHealthManager.Instance;

        if (rayCamera == null)
            rayCamera = Camera.main;

        if (kartTriggerToReenable == null)
            kartTriggerToReenable = FindFirstObjectByType<KartTrigger>();
    }

    void Update()
    {
        if (!playerInsideTrigger)
            return;

        UpdateRaycastInvestigation();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInsideTrigger = true;

        if (!hasTriggeredOnce)
        {
            hasTriggeredOnce = true;
            StartChallengeForCurrentApproach();
        }
        else
        {
            FadeNpcCanvas(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInsideTrigger = false;
        hoveredFood = null;

        if (investigateButtonRoot != null)
            investigateButtonRoot.SetActive(false);

        FadeNpcCanvas(false);
        CloseObservePanel();
    }

    void StartChallengeForCurrentApproach()
    {
        challengeFinished = false;
        answerResults.Clear();

        if (npcAnimator != null)
        {
            if (!string.IsNullOrEmpty(allergyParameterName))
                npcAnimator.SetBool(allergyParameterName, false);

            if (!string.IsNullOrEmpty(happyParameterName))
                npcAnimator.SetBool(happyParameterName, false);
        }

        GenerateNpcAllergies();
        ShowNpcAllergyIcons();
        SpawnFoodsForNpc();
        FadeNpcCanvas(true);
    }

    void BuildSpriteLookup()
    {
        spriteByCategory.Clear();

        for (int i = 0; i < categorySprites.Count; i++)
        {
            CategorySpriteEntry entry = categorySprites[i];
            if (entry == null)
                continue;

            spriteByCategory[entry.category] = entry.sprite;
        }
    }

    void WireButtons()
    {
        if (investigateButton != null)
        {
            investigateButton.onClick.RemoveListener(OnInvestigateClicked);
            investigateButton.onClick.AddListener(OnInvestigateClicked);
        }

        if (safeButton != null)
        {
            safeButton.onClick.RemoveListener(OnSafeClicked);
            safeButton.onClick.AddListener(OnSafeClicked);
        }

        if (unsafeButton != null)
        {
            unsafeButton.onClick.RemoveListener(OnUnsafeClicked);
            unsafeButton.onClick.AddListener(OnUnsafeClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    void GenerateNpcAllergies()
    {
        currentNpcAllergies.Clear();

        List<NPCAllergenCategory> pool = new List<NPCAllergenCategory>((NPCAllergenCategory[])System.Enum.GetValues(typeof(NPCAllergenCategory)));
        Shuffle(pool);

        int min = Mathf.Clamp(minAllergies, 1, pool.Count);
        int max = Mathf.Clamp(maxAllergies, min, pool.Count);
        int count = Random.Range(min, max + 1);

        for (int i = 0; i < count; i++)
            currentNpcAllergies.Add(pool[i]);
    }

    void ShowNpcAllergyIcons()
    {
        if (allergySpawnCoroutine != null)
            StopCoroutine(allergySpawnCoroutine);

        ClearAllergyGrid();
        allergySpawnCoroutine = StartCoroutine(SpawnAllergyIconsRoutine());
    }

    IEnumerator SpawnAllergyIconsRoutine()
    {
        if (allergyGridParent == null || allergyImagePrefab == null)
            yield break;

        for (int i = 0; i < currentNpcAllergies.Count; i++)
        {
            NPCAllergenCategory cat = currentNpcAllergies[i];
            GameObject iconObj = Instantiate(allergyImagePrefab, allergyGridParent);
            Image img = iconObj.GetComponent<Image>();

            if (img == null)
                img = iconObj.GetComponentInChildren<Image>(true);

            if (img != null && spriteByCategory.TryGetValue(cat, out Sprite sprite) && sprite != null)
                img.sprite = sprite;

            if (allergyPopSfx != null)
            {
                if (AudioHandler.Instance != null)
                    AudioHandler.Instance.PlayCharacterSelectionSound(allergyPopSfx);
                else
                    AudioSource.PlayClipAtPoint(allergyPopSfx, transform.position);
            }

            if (i < currentNpcAllergies.Count - 1)
                yield return new WaitForSeconds(allergySpawnInterval);
        }
    }

    void ClearAllergyGrid()
    {
        if (allergyGridParent == null)
            return;

        for (int i = allergyGridParent.childCount - 1; i >= 0; i--)
            Destroy(allergyGridParent.GetChild(i).gameObject);
    }

    void SpawnFoodsForNpc()
    {
        if (foodSpawnPoints == null || foodSpawnPoints.Length == 0)
            return;

        if (clearExistingFoodOnRespawn)
            ClearSpawnedFood();

        List<NPCChallengeFoodDefinition> unsafeCandidates = new List<NPCChallengeFoodDefinition>();
        List<NPCChallengeFoodDefinition> safeCandidates = new List<NPCChallengeFoodDefinition>();

        for (int i = 0; i < foodPrefabs.Count; i++)
        {
            NPCChallengeFoodDefinition def = foodPrefabs[i];
            if (def == null)
                continue;

            if (def.HasAnyMatchingCategory(new HashSet<NPCAllergenCategory>(currentNpcAllergies)))
                unsafeCandidates.Add(def);
            else
                safeCandidates.Add(def);
        }

        List<NPCChallengeFoodDefinition> picked = new List<NPCChallengeFoodDefinition>();

        if (unsafeCandidates.Count > 0)
            picked.Add(unsafeCandidates[Random.Range(0, unsafeCandidates.Count)]);

        while (picked.Count < 3 && safeCandidates.Count > 0)
        {
            NPCChallengeFoodDefinition safe = safeCandidates[Random.Range(0, safeCandidates.Count)];
            picked.Add(safe);
            safeCandidates.Remove(safe);
        }

        while (picked.Count < 3 && foodPrefabs.Count > 0)
        {
            NPCChallengeFoodDefinition any = foodPrefabs[Random.Range(0, foodPrefabs.Count)];
            if (any != null)
                picked.Add(any);
            else
                break;
        }

        Shuffle(picked);

        int spawnCount = Mathf.Min(foodSpawnPoints.Length, picked.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            Transform point = foodSpawnPoints[i];
            NPCChallengeFoodDefinition prefab = picked[i];
            if (point == null || prefab == null)
                continue;

            NPCChallengeFoodDefinition instance = Instantiate(prefab, point);

            // Keep prefab-authored transform values while parenting under the spawn point.
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = prefab.transform.localScale;

            instance.ResetVisualState();
            spawnedFoods.Add(instance);
        }

        answeredFoods.Clear();
    }

    void ClearSpawnedFood()
    {
        for (int i = 0; i < spawnedFoods.Count; i++)
        {
            if (spawnedFoods[i] != null)
                Destroy(spawnedFoods[i].gameObject);
        }

        spawnedFoods.Clear();
        answeredFoods.Clear();
        hoveredFood = null;
        selectedFood = null;
    }

    void UpdateRaycastInvestigation()
    {
        Ray ray;
        if (rayOrigin != null)
            ray = new Ray(rayOrigin.position + Vector3.up * rayHeightOffset, rayOrigin.forward);
        else if (rayCamera != null)
            ray = new Ray(rayCamera.transform.position + Vector3.up * rayHeightOffset, rayCamera.transform.forward);
        else
            return;

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.cyan);

        hoveredFood = null;

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, raycastMask, QueryTriggerInteraction.Collide))
        {
            NPCChallengeFoodDefinition candidate = hit.collider.GetComponentInParent<NPCChallengeFoodDefinition>();
            if (candidate != null && spawnedFoods.Contains(candidate) && !candidate.IsAnsweredLocked)
            {
                hoveredFood = candidate;
            }
        }

        bool canInvestigate = hoveredFood != null && (observeCanvasRoot == null || !observeCanvasRoot.activeSelf);
        if (investigateButtonRoot != null)
            investigateButtonRoot.SetActive(canInvestigate);
    }

    void OnInvestigateClicked()
    {
        if (hoveredFood == null)
            return;

        selectedFood = hoveredFood;

        PlaySfx(cameraTransitionSfx);

        if (smoothInvestigateTransition)
        {
            ApplyTemporaryInvestigateBlend();
        }

        selectedFood.SetObserveCameraActive(true);
        FadeObserveCanvas(true);

        if (investigateButtonRoot != null)
            investigateButtonRoot.SetActive(false);
    }

    void OnSafeClicked()
    {
        EvaluateSelection(playerChoseSafe: true);
    }

    void OnUnsafeClicked()
    {
        EvaluateSelection(playerChoseSafe: false);
    }

    void OnBackClicked()
    {
        CloseObservePanel();
    }

    void EvaluateSelection(bool playerChoseSafe)
    {
        if (selectedFood == null)
            return;

        if (answeredFoods.Contains(selectedFood))
        {
            CloseObservePanel();
            return;
        }

        bool isActuallyUnsafe = selectedFood.HasAnyMatchingCategory(new HashSet<NPCAllergenCategory>(currentNpcAllergies));
        bool isCorrect = playerChoseSafe ? !isActuallyUnsafe : isActuallyUnsafe;

        if (isCorrect)
        {
            PlaySfx(correctAnswerSfx);

            if (playerChoseSafe)
                selectedFood.ShowGreenShield();
            else
                selectedFood.ShowRedShield();
        }
        else
        {
            PlaySfx(incorrectAnswerSfx);
            DeductLife();
        }

        answeredFoods.Add(selectedFood);
        selectedFood.SetAnsweredLock(true);
        answerResults.Add(isCorrect);
        CloseObservePanel();
        TryFinishChallenge();
    }

    void ShowAnswerResultIcons()
    {
        if (allergyGridParent == null || allergyImagePrefab == null)
            return;

        ClearAllergyGrid();

        for (int i = 0; i < answerResults.Count; i++)
        {
            bool isCorrect = answerResults[i];
            GameObject iconObj = Instantiate(allergyImagePrefab, allergyGridParent);
            Image img = iconObj.GetComponent<Image>();

            if (img == null)
                img = iconObj.GetComponentInChildren<Image>(true);

            if (img == null)
                continue;

            Sprite resultSprite = isCorrect ? correctResultSprite : wrongResultSprite;
            if (resultSprite != null)
            {
                img.sprite = resultSprite;
                img.color = Color.white;
            }
            else
            {
                img.color = isCorrect ? Color.green : Color.red;
            }
        }
    }

    void TryFinishChallenge()
    {
        if (challengeFinished)
            return;

        if (spawnedFoods.Count == 0)
            return;

        if (answeredFoods.Count < spawnedFoods.Count)
            return;

        challengeFinished = true;

        ShowAnswerResultIcons();

        bool hasMistake = false;
        for (int i = 0; i < answerResults.Count; i++)
        {
            if (!answerResults[i])
            {
                hasMistake = true;
                break;
            }
        }

        if (npcAnimator != null)
        {
            if (!string.IsNullOrEmpty(allergyParameterName))
                npcAnimator.SetBool(allergyParameterName, hasMistake);

            if (!string.IsNullOrEmpty(happyParameterName))
                npcAnimator.SetBool(happyParameterName, !hasMistake);
        }

        if (!reenableWagonAfterChallenge)
            return;

        if (kartTriggerToReenable == null)
            kartTriggerToReenable = FindFirstObjectByType<KartTrigger>();

        if (kartTriggerToReenable != null)
            kartTriggerToReenable.ResetRideAvailability();
    }

    void ApplyTemporaryInvestigateBlend()
    {
        CinemachineBrain brain = FindFirstObjectByType<CinemachineBrain>();
        if (brain == null)
            return;

        if (investigateBlendRestoreCoroutine != null)
            StopCoroutine(investigateBlendRestoreCoroutine);

        CinemachineBlendDefinition originalBlend = brain.m_DefaultBlend;
        brain.m_DefaultBlend = new CinemachineBlendDefinition(investigateBlendStyle, Mathf.Max(0f, investigateBlendDuration));
        investigateBlendRestoreCoroutine = StartCoroutine(RestoreBlendAfterDelay(brain, originalBlend, Mathf.Max(0f, investigateBlendDuration) + 0.05f));
    }

    IEnumerator RestoreBlendAfterDelay(CinemachineBrain brain, CinemachineBlendDefinition originalBlend, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (brain != null)
            brain.m_DefaultBlend = originalBlend;

        investigateBlendRestoreCoroutine = null;
    }

    void DeductLife()
    {
        if (playerHealthManager == null)
            playerHealthManager = PlayerHealthManager.Instance;

        if (playerHealthManager != null)
        {
            playerHealthManager.TakeDamage(wrongAnswerDamage);
        }
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(clip);
            return;
        }

        AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    void CloseObservePanel()
    {
        if (selectedFood != null)
        {
            selectedFood.SetObserveCameraActive(false);
            selectedFood = null;
        }

        FadeObserveCanvas(false);
    }

    void FadeNpcCanvas(bool show)
    {
        if (npcCanvasRoot == null || npcCanvasGroup == null)
            return;

        if (npcCanvasFadeCoroutine != null)
            StopCoroutine(npcCanvasFadeCoroutine);

        npcCanvasFadeCoroutine = StartCoroutine(FadeCanvasRoutine(npcCanvasRoot, npcCanvasGroup, show, npcCanvasFadeDuration));
    }

    void FadeObserveCanvas(bool show)
    {
        if (observeCanvasRoot == null || observeCanvasGroup == null)
            return;

        if (observeCanvasFadeCoroutine != null)
            StopCoroutine(observeCanvasFadeCoroutine);

        observeCanvasFadeCoroutine = StartCoroutine(FadeCanvasRoutine(observeCanvasRoot, observeCanvasGroup, show, observeCanvasFadeDuration));
    }

    IEnumerator FadeCanvasRoutine(GameObject root, CanvasGroup group, bool show, float duration)
    {
        if (show)
            root.SetActive(true);

        float start = group.alpha;
        float end = show ? 1f : 0f;
        float t = 0f;

        float safeDuration = Mathf.Max(0.0001f, duration);

        while (t < safeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, end, t / safeDuration);
            yield return null;
        }

        group.alpha = end;
        group.interactable = show;
        group.blocksRaycasts = show;

        if (!show)
            root.SetActive(false);
    }

    void HideNpcCanvasImmediate()
    {
        if (npcCanvasRoot == null || npcCanvasGroup == null)
            return;

        npcCanvasGroup.alpha = 0f;
        npcCanvasGroup.interactable = false;
        npcCanvasGroup.blocksRaycasts = false;
        npcCanvasRoot.SetActive(false);
    }

    void HideObserveCanvasImmediate()
    {
        if (observeCanvasRoot == null || observeCanvasGroup == null)
            return;

        observeCanvasGroup.alpha = 0f;
        observeCanvasGroup.interactable = false;
        observeCanvasGroup.blocksRaycasts = false;
        observeCanvasRoot.SetActive(false);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[r];
            list[r] = temp;
        }
    }
}
