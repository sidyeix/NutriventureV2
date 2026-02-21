using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class UnlockableCanvasController : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private CanvasGroup unlockableCanvasGroup;
    [SerializeField] private GameObject unlockableCanvasObject;

    [Header("UI Elements")]
    [SerializeField] private Image unlockableIconImage;
    [SerializeField] private TextMeshProUGUI unlockableTitleText;
    [SerializeField] private TextMeshProUGUI unlockableNameText;
    [SerializeField] private TextMeshProUGUI unlockableDescriptionText;
    [SerializeField] private Button continueButton;

    [Header("Default Icons (Fallback)")]
    [SerializeField] private Sprite defaultCharacterIcon;
    [SerializeField] private Sprite defaultSkinIcon;
    [SerializeField] private Sprite defaultIconIcon;
    [SerializeField] private Sprite defaultFrameIcon;
    [SerializeField] private Sprite defaultEnerlingIcon;

    [Header("Audio")]
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    // Singleton instance for easy access
    public static UnlockableCanvasController Instance { get; private set; }

    // Database references (will be passed in)
    private CharacterDatabase characterDatabase;
    private ProfileIconDatabase iconDatabase;
    private FrameDatabase frameDatabase;
    private IngredientDatabase ingredientDatabase;

    private bool isShowing = false;
    private System.Action onContinueCallback;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Find databases if not assigned
        FindDatabases();

        // Ensure canvas starts hidden
        if (unlockableCanvasGroup == null && unlockableCanvasObject != null)
        {
            unlockableCanvasGroup = unlockableCanvasObject.GetComponent<CanvasGroup>();
        }

        if (unlockableCanvasObject != null)
        {
            unlockableCanvasObject.SetActive(false);
        }

        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
    }

    private void FindDatabases()
    {
        // Try to find databases through GameDataManager
        if (GameDataManager.Instance != null)
        {
            characterDatabase = GameDataManager.Instance.characterDatabase;
            iconDatabase = GameDataManager.Instance.iconDatabase;
            frameDatabase = GameDataManager.Instance.frameDatabase;
        }

        // If still null, try to find by type
        if (characterDatabase == null)
        {
            characterDatabase = Resources.FindObjectsOfTypeAll<CharacterDatabase>().FirstOrDefault();
        }

        if (iconDatabase == null)
        {
            iconDatabase = Resources.FindObjectsOfTypeAll<ProfileIconDatabase>().FirstOrDefault();
        }

        if (frameDatabase == null)
        {
            frameDatabase = Resources.FindObjectsOfTypeAll<FrameDatabase>().FirstOrDefault();
        }

        if (ingredientDatabase == null)
        {
            ingredientDatabase = Resources.FindObjectsOfTypeAll<IngredientDatabase>().FirstOrDefault();
        }
    }

    public void SetDatabases(CharacterDatabase charDb, ProfileIconDatabase iconDb, FrameDatabase frameDb, IngredientDatabase ingDb)
    {
        characterDatabase = charDb;
        iconDatabase = iconDb;
        frameDatabase = frameDb;
        ingredientDatabase = ingDb;
    }

    public void ShowUnlockable(QuestReward reward, System.Action onContinue = null)
    {
        if (unlockableCanvasObject == null) return;

        onContinueCallback = onContinue;

        // Determine the type of unlockable and set up the UI
        string unlockableType = GetUnlockableType(reward.type);
        string unlockableName = reward.rewardName;
        Sprite unlockableIcon = GetUnlockableIcon(reward);
        string description = GetUnlockableDescription(reward);

        // Set UI elements
        if (unlockableTitleText != null)
        {
            unlockableTitleText.text = $"New {unlockableType} Unlocked!";
        }

        if (unlockableNameText != null)
        {
            unlockableNameText.text = unlockableName;
        }

        if (unlockableDescriptionText != null)
        {
            unlockableDescriptionText.text = description;
        }

        if (unlockableIconImage != null)
        {
            unlockableIconImage.sprite = unlockableIcon ?? GetDefaultIconForType(reward.type);
            unlockableIconImage.preserveAspect = true;
        }

        // Play unlock sound
        if (unlockSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(unlockSound);
        }

        // Show the canvas with fade in
        StartCoroutine(ShowCanvasCoroutine());
    }

    private IEnumerator ShowCanvasCoroutine()
    {
        isShowing = true;

        unlockableCanvasObject.SetActive(true);

        if (unlockableCanvasGroup != null)
        {
            unlockableCanvasGroup.alpha = 0f;
            float timer = 0f;

            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                unlockableCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
                yield return null;
            }

            unlockableCanvasGroup.alpha = 1f;
        }
    }

    private IEnumerator HideCanvasCoroutine()
    {
        if (unlockableCanvasGroup != null)
        {
            float timer = 0f;
            float startAlpha = unlockableCanvasGroup.alpha;

            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                unlockableCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeOutDuration);
                yield return null;
            }

            unlockableCanvasGroup.alpha = 0f;
        }

        unlockableCanvasObject.SetActive(false);
        isShowing = false;
    }

    private void OnContinueButtonClicked()
    {
        // Play click sound
        if (buttonClickSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(buttonClickSound);
        }
        else if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        // Hide the canvas
        StartCoroutine(HideCanvasCoroutine());

        // Invoke callback
        onContinueCallback?.Invoke();
    }

    private string GetUnlockableType(QuestReward.RewardType type)
    {
        switch (type)
        {
            case QuestReward.RewardType.Character:
                return "Character";
            case QuestReward.RewardType.Skin:
                return "Skin";
            case QuestReward.RewardType.Icon:
                return "Icon";
            case QuestReward.RewardType.Frame:
                return "Frame";
            case QuestReward.RewardType.Enerlings:
                return "Enerling";
            default:
                return "Item";
        }
    }

    private Sprite GetUnlockableIcon(QuestReward reward)
    {
        switch (reward.type)
        {
            case QuestReward.RewardType.Character:
                if (characterDatabase != null)
                {
                    var character = characterDatabase.GetCharacterByName(reward.rewardName);
                    if (character != null)
                        return character.characterIcon;
                }
                break;

            case QuestReward.RewardType.Skin:
                if (characterDatabase != null)
                {
                    var skinInfo = characterDatabase.GetSkinByName(reward.rewardName);
                    if (skinInfo.HasValue)
                    {
                        var skin = characterDatabase.GetSkinByID(skinInfo.Value.characterId, skinInfo.Value.skinId);
                        if (skin != null)
                            return skin.skinIcon;
                    }
                }
                break;

            case QuestReward.RewardType.Icon:
                if (iconDatabase != null)
                {
                    var icon = iconDatabase.GetIconByName(reward.rewardName);
                    if (icon != null)
                        return icon.iconSprite;
                }
                break;

            case QuestReward.RewardType.Frame:
                if (frameDatabase != null)
                {
                    var frame = frameDatabase.GetFrameByName(reward.rewardName);
                    if (frame != null)
                        return frame.frameSprite;
                }
                break;

            case QuestReward.RewardType.Enerlings:
                if (ingredientDatabase != null)
                {
                    var ingredient = ingredientDatabase.GetIngredientInfo(reward.rewardName);
                    if (ingredient != null)
                        return ingredient.enerlingSprite;
                }
                break;
        }

        return null;
    }


    private Sprite GetDefaultIconForType(QuestReward.RewardType type)
    {
        switch (type)
        {
            case QuestReward.RewardType.Character:
                return defaultCharacterIcon;
            case QuestReward.RewardType.Skin:
                return defaultSkinIcon;
            case QuestReward.RewardType.Icon:
                return defaultIconIcon;
            case QuestReward.RewardType.Frame:
                return defaultFrameIcon;
            case QuestReward.RewardType.Enerlings:
                return defaultEnerlingIcon;
            default:
                return null;
        }
    }

    private string GetUnlockableDescription(QuestReward reward)
    {
        switch (reward.type)
        {
            case QuestReward.RewardType.Character:
                if (characterDatabase != null)
                {
                    var character = characterDatabase.GetCharacterByName(reward.rewardName);
                    if (character != null)
                        return character.characterDescription;
                }
                break;

            case QuestReward.RewardType.Skin:
                if (characterDatabase != null)
                {
                    var skinInfo = characterDatabase.GetSkinByName(reward.rewardName);
                    if (skinInfo.HasValue)
                    {
                        var skin = characterDatabase.GetSkinByID(skinInfo.Value.characterId, skinInfo.Value.skinId);
                        if (skin != null)
                            return skin.skinDescription;
                    }
                }
                break;

            case QuestReward.RewardType.Enerlings:
                if (ingredientDatabase != null)
                {
                    var ingredient = ingredientDatabase.GetIngredientInfo(reward.rewardName);
                    if (ingredient != null)
                        return ingredient.enerlingDescription;
                }
                break;
        }

        return $"{reward.rewardName} has been added to your collection!";
    }

    public bool IsShowing()
    {
        return isShowing;
    }

    public void ForceHide()
    {
        if (isShowing)
        {
            StopAllCoroutines();
            unlockableCanvasObject.SetActive(false);
            isShowing = false;
        }
    }
}