using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

[System.Serializable]
public class ButtonPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [System.Serializable]
    public class ButtonEvent : UnityEvent { }

    public ButtonEvent onButtonPressed = new ButtonEvent();
    public ButtonEvent onButtonReleased = new ButtonEvent();
    public ButtonEvent onButtonHeld = new ButtonEvent();

    private bool isPressed = false;
    private Coroutine holdCoroutine;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isPressed) return;

        isPressed = true;
        onButtonPressed?.Invoke();

        if (holdCoroutine != null)
            StopCoroutine(holdCoroutine);
        holdCoroutine = StartCoroutine(HoldDetectionRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;

        isPressed = false;
        onButtonReleased?.Invoke();

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPressed)
        {
            isPressed = false;
            onButtonReleased?.Invoke();

            if (holdCoroutine != null)
            {
                StopCoroutine(holdCoroutine);
                holdCoroutine = null;
            }
        }
    }

    private IEnumerator HoldDetectionRoutine()
    {
        yield return new WaitForSeconds(0.1f);

        while (isPressed)
        {
            onButtonHeld?.Invoke();
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void OnDisable()
    {
        if (isPressed)
        {
            isPressed = false;
            onButtonReleased?.Invoke();

            if (holdCoroutine != null)
            {
                StopCoroutine(holdCoroutine);
                holdCoroutine = null;
            }
        }
    }
}