using UnityEngine;

public class Ladder : MonoBehaviour
{
    public Transform ladderStart; // bottom anchor
    public Transform ladderTop;   // top anchor

    // This could be called by your UI Climb button, or by detecting player in trigger
    public void RequestClimb(PlayerClimbController playerClimb)
    {
        if (playerClimb == null) return;
        playerClimb.OnClimbPressed(ladderStart, ladderTop);
    }
}
