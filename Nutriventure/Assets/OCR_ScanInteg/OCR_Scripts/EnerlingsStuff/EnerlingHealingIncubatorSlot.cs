using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

public class EnerlingHealingIncubatorSlot : MonoBehaviour
{
  [Header("Database")]
  public IngredientDatabase ingredientDatabase;

  [Header("Incubator Parts")]
  public Transform spawningPoint;
  public Animator animator;
  public AudioSource audioSource;

  [Header("Timer Canvases")]
  public GameObject healingTimerCanvas;
  public GameObject healingTimer2Canvas;

  [Header("Timer Text (TMP)")]
  public TextMeshProUGUI healingTimerText;
  public TextMeshProUGUI healingTimer2Text;

  [Header("Life UI")]
  public Slider lifeSlider;
  public TextMeshProUGUI lifeText;

  [Header("Fasten UI")]
  public GameObject incubatorCanvas; // SHARED between all incubators
  public CanvasGroup incubatorCanvasGroup; // SHARED between all incubators
  public GameObject enerlingFastenPanel; // SHARED between all incubators
  public TextMeshProUGUI questionText; // SHARED between all incubators
  public TextMeshProUGUI costText; // SHARED between all incubators
  public GameObject actionButtonsPanel; // UNIQUE per incubator
  public Button viewButton; // UNIQUE per incubator
  public Button speedUpButton; // UNIQUE per incubator
  public Button yesButton; // SHARED (but listeners will be set per interaction)
  public Button noButton; // SHARED
  public Button fastenCloseButton; // SHARED

  [Header("View UI")]
  public GameObject viewCanvas; // SHARED between all incubators
  public CanvasGroup viewCanvasGroup; // SHARED between all incubators
  public GameObject viewPanel; // SHARED between all incubators
  public TextMeshProUGUI viewNameText; // SHARED
  public TextMeshProUGUI viewDescriptionText; // SHARED
  public Image viewRarityIcon; // SHARED
  public Button viewCloseButton; // SHARED

  [Header("Virtual Camera")]
  public CinemachineVirtualCamera incubatorVirtualCamera;
  public int activeCameraPriority = 100;
  public int inactiveCameraPriority = 0;

  [Header("Warning UI")]
  public GameObject warningPanel;
  public CanvasGroup warningCanvasGroup;
  public TextMeshProUGUI warningText;
  public float warningShowSeconds = 2f;

  [Header("UI Audio")]
  public AudioSource warningSfxSource;
  public AudioClip warningSfx;
  public AudioSource successSfxSource; // Audio source for success sound
  public AudioClip successSfx; // Success sound clip

  [Header("Disable While Panel Open")]
  public List<GameObject> disableOnPanelOpen = new List<GameObject>();

  [Header("Trigger Settings")]
  public string playerTag = "Player";
  public float canvasFadeDuration = 0.2f;

  [Header("Action Buttons Animation")]
  public AnimationCurve buttonSlideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public AnimationCurve buttonFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public float buttonSlideDistance = 120f;
  public float buttonFadeDuration = 0.25f;
  public float buttonStaggerDelay = 0.08f;

  // Static reference to track which incubator currently has the shared canvas open
  private static EnerlingHealingIncubatorSlot activeIncubator = null;

  private GameObject spawnedEnerling;
  private string assignedEnerlingName = "";
  private Coroutine regenTextCoroutine;
  private Coroutine canvasFadeCoroutine;
  private Coroutine viewFadeCoroutine;
  private Coroutine warningCoroutine;
  private Coroutine actionButtonsCoroutine;
  private Coroutine exitHideCoroutine;
  private Player_Data playerDataCache;

  // State management
  private bool isPlayerInRange = false;
  private bool hasShownButtonsThisStay = false;
  private bool areButtonsVisible = false;

  private Button[] actionButtons;
  private Vector3[] actionButtonPositions;

  public string AssignedEnerlingName => assignedEnerlingName;
  public bool IsOccupied => !string.IsNullOrEmpty(assignedEnerlingName);

  // Check if THIS incubator is the one with the shared canvas open
  private bool IsActiveIncubator => activeIncubator == this;

  void Start()
  {
    InitializeActionButtons();
    HideAllUIImmediate();
  }

  public void AssignEnerling(IngredientDatabase.IngredientInfo info)
  {
    if (info == null || string.IsNullOrEmpty(info.ingredientName))
      return;

    if (assignedEnerlingName == info.ingredientName && spawnedEnerling != null)
    {
      SetActiveState(true);
      return;
    }

    Clear();
    assignedEnerlingName = info.ingredientName;

    if (spawningPoint != null && info.modelPrefab != null)
    {
      spawnedEnerling = Instantiate(info.modelPrefab, spawningPoint);
      spawnedEnerling.transform.localPosition = Vector3.zero;
      spawnedEnerling.transform.localRotation = Quaternion.identity;
      spawnedEnerling.transform.localScale = Vector3.one;
    }

    SetActiveState(true);

    // Reset trigger state when new enerling is assigned
    hasShownButtonsThisStay = false;

    // Show buttons if player is in range and no other incubator has shared canvas open
    if (isPlayerInRange && activeIncubator == null)
      ShowActionButtons();
  }

  public void Clear()
  {
    if (spawnedEnerling != null)
    {
      Destroy(spawnedEnerling);
      spawnedEnerling = null;
    }

    StopRegenTextLoop();
    assignedEnerlingName = "";

    // Set animator parameter to false and let it play its animation
    if (animator != null)
    {
      animator.SetBool("IncubatorOn", false);
      // Don't disable animator - let it play the off animation
    }

    // Stop audio
    if (audioSource != null)
    {
      audioSource.Stop();
    }

    SetTimerText("");
    SetLifeText("");
    if (lifeSlider != null)
    {
      lifeSlider.value = 0f;
    }

    // If this incubator was the active one, clear active reference
    if (IsActiveIncubator)
    {
      activeIncubator = null;
    }

    // Hide UI and reset states
    hasShownButtonsThisStay = false;
    areButtonsVisible = false;

    CancelPendingExitHide();
    HideActionButtonsImmediate();

    // Hide timer canvases
    if (healingTimerCanvas != null) healingTimerCanvas.SetActive(false);
    if (healingTimer2Canvas != null) healingTimer2Canvas.SetActive(false);
  }

  public void UpdateTimer(float remainingSeconds)
  {
    if (!IsOccupied)
    {
      SetTimerText("");
      return;
    }

    SetTimerText(FormatTime(remainingSeconds));
  }

  public void UpdateLifeUI(IngredientDatabase.IngredientInfo info, bool isRegenerating)
  {
    if (info == null || !IsOccupied)
      return;

    if (lifeSlider != null)
    {
      lifeSlider.maxValue = info.baseLife;
      lifeSlider.value = info.currentLife;
    }

    if (isRegenerating)
    {
      StartRegenTextLoop(info);
    }
    else
    {
      StopRegenTextLoop();
      SetLifeText(info.LifeText, info.LifeTextColor);
    }
  }

  void OnTriggerEnter(Collider other)
  {
    if (!other.CompareTag(playerTag)) return;

    CancelPendingExitHide();

    isPlayerInRange = true;

    // Only show action buttons if:
    // 1. Incubator is occupied
    // 2. No other incubator has shared canvas open
    // 3. Haven't already shown buttons during this trigger stay
    if (IsOccupied && activeIncubator == null && !hasShownButtonsThisStay)
    {
      ShowActionButtons();
      hasShownButtonsThisStay = true;
    }
  }

  void OnTriggerExit(Collider other)
  {
    if (!other.CompareTag(playerTag)) return;

    isPlayerInRange = false;
    hasShownButtonsThisStay = false;

    // Only hide action buttons if this incubator is NOT the active one
    // and no other incubator has the shared canvas open
    if (!IsActiveIncubator && activeIncubator == null)
    {
      if (exitHideCoroutine != null)
        StopCoroutine(exitHideCoroutine);

      exitHideCoroutine = StartCoroutine(DelayedHideActionButtons());
    }
  }

  private void SetTimerText(string text)
  {
    if (healingTimerText != null) healingTimerText.text = text;
    if (healingTimer2Text != null) healingTimer2Text.text = text;
  }

  private void SetLifeText(string text, Color? color = null)
  {
    if (lifeText == null) return;

    lifeText.text = text;
    if (color.HasValue)
      lifeText.color = color.Value;
  }

  private void StartRegenTextLoop(IngredientDatabase.IngredientInfo info)
  {
    StopRegenTextLoop();
    regenTextCoroutine = StartCoroutine(RegenTextLoopCoroutine(info));
  }

  private void StopRegenTextLoop()
  {
    if (regenTextCoroutine != null)
    {
      StopCoroutine(regenTextCoroutine);
      regenTextCoroutine = null;
    }
  }

  private IEnumerator RegenTextLoopCoroutine(IngredientDatabase.IngredientInfo info)
  {
    while (info != null && info.currentLife < info.baseLife)
    {
      SetLifeText("Regenerating...", new Color(0.3f, 1f, 0.3f));
      yield return new WaitForSeconds(3f);

      SetLifeText(info.LifeText, info.LifeTextColor);
      yield return new WaitForSeconds(5f);
    }

    if (info != null)
    {
      SetLifeText(info.LifeText, info.LifeTextColor);
    }

    regenTextCoroutine = null;
  }

  private void BindButtons()
  {
    // UNIQUE buttons per incubator
    if (viewButton != null)
    {
      viewButton.onClick.RemoveAllListeners();
      viewButton.onClick.AddListener(OnViewClicked);
    }

    if (speedUpButton != null)
    {
      speedUpButton.onClick.RemoveAllListeners();
      speedUpButton.onClick.AddListener(OnSpeedUpClicked);
    }

    // SHARED buttons - we need to clear ALL listeners and set them fresh
    // to ensure they point to this incubator instance
    if (yesButton != null)
    {
      yesButton.onClick.RemoveAllListeners();
      yesButton.onClick.AddListener(OnYesClicked);
    }

    if (noButton != null)
    {
      noButton.onClick.RemoveAllListeners();
      noButton.onClick.AddListener(OnNoClicked);
    }

    if (fastenCloseButton != null)
    {
      fastenCloseButton.onClick.RemoveAllListeners();
      fastenCloseButton.onClick.AddListener(OnNoClicked);
    }

    if (viewCloseButton != null)
    {
      viewCloseButton.onClick.RemoveAllListeners();
      viewCloseButton.onClick.AddListener(OnViewCloseClicked);
    }
  }

  private void OnSpeedUpClicked()
  {
    PlayClickSound();
    if (!IsOccupied) return;

    // If another incubator already has the shared canvas open, don't proceed
    if (activeIncubator != null && activeIncubator != this)
      return;

    // Set this as the active incubator
    activeIncubator = this;

    // Hide THIS incubator's action buttons immediately
    HideActionButtonsImmediate();

    DisableExtras();

    // Show shared fasten UI
    SetCameraActive(true);
    ShowCanvas(true);
    if (enerlingFastenPanel != null)
      enerlingFastenPanel.SetActive(true);

    RefreshQuestionAndCost();
  }

  private void OnViewClicked()
  {
    PlayClickSound();
    if (!IsOccupied) return;

    // If another incubator already has the shared canvas open, don't proceed
    if (activeIncubator != null && activeIncubator != this)
      return;

    // Set this as the active incubator
    activeIncubator = this;

    // Hide THIS incubator's action buttons immediately
    HideActionButtonsImmediate();

    DisableExtras();

    // Show shared view UI
    SetCameraActive(true);
    ShowViewCanvas();
    RefreshViewInfo();
  }

  private void OnYesClicked()
  {
    PlayClickSound();

    // Only process if this incubator is the active one
    if (!IsActiveIncubator || !IsOccupied) return;

    if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
    {
      ShowWarning("Game data not available.");
      return;
    }

    var info = GetEnerlingInfo();
    if (info == null)
    {
      ShowWarning("Enerling data not available.");
      return;
    }

    int minutes = GetRemainingMinutes(info.ingredientName);
    GetCost(info.rarity, minutes, out int costCoins, out int costGems);

    int currentCoins = GameDataManager.Instance.CurrentGameData.nutriCoins;
    int currentGems = GameDataManager.Instance.CurrentGameData.nutriGems;

    int missingCoins = Mathf.Max(0, costCoins - currentCoins);
    int missingGems = Mathf.Max(0, costGems - currentGems);

    if (missingCoins > 0 || missingGems > 0)
    {
      ShowWarning(BuildMissingCurrencyMessage(missingCoins, missingGems));
      return;
    }

    // Play success sound using the inspector-assigned audio source and clip
    PlaySuccessSound();

    GameDataManager.Instance.CurrentGameData.nutriCoins -= costCoins;
    GameDataManager.Instance.CurrentGameData.nutriGems -= costGems;
    GameDataManager.Instance.SaveGameData();
    RefreshPlayerDataUI();

    info.currentLife = info.baseLife;
    if (PersistentDataManager.Instance != null)
    {
      PersistentDataManager.Instance.SaveEnerlingCurrentLife(info.ingredientName, info.baseLife);
      PersistentDataManager.Instance.ClearEnerlingHealthRegen(info.ingredientName);
    }

    UpdateLifeUI(info, false);
    UpdateTimer(0f);

    // Close UI and clear active incubator
    CloseFastenUI();
    activeIncubator = null;

    EnableExtras();

    // Reset trigger activation
    hasShownButtonsThisStay = false;

    SetCameraActive(false);
  }

  private void OnNoClicked()
  {
    PlayClickSound();

    // Only process if this incubator is the active one
    if (!IsActiveIncubator) return;

    // Close UI and clear active incubator
    CloseFastenUI();
    activeIncubator = null;

    EnableExtras();

    // Reset trigger activation
    hasShownButtonsThisStay = false;

    SetCameraActive(false);
  }

  private void OnViewCloseClicked()
  {
    PlayClickSound();

    // Only process if this incubator is the active one
    if (!IsActiveIncubator) return;

    // Close UI and clear active incubator
    HideViewCanvas();
    activeIncubator = null;

    EnableExtras();

    // Reset trigger activation
    hasShownButtonsThisStay = false;

    SetCameraActive(false);
  }

  private void RefreshQuestionAndCost()
  {
    var info = GetEnerlingInfo();
    if (info == null) return;

    int minutes = GetRemainingMinutes(info.ingredientName);
    GetCost(info.rarity, minutes, out int costCoins, out int costGems);

    if (questionText != null)
      questionText.text = $"Do you want to speed up the healing process of {info.ingredientName}?";

    if (costText != null)
      costText.text = $"This will cost you {costCoins} Nutri Coins and {costGems} Gems.";
  }

  private int GetRemainingMinutes(string enerlingName)
  {
    if (PersistentDataManager.Instance == null) return 0;
    float seconds = PersistentDataManager.Instance.GetEnerlingRegenRemainingSeconds(enerlingName);
    if (seconds <= 0f) return 0;
    return Mathf.CeilToInt(seconds / 60f);
  }

  private void GetCost(IngredientDatabase.Rarity rarity, int minutes, out int coins, out int gems)
  {
    coins = 0;
    gems = 0;

    if (minutes <= 0) return;

    switch (rarity)
    {
      case IngredientDatabase.Rarity.Common:
        coins = 50 * minutes;
        gems = 0;
        break;
      case IngredientDatabase.Rarity.Rare:
        coins = 50 * minutes;
        gems = 1 * minutes;
        break;
      case IngredientDatabase.Rarity.UltraRare:
        coins = 100 * minutes;
        gems = 2 * minutes;
        break;
    }
  }

  private IngredientDatabase.IngredientInfo GetEnerlingInfo()
  {
    if (string.IsNullOrEmpty(assignedEnerlingName)) return null;

    IngredientDatabase db = ingredientDatabase;
    if (db == null && PersistentDataManager.Instance != null)
      db = PersistentDataManager.Instance.ingredientDatabase;

    return db != null ? db.GetIngredientInfo(assignedEnerlingName) : null;
  }

  private string BuildMissingCurrencyMessage(int missingCoins, int missingGems)
  {
    if (missingCoins > 0 && missingGems > 0)
      return $"You don't have enough coins and gems. Need {missingCoins} more coins and {missingGems} more gems.";

    if (missingCoins > 0)
      return $"You don't have enough coins. Need {missingCoins} more coins.";

    return $"You don't have enough gems. Need {missingGems} more gems.";
  }

  private void ShowWarning(string message)
  {
    if (warningText != null)
      warningText.text = message;

    if (warningPanel != null)
      warningPanel.SetActive(true);

    if (warningCoroutine != null)
      StopCoroutine(warningCoroutine);

    warningCoroutine = StartCoroutine(WarningRoutine());

    if (warningSfxSource != null && warningSfx != null)
      warningSfxSource.PlayOneShot(warningSfx);
  }

  private IEnumerator WarningRoutine()
  {
    if (warningCanvasGroup != null)
    {
      warningCanvasGroup.alpha = 1f;
      warningCanvasGroup.interactable = false;
      warningCanvasGroup.blocksRaycasts = false;
    }

    yield return new WaitForSeconds(warningShowSeconds);

    if (warningCanvasGroup != null)
    {
      float elapsed = 0f;
      float startAlpha = warningCanvasGroup.alpha;
      while (elapsed < canvasFadeDuration)
      {
        elapsed += Time.deltaTime;
        float t = canvasFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / canvasFadeDuration);
        warningCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
        yield return null;
      }
      warningCanvasGroup.alpha = 0f;
    }

    if (warningPanel != null)
      warningPanel.SetActive(false);

    warningCoroutine = null;
  }

  private void PlaySuccessSound()
  {
    if (successSfxSource != null && successSfx != null)
    {
      successSfxSource.PlayOneShot(successSfx);
    }
  }

  private void RefreshPlayerDataUI()
  {
    if (playerDataCache == null)
      playerDataCache = FindFirstObjectByType<Player_Data>();

    if (playerDataCache != null)
      playerDataCache.ForceUpdateAllUI();
  }

  private void ShowCanvas(bool show)
  {
    if (incubatorCanvas == null && incubatorCanvasGroup == null) return;

    if (canvasFadeCoroutine != null)
      StopCoroutine(canvasFadeCoroutine);

    canvasFadeCoroutine = StartCoroutine(FadeCanvasRoutine(show));
  }

  private void ShowViewCanvas()
  {
    if (viewCanvas == null && viewCanvasGroup == null) return;

    if (viewFadeCoroutine != null)
      StopCoroutine(viewFadeCoroutine);

    viewFadeCoroutine = StartCoroutine(FadeViewCanvasRoutine(true));
  }

  private void HideViewCanvas()
  {
    if (viewCanvas == null && viewCanvasGroup == null) return;

    if (viewFadeCoroutine != null)
      StopCoroutine(viewFadeCoroutine);

    viewFadeCoroutine = StartCoroutine(FadeViewCanvasRoutine(false));
  }

  private IEnumerator FadeCanvasRoutine(bool show)
  {
    if (incubatorCanvas != null)
      incubatorCanvas.SetActive(true);

    if (incubatorCanvasGroup == null)
    {
      yield break;
    }

    incubatorCanvasGroup.interactable = show;
    incubatorCanvasGroup.blocksRaycasts = show;

    float elapsed = 0f;
    float startAlpha = incubatorCanvasGroup.alpha;
    float targetAlpha = show ? 1f : 0f;

    while (elapsed < canvasFadeDuration)
    {
      elapsed += Time.deltaTime;
      float t = canvasFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / canvasFadeDuration);
      incubatorCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
      yield return null;
    }

    incubatorCanvasGroup.alpha = targetAlpha;
    canvasFadeCoroutine = null;
  }

  private IEnumerator FadeViewCanvasRoutine(bool show)
  {
    if (viewCanvas != null)
      viewCanvas.SetActive(true);

    if (viewPanel != null)
      viewPanel.SetActive(true);

    if (viewCanvasGroup == null)
    {
      yield break;
    }

    viewCanvasGroup.interactable = show;
    viewCanvasGroup.blocksRaycasts = show;

    float elapsed = 0f;
    float startAlpha = viewCanvasGroup.alpha;
    float targetAlpha = show ? 1f : 0f;

    while (elapsed < canvasFadeDuration)
    {
      elapsed += Time.deltaTime;
      float t = canvasFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / canvasFadeDuration);
      viewCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
      yield return null;
    }

    viewCanvasGroup.alpha = targetAlpha;
    viewFadeCoroutine = null;
  }

  private void HideAllUI()
  {
    if (enerlingFastenPanel != null)
      enerlingFastenPanel.SetActive(false);
    if (warningPanel != null)
      warningPanel.SetActive(false);
    ShowCanvas(false);
    HideViewCanvas();
    HideActionButtons();
  }

  private void HideAllUIImmediate()
  {
    if (enerlingFastenPanel != null)
      enerlingFastenPanel.SetActive(false);
    if (warningPanel != null)
      warningPanel.SetActive(false);

    if (incubatorCanvasGroup != null)
    {
      incubatorCanvasGroup.alpha = 0f;
      incubatorCanvasGroup.interactable = false;
      incubatorCanvasGroup.blocksRaycasts = false;
    }

    if (incubatorCanvas != null)
      incubatorCanvas.SetActive(true);

    if (viewCanvasGroup != null)
    {
      viewCanvasGroup.alpha = 0f;
      viewCanvasGroup.interactable = false;
      viewCanvasGroup.blocksRaycasts = false;
    }

    if (viewPanel != null)
      viewPanel.SetActive(true);
    if (viewCanvas != null)
      viewCanvas.SetActive(true);

    if (actionButtonsPanel != null)
      actionButtonsPanel.SetActive(false);

    areButtonsVisible = false;
  }

  private void CloseFastenUI()
  {
    if (enerlingFastenPanel != null)
      enerlingFastenPanel.SetActive(false);
    ShowCanvas(false);
    if (warningPanel != null)
      warningPanel.SetActive(false);
  }

  private void DisableExtras()
  {
    for (int i = 0; i < disableOnPanelOpen.Count; i++)
    {
      var obj = disableOnPanelOpen[i];
      if (obj != null && obj.activeSelf)
        obj.SetActive(false);
    }
  }

  private void EnableExtras()
  {
    for (int i = 0; i < disableOnPanelOpen.Count; i++)
    {
      var obj = disableOnPanelOpen[i];
      if (obj != null && !obj.activeSelf)
        obj.SetActive(true);
    }
  }

  private void InitializeActionButtons()
  {
    if (actionButtonsPanel == null) return;

    actionButtons = actionButtonsPanel.GetComponentsInChildren<Button>(true);
    actionButtonPositions = new Vector3[actionButtons.Length];

    for (int i = 0; i < actionButtons.Length; i++)
    {
      if (actionButtons[i] == null) continue;

      actionButtonPositions[i] = actionButtons[i].transform.localPosition;
      SetButtonAlpha(actionButtons[i], 0f);
      actionButtons[i].transform.localPosition -= new Vector3(buttonSlideDistance, 0f, 0f);
      actionButtons[i].interactable = false;
    }

    actionButtonsPanel.SetActive(false);
    areButtonsVisible = false;
  }

  private void ShowActionButtons()
  {
    if (actionButtonsPanel == null) return;
    if (areButtonsVisible) return; // Prevent double-showing

    BindButtons(); // Ensure buttons are bound before showing
    actionButtonsPanel.SetActive(true);
    areButtonsVisible = true;

    if (actionButtonsCoroutine != null)
      StopCoroutine(actionButtonsCoroutine);

    actionButtonsCoroutine = StartCoroutine(AnimateActionButtons(true));
  }

  private void HideActionButtons()
  {
    if (actionButtonsPanel == null) return;
    if (!areButtonsVisible) return; // Prevent double-hiding

    areButtonsVisible = false;

    if (actionButtonsCoroutine != null)
      StopCoroutine(actionButtonsCoroutine);

    actionButtonsCoroutine = StartCoroutine(AnimateActionButtons(false));
  }

  private void HideActionButtonsImmediate()
  {
    if (actionButtonsPanel == null) return;

    // Cancel any ongoing animations
    if (actionButtonsCoroutine != null)
    {
      StopCoroutine(actionButtonsCoroutine);
      actionButtonsCoroutine = null;
    }

    // Immediately hide the panel
    actionButtonsPanel.SetActive(false);
    areButtonsVisible = false;
  }

  private IEnumerator AnimateActionButtons(bool show)
  {
    if (actionButtons == null || actionButtons.Length == 0)
    {
      actionButtonsPanel.SetActive(show);
      yield break;
    }

    if (show)
    {
      for (int i = 0; i < actionButtons.Length; i++)
      {
        if (actionButtons[i] != null)
        {
          StartCoroutine(AnimateSingleButton(actionButtons[i], actionButtonPositions[i], true));
          yield return new WaitForSeconds(buttonStaggerDelay);
        }
      }
    }
    else
    {
      for (int i = 0; i < actionButtons.Length; i++)
      {
        if (actionButtons[i] != null)
        {
          actionButtons[i].interactable = false;
          StartCoroutine(AnimateSingleButton(
              actionButtons[i],
              actionButtonPositions[i] - new Vector3(buttonSlideDistance, 0f, 0f),
              false));
        }
      }

      yield return new WaitForSeconds(buttonFadeDuration);
      actionButtonsPanel.SetActive(false);
    }

    actionButtonsCoroutine = null;
  }

  private IEnumerator AnimateSingleButton(Button button, Vector3 targetPosition, bool showIn)
  {
    float elapsed = 0f;
    Vector3 startPos = button.transform.localPosition;

    while (elapsed < buttonFadeDuration)
    {
      elapsed += Time.deltaTime;
      float t = buttonFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / buttonFadeDuration);

      float slideT = buttonSlideCurve.Evaluate(t);
      float fadeT = buttonFadeCurve.Evaluate(t);

      button.transform.localPosition = Vector3.Lerp(startPos, targetPosition, slideT);
      float alpha = showIn ? fadeT : (1f - fadeT);
      SetButtonAlpha(button, alpha);

      yield return null;
    }

    button.transform.localPosition = targetPosition;
    SetButtonAlpha(button, showIn ? 1f : 0f);

    if (showIn)
      button.interactable = true;
  }

  private void SetButtonAlpha(Button button, float alpha)
  {
    if (button == null) return;
    Graphic[] graphics = button.GetComponentsInChildren<Graphic>();
    foreach (var graphic in graphics)
    {
      Color color = graphic.color;
      color.a = alpha;
      graphic.color = color;
    }
  }

  private IEnumerator DelayedHideActionButtons()
  {
    yield return new WaitForSeconds(0.2f);

    // Double-check conditions before hiding:
    // - Player is not in range
    // - No incubator has shared canvas open
    // - This incubator is not the active one
    if (!isPlayerInRange && activeIncubator == null && !IsActiveIncubator && IsOccupied)
    {
      HideActionButtons();
      hasShownButtonsThisStay = false;
    }

    exitHideCoroutine = null;
  }

  private void CancelPendingExitHide()
  {
    if (exitHideCoroutine != null)
    {
      StopCoroutine(exitHideCoroutine);
      exitHideCoroutine = null;
    }
  }

  private void RefreshViewInfo()
  {
    var info = GetEnerlingInfo();
    if (info == null) return;

    if (viewNameText != null)
      viewNameText.text = info.ingredientName;
    if (viewDescriptionText != null)
      viewDescriptionText.text = info.enerlingDescription;

    if (viewRarityIcon != null)
    {
      IngredientDatabase db = ingredientDatabase;
      if (db == null && PersistentDataManager.Instance != null)
        db = PersistentDataManager.Instance.ingredientDatabase;

      if (db != null)
        viewRarityIcon.sprite = db.GetRarityIcon(info.rarity);
    }
  }

  private void SetCameraActive(bool active)
  {
    if (incubatorVirtualCamera == null) return;
    incubatorVirtualCamera.Priority = active ? activeCameraPriority : inactiveCameraPriority;
  }

  private void PlayClickSound()
  {
    if (AudioHandler.Instance != null)
      AudioHandler.Instance.PlayButtonClick();
  }

  private void SetActiveState(bool hasEnerling)
  {
    // Set animator parameter - let it control the animation
    if (animator != null)
    {
      animator.SetBool("IncubatorOn", hasEnerling);
      // Keep animator enabled so it can play both on and off animations
      animator.enabled = true;
    }

    // Audio control
    if (audioSource != null)
    {
      audioSource.enabled = hasEnerling;
      if (hasEnerling)
      {
        if (!audioSource.isPlaying)
          audioSource.Play();
      }
      else if (audioSource.isPlaying)
      {
        audioSource.Stop();
      }
    }

    // Timer canvases visibility
    if (healingTimerCanvas != null) healingTimerCanvas.SetActive(hasEnerling);
    if (healingTimer2Canvas != null) healingTimer2Canvas.SetActive(hasEnerling);
  }

  private string FormatTime(float seconds)
  {
    if (seconds <= 0f) return "00:00";

    int total = Mathf.CeilToInt(seconds);
    int hours = total / 3600;
    int minutes = (total % 3600) / 60;
    int secs = total % 60;

    if (hours > 0)
      return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, secs);

    return string.Format("{0:00}:{1:00}", minutes, secs);
  }
}