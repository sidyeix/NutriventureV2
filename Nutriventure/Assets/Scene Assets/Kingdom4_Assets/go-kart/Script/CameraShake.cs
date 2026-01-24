using UnityEngine;
using System.Collections;
using Cinemachine;

public class CameraShake : MonoBehaviour
{
    public CinemachineVirtualCamera vcam;

    [Header("Earthquake Settings")]
    public float earthquakeIntensity = 2.5f;
    public float earthquakeDuration = 3f;

    private CinemachineBasicMultiChannelPerlin noise;
    private Coroutine shakeRoutine;

    void Awake()
    {
        noise = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        // SAFETY: ensure no shake at start
        if (noise != null)
        {
            noise.m_AmplitudeGain = 0f;
            noise.m_FrequencyGain = 0f;
        }
    }

    public void PlayEarthquake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        noise.m_AmplitudeGain = earthquakeIntensity;
        noise.m_FrequencyGain = earthquakeIntensity * 1.2f;

        yield return new WaitForSeconds(earthquakeDuration);

        // HARD STOP (THIS IS THE KEY)
        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;
    }
}
