using UnityEngine;
using UnityEngine.EventSystems;

public class Enerling3DViewer :
    MonoBehaviour,
    IDragHandler,
    IBeginDragHandler
{
    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public Camera renderCamera;

    [Header("Rotation")]
    public float rotationSpeed = 150f;

    private GameObject currentModel;

    // =========================
    // SHOW ENERLING MODEL
    // =========================
    public void ShowEnerling(
        IngredientDatabase.IngredientInfo info)
    {
        if (info == null) return;

        // Destroy old model
        if (currentModel != null)
            Destroy(currentModel);

        if (info.modelPrefab == null)
        {
            Debug.LogWarning(
                $"No model prefab for {info.ingredientName}");
            return;
        }

        // Spawn model
        currentModel =
            Instantiate(
                info.modelPrefab,
                spawnPoint);

        currentModel.transform.localPosition =
            Vector3.zero;

        currentModel.transform.localRotation =
            Quaternion.identity;

        // Apply layer
        SetLayerRecursively(
            currentModel,
            LayerMask.NameToLayer("EnerlingOnly"));

        // Auto fit scale
        AutoScaleModel(currentModel);
    }

    // =========================
    // DRAG ROTATION
    // =========================
    public void OnBeginDrag(
        PointerEventData eventData) { }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (currentModel == null) return;

        float rotX =
            eventData.delta.x *
            rotationSpeed *
            Time.deltaTime;

        currentModel.transform.Rotate(
            Vector3.up,
            -rotX,
            Space.World);
    }

    // =========================
    // AUTO SCALE
    // =========================
    void AutoScaleModel(GameObject model)
    {
        Renderer[] renderers =
            model.GetComponentsInChildren<Renderer>();

        Bounds bounds =
            renderers[0].bounds;

        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        float size =
            Mathf.Max(
                bounds.size.x,
                bounds.size.y,
                bounds.size.z);

        float scale =
            1.5f / size;

        model.transform.localScale =
            Vector3.one * scale;
    }

    // =========================
    // LAYER HELPER
    // =========================
    void SetLayerRecursively(
        GameObject obj,
        int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(
                child.gameObject,
                layer);
        }
    }
}
