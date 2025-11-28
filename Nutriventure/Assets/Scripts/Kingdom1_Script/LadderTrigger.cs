// attach to Ladder GameObject
using UnityEngine;

public class LadderTrigger : MonoBehaviour
{
    public Ladder ladder;
    public GameObject climbUIButton; // assign the UI button GameObject
    private PlayerClimbController playerClimb;

    void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponent<PlayerClimbController>();
        if (pc != null)
        {
            playerClimb = pc;
            climbUIButton.SetActive(true);

            // Optional: set the button's OnClick to call ladder.RequestClimb(playerClimb) via code or inspector
            // If you want to set OnClick in inspector, use UnityEvent and assign Ladder.RequestClimb with the player reference
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerClimbController>() != null)
        {
            climbUIButton.SetActive(false);
            playerClimb = null;
        }
    }
}
