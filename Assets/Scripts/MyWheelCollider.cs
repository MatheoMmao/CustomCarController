using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class MyWheelCollider : MonoBehaviour
{
    [Header("State")]
    [SerializeField] bool steerable = false;
    [SerializeField] bool motorized = false;
    [SerializeField] bool breakable = true;

    [Header("Suspension")]
    [SerializeField] private float minSpringLength = 0.05f; // Recomended to not put 0 as the min distance
    [SerializeField] private float springLength = 1f;
    [SerializeField, Range(0f, 1f)] private float restRatio = 0.5f;
    [SerializeField] private float springStrength = 30000f;
    [SerializeField] private float damperStrength = 4500f;

    [Header("Wheel")]
    [SerializeField] private float wheelMass = 30f;
    [SerializeField] private float wheelRadius = 0.25f;
    [SerializeField] private Transform wheelTransform; // Visual wheel
    [SerializeField] private float maxAngleSteer = 30f;
    [SerializeField] float tireGripFactor = 1;
    [SerializeField]
    AnimationCurve tireGripCurve = new AnimationCurve(new Keyframe(0, 0.6f, 0, -0.3f), new Keyframe(0.3f, 0.5f, -0.3f, 0),
        new Keyframe(0.5f, 0.1f, 0, 0), new Keyframe(1, 0.05f, 0, 0));
    [SerializeField] float wheelTorque = 0f;
    public bool debugWheel = false;
    [SerializeField] float maxBreakTorque = 100f;

    [Header("Vehicle")]
    [SerializeField] private Rigidbody carRigidbody;

    Transform tireTransform;
    private Vector3 wheelVisualStartLocalPos;
    private float restLength;
    private float currentLength;

    float wheelRPM = 0;
    float breakValue = 0f;
    private float angleSteer = 0f;

    bool grounded = false;

    Material visualWheelMaterial;

    RaycastHit hit;
    bool hasRaycastHit;

    Vector3 lastFramePos = Vector3.zero;
    Vector3 tireVel;


    // Debug
    float upForce;
    float rightForce;
    float forwardForce;

    private void Start()
    {
        restLength = springLength * restRatio;
        currentLength = restLength;

        GameObject tireGo = new GameObject();
        tireTransform = tireGo.transform;
        tireTransform.position = wheelTransform.position;
        tireTransform.rotation = transform.rotation;
        tireTransform.SetParent(transform.parent);

        lastFramePos = tireTransform.position;

        if (wheelTransform != null)
        {
            wheelVisualStartLocalPos = wheelTransform.localPosition + Vector3.up * restLength; // save starting position
            visualWheelMaterial = wheelTransform.GetComponent<MeshRenderer>()?.material;
        }
        else
            Debug.LogError("No wheelTransform (visual wheel) has been assigned!");
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

    void CheckGroundedState()
    {
        Ray ray = new Ray(transform.position, -transform.up);

        // Reset states
        grounded = false;
        hasRaycastHit = false;
        if (debugWheel)
        {
            visualWheelMaterial.color = Color.red;
        }

        if (Physics.Raycast(ray, out hit, springLength + wheelRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            hasRaycastHit = true;
            if (hit.distance < wheelTransform.localPosition.y - wheelVisualStartLocalPos.y + springLength + wheelRadius)
            {
                grounded = true;
                if (debugWheel)
                {
                    visualWheelMaterial.color = Color.green;
                }
            }
        }
    }

    private void UpdateAccelerationForce()
    {
        forwardForce = 0f;
        if (grounded)
        {
            // Temporary acceleration and resistance force
            if (motorized)
            {
                forwardForce += wheelTorque / wheelRadius;
            }

            if (breakable)
            {
                float longitudinalSpeed = Vector3.Dot(tireVel, tireTransform.forward);

                float breakForce = (breakValue * maxBreakTorque) / wheelRadius;

                float maxForce = upForce; // TODO : Add friction coef when implemented

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

    private void UpdateWheelSteerForce()
    {
        rightForce = 0f;
        if (grounded)
        {
            // Temporary steer and resistance force
            if (steerable)
            {
                float steerAngle = Input.GetAxis("Horizontal") * 20;

                tireTransform.localRotation = Quaternion.Euler(0, steerAngle, 0);
            }

            Vector3 steeringDir = tireTransform.right;

            float rightVelocity = Vector3.Dot(steeringDir, tireVel);



            float rightAcceleration = -rightVelocity * /*tireGripCurve.Evaluate(Mathf.Abs(rightVelocity) / tireVel.magnitude)*/tireGripFactor / Time.fixedDeltaTime;

            carRigidbody.AddForceAtPosition(rightAcceleration * wheelMass * steeringDir, tireTransform.position);
            rightForce = rightAcceleration * wheelMass;
        }

        wheelTransform.rotation = tireTransform.rotation;
        wheelTransform.Rotate(-90, 90, 0);
    }

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

    private void OnDrawGizmos()
    {
        // Draw suspention length and wheel rest position
        // to help place the collider and visual wheel correctly
        Handles.DrawWireDisc(transform.position - springLength * restRatio * transform.up,
                             transform.right, wheelRadius);

        Handles.DrawLine(transform.position, transform.position - springLength * transform.up);

        if (!debugWheel)
            return;

        Color color = Handles.color;
        Handles.color = Color.green;

        Handles.ArrowHandleCap(0, wheelTransform.position, Quaternion.LookRotation(transform.up, transform.up), upForce / 1000, EventType.Repaint);

        Handles.color = Color.red;
        Handles.ArrowHandleCap(0, wheelTransform.position, Quaternion.LookRotation(transform.right, transform.up), rightForce / 1000, EventType.Repaint);

        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, wheelTransform.position, Quaternion.LookRotation(transform.forward, transform.up), forwardForce / 1000, EventType.Repaint);
        Handles.color = color;
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
}
