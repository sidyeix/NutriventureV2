using UnityEngine;

public class KartController : MonoBehaviour
{
    public float motorForce = 1500f;
    public float steerAngle = 30f;

    public WheelCollider FL;
    public WheelCollider FR;
    public WheelCollider RL;
    public WheelCollider RR;

    void FixedUpdate()
    {
        float forward = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");

        FL.steerAngle = turn * steerAngle;
        FR.steerAngle = turn * steerAngle;

        RL.motorTorque = forward * motorForce;
        RR.motorTorque = forward * motorForce;
    }
}
