#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class MyWheelCollider : MonoBehaviour
{
    #region State
    [Header("State")]
    [SerializeField] 
    bool steerable = false;
    [SerializeField] 
    bool motorized = false;
    [SerializeField] 
    bool breakable = true;
    #endregion

    #region Suspension
    [Header("Suspension")]
    [SerializeField] 
    private float minSpringLength = 0.05f; // Recomended to not put 0 as the min distance
    [SerializeField] 
    private float springLength = 1f;
    [SerializeField, Range(0f, 1f)] 
    private float restRatio = 0.5f;
    [SerializeField] 
    private float springStrength = 30000f;
    [SerializeField] 
    private float damperStrength = 4500f;
    private float restLength;
    private float currentLength;
    #endregion

    #region Wheel
    [Header("Wheel")]
    [SerializeField] 
    private float wheelMass = 30f;
    [SerializeField] 
    private float wheelRadius = 0.25f;
    [SerializeField] 
    private Transform wheelTransform; // Visual wheel
    [SerializeField] 
    private float maxAngleSteer = 30f;
    [SerializeField] 
    float tireGripFactor = 1;
    [SerializeField]
    AnimationCurve tireGripCurve = new AnimationCurve(new Keyframe(0, 0.6f, 0, -0.3f), new Keyframe(0.3f, 0.5f, -0.3f, 0),
        new Keyframe(0.5f, 0.1f, 0, 0), new Keyframe(1, 0.05f, 0, 0));
    [SerializeField] 
    float wheelTorque = 0f;
    public bool debugWheel = false;
    [SerializeField] 
    float maxBreakTorque = 100f;

    [System.Serializable]
    public class TireCoefficients
    {
        [Header("Shape Factor")]
        [Tooltip("Shape factor - controls the shape of the curve")]
        public float C = 1.3f;

        [Header("Peak Factor")]
        [Tooltip("Peak factor - determines the maximum force")]
        public float D = 1.0f;

        [Header("Stiffness Factor")]
        [Tooltip("BCD - product that represents cornering stiffness")]
        public float BCD = 10.0f;

        [Header("Curvature Factor")]
        [Tooltip("Curvature factor - affects curve shape")]
        public float E = 0.97f;

        [Header("Horizontal Shift")]
        [Tooltip("Horizontal offset")]
        public float Sh = 0.0f;

        [Header("Vertical Shift")]
        [Tooltip("Vertical offset")]
        public float Sv = 0.0f;
    }

    [Header("Tire Parameters")]
    public TireCoefficients lateralCoefficients;
    public TireCoefficients longitudinalCoefficients;

    Transform tireTransform;
    private Vector3 wheelVisualStartLocalPos;
    Vector3 wheelVisualStartRotation;
    float wheelRPM = 0;
    bool grounded = false;
    Vector3 lastFramePos = Vector3.zero;
    Vector3 tireVel;
    #endregion

    #region Other
    [Header("Vehicle")]
    [SerializeField] 
    private Rigidbody carRigidbody;
    float breakValue = 0f;
    private float angleSteer = 0f;
    RaycastHit hit;
    bool hasRaycastHit;
    float loadSupported = 0f;
    float upForce;
    float rightForce;
    float forwardForce;
    #endregion

    private void Awake()
    {
        restLength = springLength * restRatio;
        currentLength = restLength;

        // Create a gameobject to represent the tire transform (used to separate visual and physics calculations
        GameObject tireGo = new GameObject();
        tireTransform = tireGo.transform;
        tireTransform.position = wheelTransform.position;
        tireTransform.rotation = transform.rotation;
        tireTransform.SetParent(transform.parent);

        lastFramePos = tireTransform.position;

        if (wheelTransform != null)
        {
            wheelVisualStartLocalPos = wheelTransform.localPosition + Vector3.up * restLength; // save starting position
            wheelVisualStartRotation = wheelTransform.localRotation.eulerAngles;
        }
        else
        {
            Debug.LogError("No wheelTransform (visual wheel) has been assigned!");
        }
    }

    private void FixedUpdate()
    {
        CalculateTireVelocity();
        CheckGroundedState();
        UpdateWheelSuspensionForce();
        UpdateWheelSteerForce();
        UpdateAccelerationForce();
        CalculateRPM();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw suspention length and wheel rest position
        // to help place the collider and visual wheel correctly
        Handles.DrawWireDisc(transform.position - springLength * restRatio * transform.up,
                             transform.right, wheelRadius);

        Handles.DrawLine(transform.position, transform.position - springLength * transform.up);

        if (!debugWheel)
            return;

        // Force arrow of the wheel movement
        Color color = Handles.color;
        Handles.color = Color.green;

        Handles.ArrowHandleCap(0, wheelTransform.position, Quaternion.LookRotation(transform.up, transform.up), upForce / 1000, EventType.Repaint);

        Handles.color = Color.red;
        Handles.ArrowHandleCap(0, wheelTransform.position, Quaternion.LookRotation(transform.right, transform.up), rightForce / 1000, EventType.Repaint);

        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, wheelTransform.position, Quaternion.LookRotation(transform.forward, transform.up), forwardForce / 1000, EventType.Repaint);
        Handles.color = color;

        // Draw lateral force curve
        Vector3 lastPoint = transform.position;
        float normalForce = 4000f;

        Gizmos.color = Color.green;
        bool first = true;

        for (float angle = -20f; angle <= 20f; angle += 0.5f)
        {
            float force = CalculatePacejkaForce(angle, normalForce, lateralCoefficients);
            Vector3 point = transform.position + new Vector3(angle * 0.1f, force * 0.001f, 0);

            if (!first)
            {
                Gizmos.DrawLine(lastPoint, point);
            }
            first = false;
            lastPoint = point;
        }
    }

    void OnGUI()
    {
        if (!debugWheel)
        {
            return;
        }
        // Draw the load supported on the wheel position
        Vector3 pos = Camera.main.WorldToScreenPoint(wheelTransform.position);

        pos.y = Screen.height - pos.y;
        GUI.color = Color.magenta;
        GUI.Label(new Rect(pos.x, pos.y, 200, 40), $"{loadSupported}");
    }
#endif

    void CheckGroundedState()
    {
        Ray ray = new Ray(transform.position, -transform.up);

        // Reset states
        grounded = false;
        hasRaycastHit = false;

        if (Physics.Raycast(ray, out hit, springLength + wheelRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            hasRaycastHit = true;

            if (hit.distance < Mathf.Abs(tireTransform.position.y) + springLength + wheelRadius)
            {
                grounded = true;
            }
        }
    }

    /// <summary>
    /// Temporary acceleration and resistance force
    /// TODO : change force calculation with pacejka (handles grip)
    /// TODO : Research about breaking calculation to make it more realistic (wheel lock, grip)
    /// TODO : Add assistance system (ABS, TCS)
    /// </summary>
    private void UpdateAccelerationForce()
    {
        forwardForce = 0f;
        if (grounded)
        {
            if (motorized)
            {
                forwardForce += wheelTorque / wheelRadius;
            }

            if (breakable)
            {
                float longitudinalSpeed = Vector3.Dot(tireVel, tireTransform.forward);

                float breakForce = (breakValue * maxBreakTorque) / wheelRadius;

                float maxForce = upForce;

                float appliedBreakForce = Mathf.Min(breakForce, maxForce);

                if (Mathf.Abs(longitudinalSpeed) > 0.1f)
                {
                    appliedBreakForce *= Mathf.Sign(longitudinalSpeed);
                }
                else
                {
                    appliedBreakForce = 0f;

                    if (carRigidbody.linearVelocity.magnitude < 0.01f)
                    {
                        carRigidbody.linearVelocity = Vector3.zero;
                    }
                }

                forwardForce -= appliedBreakForce;
            }

            carRigidbody.AddForceAtPosition(tireTransform.forward * forwardForce, tireTransform.position);
        }
    }

    /// <summary>
    /// Temporary Steer force system (does not vary the grip, Change to Pacejka when it work)
    /// </summary>
    private void UpdateWheelSteerForce()
    {
        rightForce = 0f;
        // Temporary steer and resistance force
        if (steerable)
        {
            float steerAngle = Input.GetAxis("Horizontal") * maxAngleSteer;

            tireTransform.localRotation = Quaternion.Euler(0, steerAngle, 0);
        }
        if (grounded)
        {

            Vector3 steeringDir = tireTransform.right;

            float rightVelocity = Vector3.Dot(steeringDir, tireVel);



            float rightAcceleration = -rightVelocity * /*tireGripCurve.Evaluate(Mathf.Abs(rightVelocity) / tireVel.magnitude)*/tireGripFactor / Time.fixedDeltaTime;

            carRigidbody.AddForceAtPosition(rightAcceleration * wheelMass * steeringDir, tireTransform.position);
            rightForce = rightAcceleration * wheelMass;
        }

        wheelTransform.rotation = tireTransform.rotation;
        wheelTransform.Rotate(wheelVisualStartRotation);
    }

    /// <summary>
    /// Steer force calculation with Pacejka (may be broken, to be investigated)
    /// </summary>
    private void UpdateWheelSteerForcePacejka()
    {
        rightForce = 0f;

        if (steerable)
        {
            float steerAngle = Input.GetAxis("Horizontal") * maxAngleSteer;

            tireTransform.localRotation = Quaternion.Euler(0, steerAngle, 0);
        }
        if (grounded)
        {
            float forwardVel = Vector3.Dot(tireVel, tireTransform.forward);
            float lateralVel = Vector3.Dot(tireVel, tireTransform.right);

            float slipAngle = Mathf.Atan2(lateralVel, Mathf.Abs(forwardVel)) * Mathf.Rad2Deg;

            rightForce = -CalculatePacejkaForce(slipAngle * Mathf.Deg2Rad, upForce, lateralCoefficients);

            carRigidbody.AddForceAtPosition(rightForce * tireTransform.right, tireTransform.position);
        }

        wheelTransform.rotation = tireTransform.rotation;
        wheelTransform.Rotate(wheelVisualStartRotation);
    }

    /// <summary>
    /// Some function found on the web, check if the function is the right one/if it is working as expected
    /// </summary>
    private float CalculatePacejkaForce(float slipInput, float normalForce, TireCoefficients coeff)
    {
        // Apply horizontal shift
        float x = slipInput + coeff.Sh;

        // Calculate B from the relationship: BCD = cornering stiffness
        // B is calculated such that B * C * D = BCD
        float B = coeff.BCD / (coeff.C * coeff.D);

        // Scale peak force by normal load
        // D represents the peak force coefficient
        float D = coeff.D * normalForce;

        // Calculate intermediate values
        float Bx = B * x;
        float atanBx = Mathf.Atan(Bx);

        // Main Magic Formula
        // y = D * sin(C * atan(B*x - E*(B*x - atan(B*x))))
        float innerTerm = Bx - coeff.E * (Bx - atanBx);
        float y = D * Mathf.Sin(coeff.C * Mathf.Atan(innerTerm));

        // Apply vertical shift
        y += coeff.Sv;

        return y;
    }


    /// <summary>
    /// Wheel suspension force (Seems like it has some weird behaviour, need to check ASAP)
    /// </summary>
    void UpdateWheelSuspensionForce()
    {
        upForce = 0f;
        if (hasRaycastHit)
        {
            float targetLength = hit.distance - wheelRadius;
            targetLength = Mathf.Clamp(targetLength, minSpringLength, springLength);

            float springCompression = restLength - targetLength;

            float springForce = springStrength * springCompression;

            float suspensionVelocity = (currentLength - targetLength) / Time.fixedDeltaTime;
            float damperForce = damperStrength * suspensionVelocity;

            float suspensionForce = springForce + damperForce;

            if (suspensionForce > 0) // Prevent negative force (car stuck to the ground)
            {
                upForce += suspensionForce;
                carRigidbody.AddForceAtPosition(suspensionForce * transform.up, hit.point);
            }

            // visual wheel set to ground position
            if (grounded)
            {
                wheelTransform.localPosition = wheelVisualStartLocalPos + (wheelRadius - hit.distance) * Vector3.up;
            }

            currentLength = targetLength;
        }
        else
        {
            // no suspension force in air

            // visual wheel drop to rest position
            Vector3 restPos = transform.position - transform.up * restLength;
            wheelTransform.position = restPos;
        }
    }

    public void SetWheelTorque(float torque)
    {
        wheelTorque = torque;
    }

    public float GetWheelRadius()
    {
        return wheelRadius;
    }

    void CalculateRPM()
    {
        float longitudinalSpeed = Vector3.Dot(tireTransform.forward, tireVel);

        float wheelOmega = longitudinalSpeed / wheelRadius;

        wheelRPM = wheelOmega * 60f / (2f * Mathf.PI);
    }

    void CalculateTireVelocity()
    {
        tireVel = (tireTransform.position - lastFramePos) / Time.fixedDeltaTime;

        lastFramePos = tireTransform.position;
    }

    public bool IsMotorized()
    {
        return motorized;
    }

    public float GetRPM()
    {
        return wheelRPM;
    }

    public void SetBreakValue(float breakValue)
    {
        this.breakValue = breakValue;
    }

    public void SetLoadSupported(float load)
    {
        this.loadSupported = load;
    }

    public Vector3 GetWheelTransformPosition()
    {
        return tireTransform.position;
    }

    public bool GetLowestPoint(out Vector3 point)
    {
        point = Vector3.zero;
        if (grounded)
        {
            point = tireTransform.position - tireTransform.up * wheelRadius;
            return true;
        }
            return false;
    }
}
