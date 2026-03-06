using UnityEngine;
using System.Collections;

public class CharacterVisualSwapper : MonoBehaviour
{
    [Header("References")]
    public Transform geometryRoot;
    public Animator playerAnimator;
    public CharacterDatabase characterDatabase;

    [Header("Animation Parameters")]
    public string lookAroundParameter = "LookAround";

    [Header("Visual Settings")]
    public float initializationDelay = 0.2f;
    public float swapDelay = 0.1f;
    public float hideDuration = 0.05f;
    public float lookAroundDelay = 0.3f; // Delay before triggering LookAround

    private GameObject currentCharacterModel;
    private GameObject currentSkinModel;
    private Coroutine swapCoroutine;
    private Renderer[] currentRenderers;
    private int currentCharacterID = -1;
    private int currentSkinID = -1;

    void Start()
    {
        ForceEnableAnimator();
        Debug.Log($"CharacterVisualSwapper initialized - CharacterDB: {characterDatabase != null}");
    }

    // METHOD 1: Takes CharacterData (for CharacterSelectionPanel) - WITH ANIMATION
    public void ApplyCharacterVisuals(CharacterDatabase.CharacterData characterData)
    {
        if (characterData == null) return;

        currentCharacterID = characterData.characterID;
        Debug.Log($"Applying character visuals for: {characterData.characterName} (ID: {characterData.characterID})");

        if (swapCoroutine != null)
            StopCoroutine(swapCoroutine);

        swapCoroutine = StartCoroutine(SwapCharacterAndTriggerLookAround(characterData));
    }

    // METHOD 2: Takes characterID (for CharacterSelectionManager) - WITH ANIMATION
    public void ApplyCharacterVisuals(int characterID)
    {
        if (characterDatabase == null)
        {
            Debug.LogError("CharacterDatabase not assigned!");
            return;
        }

        CharacterDatabase.CharacterData characterData = characterDatabase.GetCharacterByID(characterID);
        if (characterData == null)
        {
            Debug.LogError($"Character {characterID} not found!");
            return;
        }

        // Call the existing method
        ApplyCharacterVisuals(characterData);
    }

    private IEnumerator SwapCharacterAndTriggerLookAround(CharacterDatabase.CharacterData characterData)
    {
        Debug.Log($"SwapCharacterAndTriggerLookAround: {characterData.characterName}");

        // PHASE 1: HIDE CURRENT
        HideCurrentCharacterImmediately();

        // PHASE 2: CLEAR EXISTING
        ClearExistingModels();

        yield return new WaitForEndOfFrame();

        // PHASE 3: APPLY AVATAR
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
            playerAnimator.avatar = characterData.characterAvatar;
            Debug.Log("Applied Character Avatar: " + characterData.characterAvatar.name);
        }

        // PHASE 4: INSTANTIATE MODEL
        if (characterData.characterPrefab != null && geometryRoot != null)
        {
            currentCharacterModel = Instantiate(characterData.characterPrefab, geometryRoot);
            SetupModelTransform(currentCharacterModel);
            DisableCharacterComponents(currentCharacterModel);
            currentRenderers = currentCharacterModel.GetComponentsInChildren<Renderer>(true);
            HideAllRenderers(currentRenderers);
        }

        yield return new WaitForEndOfFrame();

        // PHASE 5: RE-ENABLE ANIMATOR
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }

        // PHASE 6: WAIT FOR INITIALIZATION
        yield return new WaitForSeconds(initializationDelay);

        // PHASE 7: SMALL DELAY
        yield return new WaitForSeconds(hideDuration);

        // PHASE 8: SHOW CHARACTER
        ShowCurrentCharacter();

        // PHASE 9: DELAY BEFORE ANIMATION
        yield return new WaitForSeconds(swapDelay);

        // PHASE 10: TRIGGER LOOKAROUND ANIMATION
        yield return StartCoroutine(TriggerLookAroundAfterLoad());

        Debug.Log("Character swap completed! LookAround animation triggered");
        swapCoroutine = null;
    }

    // Load character with saved skin - WITH ANIMATION
    public void LoadCharacterWithSavedSkin(int characterID)
    {
        int savedSkinID = -1;

        if (GameDataManager.Instance != null)
        {
            savedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(characterID);
            Debug.Log($"LoadCharacterWithSavedSkin: CharID={characterID}, SavedSkinID={savedSkinID}");
        }

        // Get character data
        if (characterDatabase != null)
        {
            var characterData = characterDatabase.GetCharacterByID(characterID);
            if (characterData != null)
            {
                if (savedSkinID != -1)
                {
                    // Apply character with saved skin (includes LookAround animation)
                    var skinData = characterDatabase.GetSkinByID(characterID, savedSkinID);
                    if (skinData != null)
                    {
                        currentCharacterID = characterID;
                        currentSkinID = savedSkinID;
                        if (swapCoroutine != null) StopCoroutine(swapCoroutine);
                        swapCoroutine = StartCoroutine(ApplySkinAndTriggerLookAround(characterData, skinData));
                    }
                    else
                    {
                        // Skin not found, fallback to base character
                        ApplyCharacterVisuals(characterData);
                    }
                }
                else
                {
                    // No skin saved, apply base character
                    ApplyCharacterVisuals(characterData);
                }
            }
        }
    }

    // NEW: Load character with saved skin - WITHOUT ANIMATION (for game start)
    public void LoadCharacterWithSavedSkinNoAnimation(int characterID)
    {
        int savedSkinID = -1;

        if (GameDataManager.Instance != null)
        {
            savedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(characterID);
            Debug.Log($"LoadCharacterWithSavedSkinNoAnimation: CharID={characterID}, SavedSkinID={savedSkinID}");
        }

        // Get character data
        if (characterDatabase != null)
        {
            var characterData = characterDatabase.GetCharacterByID(characterID);
            if (characterData != null)
            {
                // Load character WITHOUT triggering LookAround
                StartCoroutine(LoadCharacterWithoutAnimation(characterData, savedSkinID));
            }
        }
    }

    private IEnumerator LoadCharacterWithoutAnimation(CharacterDatabase.CharacterData characterData, int skinID = -1)
    {
        Debug.Log($"LoadCharacterWithoutAnimation: {characterData.characterName}, SkinID={skinID}");

        // Store current character ID
        currentCharacterID = characterData.characterID;
        currentSkinID = skinID;

        // Clear existing models
        ClearExistingModels();

        yield return new WaitForEndOfFrame();

        // Apply avatar
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;

            // Handle skin avatar if needed
            if (skinID != -1)
            {
                var skinData = characterDatabase.GetSkinByID(characterData.characterID, skinID);
                if (skinData != null && skinData.skinAvatar != null)
                {
                    playerAnimator.avatar = skinData.skinAvatar;
                }
                else
                {
                    playerAnimator.avatar = characterData.characterAvatar;
                }
            }
            else
            {
                playerAnimator.avatar = characterData.characterAvatar;
            }
        }

        // Instantiate model
        GameObject modelToUse = characterData.characterPrefab;

        // Check if we should use skin model
        if (skinID != -1)
        {
            var skinData = characterDatabase.GetSkinByID(characterData.characterID, skinID);
            if (skinData != null && skinData.skinPrefab != null)
            {
                modelToUse = skinData.skinPrefab;
            }
        }

        if (modelToUse != null && geometryRoot != null)
        {
            if (skinID != -1)
            {
                currentSkinModel = Instantiate(modelToUse, geometryRoot);
                SetupModelTransform(currentSkinModel);
                DisableCharacterComponents(currentSkinModel);
                currentRenderers = currentSkinModel.GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                currentCharacterModel = Instantiate(modelToUse, geometryRoot);
                SetupModelTransform(currentCharacterModel);
                DisableCharacterComponents(currentCharacterModel);
                currentRenderers = currentCharacterModel.GetComponentsInChildren<Renderer>(true);
            }
        }

        yield return new WaitForEndOfFrame();

        // Re-enable animator
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }

        // Make sure LookAround is OFF
        if (playerAnimator != null && !string.IsNullOrEmpty(lookAroundParameter))
        {
            playerAnimator.SetBool(lookAroundParameter, false);
            playerAnimator.Update(0f);
        }

        Debug.Log($"Character loaded without animation: {characterData.characterName}");
    }

    // Skin application method
    public void ApplySkinToCurrentCharacter(int skinID)
    {
        Debug.Log($"ApplySkinToCurrentCharacter: SkinID={skinID}, CurrentCharacterID={currentCharacterID}");

        // Check if we're already using this skin
        if (currentSkinID == skinID)
        {
            Debug.Log($"Skin {skinID} is already applied, skipping reload");
            return;
        }

        if (currentCharacterID == -1)
        {
            Debug.LogError("No character loaded!");
            return;
        }

        if (characterDatabase == null)
        {
            Debug.LogError("CharacterDatabase not assigned!");
            return;
        }

        // Get character data
        var characterData = characterDatabase.GetCharacterByID(currentCharacterID);
        if (characterData != null)
        {
            // If skinID is -1, just reload character without skin
            if (skinID == -1)
            {
                Debug.Log("Applying default character (no skin)");
                ApplyCharacterVisuals(characterData);
                return;
            }

            // Check if skin exists
            var skinData = characterDatabase.GetSkinByID(currentCharacterID, skinID);
            if (skinData != null)
            {
                Debug.Log($"Applying skin: {skinData.skinName}");
                currentSkinID = skinID;

                // Apply skin - this will trigger LookAround animation
                StartCoroutine(ApplySkinAndTriggerLookAround(characterData, skinData));
            }
        }
    }

    private IEnumerator ApplySkinAndTriggerLookAround(CharacterDatabase.CharacterData characterData, CharacterDatabase.SkinData skinData)
    {
        Debug.Log($"Applying skin: {skinData.skinName}");

        // Hide current character
        HideCurrentCharacterImmediately();
        ClearExistingModels();

        yield return new WaitForEndOfFrame();

        // Apply avatar
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
            playerAnimator.avatar = skinData.skinAvatar != null ? skinData.skinAvatar : characterData.characterAvatar;
            Debug.Log("Applied skin avatar");
        }

        // Instantiate skin model
        if (skinData.skinPrefab != null && geometryRoot != null)
        {
            currentSkinModel = Instantiate(skinData.skinPrefab, geometryRoot);
            SetupModelTransform(currentSkinModel);
            DisableCharacterComponents(currentSkinModel);
            currentRenderers = currentSkinModel.GetComponentsInChildren<Renderer>(true);
            HideAllRenderers(currentRenderers);
        }

        yield return new WaitForEndOfFrame();

        // Re-enable animator
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }

        // Wait and show character
        yield return new WaitForSeconds(initializationDelay + hideDuration);
        ShowCurrentCharacter();

        // Trigger LookAround animation
        yield return new WaitForSeconds(lookAroundDelay);
        TriggerLookAroundAnimation();

        Debug.Log($"Skin applied and LookAround animation triggered for {skinData.skinName}");
    }

    // Trigger LookAround animation
    public void TriggerLookAroundAnimation()
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(lookAroundParameter) && playerAnimator.enabled)
        {
            StartCoroutine(TriggerLookAroundSmoothly());
        }
    }

    private IEnumerator TriggerLookAroundAfterLoad()
    {
        yield return new WaitForSeconds(lookAroundDelay);
        TriggerLookAroundAnimation();
    }

    private IEnumerator TriggerLookAroundSmoothly()
    {
        yield return new WaitForEndOfFrame();

        if (playerAnimator != null && playerAnimator.enabled)
        {
            // Reset animation state
            playerAnimator.Rebind();
            playerAnimator.Update(0f);

            // Set LookAround parameter to false first
            playerAnimator.SetBool(lookAroundParameter, false);
            playerAnimator.Update(0f);

            yield return new WaitForEndOfFrame();

            // Set LookAround parameter to true to trigger animation
            playerAnimator.SetBool(lookAroundParameter, true);
            playerAnimator.Update(0.1f);

            Debug.Log($"LookAround animation triggered! Parameter '{lookAroundParameter}' set to TRUE");
        }
    }

    // Stop LookAround animation (set bool to false)
    public void StopLookAroundAnimation()
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(lookAroundParameter))
        {
            playerAnimator.SetBool(lookAroundParameter, false);
            playerAnimator.Update(0f);
            Debug.Log($"LookAround animation stopped! Parameter '{lookAroundParameter}' set to FALSE");
        }
    }

    // Utility methods
    private void SetupModelTransform(GameObject model)
    {
        if (model == null) return;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
    }

    private void ClearExistingModels()
    {
        if (currentCharacterModel != null)
        {
            Destroy(currentCharacterModel);
            currentCharacterModel = null;
        }

        if (currentSkinModel != null)
        {
            Destroy(currentSkinModel);
            currentSkinModel = null;
        }

        currentRenderers = null;
    }

    private void HideCurrentCharacterImmediately()
    {
        if (currentRenderers != null)
        {
            foreach (var renderer in currentRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
    }

    private void HideAllRenderers(Renderer[] renderers)
    {
        if (renderers != null)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
    }

    private void ShowCurrentCharacter()
    {
        if (currentRenderers != null)
        {
            foreach (var renderer in currentRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }
    }

    private void ForceEnableAnimator()
    {
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.Update(0f);
        }
    }

    private void DisableCharacterComponents(GameObject characterModel)
    {
        Animator animator = characterModel.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        MonoBehaviour[] scripts = characterModel.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != null && script.enabled)
            {
                if (script.GetType().Name.Contains("Controller") ||
                    script.GetType().Name.Contains("Movement") ||
                    script.GetType().Name.Contains("Input") ||
                    script.GetType().Name.Contains("Camera") ||
                    script.GetType().Name.Contains("StarterAssets"))
                {
                    script.enabled = false;
                }
            }
        }
    }

    public void ClearCharacterVisuals()
    {
        ClearExistingModels();
    }

    public void EnsureAnimatorEnabled()
    {
        ForceEnableAnimator();
    }

    void OnDestroy()
    {
        if (swapCoroutine != null)
            StopCoroutine(swapCoroutine);
    }

    // Getters
    public int GetCurrentCharacterID() => currentCharacterID;
    public int GetCurrentSkinID() => currentSkinID;
}