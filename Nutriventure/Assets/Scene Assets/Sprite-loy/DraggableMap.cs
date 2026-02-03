using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableMap : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    // REQUIRED by IBeginDragHandler
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Optional: bring map to front
        rectTransform.SetAsLastSibling();
    }

    // REQUIRED by IDragHandler
public void OnDrag(PointerEventData eventData)
{
    rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    ClampToViewport();
}

void ClampToViewport()
{
    RectTransform viewport = rectTransform.parent as RectTransform;

    Vector3[] mapCorners = new Vector3[4];
    Vector3[] viewCorners = new Vector3[4];

    rectTransform.GetWorldCorners(mapCorners);
    viewport.GetWorldCorners(viewCorners);

    Vector3 offset = Vector3.zero;

    // Left
    if (mapCorners[0].x > viewCorners[0].x)
        offset.x = viewCorners[0].x - mapCorners[0].x;

    // Right
    if (mapCorners[2].x < viewCorners[2].x)
        offset.x = viewCorners[2].x - mapCorners[2].x;

    // Bottom
    if (mapCorners[0].y > viewCorners[0].y)
        offset.y = viewCorners[0].y - mapCorners[0].y;

    // Top
    if (mapCorners[2].y < viewCorners[2].y)
        offset.y = viewCorners[2].y - mapCorners[2].y;

    rectTransform.position += offset;
}


}
