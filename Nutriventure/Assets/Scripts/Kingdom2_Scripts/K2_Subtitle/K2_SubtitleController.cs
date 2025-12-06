using UnityEngine;
using TMPro;
using System.Collections;

public class K2_SubtitleController : MonoBehaviour
{
    public TextMeshProUGUI subtitleTextUI;
    private Coroutine typingRoutine;

    public void ShowSubtitle(string text, float speed)
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeText(text, speed));
    }

    IEnumerator TypeText(string text, float speed)
    {
        subtitleTextUI.text = "";

        foreach (char c in text)
        {
            subtitleTextUI.text += c;
            yield return new WaitForSeconds(speed);
        }
    }

    public void ClearSubtitle()
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        subtitleTextUI.text = "";
    }
}
