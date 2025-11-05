using Unity.VisualScripting;
using UnityEngine;

public class RealisticCarController : MonoBehaviour
{
    [SerializeField]
    WheelControl[] wheelControls;

    [SerializeField]
    float wheelTorque = 200f;

    [SerializeField]
    float steeringMax = 30f;

    float nbMotorizedWheels = 0;

    private void Start()
    {
        foreach (var wheelControl in wheelControls)
        {
            if (wheelControl.motorized)
            {
                nbMotorizedWheels++;
            }
        }
    }

    private void FixedUpdate()
    {
        foreach (var wheel in wheelControls)
        {
            if (wheel.motorized)
            {
                wheel.WheelCollider.motorTorque = (wheelTorque / nbMotorizedWheels) * Input.GetAxis("Vertical");
            }

            if (wheel.steerable)
            {
                if (Input.GetAxis("Horizontal") >0)
                {

                }
                wheel.WheelCollider.steerAngle = Input.GetAxis("Horizontal") * steeringMax;
            }
        }
    }
}
