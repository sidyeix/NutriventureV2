using UnityEngine;
using System.Collections.Generic;

public class LookUIRaycastController : MonoBehaviour
{
    public static LookUIRaycastController Instance;

    private CanvasGroup canvasGroup;
    private HashSet<object> activeRequests = new HashSet<object>();

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // Call when something wants world touch
    public void RequestWorldTouch(object requester)
    {
        activeRequests.Add(requester);
        UpdateState();
    }

    // Call when interaction ends
    public void ReleaseWorldTouch(object requester)
    {
        activeRequests.Remove(requester);
        UpdateState();
    }

    private void UpdateState()
    {
        // If ANYONE requests world touch → allow it
        canvasGroup.blocksRaycasts = activeRequests.Count == 0;
    }
}
