using UnityEngine;

public class FloatingAnimation : MonoBehaviour
{
    [HideInInspector] public float amplitude;
    [HideInInspector] public float speed;
    [HideInInspector] public float phaseOffset;

    private Vector3 startPos;

    void Awake()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float yOffset = Mathf.Sin((Time.time * speed) + phaseOffset) * amplitude;
        transform.localPosition = startPos + Vector3.up * yOffset;
    }
}
