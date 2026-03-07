using UnityEngine;
using System.Collections;

public class K3_CompletionHandler : MonoBehaviour
{
    [Header("References")]
    public K3_KingAssessment kingAssessment;
    public K3_GameSummary gameSummary;
    public K3_KingCS2 kingCutscene;

    [Header("Settings")]
    public int requiredFoods = 8;

    private bool hasTriggered = false;
    private bool cutscenePlayed = false;

    void Start()
    {
        // Find references if not assigned
        if (kingAssessment == null)
            kingAssessment = FindObjectOfType<K3_KingAssessment>();

        if (gameSummary == null)
            gameSummary = FindObjectOfType<K3_GameSummary>();

        if (kingCutscene == null)
            kingCutscene = FindObjectOfType<K3_KingCS2>();

        // Check if cutscene GameObject is disabled and enable it
        if (kingCutscene != null && !kingCutscene.gameObject.activeSelf)
        {
            Debug.Log("K3 Completion Handler: Cutscene was disabled, enabling it...");
            kingCutscene.gameObject.SetActive(true);

            // Also make sure the script is enabled
            if (!kingCutscene.enabled)
                kingCutscene.enabled = true;
        }

        // Check if key is already collected
        CheckKeyStatus();
    }

    void Update()
    {
        if (hasTriggered) return;

        // Check if all foods are preserved
        if (AreAllFoodsPreserved())
        {
            hasTriggered = true;
            StartCoroutine(HandleCompletion());
        }
    }

    private bool AreAllFoodsPreserved()
    {
        if (kingAssessment == null) return false;

        // Use the GetPreservedFoodCount method you added
        int preservedCount = kingAssessment.GetPreservedFoodCount();
        Debug.Log($"K3: {preservedCount}/{requiredFoods} foods preserved");

        return preservedCount >= requiredFoods;
    }

    private IEnumerator HandleCompletion()
    {
        Debug.Log("=== ALL K3 FOODS PRESERVED! ===");

        // Small delay to ensure UI is closed
        yield return new WaitForSeconds(0.5f);

        // Check if key is already collected
        bool keyAlreadyCollected = GameDataManager.Instance != null &&
                                  GameDataManager.Instance.CurrentGameData.HasAllerthiaKey();

        // Also check if key was collected in this session
        K3_CollectKey collectKeyScript = FindObjectOfType<K3_CollectKey>();
        bool keyCollectedThisSession = collectKeyScript != null && collectKeyScript.HasTriggeredSummary();

        bool keyIsCollected = keyAlreadyCollected || keyCollectedThisSession;

        if (keyIsCollected)
        {
            Debug.Log("K3: Key already collected, skipping to game summary");
            TriggerGameSummary();
        }
        else
        {
            Debug.Log("K3: Key not collected yet, activating cutscene");
            ActivateKingCutscene();
        }
    }

    private void ActivateKingCutscene()
    {
        if (kingCutscene == null)
        {
            Debug.LogError("K3: King cutscene not found! Trying to find it...");
            kingCutscene = FindObjectOfType<K3_KingCS2>();

            if (kingCutscene == null)
            {
                Debug.LogError("K3: Still cannot find cutscene. Falling back to summary.");
                TriggerGameSummary();
                return;
            }
        }

        // Make sure cutscene is active and enabled
        if (!kingCutscene.gameObject.activeSelf)
            kingCutscene.gameObject.SetActive(true);

        if (!kingCutscene.enabled)
            kingCutscene.enabled = true;

        Debug.Log("K3: Activating King cutscene...");

        // Trigger the cutscene
        kingCutscene.TriggerCutscene();

        // Mark that cutscene has played
        cutscenePlayed = true;
    }

    private void TriggerGameSummary()
    {
        if (gameSummary != null && !gameSummary.IsSummaryActive())
        {
            Debug.Log("K3: Triggering game summary...");

            // Check if this should be triggered by key collection
            K3_CollectKey collectKeyScript = FindObjectOfType<K3_CollectKey>();
            if (collectKeyScript != null && collectKeyScript.HasTriggeredSummary())
            {
                // Summary was triggered by key collection
                gameSummary.TriggerSummaryFromKey();
            }
            else
            {
                // Summary triggered by completing foods without key
                StartCoroutine(TriggerSummaryDelayed());
            }
        }
        else
        {
            Debug.LogWarning("K3: Game summary not found or already active");
        }
    }

    private IEnumerator TriggerSummaryDelayed()
    {
        // Small delay
        yield return new WaitForSeconds(0.5f);

        // We need to trigger summary for completion without key
        // This requires a new method in K3_GameSummary
        if (gameSummary != null)
        {
            // Use reflection or add a new method
            System.Reflection.MethodInfo method = gameSummary.GetType().GetMethod("TriggerAssessmentCompletionSummary");
            if (method != null)
            {
                method.Invoke(gameSummary, null);
            }
            else
            {
                // Fallback to existing method
                gameSummary.TriggerSummaryFromKey();
            }
        }
    }

    private void CheckKeyStatus()
    {
        if (GameDataManager.Instance != null)
        {
            bool keyAlreadyCollected = GameDataManager.Instance.CurrentGameData.HasAllerthiaKey();
            if (keyAlreadyCollected)
            {
                Debug.Log("K3: Key already collected from previous session.");

                // If key is already collected and we have the cutscene, disable it
                if (kingCutscene != null)
                {
                    kingCutscene.gameObject.SetActive(false);
                    kingCutscene.enabled = false;
                }
            }
        }
    }

    // Public method to manually trigger (for testing)
    [ContextMenu("Force Check Completion")]
    public void ForceCheckCompletion()
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(HandleCompletion());
        }
    }

    // Reset for new game
    public void ResetHandler()
    {
        hasTriggered = false;
        cutscenePlayed = false;

        // Re-enable cutscene for new game if key is not collected
        if (kingCutscene != null && GameDataManager.Instance != null)
        {
            bool keyAlreadyCollected = GameDataManager.Instance.CurrentGameData.HasAllerthiaKey();
            if (!keyAlreadyCollected)
            {
                kingCutscene.gameObject.SetActive(true);
                kingCutscene.enabled = true;
            }
        }

        Debug.Log("K3 Completion Handler reset");
    }

    [ContextMenu("Debug Status")]
    public void DebugStatus()
    {
        Debug.Log("=== K3 COMPLETION HANDLER STATUS ===");
        Debug.Log($"Has Triggered: {hasTriggered}");
        Debug.Log($"Cutscene Played: {cutscenePlayed}");
        Debug.Log($"King Cutscene: {(kingCutscene != null ? kingCutscene.gameObject.name : "Not Found")}");
        Debug.Log($"Cutscene Active: {(kingCutscene != null ? kingCutscene.gameObject.activeSelf : false)}");
        Debug.Log($"Cutscene Enabled: {(kingCutscene != null ? kingCutscene.enabled : false)}");
        Debug.Log($"All Foods Preserved: {AreAllFoodsPreserved()}");

        if (kingAssessment != null)
        {
            Debug.Log($"Preserved Foods: {kingAssessment.GetPreservedFoodCount()}/{requiredFoods}");
        }
    }
}