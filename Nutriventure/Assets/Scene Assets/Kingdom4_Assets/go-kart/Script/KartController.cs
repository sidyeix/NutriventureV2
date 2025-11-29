using UnityEngine;
using UnityEngine.InputSystem;

public class KartController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 10f;
    public float turnSpeed = 100f;

    [Header("Optional Mobile Controls")]
    public float mobileVertical;
    public float mobileHorizontal;

    private Rigidbody rb;
    private Vector2 input;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.3f, 0);

        enabled = false; // Disabled until player enters kart
    }

    void Update()
    {
        ReadKeyboardAndGamepadInput();

        float vertical = input.y;
        float horizontal = input.x;

        // Mobile input overrides keyboard/gamepad
        if (mobileVertical != 0) vertical = mobileVertical;
        if (mobileHorizontal != 0) horizontal = mobileHorizontal;

        Move(vertical, horizontal);
    }

    void ReadKeyboardAndGamepadInput()
    {
        Vector2 newInput = Vector2.zero;

        // --- Keyboard movement (WASD / Arrow keys) ---
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) newInput.y = 1;
            if (Keyboard.current.sKey.isPressed) newInput.y = -1;
            if (Keyboard.current.aKey.isPressed) newInput.x = -1;
            if (Keyboard.current.dKey.isPressed) newInput.x = 1;
        }

        // --- Gamepad stick movement ---
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.1f) newInput.x = stick.x;
            if (Mathf.Abs(stick.y) > 0.1f) newInput.y = stick.y;
        }

        input = newInput;
    }

    void Move(float forward, float turn)
    {
        Vector3 moveDir = transform.forward * forward * speed;
        rb.MovePosition(rb.position + moveDir * Time.deltaTime);

        float turnAmount = turn * turnSpeed * Time.deltaTime;
        Quaternion turnRotation = Quaternion.Euler(0, turnAmount, 0);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    public void SetControllable(bool controllable)
    {
        enabled = controllable;

        if (!controllable)
        {
            input = Vector2.zero;
            mobileVertical = 0f;
            mobileHorizontal = 0f;
        }
    }

    // Mobile UI buttons
    public void Mobile_MoveForward(bool isPressed) {
        mobileVertical = isPressed ? 1f : 0f;
    }
    public void Mobile_MoveBackward(bool isPressed) {
        mobileVertical = isPressed ? -1f : 0f;
    }
    public void Mobile_TurnLeft(bool isPressed) {
        mobileHorizontal = isPressed ? -1f : 0f;
    }
    public void Mobile_TurnRight(bool isPressed) {
        mobileHorizontal = isPressed ? 1f : 0f;
    }
}