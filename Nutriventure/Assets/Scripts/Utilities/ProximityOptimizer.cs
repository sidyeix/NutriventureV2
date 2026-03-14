using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to any GameObject with heavy components (Animator, ParticleSystem, AudioSource).
/// Disables them when the player is far away and re-enables when the player is near.
/// </summary>
public class ProximityOptimizer : MonoBehaviour
{
  [Header("Settings")]
  [Tooltip("Distance at which components get enabled")]
  public float enableDistance = 30f;
  [Tooltip("Extra buffer distance before disabling (prevents flickering)")]
  public float disableBuffer = 5f;
  [Tooltip("How often to check distance (seconds)")]
  public float checkInterval = 0.5f;

  [Header("What to Optimize")]
  public bool optimizeAnimators = true;
  public bool optimizeParticles = true;
  public bool optimizeAudioSources = true;
  public bool optimizeRenderers = false;

  private Transform playerTransform;
  private float enableDistanceSqr;
  private float disableDistanceSqr;
  private float nextCheckTime;
  private bool isEnabled = true;

  // Cached components
  private Animator[] animators;
  private ParticleSystem[] particles;
  private AudioSource[] audioSources;
  private Renderer[] renderers;

  private void Start()
  {
    enableDistanceSqr = enableDistance * enableDistance;
    float totalDist = enableDistance + disableBuffer;
    disableDistanceSqr = totalDist * totalDist;

    // Cache components
    if (optimizeAnimators)
      animators = GetComponentsInChildren<Animator>(true);
    if (optimizeParticles)
      particles = GetComponentsInChildren<ParticleSystem>(true);
    if (optimizeAudioSources)
      audioSources = GetComponentsInChildren<AudioSource>(true);
    if (optimizeRenderers)
      renderers = GetComponentsInChildren<Renderer>(true);

    // Find player
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
      playerTransform = player.transform;

    // Stagger check times to spread load across frames
    nextCheckTime = Time.time + Random.Range(0f, checkInterval);
  }

  private void Update()
  {
    if (playerTransform == null || Time.time < nextCheckTime) return;

    nextCheckTime = Time.time + checkInterval;

    float sqrDist = (transform.position - playerTransform.position).sqrMagnitude;

    if (isEnabled && sqrDist > disableDistanceSqr)
    {
      SetComponentsEnabled(false);
      isEnabled = false;
    }
    else if (!isEnabled && sqrDist <= enableDistanceSqr)
    {
      SetComponentsEnabled(true);
      isEnabled = true;
    }
  }

  private void SetComponentsEnabled(bool enabled)
  {
    if (optimizeAnimators && animators != null)
    {
      for (int i = 0; i < animators.Length; i++)
      {
        if (animators[i] != null)
          animators[i].enabled = enabled;
      }
    }

    if (optimizeParticles && particles != null)
    {
      for (int i = 0; i < particles.Length; i++)
      {
        if (particles[i] != null)
        {
          if (enabled)
            particles[i].Play();
          else
            particles[i].Pause();
        }
      }
    }

    if (optimizeAudioSources && audioSources != null)
    {
      for (int i = 0; i < audioSources.Length; i++)
      {
        if (audioSources[i] != null)
          audioSources[i].enabled = enabled;
      }
    }

    if (optimizeRenderers && renderers != null)
    {
      for (int i = 0; i < renderers.Length; i++)
      {
        if (renderers[i] != null)
          renderers[i].enabled = enabled;
      }
    }
  }

  private void OnDisable()
  {
    // Re-enable everything when this component is disabled
    if (!isEnabled)
    {
      SetComponentsEnabled(true);
      isEnabled = true;
    }
  }
}
