using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class NPCChallengeFoodDefinition : MonoBehaviour
{
    [Header("Food Categories")]
    [Tooltip("A food can belong to one or more allergen categories.")]
    public List<NPCAllergenCategory> categories = new List<NPCAllergenCategory>();

    [Header("Prefab Child References")]
    public CinemachineVirtualCamera virtualCamera;
    public GameObject redShield;
    public GameObject greenShield;
    [Tooltip("Optional visual shown when this food has already been judged.")]
    public GameObject answeredMarker;
    public Collider targetCollider;

    [Header("Camera Settings")]
    public int observeCameraPriority = 100;

    private int originalCameraPriority;
    private bool originalPriorityCached;
    private bool isAnsweredLocked;

    public bool IsAnsweredLocked => isAnsweredLocked;

    public bool HasAnyMatchingCategory(HashSet<NPCAllergenCategory> npcAllergies)
    {
        if (npcAllergies == null || npcAllergies.Count == 0 || categories == null || categories.Count == 0)
            return false;

        for (int i = 0; i < categories.Count; i++)
        {
            if (npcAllergies.Contains(categories[i]))
                return true;
        }

        return false;
    }

    public void SetObserveCameraActive(bool active)
    {
        EnsureReferences();

        if (virtualCamera == null)
            return;

        if (!originalPriorityCached)
        {
            originalCameraPriority = virtualCamera.Priority;
            originalPriorityCached = true;
        }

        virtualCamera.Priority = active ? observeCameraPriority : originalCameraPriority;
    }

    public void ShowGreenShield()
    {
        EnsureReferences();
        if (greenShield != null)
            greenShield.SetActive(true);
    }

    public void ShowRedShield()
    {
        EnsureReferences();
        if (redShield != null)
            redShield.SetActive(true);
    }

    public void ResetVisualState()
    {
        EnsureReferences();

        if (redShield != null)
            redShield.SetActive(false);

        if (greenShield != null)
            greenShield.SetActive(false);

        if (answeredMarker != null)
            answeredMarker.SetActive(false);

        isAnsweredLocked = false;

        if (virtualCamera != null)
        {
            if (!originalPriorityCached)
            {
                originalCameraPriority = virtualCamera.Priority;
                originalPriorityCached = true;
            }

            virtualCamera.Priority = originalCameraPriority;
        }
    }

    public void SetAnsweredLock(bool locked)
    {
        EnsureReferences();

        isAnsweredLocked = locked;

        if (answeredMarker != null)
            answeredMarker.SetActive(locked);
    }

    private void EnsureReferences()
    {
        if (virtualCamera == null)
            virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);

        if (targetCollider == null)
            targetCollider = GetComponentInChildren<Collider>(true);

        if (redShield == null)
        {
            Transform t = transform.Find("RedShield");
            if (t != null)
                redShield = t.gameObject;
        }

        if (greenShield == null)
        {
            Transform t = transform.Find("GreenShield");
            if (t != null)
                greenShield = t.gameObject;
        }
    }
}
