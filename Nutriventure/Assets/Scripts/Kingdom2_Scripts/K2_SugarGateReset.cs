using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to a GameObject with a trigger collider.
/// When the player enters the trigger, all sugar-gate objects
/// snap back to the positions they had at scene start.
/// </summary>
public class K2_SugarGateReset : MonoBehaviour
{
    [Header("Sugar Gate Objects")]
    [Tooltip("Drag every sugar-gate object here. Their positions will be recorded on Start and restored when the player enters this trigger.")]
    [SerializeField] private List<GameObject> sugarGates = new List<GameObject>();

    [Header("Settings")]
    [Tooltip("Tag used to identify the player. Leave as 'Player' unless your player uses a different tag.")]
    [SerializeField] private string playerTag = "Player";

    // Saved transforms (position + rotation) per gate
    private struct SavedTransform
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private readonly List<SavedTransform> savedTransforms = new List<SavedTransform>();

    private void Start()
    {
        SaveOriginalPositions();
    }

    private void SaveOriginalPositions()
    {
        savedTransforms.Clear();

        for (int i = 0; i < sugarGates.Count; i++)
        {
            if (sugarGates[i] != null)
            {
                savedTransforms.Add(new SavedTransform
                {
                    position = sugarGates[i].transform.position,
                    rotation = sugarGates[i].transform.rotation
                });
            }
            else
            {
                // Placeholder so indices stay aligned
                savedTransforms.Add(default);
                Debug.LogWarning($"K2_SugarGateReset: Slot {i} in sugarGates list is null.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        ResetGates();
    }

    /// <summary>
    /// Moves every sugar-gate object back to its saved position and rotation.
    /// Can also be called from other scripts if needed.
    /// </summary>
    public void ResetGates()
    {
        for (int i = 0; i < sugarGates.Count; i++)
        {
            if (sugarGates[i] == null) continue;

            sugarGates[i].transform.position = savedTransforms[i].position;
            sugarGates[i].transform.rotation = savedTransforms[i].rotation;

            // If the gate has a Rigidbody, zero out any velocity so it doesn't drift
            Rigidbody rb = sugarGates[i].GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
