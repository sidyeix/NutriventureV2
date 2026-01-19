using UnityEngine;

public class MilkDeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 🔥 Force detach from moving platform
            other.transform.SetParent(null);

            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RespawnPlayer();
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 1, 0, 0.3f);

        if (GetComponent<BoxCollider>() != null)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            Gizmos.DrawCube(transform.position + col.center, col.size);
        }
        else if (GetComponent<SphereCollider>() != null)
        {
            SphereCollider col = GetComponent<SphereCollider>();
            Gizmos.DrawSphere(transform.position + col.center, col.radius);
        }
    }
}
