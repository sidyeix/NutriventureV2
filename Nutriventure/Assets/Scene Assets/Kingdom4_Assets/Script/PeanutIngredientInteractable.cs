using UnityEngine;

public class PeanutIngredientInteractable : IngredientInteractable
{
    private static bool timelinePlayed = false;

    protected override void OnCollected()
    {
        if (timelinePlayed) return;

        if (TimelineManager.Instance != null)
        {
            timelinePlayed = true;
            TimelineManager.Instance.PlayPeanutTimeline();
        }
    }
}
