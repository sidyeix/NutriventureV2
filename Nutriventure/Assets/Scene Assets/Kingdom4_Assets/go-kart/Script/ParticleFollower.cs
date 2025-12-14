using UnityEngine;

public class ParticleFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector3 offset = Vector3.zero;
    public float followSpeed = 10f;
    public bool rotateWithTarget = true;
    public bool useSmoothFollow = true;
    
    [Header("Particle Settings")]
    public ParticleSystem particles;
    public bool playOnStart = true;
    
    private Vector3 velocity = Vector3.zero;
    
    void Start()
    {
        if (particles == null)
            particles = GetComponent<ParticleSystem>();
            
        if (playOnStart && particles != null)
            particles.Play();
            
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    void Update()
    {
        if (target == null) return;
        
        Vector3 targetPosition = target.position + offset;
        
        if (useSmoothFollow)
        {
            // Smooth follow
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, followSpeed * Time.deltaTime);
        }
        else
        {
            // Instant follow
            transform.position = targetPosition;
        }
        
        if (rotateWithTarget)
        {
            transform.rotation = target.rotation;
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    public void Play()
    {
        if (particles != null)
            particles.Play();
    }
    
    public void Stop()
    {
        if (particles != null)
            particles.Stop();
    }
    
    public void DestroyAfter(float delay)
    {
        Destroy(gameObject, delay);
    }
}