using UnityEngine;

public class SimpleKeyCollector : MonoBehaviour
{
    [Header("Key Settings")]
    public bool isCollectible = true;
    
    [Header("Effects")]
    public GameObject collectEffect;
    public AudioClip collectSound;
    
    [Header("Touch Settings")]
    public float touchRadius = 1.5f; // How close player needs to be
    
    private bool isCollected = false;
    private Transform player;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Load saved state
        if (PlayerPrefs.HasKey("KeyCollected"))
        {
            isCollected = true;
            gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (!isCollectible || isCollected || player == null) return;
        
        // Check if player is close enough
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= touchRadius)
        {
            // Auto-collect when player is close
            CollectKey();
        }
        
        // Alternative: Check for touch input on the key
        CheckTouchInput();
    }
    
    void CheckTouchInput()
    {
        // Handle mobile touches
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, 100f))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        CollectKey();
                    }
                }
            }
        }
        
        // Handle mouse click for testing
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    CollectKey();
                }
            }
        }
    }
    
    void OnMouseDown()
    {
        // Simple click collection (requires collider)
        if (isCollectible && !isCollected)
        {
            CollectKey();
        }
    }
    
    void CollectKey()
    {
        if (isCollected || !isCollectible) return;
        
        isCollected = true;
        
        // Notify Game Manager
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.ReceiveKey();
        }
        
        // Play effects
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Save state
        PlayerPrefs.SetInt("KeyCollected", 1);
        PlayerPrefs.Save();
        
        // Hide the key
        gameObject.SetActive(false);
        
        Debug.Log("Key collected!");
    }
    
    public void MakeCollectible()
    {
        isCollectible = true;
        Debug.Log("Key can now be collected");
        
        // Visual feedback
        StartCoroutine(ShowCollectibleEffect());
    }
    
    System.Collections.IEnumerator ShowCollectibleEffect()
    {
        // Simple pulse effect
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            
            for (int i = 0; i < 3; i++)
            {
                renderer.material.color = Color.yellow;
                yield return new WaitForSeconds(0.3f);
                renderer.material.color = originalColor;
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
    
    [ContextMenu("Reset Key")]
    public void ResetKey()
    {
        isCollected = false;
        isCollectible = false;
        gameObject.SetActive(true);
        PlayerPrefs.DeleteKey("KeyCollected");
    }
    
    void OnDrawGizmos()
    {
        if (isCollectible)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, touchRadius);
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, touchRadius);
        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, touchRadius);
        }
    }
}