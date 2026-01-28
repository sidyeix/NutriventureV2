using StarterAssets;
using UnityEngine;

public class MechanicsBoardManager : MonoBehaviour
{
    [Header("References")]
    public GameObject mechanicsBoard;
    public AutoUISlider autoUISlider;

    [Header("Game Manager Reference")]
    public GoGrowGlowGameManager gameManager;

    [Header("Settings")]
    public bool hideOnStart = true;

    private int targetSlideIndex = 0;
    private bool wasEnergyPaused = false;
    private bool wasTimerPaused = false;
    private bool isPaused = false;

    void Start()
    {
        if (hideOnStart && mechanicsBoard != null)
        {
            mechanicsBoard.SetActive(false);
        }

        if (autoUISlider == null && mechanicsBoard != null)
        {
            autoUISlider = mechanicsBoard.GetComponent<AutoUISlider>();
        }

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GoGrowGlowGameManager>();
        }
    }

    public void SetTargetSlide(int slideIndex)
    {
        if (slideIndex >= 0)
        {
            targetSlideIndex = slideIndex;
            Debug.Log($"Target slide set to: {targetSlideIndex}");
        }
    }

    public void OpenMechanicsBoard()
    {
        if (mechanicsBoard == null) return;

        // Save current state before pausing
        if (gameManager != null && gameManager.IsGameActive())
        {
            wasEnergyPaused = gameManager.IsEnergyDecreasePaused();
            wasTimerPaused = gameManager.IsGameTimerPaused();

            // Pause both energy decrease and game timer
            gameManager.PauseEnergyDecrease();
            gameManager.PauseGameTimer();

            // Optional: Pause player movement
            PausePlayerMovement(true);

            // Mark as paused
            isPaused = true;
        }

        mechanicsBoard.SetActive(true);

        if (autoUISlider != null)
        {
            autoUISlider.JumpToSlide(targetSlideIndex);
        }
    }

    public void CloseMechanicsBoard()
    {
        if (mechanicsBoard != null)
        {
            mechanicsBoard.SetActive(false);
        }

        // Resume if we were paused
        if (gameManager != null && isPaused)
        {
            ResumeGame();
        }
    }

    private void ResumeGame()
    {
        // Resume timer if it wasn't paused before
        if (!wasTimerPaused)
        {
            gameManager.ResumeGameTimer();
        }

        // Resume energy if it wasn't paused before
        if (!wasEnergyPaused)
        {
            gameManager.ResumeEnergyDecrease();
        }

        // Resume player movement
        PausePlayerMovement(false);

        isPaused = false;
        Debug.Log("Game resumed - Timer and energy resumed");
    }

    public void ToggleMechanicsBoard()
    {
        if (mechanicsBoard == null) return;

        if (mechanicsBoard.activeSelf)
        {
            CloseMechanicsBoard();
        }
        else
        {
            OpenMechanicsBoard();
        }
    }

    public void ResetToFirstSlide()
    {
        SetTargetSlide(0);
    }

    // Optional: Pause/Resume player movement
    public void PausePlayerMovement(bool pause)
    {
        ThirdPersonController playerController = gameManager.GetComponent<ThirdPersonController>();
        if (playerController != null)
        {
            playerController.enabled = !pause;
        }
    }
}