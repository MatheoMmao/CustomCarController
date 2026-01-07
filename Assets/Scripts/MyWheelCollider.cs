using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class MyWheelCollider : MonoBehaviour
{
    [SerializeField] bool steerable = false;
    [SerializeField] bool motorized = false;

    [Header("Suspension")]
    [SerializeField] private float minSpringLength = 0.05f; // Recomended to not put 0 as the min distance
    [SerializeField] private float springLength = 1f;
    [SerializeField, Range(0f, 1f)] private float restRatio = 0.5f;
    [SerializeField] private float springStrength = 30000f;
    [SerializeField] private float damperStrength = 4500f;
    [SerializeField] private float wheelMass = 30f;

    [Header("Wheel")]
    [SerializeField] private float wheelRadius = 0.25f;
    [SerializeField] private Transform wheelTransform; // Visual wheel
    [SerializeField] private float maxAngleSteer = 30f;
    private float angleSteer = 0f;
    [SerializeField] float tireGripFactor = 1;
    [SerializeField] AnimationCurve tireGripCurve = new AnimationCurve(new Keyframe(0, 0.6f, 0, -0.3f), new Keyframe(0.3f, 0.5f, -0.3f,0),
        new Keyframe(0.5f,0.1f,0,0), new Keyframe(1,0.05f,0,0));

    [Header("Vehicle")]
    [SerializeField] private Rigidbody carRigidbody;

    private float restLength;
    private float currentLength;
    private float springVelocity;
    Transform tireTransform;

    // Store visual wheel’s rest local position
    private Vector3 wheelVisualStartLocalPos;

    public bool debugWheel = false;
    bool grounded = false;

    Material visualWheelMaterial;

    RaycastHit hit;
    bool hasRaycastHit;

    Vector3 lastFramePos = Vector3.zero;


    // Debug
    float upForce;
    float rightForce;
    float forwardForce;

    private void Start()
    {
        restLength = springLength * restRatio;
        currentLength = restLength;
        springVelocity = 0f;

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
        CheckGroundedState();
        UpdateWheelSuspensionForce();
        UpdateWheelSteerForce();
        UpdateAccelerationForce();
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

        if (Physics.Raycast(ray, out hit, springLength + wheelRadius))
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
                float move = Input.GetAxis("Vertical");

                forwardForce += move * 3000;

                carRigidbody.AddForceAtPosition(move * transform.forward * 3000, transform.position);
            }

            Vector3 velocity = carRigidbody.GetPointVelocity(transform.position);

            float carSpeed = Vector3.Dot(transform.forward, velocity);

            forwardForce += (-carSpeed / 0.5f) / Time.fixedDeltaTime;

            carRigidbody.AddForceAtPosition((-carSpeed * 0.5f) / Time.fixedDeltaTime * transform.forward, transform.position);
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

            Vector3 tireVel = (tireTransform.position - lastFramePos) / Time.fixedDeltaTime;

            float rightVelocity = Vector3.Dot(steeringDir, tireVel);

            

            float rightAcceleration = -rightVelocity* /*tireGripCurve.Evaluate(Mathf.Abs(rightVelocity) / tireVel.magnitude)*/0.2f / Time.fixedDeltaTime;

            carRigidbody.AddForceAtPosition(rightAcceleration * wheelMass * steeringDir, tireTransform.position);
            rightForce = rightAcceleration * wheelMass;
        }

        wheelTransform.rotation = tireTransform.rotation;
        wheelTransform.Rotate(-90, 90, 0);

        lastFramePos = tireTransform.position;
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
            float damperForce = damperStrength * (currentLength - targetLength) / Time.fixedDeltaTime;

            float suspensionForce = springForce + damperForce;

            if (suspensionForce > 0) // Prevent negative force (car stuck to the ground)
            {
                upForce += suspensionForce;
                carRigidbody.AddForceAtPosition(suspensionForce * transform.up, hit.point);
            }

            // visual wheel set to ground position
            if (grounded)
            {
                grounded = true;
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
        Handles.ArrowHandleCap(0, wheelTransform.position, Quaternion.LookRotation(transform.right, transform.up), rightForce/1000, EventType.Repaint);

        Handles.color = Color.blue;
        Handles.ArrowHandleCap(0, wheelTransform.position, Quaternion.LookRotation(transform.forward, transform.up), forwardForce / 1000, EventType.Repaint);
        Handles.color = color;
    }
}
