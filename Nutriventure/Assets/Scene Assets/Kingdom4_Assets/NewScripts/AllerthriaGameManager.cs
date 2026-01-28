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
    }
    
    public void StartPhase(GamePhase phase)
    {
        currentPhase = phase;
        Debug.Log($"Starting phase: {phase}");
        
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
            UpdateQuestText($"Find allergens: {collectedAllergens.Count}/9");
            
            if (collectedAllergens.Count >= 9)
            {
                StartPhase(GamePhase.WagonPhase);
            }
        }
    }
    
    private void StartWagonPhase()
    {
        UpdateQuestText("Drive the wagon");
        if (wagon != null)
            wagon.SetActive(true);
    }
    
    public void CompleteWagonPhase()
    {
        StartPhase(GamePhase.PlatformPhase);
    }
    
    private void StartPlatformPhase()
    {
        UpdateQuestText("Land on healthy foods");
        if (movingPlatform != null)
            movingPlatform.SetActive(true);
    }
    
    public void CompletePlatformPhase()
    {
        Debug.Log("Platform phase completed!");
        StartPhase(GamePhase.CastlePhase);
    }
    
    private void StartCastlePhase()
    {
        UpdateQuestText("Go to the castle");
    }
    
    public void ReachQueen()
    {
        Debug.Log("Reached the queen!");
        StartPhase(GamePhase.KeyPhase);
    }
    
    private void StartKeyPhase()
    {
        UpdateQuestText("Get the key");
    }
    
    public void ReceiveKey()
    {
        hasKey = true;
        StartPhase(GamePhase.EndGame);
    }
    
    private void StartEndGame()
    {
        UpdateQuestText("Return to entrance");
    }
    
    public void CompleteGame()
    {
        UpdateQuestText("Mission Complete!");
        Debug.Log("Game Complete!");
        
        // Optional: Add celebration effects, fade out, load menu, etc.
    }
    
    // Safe method to update text
    private void UpdateQuestText(string text)
    {
        Debug.Log($"[QUEST] {text}");
        
        if (questText != null)
        {
            questText.text = text;
        }
    }
    
    // Helper method to check current phase
    public bool IsCurrentPhase(GamePhase phase)
    {
        return currentPhase == phase;
    }
}