using System.Diagnostics.CodeAnalysis;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class CarPhysics : MonoBehaviour
{
    [Header("Outside Input")]
    [SerializeField][Tooltip("Air density in km/m^3")]float airDensity = 1.29f;
    [SerializeField] float dragCoef = 0.3f;
    [SerializeField] [Tooltip("Frontal area of the car in m^2")]float frontalArea = 2.2f;
    [SerializeField] Vector3 dragForce;

    [Header("Inputs")]
    [SerializeField][Range(0f, 1f)] float throttleValue;
    [SerializeField] float speedChangeThrottleValue = 1f;
    [SerializeField][Range(0f, 1f)] float breakValue;
    [SerializeField] float speedChangeBreakValue = 1f;

    [Header("Engine")]
    [SerializeField] float minEngineTorque = 400;
    [SerializeField] float maxEngineTorque = 500;
    [SerializeField] float minEngineRPM = 1000;
    [SerializeField] float maxEngineRPM = 6000;
    [SerializeField] float RPM;
    [SerializeField] float engineTorque;

    [SerializeField]
    AnimationCurve RPMToTorqueCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0.5f),
        new Keyframe(0.8f, 1, 0, 0), new Keyframe(1, 0, -4, 0));

    [Header("Transmission")]
    [SerializeField] float gearRatio = 2.66f;
    [SerializeField] float differentialRatio = 3.42f;
    [SerializeField][Range(0f, 1f)] float transmissionEfficiency = 0.7f;

    [Header("Wheels")]
    [SerializeField] MyWheelCollider[] wheelColliders;
    [SerializeField] float wheelTorque;
    [SerializeField] float wheelsRPM;


    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float directionInput = Input.GetAxisRaw("Vertical");

        // Delay the inputs to avoid sharp inputs on keyboard
        if (directionInput == 1 && throttleValue < 1)
        {
            throttleValue += speedChangeThrottleValue * Time.deltaTime;
        }
        else if (directionInput <= 0 && throttleValue > 0)
        {
            throttleValue -= speedChangeThrottleValue * Time.deltaTime;
        }

        if (directionInput == -1 && breakValue < 1)
        {
            breakValue += speedChangeBreakValue * Time.deltaTime;
        }
        else if (directionInput >= 0 && breakValue > 0)
        {
            breakValue -= speedChangeBreakValue * Time.deltaTime;
        }

        throttleValue = Mathf.Clamp(throttleValue, 0, 1);
        breakValue = Mathf.Clamp(breakValue, 0, 1);
    }

    private void FixedUpdate()
    {
        // Get the previous frame wheelsRPM
        wheelsRPM = CalculateWheelRPM();

        // Compute the engine torque depending on the wheelsRPM
        RPM = CalculateEngineRPM(wheelsRPM, gearRatio, differentialRatio);
        RPM = Mathf.Clamp(RPM, minEngineRPM, maxEngineRPM);
        engineTorque = GetEngineTorque(RPM, throttleValue);

        // Aerodynamic drag
        dragForce = CalculateDragForce(dragCoef, frontalArea, airDensity, rb.linearVelocity);
        rb.AddForce(dragForce);

        // Give the values to all the wheelColliders
        foreach (var collider in wheelColliders)
        {
            wheelTorque = CalculateWheelTorque(engineTorque, gearRatio, differentialRatio, transmissionEfficiency,
                collider.GetWheelRadius());
            collider.SetWheelTorque(wheelTorque);
            collider.SetBreakValue(breakValue);
        }

        Debug.Log(rb.linearVelocity.magnitude);
    }

    float CalculateEngineRPM(float wheelRPM, float gearRatio, float differentialRatio)
    {
        return wheelRPM * gearRatio * differentialRatio;
    }

    float GetEngineTorque(float RPM, float throttleValue)
    {
        return throttleValue *
               (RPMToTorqueCurve.Evaluate((RPM - minEngineRPM) / (maxEngineRPM - minEngineRPM)) *
                   (maxEngineTorque - minEngineTorque) + minEngineTorque);
    }

    float CalculateWheelTorque(float torqueEngine, float gearRatio, float differentialRatio,
        float transmissionEfficiency, float wheelRadius)
    {
        return torqueEngine * gearRatio * differentialRatio * transmissionEfficiency;
    }

    // Calculate the average rpm for the wheels
    float CalculateWheelRPM()
    {
        float sum = 0;
        int nbDrivenWheel = 0;

        foreach (MyWheelCollider wheel in wheelColliders)
        {
            if (wheel.IsMotorized())
            {
                sum += wheel.GetRPM();
                nbDrivenWheel++;
            }
        }

        return sum / nbDrivenWheel;
    }

    Vector3 CalculateDragForce(float dragCoef, float frontalArea, float airDensity, Vector3 velocity)
    {
        Vector3 forwardVelocity = Vector3.Project(velocity, transform.forward);

        return -0.5f * dragCoef * frontalArea * airDensity * forwardVelocity.magnitude * forwardVelocity;
    }
}