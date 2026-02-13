#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarPhysics : MonoBehaviour
{
    #region OutsideInput
    [Header("Outside Input")]
    [SerializeField, Tooltip("Air density in km/m^3")] 
    float airDensity = 1.29f;
    [SerializeField] 
    float dragCoef = 0.3f;
    [SerializeField, Tooltip("Frontal area of the car in m^2")] 
    float frontalArea = 2.2f;
    [SerializeField] 
    Vector3 dragForce;
    #endregion

    #region Inputs
    [Header("Inputs")]
    [SerializeField, Range(0f, 1f)] 
    float throttleValue;
    [SerializeField] 
    float speedChangeThrottleValue = 1f;
    [SerializeField, Range(0f, 1f)] 
    float breakValue;
    [SerializeField] 
    float speedChangeBreakValue = 1f;
    #endregion

    #region Engine
    [Header("Engine")]
    [SerializeField] 
    float minEngineTorque = 400;
    [SerializeField] 
    float maxEngineTorque = 500;
    [SerializeField] 
    float minEngineRPM = 1000;
    [SerializeField] 
    float maxEngineRPM = 6000;
    [SerializeField] 
    float RPM;
    [SerializeField] 
    float engineTorque;

    [SerializeField]
    AnimationCurve RPMToTorqueCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0.5f),
        new Keyframe(0.8f, 1, 0, 0), new Keyframe(1, 0, -4, 0));
    #endregion

    #region Transmission
    [Header("Transmission")]
    [SerializeField] 
    float gearRatio = 2.66f;
    [SerializeField] 
    float differentialRatio = 3.42f;
    [SerializeField, Range(0f, 1f)] 
    float transmissionEfficiency = 0.7f;
    #endregion

    #region Wheels
    [Header("Wheels")]
    [SerializeField] 
    MyWheelCollider[] wheelColliders;
    [SerializeField] 
    float wheelTorque;
    [SerializeField] 
    float wheelsRPM;

    Vector3 wheelBase;
    #endregion


    Vector3 lastFrameVelocity = Vector3.zero;
    Vector3 lastFrameAcceleration = Vector3.zero;
    Rigidbody rb;

    public bool debug = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        CalculateWheelBase();
    }

    private void Update()
    {
        // Debug Reverse Input
        if (Input.GetKeyDown(KeyCode.R))
        {
            gearRatio *= -1;
        }

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
        CalculateLastFrameAcceleration();

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
            Vector3 wheelPosLocal = collider.transform.localPosition- rb.centerOfMass;

            float loadSupported = CalculateWheelLoadSupported(wheelPosLocal);
            collider.SetLoadSupported(loadSupported);

            wheelTorque = CalculateWheelTorque(engineTorque, gearRatio, differentialRatio, transmissionEfficiency);
            collider.SetWheelTorque(wheelTorque);
            collider.SetBreakValue(breakValue);
        }

        // Display speed debug waiting for ui (km/h)
        Debug.Log(rb.linearVelocity.magnitude * 3.6f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!debug)
        {
            return;
        }
        UnityEngine.Color color = Handles.color;

        // Acceleration arrow
        Handles.color = UnityEngine.Color.red;
        Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(lastFrameAcceleration, transform.up), lastFrameAcceleration.magnitude * 1000, EventType.Repaint);

        // Velocity arrow
        Handles.color = UnityEngine.Color.blue;
        if (rb)
        {
            Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(rb.linearVelocity, transform.up), rb.linearVelocity.magnitude * 1000, EventType.Repaint);
        }
        Handles.color = color;
    }
#endif

    void CalculateLastFrameAcceleration()
    {
        Vector3 velocityChange = rb.linearVelocity - lastFrameVelocity;

        lastFrameAcceleration = velocityChange / Time.fixedDeltaTime;

        lastFrameVelocity = rb.linearVelocity;
    }

    float CalculateEngineRPM(float wheelRPM, float gearRatio, float differentialRatio)
    {
        // Get the engine RPM with the wheel rpm to complete the loop
        return wheelRPM * gearRatio * differentialRatio;
    }

    float GetEngineTorque(float RPM, float throttleValue)
    {
        return throttleValue *
               (RPMToTorqueCurve.Evaluate((RPM - minEngineRPM) / (maxEngineRPM - minEngineRPM)) *
                   (maxEngineTorque - minEngineTorque) + minEngineTorque);
    }

    float CalculateWheelTorque(float torqueEngine, float gearRatio, float differentialRatio,
        float transmissionEfficiency)
    {
        return (torqueEngine * gearRatio * differentialRatio * transmissionEfficiency)/2f;
    }

    /// <summary>
    /// Calculate the average rpm for the motorized wheels
    /// </summary>
    float CalculateWheelRPM()
    {
        float sum = 0;
        int nbMotorizedWheel = 0;

        foreach (MyWheelCollider wheel in wheelColliders)
        {
            if (wheel.IsMotorized())
            {
                sum += wheel.GetRPM();
                nbMotorizedWheel++;
            }
        }

        return sum / nbMotorizedWheel;
    }

    /// <summary>
    /// Calculate the air resistance of the car (only work for front resistance for now, need to change it later)
    /// </summary>
    Vector3 CalculateDragForce(float dragCoef, float frontalArea, float airDensity, Vector3 velocity)
    {
        Vector3 forwardVelocity = Vector3.Project(velocity, transform.forward);

        return -0.5f * dragCoef * frontalArea * airDensity * forwardVelocity.magnitude * forwardVelocity;
    }

    /// <summary>
    /// Calculate the load that each wheel support (may be broken to be investigated)
    /// </summary>
    /// <param name="wheelPos"></param>
    /// <returns></returns>
    float CalculateWheelLoadSupported(Vector3 wheelPos)
    {
        float heightCG = GetCOMHeight();

        float totalWeight = rb.mass * Physics.gravity.magnitude;

        float axleLoad = totalWeight * (Mathf.Abs(wheelPos.z) / wheelBase.z);

        float wheelLoad = axleLoad * (Mathf.Abs(wheelPos.x) / wheelBase.x);

        Vector3 localAcceleration = transform.InverseTransformDirection(lastFrameAcceleration);

        float dF_long = (rb.mass * localAcceleration.z * heightCG) / wheelBase.z;
        float dF_lat = (rb.mass * localAcceleration.x * heightCG) / wheelBase.x;

        wheelLoad += Mathf.Sign(-wheelPos.z) * dF_long * 0.5f;

        wheelLoad += Mathf.Sign(wheelPos.x) * dF_lat * 0.5f;

        return wheelLoad;
    }


    /// <summary>
    /// Calculate the Wheel Base (The distance between the extremum wheels)
    /// </summary>
    void CalculateWheelBase()
    {
        Vector3 frontMaxWheelPos = Vector3.zero;
        Vector3 rearMaxWheelPos = Vector3.zero;
        Vector3 leftMaxWheelPos = Vector3.zero;
        Vector3 rightMaxWheelPos = Vector3.zero;

        bool first = true;

        foreach (var control in wheelColliders)
        {
            Vector3 pos = transform.InverseTransformPoint(control.GetWheelTransformPosition()) - rb.centerOfMass;

            if (first)
            {
                frontMaxWheelPos = pos;
                rearMaxWheelPos = pos;
                leftMaxWheelPos = pos;
                rightMaxWheelPos = pos;

                first = false;
            }

            if (pos.z > frontMaxWheelPos.z)
            {
                frontMaxWheelPos = pos;
            }

            if (pos.z < rearMaxWheelPos.z)
            {
                rearMaxWheelPos = pos;
            }

            if (pos.x > rightMaxWheelPos.x)
            {
                rightMaxWheelPos = pos;
            }

            if (pos.x < leftMaxWheelPos.x)
            {
                leftMaxWheelPos = pos;
            }
        }

        wheelBase = new Vector3(rightMaxWheelPos.x - leftMaxWheelPos.x, 0, frontMaxWheelPos.z - rearMaxWheelPos.z);
    }

    float GetCOMHeight()
    {
        // Convert COM to car local space
        Vector3 comLocal = rb.centerOfMass;

        // Find lowest wheel contact in car local space
        float lowestYLocal = float.MaxValue;

        foreach (var wc in wheelColliders)
        {
            if (wc.GetLowestPoint(out Vector3 lowestPoint))
            {
                // Convert wheel contact point to car local space
                Vector3 contactLocal = lowestPoint - transform.position;
                lowestYLocal = Mathf.Min(lowestYLocal, contactLocal.y);
            }
        }

        // COM height relative to lowest wheel contact
        float h = comLocal.y - lowestYLocal;

        return h;
    }
}