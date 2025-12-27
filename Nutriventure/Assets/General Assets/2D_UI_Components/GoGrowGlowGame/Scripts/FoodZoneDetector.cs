using UnityEngine;

public class FoodZoneDetector : MonoBehaviour
{
    public enum FoodType { Go, Grow, Glow }

    [Header("Zone Settings")]
    public FoodType zoneType = FoodType.Go;

    [Header("UI Settings")]
    public Color sliderFillColor = Color.red;
    public Sprite sliderHandleSprite;

    [Header("Audio")]
    public AudioClip zoneEnterSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EnterZone();
        }
    }

    private void EnterZone()
    {
        if (GoGrowGlowGameManager.Instance != null)
        {
            // Convert our enum to the manager's enum
            GoGrowGlowGameManager.FoodType managerZoneType =
                (GoGrowGlowGameManager.FoodType)zoneType;

            GoGrowGlowGameManager.Instance.SetCurrentFoodZone(
                managerZoneType,
                sliderFillColor,
                sliderHandleSprite
            );
        }

        // Play zone enter sound
        if (zoneEnterSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(zoneEnterSound);
        }

        Debug.Log($"Entered {zoneType} Zone");
    }
}