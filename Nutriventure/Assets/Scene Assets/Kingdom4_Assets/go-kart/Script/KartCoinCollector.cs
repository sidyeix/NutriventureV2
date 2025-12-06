using UnityEngine;

public class KartCoinCollector : MonoBehaviour
{
    [Header("Coin Collection Settings")]
    public string coinTag = "Coin";
    public int coinValue = 1;
    public AudioClip coinCollectSound;
    public ParticleSystem collectParticles;
    
    [Header("Collection Area")]
    public float collectionRadius = 2f;
    public Vector3 collectionCenterOffset = Vector3.forward * 1f; // Slightly in front of kart
    public bool useSphereCollider = true; // Use sphere collider or distance check
    
    [Header("Effects")]
    public bool rotateCollectedCoins = true;
    public float coinAttractionSpeed = 5f;
    public float coinCollectionDelay = 0.3f;
    
    private SphereCollider coinCollider;
    private AudioSource audioSource;
    
    void Start()
    {
        SetupCoinCollection();
        
        // Add AudioSource if needed
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && coinCollectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    void SetupCoinCollection()
    {
        if (useSphereCollider)
        {
            // Add sphere collider as trigger for coin collection
            coinCollider = gameObject.AddComponent<SphereCollider>();
            coinCollider.isTrigger = true;
            coinCollider.radius = collectionRadius;
            coinCollider.center = collectionCenterOffset;
            
            // Remove existing trigger colliders that might interfere
            Collider[] existingColliders = GetComponents<Collider>();
            foreach (Collider col in existingColliders)
            {
                if (col != coinCollider && col.isTrigger)
                {
                    Destroy(col);
                }
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(coinTag))
        {
            CollectCoin(other.gameObject);
        }
    }
    
    void Update()
    {
        // Alternative: Distance-based collection (if not using collider)
        if (!useSphereCollider && enabled)
        {
            CollectCoinsByDistance();
        }
    }
    
    void CollectCoinsByDistance()
    {
        GameObject[] coins = GameObject.FindGameObjectsWithTag(coinTag);
        Vector3 collectionCenter = transform.TransformPoint(collectionCenterOffset);
        
        foreach (GameObject coin in coins)
        {
            if (coin == null) continue;
            
            float distance = Vector3.Distance(coin.transform.position, collectionCenter);
            if (distance <= collectionRadius)
            {
                CollectCoin(coin);
            }
        }
    }
    
    void CollectCoin(GameObject coinObject)
    {
        if (coinObject == null) return;
        
        // Get the CoinCollectionSystem component if it exists
        CoinCollectionSystem coinSystem = coinObject.GetComponent<CoinCollectionSystem>();
        
        if (coinSystem != null)
        {
            // If the coin has its own collection system, use it
            coinSystem.CollectCoin();
        }
        else
        {
            // Alternative: Create coin effect and collect
            StartCoroutine(CollectCoinWithEffects(coinObject));
        }
    }
    
    System.Collections.IEnumerator CollectCoinWithEffects(GameObject coinObject)
    {
        // Disable the coin's collider to prevent multiple collections
        Collider coinCollider = coinObject.GetComponent<Collider>();
        if (coinCollider != null)
        {
            coinCollider.enabled = false;
        }
        
        // Attract coin to kart with smooth animation
        float elapsedTime = 0f;
        Vector3 startPosition = coinObject.transform.position;
        
        while (elapsedTime < coinCollectionDelay)
        {
            if (coinObject == null) yield break;
            
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / coinCollectionDelay;
            
            // Move coin toward kart with easing
            Vector3 targetPosition = transform.TransformPoint(collectionCenterOffset);
            coinObject.transform.position = Vector3.Lerp(startPosition, targetPosition, t * coinAttractionSpeed);
            
            // Rotate coin for visual effect
            if (rotateCollectedCoins)
            {
                coinObject.transform.Rotate(Vector3.up * 360f * Time.deltaTime, Space.World);
            }
            
            yield return null;
        }
        
        // Play collection effects
        PlayCollectionEffects();
        
        // Add coins to game data
        AddCoinsToGameData(coinValue);
        
        // Destroy or disable the coin
        if (coinObject != null)
        {
            Destroy(coinObject);
        }
    }
    
    void PlayCollectionEffects()
    {
        // Play sound
        if (coinCollectSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(coinCollectSound);
            }
            else if (AudioHandler.Instance != null)
            {
                AudioHandler.Instance.PlayCharacterSelectionSound(coinCollectSound);
            }
        }
        
        // Play particles
        if (collectParticles != null)
        {
            Vector3 particlePosition = transform.TransformPoint(collectionCenterOffset);
            ParticleSystem particles = Instantiate(collectParticles, particlePosition, Quaternion.identity);
            particles.Play();
            
            // Destroy particles after they finish
            Destroy(particles.gameObject, particles.main.duration);
        }
    }
    
    void AddCoinsToGameData(int amount)
    {
        // Add coins using the existing CoinCollectionSystem static methods
        CoinCollectionSystem.AddCoins(amount);
        
        // Or directly if needed:
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.nutriCoins += amount;
            GameDataManager.Instance.SaveGameData();
            
            Debug.Log($"🎮 Kart collected coin! +{amount} coins. Total: {GameDataManager.Instance.CurrentGameData.nutriCoins}");
        }
    }
    
    // Public method to collect coins from other scripts
    public void CollectCoinManually(GameObject coinObject)
    {
        CollectCoin(coinObject);
    }
    
    // Method to enable/disable coin collection
    public void SetCoinCollectionEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (coinCollider != null)
        {
            coinCollider.enabled = enabled;
        }
    }
    
    // Visual debugging in editor
    void OnDrawGizmosSelected()
    {
        if (enabled)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.TransformPoint(collectionCenterOffset);
            
            if (useSphereCollider)
            {
                Gizmos.DrawWireSphere(center, collectionRadius);
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawSphere(center, collectionRadius);
            }
            else
            {
                Gizmos.DrawWireSphere(center, collectionRadius);
            }
            
            // Draw line from kart to collection center
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, center);
        }
    }
}