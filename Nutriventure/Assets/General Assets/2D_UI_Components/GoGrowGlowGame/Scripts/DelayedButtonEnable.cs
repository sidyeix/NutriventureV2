using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Attach to a Button. Every time the GameObject becomes active,
/// the button is non-interactable for <see cref="delay"/> seconds,
/// then automatically becomes interactable.
/// </summary>
public class DelayedButtonEnable : MonoBehaviour
{
  [Tooltip("Seconds to wait before the button becomes interactable.")]
  public float delay = 1.5f;

  private Button button;

  private void Awake()
  {
    button = GetComponent<Button>();
  }

  private void OnEnable()
  {
    if (button != null)
    {
      button.interactable = false;
      StartCoroutine(EnableAfterDelay());
    }
  }

  private void OnDisable()
  {
    StopAllCoroutines();
  }

  private IEnumerator EnableAfterDelay()
  {
    yield return new WaitForSecondsRealtime(delay);
    if (button != null)
      button.interactable = true;
  }
}
