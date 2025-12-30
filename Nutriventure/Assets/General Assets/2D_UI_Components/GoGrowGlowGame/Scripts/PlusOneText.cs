using TMPro;
using UnityEngine;

public class PlusOneText : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float floatSpeed = 50f;
    [SerializeField] private float fadeSpeed = 1f;
    [SerializeField] private float scaleSpeed = 2f;
    [SerializeField] private float maxScale = 1.3f;
    [SerializeField] private float lifeTime = 1.5f;

    private TMP_Text textComponent;
    private float timer = 0f;
    private Vector3 initialPosition;
    private Color initialColor;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        initialPosition = transform.position;
        initialColor = textComponent.color;

        // Ensure proper rendering
        if (GetComponent<Canvas>() == null)
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 999; // Very high to be on top
        }
    }

    private void Start()
    {
        // Initial scale effect
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Float upward
        transform.position = initialPosition + Vector3.up * floatSpeed * timer;

        // Scale in then out
        float scaleProgress = timer * scaleSpeed;
        if (scaleProgress < 1f)
        {
            // Scale up
            float scale = Mathf.Lerp(0, maxScale, scaleProgress);
            transform.localScale = Vector3.one * scale;
        }
        else
        {
            // Scale down
            float scale = Mathf.Lerp(maxScale, 0, (scaleProgress - 1f) * 0.5f);
            transform.localScale = Vector3.one * scale;
        }

        // Fade out
        float alpha = Mathf.Lerp(1, 0, timer * fadeSpeed);
        textComponent.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);

        // Destroy when lifetime ends
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    // Optional: Add a color effect
    public void SetColor(Color color)
    {
        if (textComponent != null)
        {
            initialColor = color;
            textComponent.color = color;
        }
    }
}