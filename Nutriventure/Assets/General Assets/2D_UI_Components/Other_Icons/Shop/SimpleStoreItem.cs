using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleStoreItem : MonoBehaviour
{
    [Header("Item Type")]
    public bool isCharacter = true;
    public int characterID = -1;

    [Header("Audio")]
    public AudioClip clickSound;

    private SimpleStoreUI storeUI;
    private bool isVisible = true;

    void Start()
    {
        // Find store UI
        storeUI = SimpleStoreUI.Instance;

        if (storeUI == null)
        {
            Debug.LogError($"StoreUI not found for {gameObject.name}!");
        }

        // Add collider if needed
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }

        // Check if this item should be visible
        CheckVisibility();
    }

    private Camera cachedCamera;

    void Update()
    {
        // Determine if a press happened and get the input position
        bool pressed = false;
        Vector2 inputPos = Vector2.zero;

        // Check touch first (mobile), then mouse (editor/desktop)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pressed = true;
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed = true;
            inputPos = Mouse.current.position.ReadValue();
        }

        if (!pressed) return;

        if (cachedCamera == null) cachedCamera = Camera.main;
        if (cachedCamera == null) return;

        Ray ray = cachedCamera.ScreenPointToRay(inputPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.gameObject == this.gameObject && isVisible)
            {
                OnItemClicked();
            }
        }
    }

    void OnItemClicked()
    {
        Debug.Log($"CLICKED: {gameObject.name} (ID: {characterID})");

        // Play click sound using AudioHandler
        PlayClickSound();

        if (storeUI != null)
        {
            storeUI.ShowItemDetails(this);
        }
        else
        {
            Debug.LogError("StoreUI is null!");
        }
    }

    void PlayClickSound()
    {
        // Use custom click sound if provided
        if (clickSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(clickSound);
        }
        // Otherwise use default button click sound
        else if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    public void CheckVisibility()
    {
        // Check if character is already unlocked
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            bool isUnlocked = GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs.Contains(characterID);

            // Also check if unlocked by default in database
            CharacterDatabase.CharacterData charData = storeUI?.characterDatabase?.GetCharacterByID(characterID);
            if (charData != null && charData.unlockedByDefault)
            {
                isUnlocked = true;
            }

            isVisible = !isUnlocked;
            gameObject.SetActive(isVisible);

            Debug.Log($"Item {characterID} visibility: {isVisible} (Unlocked: {isUnlocked})");
        }
    }

    public void HideItem()
    {
        Debug.Log($"Hiding item {characterID}");
        gameObject.SetActive(false);
        isVisible = false;
    }

    public void RefreshVisibility()
    {
        CheckVisibility();
    }
}