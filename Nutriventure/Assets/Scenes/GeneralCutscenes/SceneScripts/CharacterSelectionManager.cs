using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Playables;
using System.Collections.Generic;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("References")]
    public CharacterDatabase characterDatabase;
    public CharacterVisualSwapper characterVisualSwapper;
    public Transform buttonContainer;
    public GameObject characterButtonPrefab;

    [Header("UI References")]
    public TMP_InputField nicknameInputField;
    public Image nicknameInputBorder;
    public Button selectHeroButton;
    public GameObject characterSelectionPanel;

    [Header("Timeline References")]
    public PlayableDirector playableDirector; // Your Playable Director component

    [Header("Validation Colors")]
    public Color normalBorderColor = Color.white;
    public Color errorBorderColor = Color.red;

    private Dictionary<GameObject, int> buttonToCharacterID = new Dictionary<GameObject, int>();
    private int selectedCharacterID = -1;

    void Start()
    {
        CreateCharacterButtons();
        LoadSelectedCharacter();

        // Setup button listeners
        if (selectHeroButton != null)
        {
            selectHeroButton.onClick.AddListener(OnSelectHeroClicked);
        }

        // Setup input field listener
        if (nicknameInputField != null)
        {
            nicknameInputField.onValueChanged.AddListener(OnNicknameChanged);
        }

        // Disable Playable Director at start
        if (playableDirector != null)
        {
            playableDirector.enabled = false;
            playableDirector.playOnAwake = false;
            playableDirector.stopped += OnTimelineFinished; // Listen for timeline end
        }

        // Show character selection panel
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(true);
        }
    }

    private void CreateCharacterButtons()
    {
        // Clear existing
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
        buttonToCharacterID.Clear();

        // Create buttons ONLY for characters that are unlockedByDefault
        foreach (var character in characterDatabase.characters)
        {
            if (!character.unlockedByDefault) continue;

            // Create button
            GameObject buttonObj = Instantiate(characterButtonPrefab, buttonContainer);

            // Store in dictionary
            buttonToCharacterID[buttonObj] = character.characterID;

            // Set UI
            Transform t = buttonObj.transform;

            // Icon
            Image icon = t.Find("CharacterIcon")?.GetComponent<Image>();
            if (icon != null && character.characterIcon != null)
                icon.sprite = character.characterIcon;

            // Hide highlight initially
            GameObject highlight = t.Find("SelectedHighlight")?.gameObject;
            if (highlight != null) highlight.SetActive(false);

            // Hide lock elements
            GameObject lockedOverlay = t.Find("LockedOverlay")?.gameObject;
            if (lockedOverlay != null) lockedOverlay.SetActive(false);

            GameObject lockIcon = t.Find("LockIcon")?.gameObject;
            if (lockIcon != null) lockIcon.SetActive(false);

            // Click handler
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnButtonClicked(buttonObj));
            }
        }
    }

    private void OnButtonClicked(GameObject buttonObj)
    {
        if (buttonToCharacterID.TryGetValue(buttonObj, out int characterID))
        {
            SelectCharacter(characterID);
        }
    }

    private void LoadSelectedCharacter()
    {
        if (GameDataManager.Instance == null) return;

        int savedID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;

        // Check if saved character is unlockedByDefault
        foreach (var character in characterDatabase.characters)
        {
            if (character.characterID == savedID && character.unlockedByDefault)
            {
                SelectCharacter(savedID);
                return;
            }
        }

        // Select first available character
        foreach (var kvp in buttonToCharacterID)
        {
            SelectCharacter(kvp.Value);
            break;
        }
    }

    private void SelectCharacter(int characterID)
    {
        selectedCharacterID = characterID;

        // Update all button highlights
        foreach (var kvp in buttonToCharacterID)
        {
            GameObject highlight = kvp.Key.transform.Find("SelectedHighlight")?.gameObject;
            if (highlight != null)
            {
                highlight.SetActive(kvp.Value == characterID);
            }
        }

        // Apply visual change
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(characterID);
        }

        Debug.Log("Selected Character ID: " + characterID);
    }

    // Called when nickname input changes
    private void OnNicknameChanged(string newText)
    {
        ValidateNickname();
    }

    // Validates the nickname and updates UI
    private void ValidateNickname()
    {
        bool isValid = IsNicknameValid();

        // Update border color
        if (nicknameInputBorder != null)
        {
            nicknameInputBorder.color = isValid ? normalBorderColor : errorBorderColor;
        }

        // Update button interactability
        if (selectHeroButton != null)
        {
            selectHeroButton.interactable = isValid && selectedCharacterID != -1;
        }
    }

    // Checks if nickname is valid
    private bool IsNicknameValid()
    {
        if (nicknameInputField == null) return false;

        string nickname = nicknameInputField.text.Trim();
        return !string.IsNullOrEmpty(nickname) && nickname.Length > 0;
    }

    // Called when "Select Hero" button is clicked
    private void OnSelectHeroClicked()
    {
        // Validate nickname one more time
        if (!IsNicknameValid())
        {
            // Show error effect
            StartCoroutine(ShakeInputField());
            return;
        }

        // Validate character is selected
        if (selectedCharacterID == -1)
        {
            Debug.LogWarning("Please select a character first!");
            return;
        }

        // Save everything to GameData
        SaveSelectionToGameData();

        // Start the timeline
        StartTimeline();
    }

    // Saves character selection and nickname to GameData
    private void SaveSelectionToGameData()
    {
        if (GameDataManager.Instance == null) return;

        // Save character selection
        GameDataManager.Instance.CurrentGameData.selectedCharacterID = selectedCharacterID;

        // Unlock character in GameData if not already
        if (!GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs.Contains(selectedCharacterID))
        {
            GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs.Add(selectedCharacterID);
        }

        // Save nickname
        if (nicknameInputField != null)
        {
            string nickname = nicknameInputField.text.Trim();
            GameDataManager.Instance.CurrentGameData.playerName = nickname;
        }

        // Save to disk
        GameDataManager.Instance.SaveGameData();

        Debug.Log($"Saved: Character={selectedCharacterID}, Nickname={GameDataManager.Instance.CurrentGameData.playerName}");
    }

    // Starts the timeline/cutscene
    private void StartTimeline()
    {
        // Hide character selection UI
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(false);
        }

        // Enable and play the timeline
        if (playableDirector != null)
        {
            playableDirector.enabled = true;
            playableDirector.Play();
            Debug.Log("Timeline started!");
        }
        else
        {
            Debug.LogError("Playable Director not assigned!");
        }
    }

    // Called when timeline finishes playing
    private void OnTimelineFinished(PlayableDirector director)
    {
        Debug.Log("Timeline finished!");

        // Here you can load the next scene or show the game UI
        // Example: SceneManager.LoadScene("GameScene");

        // Optional: Disable the director after it finishes
        if (playableDirector != null)
        {
            playableDirector.enabled = false;
        }
    }

    // Simple shake animation for error feedback
    private System.Collections.IEnumerator ShakeInputField()
    {
        if (nicknameInputField == null) yield break;

        Transform inputTransform = nicknameInputField.transform;
        Vector3 originalPosition = inputTransform.localPosition;
        float shakeDuration = 0.5f;
        float shakeMagnitude = 5f;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            inputTransform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        inputTransform.localPosition = originalPosition;
    }

    // Update validation when character is selected
    private void Update()
    {
        // Update validation in real-time
        if (selectedCharacterID != -1)
        {
            ValidateNickname();
        }
    }

    void OnDestroy()
    {
        // Clean up event listener
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineFinished;
        }
    }
}