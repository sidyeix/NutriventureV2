using UnityEngine;

public class QA2_CompletionHandler : MonoBehaviour
{
    [Header("References")]
    public K2_QA2system qa2System;
    public K2_GameSummary gameSummary;
    public K2_QueenACS2 queenCutscene;
    
    [Header("Settings")]
    public int requiredAnswers = 5;
    
    private bool hasTriggered = false;
    
    void Start()
    {
        if (qa2System == null)
            qa2System = FindObjectOfType<K2_QA2system>();
        
        if (gameSummary == null)
            gameSummary = FindObjectOfType<K2_GameSummary>();
        
        if (queenCutscene == null)
            queenCutscene = FindObjectOfType<K2_QueenACS2>();
    }
    
    void Update()
    {
        if (hasTriggered || qa2System == null || gameSummary == null) return;
        
        // Check if QA2 is completed
        if (qa2System.GetCorrectlyAnsweredCount() >= requiredAnswers)
        {
            // Check if key is collected
            bool keyAlreadyCollected = GameDataManager.Instance != null && 
                                       GameDataManager.Instance.CurrentGameData.HasSugariaKey();
            
            if (keyAlreadyCollected)
            {
                Debug.Log("QA2 completed with key collected. Triggering summary.");
                gameSummary.TriggerQA2CompletionSummary();
                hasTriggered = true;
            }
            else
            {
                Debug.Log("QA2 completed but key not collected. Waiting for timeline.");
                hasTriggered = true; // Prevent checking again
            }
        }
    }
    
    // Reset for new game
    public void ResetHandler()
    {
        hasTriggered = false;
    }
}