using UnityEngine;

public class CharacterVisualSwapper : MonoBehaviour
{
    [Header("References")]
    public Transform geometryRoot; // Assign your "Geometry" transform in inspector
    public Animator playerAnimator; // Assign your PlayerArmature's Animator in inspector

    private GameObject currentCharacterModel;

    public void ApplyCharacterVisuals(CharacterDatabase.CharacterData characterData)
    {
        if (characterData == null)
        {
            Debug.LogError("Character data is null!");
            return;
        }

        // Clear existing character model
        if (currentCharacterModel != null)
        {
            Destroy(currentCharacterModel);
        }

        // IMPORTANT: Reset animator before applying new avatar
        if (playerAnimator != null)
        {
            // Disable the animator temporarily
            playerAnimator.enabled = false;

            // Apply the avatar
            if (characterData.characterAvatar != null)
            {
                playerAnimator.avatar = characterData.characterAvatar;
                Debug.Log($"Applied avatar for: {characterData.characterName}");
            }
            else
            {
                Debug.LogWarning($"No avatar found for: {characterData.characterName}");
            }

            // Re-enable the animator
            playerAnimator.enabled = true;

            // Force the animator to update immediately
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }

        // Instantiate the character prefab as child of geometry
        if (characterData.characterPrefab != null && geometryRoot != null)
        {
            currentCharacterModel = Instantiate(characterData.characterPrefab, geometryRoot);
            currentCharacterModel.transform.localPosition = Vector3.zero;
            currentCharacterModel.transform.localRotation = Quaternion.identity;
            currentCharacterModel.transform.localScale = Vector3.one;

            // Disable any components in the instantiated prefab to avoid conflicts
            DisableCharacterComponents(currentCharacterModel);

            Debug.Log($"Applied mesh for: {characterData.characterName}");

            // Force the character to play default animation
            PlayDefaultAnimation();
        }
        else
        {
            Debug.LogError($"Character prefab or geometry root is null for: {characterData.characterName}");
        }
    }

    private void DisableCharacterComponents(GameObject characterModel)
    {
        // Disable animator (we're using the PlayerArmature's animator)
        Animator animator = characterModel.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Disable any controller scripts that might interfere
        MonoBehaviour[] scripts = characterModel.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script.enabled)
            {
                if (script.GetType().Name.Contains("Controller") ||
                    script.GetType().Name.Contains("Movement") ||
                    script.GetType().Name.Contains("Input") ||
                    script.GetType().Name.Contains("Camera"))
                {
                    script.enabled = false;
                }
            }
        }
    }

    private void PlayDefaultAnimation()
    {
        if (playerAnimator != null)
        {
            // Make sure the animator is enabled and has an avatar
            if (playerAnimator.avatar != null && playerAnimator.avatar.isValid)
            {
                // Reset to entry state and play
                playerAnimator.Rebind();

                // Try to play the default animation (usually "Idle" or "Entry")
                if (playerAnimator.HasState(0, Animator.StringToHash("Idle")))
                {
                    playerAnimator.Play("Idle", 0, 0f);
                }
                else if (playerAnimator.HasState(0, Animator.StringToHash("Entry")))
                {
                    playerAnimator.Play("Entry", 0, 0f);
                }
                else
                {
                    // Play the first state available
                    playerAnimator.Play(0, 0, 0f);
                }

                // Force immediate update
                playerAnimator.Update(0.1f);

                Debug.Log("Default animation playing");
            }
            else
            {
                Debug.LogWarning("Animator avatar is null or invalid - animations may not play correctly");
            }
        }
    }

    // Call this when you want to clear the character visuals
    public void ClearCharacterVisuals()
    {
        if (currentCharacterModel != null)
        {
            Destroy(currentCharacterModel);
            currentCharacterModel = null;
        }

        if (playerAnimator != null)
        {
            playerAnimator.avatar = null;
        }
    }

    // Optional: Method to force animation refresh
    public void RefreshAnimator()
    {
        if (playerAnimator != null)
        {
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
            PlayDefaultAnimation();
        }
    }
}