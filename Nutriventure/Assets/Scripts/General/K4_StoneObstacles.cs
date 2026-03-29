using UnityEngine;

public class K4_StoneObstacles : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private bool useTriggerParam;
    [SerializeField] private string stateName;
    [SerializeField] private string triggerParamName;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (useTriggerParam)
                animator.SetTrigger(triggerParamName);
            else
                animator.Play(stateName);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.transform.SetParent(null);
        }
    }

    /// <summary>
    /// Resets this obstacle so it can be triggered again on restart.
    /// </summary>
    public void ResetObstacle()
    {
        hasTriggered = false;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
