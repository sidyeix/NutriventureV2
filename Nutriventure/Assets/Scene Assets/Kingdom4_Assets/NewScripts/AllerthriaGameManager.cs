using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class AllerthriaGameManager : MonoBehaviour
{
    public static AllerthriaGameManager Instance { get; private set; }
    
    public enum GamePhase
    {
        ScrollQuest,
        AllergenHunt,
        WagonPhase,
        PlatformPhase,
        CastlePhase,
        KeyPhase,
        EndGame
    }
    
    [Header("Game Flow")]
    public GamePhase currentPhase = GamePhase.ScrollQuest;
    
    [Header("Quest Items")]
    public bool hasScroll = false;
    public List<string> collectedAllergens = new List<string>();
    public bool hasKey = false;
    
    [Header("References")]
    public GameObject scroll;
    public GameObject wagon;
    public GameObject movingPlatform;
    
    [Header("UI")]
    public TextMeshProUGUI questText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI allergenCountText;
    public TextMeshProUGUI wagonHitsText;
    public GameObject gameCompletePanel;
    public TextMeshProUGUI finalScoreText;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        StartPhase(GamePhase.ScrollQuest);
        
        UpdateQuestText("Find the scroll");
        UpdateScoreDisplay();
        
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.OnScoreChanged.AddListener(OnScoreChanged);
            Kingdom4ScoreManager.Instance.OnMultiplierChanged.AddListener(OnMultiplierChanged);
        }
    }
    
    void Update()
    {
        UpdatePhaseSpecificDisplay();
    }
    
    private void UpdatePhaseSpecificDisplay()
    {
        switch (currentPhase)
        {
            case GamePhase.AllergenHunt:
                if (allergenCountText != null && Kingdom4ScoreManager.Instance != null)
                {
                    allergenCountText.text = $"Allergens: {collectedAllergens.Count}/9";
                }
                break;
                
            case GamePhase.WagonPhase:
                if (wagonHitsText != null && Kingdom4ScoreManager.Instance != null)
                {
                    wagonHitsText.text = $"Wagon Hits: {Kingdom4ScoreManager.Instance.totalWagonHits}";
                }
                break;
        }
    }
    
    private void OnScoreChanged(int newScore)
    {
        UpdateScoreDisplay();
    }
    
    private void OnMultiplierChanged(int newMultiplier)
    {
        UpdateMultiplierDisplay();
    }
    
    private void UpdateScoreDisplay()
    {
        if (scoreText != null && Kingdom4ScoreManager.Instance != null)
        {
            scoreText.text = $"SCORE: {Kingdom4ScoreManager.Instance.GetFinalScore()}";
        }
    }
    
    private void UpdateMultiplierDisplay()
    {
        if (multiplierText != null && Kingdom4ScoreManager.Instance != null)
        {
            multiplierText.text = $"x{Kingdom4ScoreManager.Instance.comboMultiplier}";
            multiplierText.gameObject.SetActive(currentPhase == GamePhase.PlatformPhase);
        }
    }
    
    public void StartPhase(GamePhase phase)
    {
        currentPhase = phase;
        Debug.Log($"Starting phase: {phase}");
        
        UpdateUIVisibility();
        
        switch (phase)
        {
            case GamePhase.ScrollQuest:
                StartScrollQuest();
                break;
            case GamePhase.AllergenHunt:
                StartAllergenHunt();
                break;
            case GamePhase.WagonPhase:
                StartWagonPhase();
                break;
            case GamePhase.PlatformPhase:
                StartPlatformPhase();
                break;
            case GamePhase.CastlePhase:
                StartCastlePhase();
                break;
            case GamePhase.KeyPhase:
                StartKeyPhase();
                break;
            case GamePhase.EndGame:
                StartEndGame();
                break;
        }
    }
    
    private void UpdateUIVisibility()
    {
        if (allergenCountText != null)
        {
            allergenCountText.gameObject.SetActive(currentPhase == GamePhase.AllergenHunt);
        }
        
        if (wagonHitsText != null)
        {
            wagonHitsText.gameObject.SetActive(currentPhase == GamePhase.WagonPhase);
        }
        
        UpdateMultiplierDisplay();
    }
    
    private void StartScrollQuest()
    {
        UpdateQuestText("Find the scroll");
        if (scroll != null)
            scroll.SetActive(true);
    }
    
    public void CollectScroll()
    {
        hasScroll = true;
        StartPhase(GamePhase.AllergenHunt);
    }
    
    private void StartAllergenHunt()
    {
        UpdateQuestText($"Find allergens: {collectedAllergens.Count}/9");
        
        AllergenSpawnManager spawner = FindObjectOfType<AllergenSpawnManager>();
        if (spawner != null)
            spawner.SpawnNow();
    }
    
    public void CollectAllergen(string allergenId)
    {
        if (!collectedAllergens.Contains(allergenId))
        {
            collectedAllergens.Add(allergenId);
            
            if (Kingdom4ScoreManager.Instance != null)
            {
                Kingdom4ScoreManager.Instance.AddAllergenFound();
            }
            
            UpdateQuestText($"Find allergens: {collectedAllergens.Count}/9");
            
            if (collectedAllergens.Count >= 9)
            {
                StartPhase(GamePhase.WagonPhase);
            }
        }
    }
    
    private void StartWagonPhase()
    {
        UpdateQuestText("Drive the wagon to the platform");
        if (wagon != null)
            wagon.SetActive(true);
    }
    
    public void CompleteWagonPhase()
    {
        StartPhase(GamePhase.PlatformPhase);
    }
    
    public void WagonHitAllergen()
    {
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.WagonHitAllergen();
        }
    }
    
    private void StartPlatformPhase()
    {
        UpdateQuestText("Land on healthy foods to build combo!");
        if (movingPlatform != null)
            movingPlatform.SetActive(true);
    }
    
    public void CompletePlatformPhase()
    {
        Debug.Log("Platform phase completed!");
        StartPhase(GamePhase.CastlePhase);
    }
    
    public void HitHealthyFood()
    {
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.HitHealthyFood();
        }
    }
    
    public void HitAllergenInPhase3()
    {
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.HitAllergenInPhase3();
        }
    }
    
    private void StartCastlePhase()
    {
        UpdateQuestText("Go to the castle and meet the queen");
    }
    
    public void ReachQueen()
    {
        Debug.Log("Reached the queen!");
        StartPhase(GamePhase.KeyPhase);
    }
    
    private void StartKeyPhase()
    {
        UpdateQuestText("Get the key from the queen");
    }
    
    public void ReceiveKey()
    {
        hasKey = true;
        StartPhase(GamePhase.EndGame);
    }
    
    private void StartEndGame()
    {
        UpdateQuestText("Return to the entrance with the key");
    }
    
    public void CompleteGame()
    {
        UpdateQuestText("Mission Complete!");
        Debug.Log("Game Complete!");
        
        ShowFinalScore();
    }
    
    private void ShowFinalScore()
    {
        if (gameCompletePanel != null)
        {
            gameCompletePanel.SetActive(true);
            
            if (finalScoreText != null && Kingdom4ScoreManager.Instance != null)
            {
                int finalScore = Kingdom4ScoreManager.Instance.GetFinalScore();
                finalScoreText.text = $"FINAL SCORE: {finalScore}";
                
                Debug.Log($"Final Score Breakdown:");
                Debug.Log($"- Allergens Found: {Kingdom4ScoreManager.Instance.allergensFound}");
                Debug.Log($"- Wagon Hits: {Kingdom4ScoreManager.Instance.totalWagonHits}");
                Debug.Log($"- Time Bonus: {Kingdom4ScoreManager.Instance.timeBonus}");
            }
        }
    }
    
    private void UpdateQuestText(string text)
    {
        Debug.Log($"[QUEST] {text}");
        
        if (questText != null)
        {
            questText.text = text;
        }
    }
    
    public bool IsCurrentPhase(GamePhase phase)
    {
        return currentPhase == phase;
    }
    
    public void ResetGame()
    {
        hasScroll = false;
        collectedAllergens.Clear();
        hasKey = false;
        
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.ResetScore();
        }
        
        UpdateScoreDisplay();
        StartPhase(GamePhase.ScrollQuest);
        
        if (gameCompletePanel != null)
        {
            gameCompletePanel.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.OnScoreChanged.RemoveListener(OnScoreChanged);
            Kingdom4ScoreManager.Instance.OnMultiplierChanged.RemoveListener(OnMultiplierChanged);
        }
    }
    
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 200, 30), "Test: Add Allergen (+100)"))
        {
            if (Kingdom4ScoreManager.Instance != null)
            {
                Kingdom4ScoreManager.Instance.AddAllergenFound();
            }
        }
        
        if (GUI.Button(new Rect(10, 50, 200, 30), "Test: Wagon Hit (-50)"))
        {
            if (Kingdom4ScoreManager.Instance != null)
            {
                Kingdom4ScoreManager.Instance.WagonHitAllergen();
            }
        }
        
        if (GUI.Button(new Rect(10, 90, 200, 30), "Test: Healthy Food (+combo)"))
        {
            if (Kingdom4ScoreManager.Instance != null)
            {
                Kingdom4ScoreManager.Instance.HitHealthyFood();
            }
        }
        
        if (GUI.Button(new Rect(10, 130, 200, 30), "Test: Reset Score"))
        {
            if (Kingdom4ScoreManager.Instance != null)
            {
                Kingdom4ScoreManager.Instance.ResetScore();
            }
        }
    }
}