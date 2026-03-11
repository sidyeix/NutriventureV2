using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GlowTower : MonoBehaviour
{
    [Header("Tower Settings")]
    [SerializeField] private float range = 5f;
    [SerializeField] private float initialEnergy = 0f;
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private Transform centerPoint;

    [Header("Text Indicators - Assign ALL text objects here")]
    [SerializeField] private List<TMP_Text> energyTextIndicators = new List<TMP_Text>();

    [Header("Animation Settings")]
    [SerializeField] private Animator towerAnimator;
    [SerializeField] private string isLightingParam = "isLightingUp";
    [SerializeField] private string isLightedParam = "isLighted";
    [SerializeField] private float lightingToLitDelay = 2f;

    [Header("Beam Hit Point")]
    [SerializeField] private Transform beamHitPoint;

    [Header("Visual Effects")]
    [SerializeField] private GameObject lightingEffect;
    [SerializeField] private GameObject fullyLitEffect;
    [SerializeField] private GameObject rangeIndicator;

    [Header("Text Display Settings")]
    [SerializeField] private Color lowEnergyColor = Color.red;
    [SerializeField] private Color mediumEnergyColor = Color.yellow;
    [SerializeField] private Color highEnergyColor = Color.green;
    [SerializeField] private Color transferringColor = Color.white;
    [SerializeField] private bool showPulsingEffect = true;
    [SerializeField] private float textUpdateInterval = 0.1f;

    // State
    private float currentEnergy = 0f;
    private bool isActive = false;
    private bool isFullyLit = false;
    private bool isLighting = false;
    private Coroutine updateTextCoroutine;
    private Coroutine animationSequenceCoroutine;

    // Store original scales for each text
    private Dictionary<TMP_Text, Vector3> originalTextScales = new Dictionary<TMP_Text, Vector3>();

    // NEW: Store initial animation state
    private AnimatorStateInfo initialAnimatorState;

    private void Start()
    {
        currentEnergy = Mathf.Clamp(initialEnergy, 0f, maxEnergy);

        if (towerAnimator != null)
        {
            // Store initial animator state
            initialAnimatorState = towerAnimator.GetCurrentAnimatorStateInfo(0);

            // Reset animator parameters
            towerAnimator.SetBool(isLightingParam, false);
            towerAnimator.SetBool(isLightedParam, false);

            // Play default state
            towerAnimator.Play("Default", -1, 0f);
        }

        if (lightingEffect != null) lightingEffect.SetActive(false);
        if (fullyLitEffect != null) fullyLitEffect.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.SetActive(false);

        InitializeTextIndicators();

        if (GlowPartManager.Instance != null)
            GlowPartManager.Instance.RegisterTower(this);
    }

    private void InitializeTextIndicators()
    {
        originalTextScales.Clear();

        if (energyTextIndicators.Count == 0)
        {
            TMP_Text[] foundTexts = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in foundTexts)
            {
                if (!energyTextIndicators.Contains(text))
                {
                    energyTextIndicators.Add(text);
                }
            }
        }

        foreach (TMP_Text textIndicator in energyTextIndicators)
        {
            if (textIndicator != null)
            {
                originalTextScales[textIndicator] = textIndicator.transform.localScale;
            }
        }

        UpdateEnergyTextIndicators();
    }

    public void ActivateTower()
    {
        if (isActive) return;

        isActive = true;

        if (rangeIndicator != null)
            rangeIndicator.SetActive(true);

        if (updateTextCoroutine != null)
            StopCoroutine(updateTextCoroutine);
        updateTextCoroutine = StartCoroutine(UpdateTextIndicatorsRoutine());

        #if UNITY_EDITOR
        Debug.Log($"Tower {gameObject.name} activated");
        #endif
    }

    public void DeactivateTower()
    {
        if (!isActive) return;

        isActive = false;

        if (rangeIndicator != null)
            rangeIndicator.SetActive(false);

        if (animationSequenceCoroutine != null)
        {
            StopCoroutine(animationSequenceCoroutine);
            animationSequenceCoroutine = null;
        }

        SetLightingAnimation(false);

        if (updateTextCoroutine != null)
        {
            StopCoroutine(updateTextCoroutine);
            updateTextCoroutine = null;
        }

        #if UNITY_EDITOR
        Debug.Log($"Tower {gameObject.name} deactivated");
        #endif
    }

    public void AddEnergy(float amount)
    {
        if (!isActive || isFullyLit) return;

        float previousEnergy = currentEnergy;
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

        UpdateEnergyTextIndicators();

        if (currentEnergy > previousEnergy && !isLighting && currentEnergy < maxEnergy)
        {
            SetLightingAnimation(true);
        }

        if (currentEnergy >= maxEnergy && !isFullyLit)
        {
            isFullyLit = true;
            #if UNITY_EDITOR
            Debug.Log($"Tower {gameObject.name} reached maximum energy!");
            #endif
            StartLightingToLitSequence();
        }
    }

    private void StartLightingToLitSequence()
    {
        if (animationSequenceCoroutine != null)
        {
            StopCoroutine(animationSequenceCoroutine);
        }

        animationSequenceCoroutine = StartCoroutine(LightingToLitSequenceRoutine());
    }

    private IEnumerator LightingToLitSequenceRoutine()
    {
        if (!isLighting)
        {
            SetLightingAnimation(true);
        }

        if (towerAnimator != null)
        {
            towerAnimator.SetBool(isLightedParam, true);
        }

        if (fullyLitEffect != null)
        {
            fullyLitEffect.SetActive(true);
        }

        yield return CoroutineYieldCache.WaitForSeconds(lightingToLitDelay);

        SetLightingAnimation(false);

        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
        }
    }

    public void SetLightingAnimation(bool lighting)
    {
        if (isLighting == lighting) return;

        isLighting = lighting;

        if (towerAnimator != null)
        {
            towerAnimator.SetBool(isLightingParam, lighting);
        }

        if (lightingEffect != null)
            lightingEffect.SetActive(lighting);

        UpdateEnergyTextIndicators();
    }

    public void SetFullyLitAnimation(bool lighted)
    {
        if (lighted)
        {
            StartLightingToLitSequence();
        }
        else
        {
            isFullyLit = false;
            if (towerAnimator != null)
            {
                towerAnimator.SetBool(isLightedParam, false);
            }
            if (fullyLitEffect != null)
                fullyLitEffect.SetActive(false);
        }
    }

    private void UpdateEnergyTextIndicators()
    {
        foreach (TMP_Text textIndicator in energyTextIndicators)
        {
            if (textIndicator != null && textIndicator.gameObject.activeInHierarchy)
            {
                if (isFullyLit)
                {
                    textIndicator.text = "FULL!";
                    textIndicator.color = highEnergyColor;

                    if (showPulsingEffect)
                    {
                        float pulse = Mathf.Sin(Time.time * 2f) * 0.2f;
                        textIndicator.transform.localScale = new Vector3(4 + pulse, 4 + pulse, 4 + pulse);
                    }
                    else
                    {
                        textIndicator.transform.localScale = new Vector3(4, 4, 4);
                    }
                }
                else
                {
                    textIndicator.text = $"{currentEnergy:F0}/{maxEnergy}";

                    float energyRatio = currentEnergy / maxEnergy;

                    Color baseColor;
                    if (energyRatio <= 0.33f)
                        baseColor = lowEnergyColor;
                    else if (energyRatio <= 0.66f)
                        baseColor = mediumEnergyColor;
                    else
                        baseColor = highEnergyColor;

                    if (isLighting)
                    {
                        textIndicator.color = Color.Lerp(baseColor, transferringColor, 0.3f);

                        if (showPulsingEffect)
                        {
                            float pulse = Mathf.Sin(Time.time * 3f) * 0.3f;
                            textIndicator.transform.localScale = new Vector3(4 + pulse, 4 + pulse, 4 + pulse);
                        }
                        else
                        {
                            textIndicator.transform.localScale = new Vector3(4, 4, 4);
                        }
                    }
                    else
                    {
                        textIndicator.color = baseColor;
                        textIndicator.transform.localScale = new Vector3(4, 4, 4);
                    }

                    if (energyRatio >= 0.9f && currentEnergy < maxEnergy && showPulsingEffect)
                    {
                        float pulse = Mathf.Sin(Time.time * 4f) * 0.15f;
                        textIndicator.transform.localScale = new Vector3(4 + pulse, 4 + pulse, 4 + pulse);
                    }
                }
            }
        }
    }

    private IEnumerator UpdateTextIndicatorsRoutine()
    {
        while (isActive)
        {
            yield return CoroutineYieldCache.WaitForSeconds(textUpdateInterval);
            UpdateEnergyTextIndicators();
        }
    }

    // PUBLIC GETTERS
    public float GetCurrentEnergy() => currentEnergy;
    public float GetMaxEnergy() => maxEnergy;
    public bool IsFullyLit() => isFullyLit;
    public bool IsActive() => isActive;
    public bool IsLighting() => isLighting;
    public float GetRange() => range;
    public Transform GetCenterPoint() => centerPoint != null ? centerPoint : transform;
    public Vector3 GetCenterPointPosition() => GetCenterPoint().position;

    public Transform GetBeamHitPoint()
    {
        if (beamHitPoint != null)
            return beamHitPoint;
        else if (centerPoint != null)
            return centerPoint;
        else
            return transform;
    }

    public void SetEnergy(float energy)
    {
        float previousEnergy = currentEnergy;
        currentEnergy = Mathf.Clamp(energy, 0f, maxEnergy);

        bool wasFullyLit = isFullyLit;
        isFullyLit = currentEnergy >= maxEnergy;

        if (isFullyLit && !wasFullyLit)
        {
            StartLightingToLitSequence();
        }
        else if (!isFullyLit && wasFullyLit)
        {
            isFullyLit = false;
            if (towerAnimator != null)
            {
                towerAnimator.SetBool(isLightedParam, false);
            }
            if (fullyLitEffect != null)
                fullyLitEffect.SetActive(false);
            if (rangeIndicator != null && isActive)
                rangeIndicator.SetActive(true);
        }

        if (Mathf.Abs(currentEnergy - previousEnergy) > 0.01f)
            UpdateEnergyTextIndicators();
    }

    // NEW: COMPLETE RESET METHOD
    public void ResetTower()
    {
        #if UNITY_EDITOR
        Debug.Log($"Resetting tower: {gameObject.name}");
        #endif

        // Reset energy
        currentEnergy = 0f;
        isFullyLit = false;
        isLighting = false;
        isActive = false;

        // Stop all coroutines
        if (animationSequenceCoroutine != null)
        {
            StopCoroutine(animationSequenceCoroutine);
            animationSequenceCoroutine = null;
        }

        if (updateTextCoroutine != null)
        {
            StopCoroutine(updateTextCoroutine);
            updateTextCoroutine = null;
        }

        // Reset animator to default state
        if (towerAnimator != null)
        {
            towerAnimator.SetBool(isLightingParam, false);
            towerAnimator.SetBool(isLightedParam, false);

            // Play default state to reset animations
            towerAnimator.Play("Default", -1, 0f);
            towerAnimator.Update(0f);
        }

        // Deactivate visual effects
        if (lightingEffect != null) lightingEffect.SetActive(false);
        if (fullyLitEffect != null) fullyLitEffect.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.SetActive(false);

        // Reset text indicators
        ResetTextScales();
        UpdateEnergyTextIndicators();

        #if UNITY_EDITOR
        Debug.Log($"Tower {gameObject.name} reset complete - Energy: {currentEnergy}, FullyLit: {isFullyLit}, Lighting: {isLighting}");
        #endif
    }

    private void ResetTextScales()
    {
        foreach (var kvp in originalTextScales)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.localScale = kvp.Value;
            }
        }
    }

    // NEW: Force reset method for external calls
    public void ForceReset()
    {
        ResetTower();
        DeactivateTower();
    }

    public void AddTextIndicator(TMP_Text textIndicator)
    {
        if (textIndicator != null && !energyTextIndicators.Contains(textIndicator))
        {
            energyTextIndicators.Add(textIndicator);
            originalTextScales[textIndicator] = textIndicator.transform.localScale;
            UpdateEnergyTextIndicators();
        }
    }

    public void RemoveTextIndicator(TMP_Text textIndicator)
    {
        if (energyTextIndicators.Contains(textIndicator))
        {
            energyTextIndicators.Remove(textIndicator);
            if (originalTextScales.ContainsKey(textIndicator))
            {
                originalTextScales.Remove(textIndicator);
            }
        }
    }

    public void ClearTextIndicators()
    {
        energyTextIndicators.Clear();
        originalTextScales.Clear();
    }
}
