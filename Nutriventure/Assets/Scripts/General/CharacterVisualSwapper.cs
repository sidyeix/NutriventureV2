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

    // OLD METHOD - For backward compatibility
    public void ApplyCharacterVisuals(CharacterDatabase.CharacterData characterData)
    {
        if (characterData == null) return;

        ApplyCharacterVisuals(characterData.characterID, -1);
    }

    // NEW METHOD - Apply character with optional skin
    public void ApplyCharacterVisuals(int characterID, int skinID = -1)
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

        currentCharacterID = characterID;
        currentSkinID = skinID;
        Debug.Log($"ApplyCharacterVisuals: CharID={characterID}, SkinID={skinID}");

        if (swapCoroutine != null)
            StopCoroutine(swapCoroutine);

        swapCoroutine = StartCoroutine(SwapCharacterVisualsCoroutine(characterData, skinID));
    }

    private IEnumerator SwapCharacterVisualsCoroutine(CharacterDatabase.CharacterData characterData, int skinID = -1)
    {
        Debug.Log($"SwapCoroutine: {characterData.characterName}, SkinID={skinID}");

        // PHASE 1: HIDE CURRENT
        HideCurrentCharacterImmediately();

        // PHASE 2: CLEAR EXISTING
        ClearExistingModels();

        yield return new WaitForEndOfFrame();

        // PHASE 3: APPLY AVATAR
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;

            // If skin is used and contains its own avatar
            if (skinID != -1)
            {
                var skinData = characterDatabase.GetSkinByID(characterData.characterID, skinID);
                if (skinData != null && skinData.skinAvatar != null)
                {
                    playerAnimator.avatar = skinData.skinAvatar;
                    Debug.Log("Applied Skin Avatar: " + skinData.skinName);
                }
                else
                {
                    playerAnimator.avatar = characterData.characterAvatar;
                    Debug.Log("Skin has NO avatar — using character avatar");
                }
            }
            else
            {
                playerAnimator.avatar = characterData.characterAvatar;
                Debug.Log("Applied Character Avatar");
            }
        }


        bool shouldUseSkin = (skinID != -1);
        GameObject modelToUse = null;

        // PHASE 4: DECIDE WHICH MODEL TO USE
        if (shouldUseSkin)
        {
            // Try to use skin prefab
            var skinData = characterDatabase.GetSkinByID(characterData.characterID, skinID);
            if (skinData != null && skinData.skinPrefab != null)
            {
                modelToUse = skinData.skinPrefab;
                Debug.Log($"Using skin: {skinData.skinName}");
                currentSkinID = skinID;
            }
            else
            {
                Debug.LogWarning($"Skin {skinID} not found, using default character model");
                shouldUseSkin = false;
            }
        }

        if (!shouldUseSkin)
        {
            // Use character's default prefab
            modelToUse = characterData.characterPrefab;
            currentSkinID = -1;
            Debug.Log("Using default character model");
        }

        // PHASE 5: INSTANTIATE MODEL
        if (modelToUse != null && geometryRoot != null)
        {
            if (shouldUseSkin)
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

            HideAllRenderers(currentRenderers);
        }

        yield return new WaitForEndOfFrame();

        // PHASE 6: RE-ENABLE ANIMATOR
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }

        // PHASE 7: WAIT FOR INITIALIZATION
        yield return new WaitForSeconds(initializationDelay);

        // PHASE 8: SMALL DELAY
        yield return new WaitForSeconds(hideDuration);

        // PHASE 9: SHOW CHARACTER
        ShowCurrentCharacter();

        // PHASE 10: DELAY BEFORE ANIMATION
        yield return new WaitForSeconds(swapDelay);

        // PHASE 11: TRIGGER ANIMATION
        TriggerLookAroundAnimation();

        Debug.Log("Character swap completed!");
        swapCoroutine = null;
    }

    // In CharacterVisualSwapper.cs - Optimize ApplySkinToCurrentCharacter
    public void ApplySkinToCurrentCharacter(int skinID)
    {
        Debug.Log($"ApplySkinToCurrentCharacter: SkinID={skinID}, CurrentSkinID={currentSkinID}");

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

        // If skinID is -1, just reload character without skin
        if (skinID == -1)
        {
            Debug.Log("Applying default character (no skin)");
            ApplyCharacterVisuals(currentCharacterID, -1);
            return;
        }

        // Check if skin exists
        var skinData = characterDatabase.GetSkinByID(currentCharacterID, skinID);
        if (skinData == null)
        {
            Debug.LogError($"Skin {skinID} not found for character {currentCharacterID}!");
            return;
        }

        if (skinData.skinPrefab == null)
        {
            Debug.LogError($"Skin prefab is null for skin {skinData.skinName}!");
            return;
        }

        Debug.Log($"Applying skin: {skinData.skinName}");
        currentSkinID = skinID;

        // Use the main swap method
        ApplyCharacterVisuals(currentCharacterID, skinID);
    }

    // Load saved skin for character
    public void LoadCharacterWithSavedSkin(int characterID)
    {
        int savedSkinID = -1;

        if (GameDataManager.Instance != null)
        {
            savedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(characterID);
            Debug.Log($"LoadCharacterWithSavedSkin: CharID={characterID}, SavedSkinID={savedSkinID}");
        }

        // Apply character with saved skin (or default if -1)
        ApplyCharacterVisuals(characterID, savedSkinID);
    }

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

    public void TriggerLookAroundAnimation()
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(lookAroundParameter) && playerAnimator.enabled)
        {
            StartCoroutine(TriggerLookAroundSmoothly());
        }
    }

    private IEnumerator TriggerLookAroundSmoothly()
    {
        yield return new WaitForEndOfFrame();

        if (playerAnimator != null && playerAnimator.enabled)
        {
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
            playerAnimator.SetBool(lookAroundParameter, false);
            playerAnimator.Update(0f);

            yield return new WaitForEndOfFrame();

            playerAnimator.SetBool(lookAroundParameter, true);
            playerAnimator.Update(0.1f);
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

    public void StopLookAroundAnimation()
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(lookAroundParameter))
        {
            playerAnimator.SetBool(lookAroundParameter, false);
            playerAnimator.Update(0f);
        }
    }

    // Getters
    public int GetCurrentCharacterID() => currentCharacterID;
    public int GetCurrentSkinID() => currentSkinID;
}