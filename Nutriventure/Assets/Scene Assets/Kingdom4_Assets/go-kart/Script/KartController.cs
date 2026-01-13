using UnityEngine;
using UnityEngine.InputSystem;

public class KartController : MonoBehaviour
{
    public float speed = 10f;
    public float turnSpeed = 100f;
    public float autoBrakeDistance = 3f;
    public float stopDistance = 2f;

    public bool useTriggerDetection = true;
    public string destinationTag = "Destination";

    public Transform destination;
    public bool hasDestination = false;
    public bool autoExitOnArrival = true;
    public float autoExitDelay = 2f;

    public float mobileVertical;
    public float mobileHorizontal;

    private Rigidbody rb;
    private Vector2 input;
    private bool isAutoMoving = true;
    private float currentSpeed = 0f;
    private bool isStopped = false;
    private bool hasArrived = false;
    private bool hasTriggered = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.3f, 0);
        currentSpeed = speed;
        enabled = false;
        hasTriggered = false;
    }

    void Update()
    {
        ReadKeyboardAndGamepadInput();

        float vertical = input.y;
        float horizontal = input.x;

        if (mobileVertical != 0) vertical = mobileVertical;
        if (mobileHorizontal != 0) horizontal = mobileHorizontal;

        if (isAutoMoving && !isStopped && !hasArrived)
        {
            vertical = 1f;
        }

        if (hasDestination && destination != null && !hasArrived)
        {
            if (useTriggerDetection)
            {
                currentSpeed = speed;

                float distanceToDestination = Vector3.Distance(transform.position, destination.position);
                if (distanceToDestination <= autoBrakeDistance * 1.5f)
                {
                    AutoSteerToDestination();
                }
            }
            else
            {
                HandleDestination();
            }
        }

        Move(vertical, horizontal);
    }

    void HandleDestination()
    {
        if (isStopped || hasArrived) return;

        float distanceToDestination = Vector3.Distance(transform.position, destination.position);

        if (distanceToDestination <= stopDistance)
        {
            ArrivedAtDestination();
            return;
        }

        if (distanceToDestination <= autoBrakeDistance)
        {
            float brakeFactor = Mathf.Clamp01(distanceToDestination / autoBrakeDistance);
            currentSpeed = speed * brakeFactor;

            if (distanceToDestination <= autoBrakeDistance * 1.5f)
            {
                AutoSteerToDestination();
            }
        }
        else
        {
            currentSpeed = speed;
        }
    }

    void AutoSteerToDestination()
    {
        if (destination == null) return;

        Vector3 directionToDest = (destination.position - transform.position).normalized;
        directionToDest.y = 0;

        float angle = Vector3.SignedAngle(transform.forward, directionToDest, Vector3.up);
        float autoSteer = Mathf.Clamp(angle / 45f, -1f, 1f) * 0.5f;

        float playerInput = input.x;
        if (mobileHorizontal != 0) playerInput = mobileHorizontal;

        if (Mathf.Abs(playerInput) < 0.1f)
        {
            input.x = autoSteer;
        }
        else
        {
            input.x = Mathf.Clamp(playerInput + autoSteer * 0.3f, -1f, 1f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useTriggerDetection || hasArrived || !hasDestination) return;

        if (destination != null && other.transform == destination)
        {
            ArrivedAtDestination();
            hasTriggered = true;
            return;
        }

        if (!string.IsNullOrEmpty(destinationTag) && other.CompareTag(destinationTag))
        {
            if (destination != null && other.transform == destination)
            {
                ArrivedAtDestination();
                hasTriggered = true;
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!useTriggerDetection || hasArrived || !hasDestination || hasTriggered) return;

        if (destination != null && other.transform == destination)
        {
            ArrivedAtDestination();
            hasTriggered = true;
        }
    }

    void ArrivedAtDestination()
    {
        if (hasArrived) return;

        hasArrived = true;
        isStopped = true;
        currentSpeed = 0f;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        SetControllable(false);

        if (autoExitOnArrival)
        {
            StartCoroutine(AutoExitAfterDelay(autoExitDelay));
        }
    }

    System.Collections.IEnumerator AutoExitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        KartTrigger kartTrigger = FindAnyObjectByType<KartTrigger>();
        if (kartTrigger != null)
        {
            kartTrigger.AutoExitKart();
        }
    }

    void ReadKeyboardAndGamepadInput()
    {
        Vector2 newInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) newInput.x = -1;
            if (Keyboard.current.dKey.isPressed) newInput.x = 1;

            if (Keyboard.current.spaceKey.isPressed)
            {
                currentSpeed = 0f;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                ManualExit();
            }
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.1f) newInput.x = stick.x;

            if (Gamepad.current.buttonSouth.isPressed)
            {
                currentSpeed = 0f;
            }

            if (Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                ManualExit();
            }
        }

        input = newInput;
    }

    void ManualExit()
    {
        if (hasArrived) return;

        KartTrigger kartTrigger = FindAnyObjectByType<KartTrigger>();
        if (kartTrigger != null)
        {
            kartTrigger.ExitKart();
        }
    }

    void Move(float forward, float turn)
    {
        Vector3 moveDir = transform.forward * forward * currentSpeed;
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
            currentSpeed = 0f;
            isStopped = true;
            hasTriggered = false;
        }
        else
        {
            currentSpeed = speed;
            isStopped = false;
            hasArrived = false;
            hasTriggered = false;
            isAutoMoving = true;
        }
    }

    public void SetDestination(Transform newDestination)
    {
        destination = newDestination;
        hasDestination = true;
        hasArrived = false;
        hasTriggered = false;
        isStopped = false;
        currentSpeed = speed;
    }

    public void ClearDestination()
    {
        hasDestination = false;
        hasArrived = false;
        hasTriggered = false;
        isStopped = false;
        currentSpeed = speed;
    }

    public void Mobile_TurnLeft(bool isPressed)
    {
        mobileHorizontal = isPressed ? -1f : 0f;
    }

    public void Mobile_TurnRight(bool isPressed)
    {
        mobileHorizontal = isPressed ? 1f : 0f;
    }

    public void Mobile_Brake(bool isPressed)
    {
        if (isPressed)
        {
            currentSpeed = 0f;
        }
        else if (!isStopped && !hasArrived)
        {
            currentSpeed = speed;
        }
    }

    public void Mobile_ManualExit()
    {
        ManualExit();
    }

    public bool HasArrived => hasArrived;
    public Transform CurrentDestination => destination;
    public bool UseTriggerDetection => useTriggerDetection;

    void OnDrawGizmosSelected()
    {
        if (hasDestination && destination != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destination.position);

            if (!useTriggerDetection)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(destination.position, stopDistance);

                Gizmos.color = new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(destination.position, autoBrakeDistance);
            }
        }
    }
}
