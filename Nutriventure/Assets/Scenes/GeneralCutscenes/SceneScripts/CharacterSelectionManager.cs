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

    [Header("Audio References")]
    public AudioSource audioSource; // Reference to an AudioSource component
    public AudioSource backgroundMusicSource; // Reference to background music AudioSource
    public AudioClip buttonClickSound; // Sound for character button clicks
    public AudioClip confirmClickSound; // Sound for confirm/select hero button
    public AudioClip errorSound; // Sound when trying to proceed without nickname

    [Header("Validation Colors")]
    public Color normalBorderColor = Color.white;
    public Color errorBorderColor = Color.red;

    [Header("Blink Settings")]
    public float blinkDuration = 1f; // Duration of the blink effect
    public float blinkSpeed = 0.2f; // Speed of each blink
    private bool isBlinking = false;
    private Coroutine blinkCoroutine;

    private Dictionary<GameObject, int> buttonToCharacterID = new Dictionary<GameObject, int>();
    private int selectedCharacterID = -1;

    void Start()
    {
        // Initialize audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }

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

            // Set initial border color to normal (not error)
            if (nicknameInputBorder != null)
            {
                nicknameInputBorder.color = normalBorderColor;
            }
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

        // Update button visual state initially
        UpdateButtonVisualState();
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
            PlayButtonClickSound(); // Play sound when character button is clicked
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

        // Update button visual state
        UpdateButtonVisualState();
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

    // Called when nickname input changes
    private void OnNicknameChanged(string newText)
    {
        UpdateButtonVisualState();

        // Only validate and update border color if not currently blinking
        if (!isBlinking && nicknameInputBorder != null)
        {
            nicknameInputBorder.color = normalBorderColor;
        }
    }

    // Checks if nickname is valid
    private bool IsNicknameValid()
    {
        if (nicknameInputField == null) return false;

        string nickname = nicknameInputField.text.Trim();
        return !string.IsNullOrEmpty(nickname) && nickname.Length > 0;
    }

    // Updates the button's visual state without affecting clickability
    private void UpdateButtonVisualState()
    {
        if (selectHeroButton == null) return;

        // Check if conditions are met
        bool isReady = IsNicknameValid() && selectedCharacterID != -1;

        // Get the Image component for color changes
        Image buttonImage = selectHeroButton.GetComponent<Image>();
        TMP_Text buttonText = selectHeroButton.GetComponentInChildren<TMP_Text>();

        if (buttonImage != null)
        {
            // Change alpha to indicate state while keeping button clickable
            Color currentColor = buttonImage.color;
            currentColor.a = isReady ? 1f : 0.5f; // Dim when not ready
            buttonImage.color = currentColor;
        }

        if (buttonText != null)
        {
            // Also adjust text alpha
            Color textColor = buttonText.color;
            textColor.a = isReady ? 1f : 0.5f;
            buttonText.color = textColor;
        }
    }

    // Called when "Select Hero" button is clicked
    private void OnSelectHeroClicked()
    {
        // Play button click sound regardless
        PlayButtonClickSound();

        // Validate nickname
        if (!IsNicknameValid())
        {
            // Play error sound
            PlayErrorSound();

            // Start blinking effect
            StartBlinkEffect();
            return;
        }

        // Validate character is selected
        if (selectedCharacterID == -1)
        {
            Debug.LogWarning("Please select a character first!");
            // Play error sound for missing character selection too
            PlayErrorSound();
            return;
        }

        // Play confirm sound
        PlayConfirmSound();

        // Disable background music if assigned
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Stop();
            Debug.Log("Background music stopped");
        }

        // Save everything to GameData
        SaveSelectionToGameData();

        // Start the timeline
        StartTimeline();
    }

    // Starts the blinking effect on the input border
    private void StartBlinkEffect()
    {
        if (isBlinking) return;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        blinkCoroutine = StartCoroutine(BlinkBorder());
    }

    private System.Collections.IEnumerator BlinkBorder()
    {
        isBlinking = true;
        float elapsedTime = 0f;
        Image border = nicknameInputBorder;

        if (border == null)
        {
            isBlinking = false;
            yield break;
        }

        // Start with normal color
        border.color = normalBorderColor;

        while (elapsedTime < blinkDuration)
        {
            // Blink between normal color and error color
            float t = Mathf.PingPong(elapsedTime * (1f / blinkSpeed), 1f);
            border.color = Color.Lerp(normalBorderColor, errorBorderColor, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Always return to normal color after blinking
        border.color = normalBorderColor;
        isBlinking = false;
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

        // Mark profile as completed so LogoManager knows this isn't a first-time player
        PlayerPrefs.SetInt("ProfileCompleted", 1);
        PlayerPrefs.Save();

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

    // Audio Methods
    private void PlayButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    private void PlayConfirmSound()
    {
        if (audioSource != null && confirmClickSound != null)
        {
            audioSource.PlayOneShot(confirmClickSound);
        }
    }

    private void PlayErrorSound()
    {
        if (audioSource != null && errorSound != null)
        {
            audioSource.PlayOneShot(errorSound);
        }
        else
        {
            // Fallback: play button click sound if no error sound is assigned
            PlayButtonClickSound();
        }
    }

    void OnDestroy()
    {
        // Clean up event listener
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineFinished;
        }

        // Stop any running coroutines
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
    }
}