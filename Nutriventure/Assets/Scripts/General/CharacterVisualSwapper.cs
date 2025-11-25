using UnityEngine;
using System.Collections;

public class CharacterVisualSwapper : MonoBehaviour
{
    [Header("References")]
    public Transform geometryRoot; // Assign your "Geometry" transform in inspector
    public Animator playerAnimator; // Assign your PlayerArmature's Animator in inspector

    [Header("Animation Parameters")]
    public string lookAroundParameter = "LookAround"; // Name of the bool parameter in Animator

    [Header("Visual Settings")]
    public float initializationDelay = 0.2f; // Delay before showing character to ensure proper setup
    public float swapDelay = 0.1f; // Delay to prevent T-pose flash

    private GameObject currentCharacterModel;
    private Coroutine swapCoroutine;
    private Renderer[] currentRenderers; // Changed to Renderer to catch all types

    void Start()
    {
        ForceEnableAnimator();
    }

    public void ApplyCharacterVisuals(CharacterDatabase.CharacterData characterData)
    {
        if (characterData == null) return;

        if (swapCoroutine != null)
            StopCoroutine(swapCoroutine);

        swapCoroutine = StartCoroutine(SwapCharacterVisualsCoroutine(characterData));
    }

    private IEnumerator SwapCharacterVisualsCoroutine(CharacterDatabase.CharacterData characterData)
    {
        Debug.Log("Starting character swap with initial hiding...");

        // PHASE 1: HIDE CURRENT CHARACTER (if exists)
        HideCurrentCharacter();

        // PHASE 2: CLEAR EXISTING MODEL
        if (currentCharacterModel != null)
        {
            Destroy(currentCharacterModel);
            currentCharacterModel = null;
            currentRenderers = null;
        }

        yield return new WaitForEndOfFrame(); // Ensure cleanup

        // PHASE 3: APPLY AVATAR FIRST
        Debug.Log("Applying avatar first...");
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
            playerAnimator.avatar = characterData.characterAvatar;
        }

        // PHASE 4: INSTANTIATE NEW MODEL BUT KEEP IT HIDDEN
        Debug.Log("Instantiating new model (hidden)...");
        if (characterData.characterPrefab != null && geometryRoot != null)
        {
            currentCharacterModel = Instantiate(characterData.characterPrefab, geometryRoot);
            currentCharacterModel.transform.localPosition = Vector3.zero;
            currentCharacterModel.transform.localRotation = Quaternion.identity;
            currentCharacterModel.transform.localScale = Vector3.one;

            DisableCharacterComponents(currentCharacterModel);

            // Get ALL renderers (SkinnedMeshRenderer and MeshRenderer)
            currentRenderers = currentCharacterModel.GetComponentsInChildren<Renderer>(true);

            // IMPORTANT: Keep the character hidden initially
            HideCurrentCharacterImmediately();
        }

        yield return new WaitForEndOfFrame(); // Ensure instantiation completes

        // PHASE 5: RE-ENABLE ANIMATOR WITH NEW AVATAR
        Debug.Log("Re-enabling animator with new avatar...");
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }

        // PHASE 6: WAIT FOR INITIALIZATION - CRITICAL STEP!
        Debug.Log($"Waiting {initializationDelay}s for proper initialization...");
        yield return new WaitForSeconds(initializationDelay);

        // PHASE 7: NOW SHOW THE CHARACTER (everything should be initialized)
        Debug.Log("Showing character after initialization...");
        ShowCurrentCharacter();

        // PHASE 8: SMALL DELAY BEFORE ANIMATION
        yield return new WaitForSeconds(swapDelay);

        // PHASE 9: TRIGGER ANIMATION
        Debug.Log("Triggering LookAround animation...");
        TriggerLookAroundAnimation();

        Debug.Log("Character swap completed successfully!");
        swapCoroutine = null;
    }

    private void HideCurrentCharacter()
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

    private void HideCurrentCharacterImmediately()
    {
        if (currentCharacterModel != null)
        {
            // Disable ALL renderers in the new character
            Renderer[] allRenderers = currentCharacterModel.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in allRenderers)
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
        else if (currentCharacterModel != null)
        {
            // Fallback: if currentRenderers is null, find them again
            currentRenderers = currentCharacterModel.GetComponentsInChildren<Renderer>(true);
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
            // Ensure animator is in a clean state
            playerAnimator.Rebind();
            playerAnimator.Update(0f);

            // Reset parameter
            playerAnimator.SetBool(lookAroundParameter, false);
            playerAnimator.Update(0f);

            yield return new WaitForEndOfFrame();

            // Trigger animation
            playerAnimator.SetBool(lookAroundParameter, true);
            playerAnimator.Update(0.1f);

            Debug.Log("LookAround animation triggered after proper initialization");
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
        // Disable animator in the instantiated model
        Animator animator = characterModel.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        // Disable any movement/input scripts
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
        if (currentCharacterModel != null)
        {
            Destroy(currentCharacterModel);
            currentCharacterModel = null;
            currentRenderers = null;
        }
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
            Debug.Log("LookAround animation stopped - parameter set to false");
        }
    }
}