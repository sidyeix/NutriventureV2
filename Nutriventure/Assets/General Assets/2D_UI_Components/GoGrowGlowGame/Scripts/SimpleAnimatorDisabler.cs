using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleAnimatorDisabler : MonoBehaviour
{
    [Header("Animators to Disable/Enable")]
    public List<Animator> animators = new List<Animator>();

    [Header("Settings")]
    public float disableTime = 1f; // How long to disable animators

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(DisableThenEnable());
        }
    }

    private IEnumerator DisableThenEnable()
    {
        // 1. Disable all animators
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        // 2. Wait for disableTime seconds
        yield return CoroutineYieldCache.WaitForSeconds(disableTime);

        // 3. Enable all animators
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                animator.enabled = true;
            }
        }
    }
}