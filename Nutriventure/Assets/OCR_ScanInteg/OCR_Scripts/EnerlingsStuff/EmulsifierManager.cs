using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class EmulsifierManager : MonoBehaviour
{
  [Header("Database")]
  public IngredientDatabase ingredientDatabase;

  [Header("Trigger")]
  public Collider entryTrigger; // Assign the trigger collider used to show the enter prompt

  [Header("Camera Control")]
  public CinemachineVirtualCamera emulsifierCamera;
  public int focusedPriority = 100;

  [Header("Interaction UI")]
  public GameObject enterButtonCanvas;
  public CanvasGroup enterCanvasGroup;
  public Button enterButton;
  public GameObject emulsifierCanvas;
  public Button exitButton;

  [Header("Enerling Panel")]
  public Transform enerlingContentParent;
  public GameObject enerlingOddRowPrefab; // 3 buttons
  public GameObject enerlingEvenRowPrefab; // 4 buttons
  public GameObject enerlingButtonPrefab;

  [Header("Emulsifier Panel")]
  public Transform emulsifierContentParent; // Uses the same row and button prefabs as enerlings

  [Header("Spawn Points")]
  public Transform enerlingSpawnPoint;
  public Transform emulsifierSpawnPoint;
  public Transform emulsifiedSpawnPoint;

  [Header("Timeline")]
  public PlayableDirector emulsifyDirector;
  public PlayableAsset emulsifyTimeline;

  [Header("Confirmation UI")]
  public GameObject emulsifierConfirmCanvas;
  public GameObject confirmPanel;
  public Button claimButton;

  [Header("Effects / Output")]
  public List<GameObject> emulsifiedEffects = new List<GameObject>();

  [Header("Animator")]
  public Animator emulsifierAnimator;

  [Header("Action")]
  public Button emulsifyButton;
  public List<GameObject> disableOnEntry = new List<GameObject>();

  [Header("Enter UI Animation")]
  public AnimationCurve enterSlideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public AnimationCurve enterFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public float enterFadeDuration = 0.25f;
  public float enterSlideDistance = 120f;

  private readonly Dictionary<string, GameObject> enerlingButtons = new Dictionary<string, GameObject>();
  private readonly Dictionary<string, GameObject> emulsifierButtons = new Dictionary<string, GameObject>();
  private readonly List<GameObject> enerlingRows = new List<GameObject>();
  private readonly List<GameObject> emulsifierRows = new List<GameObject>();

  private IngredientDatabase.IngredientInfo selectedEnerling;
  private IngredientDatabase.IngredientInfo selectedEmulsifier;

  private GameObject spawnedEnerling;
  private GameObject spawnedEmulsifier;
  private GameObject spawnedEmulsified;
  private int defaultCameraPriority;
  private Coroutine enterCanvasCoroutine;
  private RectTransform enterCanvasRect;
  private Vector2 enterInitialPos;
  private bool isPlayerInside = false;
  private bool isEmulsifierOpen = false;
  private bool isTimelinePlaying = false;

  private void Awake()
  {
    if (entryTrigger == null)
    {
      entryTrigger = GetComponent<Collider>();
    }

    if (entryTrigger != null && !entryTrigger.isTrigger)
    {
      entryTrigger.isTrigger = true;
    }

    // If the trigger is on a different GameObject, attach a relay to forward trigger events to this manager
    if (entryTrigger != null && entryTrigger.gameObject != gameObject)
    {
      var relay = entryTrigger.gameObject.GetComponent<EmulsifierTriggerRelay>();
      if (relay == null)
      {
        relay = entryTrigger.gameObject.AddComponent<EmulsifierTriggerRelay>();
      }
      relay.manager = this;
    }

    if (enterButtonCanvas != null)
    {
      enterCanvasRect = enterButtonCanvas.GetComponent<RectTransform>();
      if (enterCanvasGroup == null)
      {
        enterCanvasGroup = enterButtonCanvas.GetComponent<CanvasGroup>();
      }

      if (enterCanvasRect != null)
      {
        enterInitialPos = enterCanvasRect.anchoredPosition;
      }
    }

    defaultCameraPriority = emulsifierCamera != null ? emulsifierCamera.Priority : 0;

    if (enterButton != null)
    {
      enterButton.onClick.AddListener(() =>
      {
        PlayClickSound();
        EnterEmulsifier();
      });
    }

    if (exitButton != null)
    {
      exitButton.onClick.AddListener(() =>
      {
        PlayClickSound();
        ExitEmulsifier();
      });
    }

    if (emulsifyButton != null)
    {
      emulsifyButton.onClick.AddListener(() =>
      {
        PlayClickSound();
        OnEmulsifyClicked();
      });
      emulsifyButton.interactable = false;
    }

    if (claimButton != null)
    {
      claimButton.onClick.AddListener(() =>
      {
        PlayClickSound();
        OnClaimClicked();
      });
    }

    HideEnterUIImmediate();

    if (emulsifierCanvas != null)
    {
      emulsifierCanvas.SetActive(false);
    }

    if (emulsifierConfirmCanvas != null)
    {
      emulsifierConfirmCanvas.SetActive(false);
    }
  }

  private void OnTriggerEnter(Collider other)
  {
    HandleTriggerEnter(other);
  }

  private void OnTriggerExit(Collider other)
  {
    HandleTriggerExit(other);
  }

  public void HandleTriggerEnter(Collider other)
  {
    if (!other.CompareTag("Player"))
    {
      return;
    }

    isPlayerInside = true;

    if (!isEmulsifierOpen)
    {
      ShowEnterUI(true);
    }
  }

  public void HandleTriggerExit(Collider other)
  {
    if (!other.CompareTag("Player"))
    {
      return;
    }

    isPlayerInside = false;

    ShowEnterUI(false);
    ExitEmulsifier();
  }

  private void EnterEmulsifier()
  {
    if (emulsifierCamera != null)
    {
      defaultCameraPriority = emulsifierCamera.Priority;
      emulsifierCamera.Priority = focusedPriority;
    }

    EnableSpawnPoints();
    SetEntryObjectsActive(false);
    ShowEnterUI(false);
    isEmulsifierOpen = true;

    StartCoroutine(OpenCanvasAfterFrame());
  }

  private IEnumerator OpenCanvasAfterFrame()
  {
    yield return null; // Wait a frame so the virtual camera can settle

    if (emulsifierCanvas != null)
    {
      emulsifierCanvas.SetActive(true);
    }

    RefreshEnerlingList();
    RefreshEmulsifierList();
    UpdateAnimatorStates();
  }

  public void ExitEmulsifier()
  {
    ClearSelections();

    if (emulsifierConfirmCanvas != null)
    {
      emulsifierConfirmCanvas.SetActive(false);
    }

    if (emulsifyDirector != null)
    {
      emulsifyDirector.Stop();
    }
    isTimelinePlaying = false;

    if (emulsifierCanvas != null)
    {
      emulsifierCanvas.SetActive(false);
    }

    if (emulsifierCamera != null)
    {
      emulsifierCamera.Priority = defaultCameraPriority;
    }

    SetEntryObjectsActive(true);
    isEmulsifierOpen = false;

    if (isPlayerInside)
    {
      ShowEnterUI(true);
    }
    else
    {
      ShowEnterUI(false);
    }
  }

  private void RefreshEnerlingList()
  {
    ClearEnerlingDisplay();

    if (ingredientDatabase == null || enerlingContentParent == null)
    {
      return;
    }

    List<IngredientDatabase.IngredientInfo> enerlingsWithSkin = ingredientDatabase.GetEnerlingsWithSkin();
    DisplayList(enerlingsWithSkin, enerlingContentParent, enerlingOddRowPrefab, enerlingEvenRowPrefab, enerlingButtonPrefab, OnEnerlingButtonClicked, enerlingButtons, selectedEnerling);
  }

  private void RefreshEmulsifierList()
  {
    ClearEmulsifierDisplay();

    if (ingredientDatabase == null || emulsifierContentParent == null)
    {
      return;
    }

    List<IngredientDatabase.IngredientInfo> emulsifiers = ingredientDatabase.GetEmulsifierIngredients();
    DisplayList(emulsifiers, emulsifierContentParent, enerlingOddRowPrefab, enerlingEvenRowPrefab, enerlingButtonPrefab, OnEmulsifierButtonClicked, emulsifierButtons, selectedEmulsifier);
  }

  private void DisplayList(
      List<IngredientDatabase.IngredientInfo> items,
      Transform contentParent,
      GameObject oddRowPrefab,
      GameObject evenRowPrefab,
      GameObject buttonPrefab,
      System.Action<string> clickHandler,
      Dictionary<string, GameObject> buttonMap,
      IngredientDatabase.IngredientInfo currentSelection)
  {
    if (items == null || items.Count == 0 || contentParent == null || buttonPrefab == null)
    {
      return;
    }

    items.Sort((a, b) =>
    {
      int rarityCompare = a.rarity.CompareTo(b.rarity);
      return rarityCompare != 0 ? rarityCompare : a.ingredientName.CompareTo(b.ingredientName);
    });

    int index = 0;
    int rowIndex = 0;
    while (index < items.Count)
    {
      GameObject rowPrefab = rowIndex % 2 == 0 ? oddRowPrefab : evenRowPrefab; // start with odd
      GameObject row = Instantiate(rowPrefab, contentParent);

      if (contentParent == enerlingContentParent)
      {
        enerlingRows.Add(row);
      }
      else
      {
        emulsifierRows.Add(row);
      }

      int maxButtons = rowIndex % 2 == 0 ? 3 : 4;
      for (int i = 0; i < maxButtons && index < items.Count; i++)
      {
        var item = items[index];
        CreateButton(item, row.transform, buttonPrefab, clickHandler, buttonMap, currentSelection);
        index++;
      }

      rowIndex++;
    }
  }

  private void CreateButton(
      IngredientDatabase.IngredientInfo info,
      Transform parent,
      GameObject buttonPrefab,
      System.Action<string> clickHandler,
      Dictionary<string, GameObject> buttonMap,
      IngredientDatabase.IngredientInfo currentSelection)
  {
    if (info == null || parent == null)
    {
      return;
    }

    GameObject buttonObj = Instantiate(buttonPrefab, parent);
    EnerlingButtonController controller = buttonObj.GetComponent<EnerlingButtonController>();
    if (controller != null)
    {
      controller.Initialize(info.ingredientName, info.enerlingSprite, info.rarity, ingredientDatabase);
    }

    Button btn = buttonObj.GetComponent<Button>();
    if (btn != null)
    {
      btn.onClick.AddListener(() =>
      {
        PlayClickSound();
        clickHandler?.Invoke(info.ingredientName);
      });
    }

    buttonMap[info.ingredientName] = buttonObj;
    HighlightButton(buttonObj, currentSelection != null && currentSelection.ingredientName == info.ingredientName);
  }

  private void HighlightButton(GameObject buttonObj, bool highlighted)
  {
    EnerlingButtonController controller = buttonObj.GetComponent<EnerlingButtonController>();
    if (controller != null)
    {
      controller.SetHighlight(highlighted);
      return;
    }

    Image image = buttonObj.GetComponent<Image>();
    if (image != null)
    {
      image.color = highlighted ? new Color(0.8f, 0.8f, 0.8f) : Color.white;
    }
  }

  private void OnEnerlingButtonClicked(string enerlingName)
  {
    if (ingredientDatabase == null)
    {
      return;
    }

    if (selectedEnerling != null && selectedEnerling.ingredientName == enerlingName)
    {
      selectedEnerling = null;
      ClearSpawned(ref spawnedEnerling);
    }
    else
    {
      selectedEnerling = ingredientDatabase.GetIngredientInfo(enerlingName);
      SpawnEnerling();
    }

    RefreshEnerlingList();
    UpdateAnimatorStates();
  }

  private void OnEmulsifierButtonClicked(string emulsifierName)
  {
    if (ingredientDatabase == null)
    {
      return;
    }

    if (selectedEmulsifier != null && SelectedEmulsifierMatches(emulsifierName))
    {
      selectedEmulsifier = null;
      ClearSpawned(ref spawnedEmulsifier);
    }
    else
    {
      selectedEmulsifier = ingredientDatabase.GetIngredientInfo(emulsifierName);
      SpawnEmulsifier();
    }

    RefreshEmulsifierList();
    UpdateAnimatorStates();
  }

  private bool SelectedEmulsifierMatches(string emulsifierName)
  {
    return selectedEmulsifier != null && selectedEmulsifier.ingredientName == emulsifierName;
  }

  private void SpawnEnerling()
  {
    ClearSpawned(ref spawnedEnerling);

    if (selectedEnerling == null || enerlingSpawnPoint == null)
    {
      return;
    }

    GameObject prefab = selectedEnerling.modelPrefab;
    if (prefab == null && selectedEnerling.skinPrefab != null)
    {
      // Fallback only if model prefab is missing
      prefab = selectedEnerling.skinPrefab;
    }
    if (prefab == null)
    {
      return;
    }

    spawnedEnerling = Instantiate(prefab, enerlingSpawnPoint);
    spawnedEnerling.transform.localPosition = Vector3.zero;
    spawnedEnerling.transform.localRotation = Quaternion.identity;
    spawnedEnerling.transform.localScale = Vector3.one;
  }

  private void SpawnEmulsifier()
  {
    ClearSpawned(ref spawnedEmulsifier);

    if (selectedEmulsifier == null || emulsifierSpawnPoint == null)
    {
      return;
    }

    GameObject prefab = selectedEmulsifier.modelPrefab;
    if (prefab == null)
    {
      return;
    }

    spawnedEmulsifier = Instantiate(prefab, emulsifierSpawnPoint);
    spawnedEmulsifier.transform.localPosition = Vector3.zero;
    spawnedEmulsifier.transform.localRotation = Quaternion.identity;
    spawnedEmulsifier.transform.localScale = Vector3.one;
  }

  private void SpawnEmulsifiedSkin()
  {
    ClearSpawned(ref spawnedEmulsified);

    if (selectedEnerling == null || emulsifiedSpawnPoint == null)
    {
      return;
    }

    GameObject prefab = selectedEnerling.skinPrefab;
    if (prefab == null)
    {
      return;
    }

    spawnedEmulsified = Instantiate(prefab, emulsifiedSpawnPoint);
    spawnedEmulsified.transform.localPosition = Vector3.zero;
    spawnedEmulsified.transform.localRotation = Quaternion.identity;
    spawnedEmulsified.transform.localScale = Vector3.one;
  }

  private void ClearSpawned(ref GameObject spawned)
  {
    if (spawned != null)
    {
      Destroy(spawned);
      spawned = null;
    }
  }

  private void UpdateAnimatorStates()
  {
    bool hasEnerling = selectedEnerling != null;
    bool hasEmulsifier = selectedEmulsifier != null;

    if (emulsifierAnimator != null)
    {
      emulsifierAnimator.SetBool("isEnerlingSelected", hasEnerling);
      emulsifierAnimator.SetBool("isEmulsifierSelected", hasEmulsifier);
      emulsifierAnimator.SetBool("isBothSelected", hasEnerling && hasEmulsifier);
    }

    if (emulsifyButton != null)
    {
      emulsifyButton.interactable = hasEnerling && hasEmulsifier;
    }
  }

  private void OnEmulsifyClicked()
  {
    if (selectedEnerling == null || selectedEmulsifier == null)
    {
      return;
    }

    if (isTimelinePlaying)
    {
      return;
    }

    SpawnEmulsifiedSkin();
    ResetAnimatorSelectionFlags();
    PlayEmulsifyTimeline();

    if (emulsifierCanvas != null)
    {
      emulsifierCanvas.SetActive(false);
    }
  }

  private void ClearEnerlingDisplay()
  {
    foreach (GameObject row in enerlingRows)
    {
      Destroy(row);
    }

    enerlingRows.Clear();
    enerlingButtons.Clear();
  }

  private void ClearEmulsifierDisplay()
  {
    foreach (GameObject row in emulsifierRows)
    {
      Destroy(row);
    }

    emulsifierRows.Clear();
    emulsifierButtons.Clear();
  }

  private void ClearSelections()
  {
    selectedEnerling = null;
    selectedEmulsifier = null;
    ClearSpawned(ref spawnedEnerling);
    ClearSpawned(ref spawnedEmulsifier);
    ClearSpawned(ref spawnedEmulsified);
    RefreshEnerlingList();
    RefreshEmulsifierList();
    UpdateAnimatorStates();
    ShowEnterUI(false);
  }

  private void SetEntryObjectsActive(bool active)
  {
    foreach (GameObject go in disableOnEntry)
    {
      if (go != null)
      {
        go.SetActive(active);
      }
    }
  }

  private void HideEnterUIImmediate()
  {
    if (enterButtonCanvas == null)
    {
      return;
    }

    enterButtonCanvas.SetActive(true); // keep it in hierarchy for animation

    if (enterCanvasGroup != null)
    {
      enterCanvasGroup.alpha = 0f;
      enterCanvasGroup.interactable = false;
      enterCanvasGroup.blocksRaycasts = false;
    }

    if (enterCanvasRect != null)
    {
      enterCanvasRect.anchoredPosition = enterInitialPos - new Vector2(enterSlideDistance, 0f);
    }
  }

  private void ShowEnterUI(bool show)
  {
    if (enterButtonCanvas == null)
    {
      return;
    }

    if (isEmulsifierOpen && show)
    {
      // Do not show enter UI while emulsifier is open
      show = false;
    }

    if (enterCanvasCoroutine != null)
    {
      StopCoroutine(enterCanvasCoroutine);
    }

    enterCanvasCoroutine = StartCoroutine(AnimateEnterUI(show));
  }

  private IEnumerator AnimateEnterUI(bool show)
  {
    enterButtonCanvas.SetActive(true);

    if (enterCanvasGroup == null || enterCanvasRect == null)
    {
      enterButtonCanvas.SetActive(show);
      enterCanvasCoroutine = null;
      yield break;
    }

    enterCanvasGroup.interactable = show;
    enterCanvasGroup.blocksRaycasts = show;

    Vector2 startPos = enterCanvasRect.anchoredPosition;
    Vector2 targetPos = show ? enterInitialPos : enterInitialPos - new Vector2(enterSlideDistance, 0f);

    float elapsed = 0f;
    float startAlpha = enterCanvasGroup.alpha;
    float targetAlpha = show ? 1f : 0f;

    while (elapsed < enterFadeDuration)
    {
      elapsed += Time.deltaTime;
      float t = enterFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / enterFadeDuration);
      float slideT = enterSlideCurve.Evaluate(t);
      float fadeT = enterFadeCurve.Evaluate(t);

      enterCanvasRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, slideT);
      enterCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, fadeT);

      yield return null;
    }

    enterCanvasRect.anchoredPosition = targetPos;
    enterCanvasGroup.alpha = targetAlpha;
    enterCanvasCoroutine = null;
  }

  private void PlayEmulsifyTimeline()
  {
    if (emulsifyDirector == null)
    {
      if (emulsifierConfirmCanvas != null)
      {
        emulsifierConfirmCanvas.SetActive(true);
      }
      return;
    }

    if (emulsifyTimeline != null)
    {
      emulsifyDirector.playableAsset = emulsifyTimeline;
    }

    emulsifyDirector.time = 0;
    emulsifyDirector.Play();
    isTimelinePlaying = true;
    StartCoroutine(WaitForTimelineAndShowConfirm());
  }

  private IEnumerator WaitForTimelineAndShowConfirm()
  {
    while (emulsifyDirector != null && emulsifyDirector.state == PlayState.Playing)
    {
      yield return null;
    }

    isTimelinePlaying = false;

    if (emulsifierConfirmCanvas != null)
    {
      emulsifierConfirmCanvas.SetActive(true);
    }
  }

  private void ResetAnimatorSelectionFlags()
  {
    if (emulsifierAnimator != null)
    {
      emulsifierAnimator.SetBool("isEnerlingSelected", false);
      emulsifierAnimator.SetBool("isEmulsifierSelected", false);
      emulsifierAnimator.SetBool("isBothSelected", false);
    }
  }

  private void OnClaimClicked()
  {
    if (selectedEnerling != null)
    {
      selectedEnerling.isSkinEquipped = true;
    }

    DisableEmulsifiedEffects();
    ResetSpawnPoints();

    if (emulsifierConfirmCanvas != null)
    {
      emulsifierConfirmCanvas.SetActive(false);
    }

    ExitEmulsifier();
  }

  private void DisableEmulsifiedEffects()
  {
    foreach (GameObject fx in emulsifiedEffects)
    {
      if (fx != null)
      {
        fx.SetActive(false);
      }
    }
  }

  private void ResetSpawnPoints()
  {
    ClearSpawned(ref spawnedEnerling);
    ClearSpawned(ref spawnedEmulsifier);
    ClearSpawned(ref spawnedEmulsified);

    if (enerlingSpawnPoint != null)
    {
      enerlingSpawnPoint.gameObject.SetActive(true);
    }

    if (emulsifierSpawnPoint != null)
    {
      emulsifierSpawnPoint.gameObject.SetActive(true);
    }

    if (emulsifiedSpawnPoint != null)
    {
      emulsifiedSpawnPoint.gameObject.SetActive(false);
    }
  }

  private void EnableSpawnPoints()
  {
    if (enerlingSpawnPoint != null)
    {
      enerlingSpawnPoint.gameObject.SetActive(true);
    }

    if (emulsifierSpawnPoint != null)
    {
      emulsifierSpawnPoint.gameObject.SetActive(true);
    }

    if (emulsifiedSpawnPoint != null)
    {
      emulsifiedSpawnPoint.gameObject.SetActive(true);
    }
  }

  private void PlayClickSound()
  {
    if (AudioHandler.Instance != null)
    {
      AudioHandler.Instance.PlayButtonClick();
    }
  }
}
