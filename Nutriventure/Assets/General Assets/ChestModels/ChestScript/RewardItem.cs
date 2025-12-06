using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RewardItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Image rewardIcon;
    public TextMeshProUGUI rewardNameText;
    public TextMeshProUGUI rewardAmountText;
    public CanvasGroup canvasGroup;

    // ADD THIS: Background image component
    public Image backgroundImage;

    [Header("Effects")]
    public GameObject popEffectPrefab;
    public float appearDuration = 0.5f;

    private void Start()
    {
        // Start invisible
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    public void SetReward(string rewardName, string rewardType, int amount, Sprite icon = null, Sprite background = null)
    {
        // Set reward name
        if (rewardNameText != null)
            rewardNameText.text = rewardName;

        // Set reward amount
        if (rewardAmountText != null)
            rewardAmountText.text = "x" + amount;

        // Set reward icon
        if (rewardIcon != null)
        {
            if (icon != null)
            {
                // Use the icon from database
                rewardIcon.sprite = icon;
            }
            else
            {
                // Fallback: Use icon based on type
                SetIconByType(rewardType);
            }
        }

        // SET BACKGROUND IMAGE
        if (backgroundImage != null)
        {
            if (background != null)
            {
                backgroundImage.sprite = background;
                backgroundImage.gameObject.SetActive(true);
            }
            else
            {
                // Hide background if none provided
                backgroundImage.gameObject.SetActive(false);
            }
        }
    }

    private void SetIconByType(string rewardType)
    {
        if (rewardIcon == null) return;

        // Fallback icons if no icon provided
        switch (rewardType.ToLower())
        {
            case "coin":
                rewardIcon.sprite = Resources.Load<Sprite>("Icons/CoinIcon");
                break;
            case "gem":
                rewardIcon.sprite = Resources.Load<Sprite>("Icons/GemIcon");
                break;
            case "key":
                rewardIcon.sprite = Resources.Load<Sprite>("Icons/KeyIcon");
                break;
            case "powerup":
                rewardIcon.sprite = Resources.Load<Sprite>("Icons/PowerUpIcon");
                break;
            default:
                rewardIcon.sprite = Resources.Load<Sprite>("Icons/DefaultIcon");
                break;
        }
    }

    public void ShowReward()
    {
        StartCoroutine(AnimateAppearance());
    }

    IEnumerator AnimateAppearance()
    {
        // Play pop sound effect using AudioHandler
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayRewardPop();
        }

        // Create pop effect
        if (popEffectPrefab != null)
        {
            GameObject popEffect = Instantiate(popEffectPrefab, transform);
            popEffect.transform.localPosition = Vector3.zero;

            // Auto-destroy pop effect after it finishes
            ParticleSystem ps = popEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(popEffect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(popEffect, 2f);
            }
        }

        // Fade in
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < appearDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / appearDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
    }

    public Vector3 GetWorldPosition()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.position;
        }
        return transform.position;
    }
}