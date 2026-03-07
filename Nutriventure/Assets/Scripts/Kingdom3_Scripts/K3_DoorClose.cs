using UnityEngine;
using System.Collections;

public class K3_DoorClose : MonoBehaviour
{
  [Header("Door References")]
  [SerializeField] private Transform door1;
  [SerializeField] private Transform door2;

  [Header("Closed Positions (Local)")]
  [SerializeField] private Vector3 door1ClosedPosition = new Vector3(2.480969f, 0f, 0.04f);
  [SerializeField] private Vector3 door2ClosedPosition = new Vector3(-2.479136f, 0f, 0.04f);

  [Header("Settings")]
  [SerializeField] private float closeSpeed = 2f;
  [SerializeField] private bool triggerOnce = true;

  private bool hasClosed = false;
  private Coroutine closeCoroutine;

  // Store initial (open) positions so we can restore them on reset
  private Vector3 door1OpenPosition;
  private Quaternion door1OpenRotation;
  private Vector3 door2OpenPosition;
  private Quaternion door2OpenRotation;
  private bool openPositionsStored = false;

  private void Start()
  {
    StoreOpenPositions();
  }

  private void StoreOpenPositions()
  {
    if (door1 != null)
    {
      door1OpenPosition = door1.localPosition;
      door1OpenRotation = door1.localRotation;
    }
    if (door2 != null)
    {
      door2OpenPosition = door2.localPosition;
      door2OpenRotation = door2.localRotation;
    }
    openPositionsStored = true;
  }

  private void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("Player") && (!triggerOnce || !hasClosed))
    {
      hasClosed = true;
      if (closeCoroutine != null)
        StopCoroutine(closeCoroutine);
      closeCoroutine = StartCoroutine(CloseDoors());
    }
  }

  private IEnumerator CloseDoors()
  {
    Quaternion targetRotation = Quaternion.identity;
    bool door1Done = door1 == null;
    bool door2Done = door2 == null;

    while (!door1Done || !door2Done)
    {
      float step = closeSpeed * Time.deltaTime;

      if (!door1Done)
      {
        door1.localPosition = Vector3.MoveTowards(door1.localPosition, door1ClosedPosition, step);
        door1.localRotation = Quaternion.RotateTowards(door1.localRotation, targetRotation, closeSpeed * 90f * Time.deltaTime);
        if (Vector3.Distance(door1.localPosition, door1ClosedPosition) < 0.001f)
        {
          door1.localPosition = door1ClosedPosition;
          door1.localRotation = targetRotation;
          door1Done = true;
        }
      }

      if (!door2Done)
      {
        door2.localPosition = Vector3.MoveTowards(door2.localPosition, door2ClosedPosition, step);
        door2.localRotation = Quaternion.RotateTowards(door2.localRotation, targetRotation, closeSpeed * 90f * Time.deltaTime);
        if (Vector3.Distance(door2.localPosition, door2ClosedPosition) < 0.001f)
        {
          door2.localPosition = door2ClosedPosition;
          door2.localRotation = targetRotation;
          door2Done = true;
        }
      }

      yield return null;
    }

#if UNITY_EDITOR
        Debug.Log("K3_DoorClose: Doors closed.");
#endif
  }

  /// <summary>
  /// Fully resets the doors back to their open state.
  /// </summary>
  public void ResetDoor()
  {
    if (closeCoroutine != null)
    {
      StopCoroutine(closeCoroutine);
      closeCoroutine = null;
    }

    hasClosed = false;

    if (openPositionsStored)
    {
      if (door1 != null)
      {
        door1.localPosition = door1OpenPosition;
        door1.localRotation = door1OpenRotation;
      }
      if (door2 != null)
      {
        door2.localPosition = door2OpenPosition;
        door2.localRotation = door2OpenRotation;
      }
    }
  }
}
