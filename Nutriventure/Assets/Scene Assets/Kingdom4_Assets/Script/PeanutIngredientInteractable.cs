using UnityEngine;

public class PeanutIngredientInteractable : IngredientInteractable
{
    private static bool timelinePlayed = false;

    public override void Pickup()
    {
        if (!timelinePlayed && TimelineManager.Instance != null)
        {
            timelinePlayed = true;
            TimelineManager.Instance.PlayPeanutTimeline();
        }

        base.Pickup(); // Ingredient can now safely disappear
    }
}
