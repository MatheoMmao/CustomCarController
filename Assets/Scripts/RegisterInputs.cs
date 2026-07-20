using UnityEngine;
using UnityEngine.InputSystem;

public class RegisterInputs : MonoBehaviour
{
    [SerializeField] CarPhysics controlledCar;

    [SerializeField] InputActionReference accelerationAction;
    [SerializeField] InputActionReference brakingAction;
    [SerializeField] InputActionReference steerAction;
    [SerializeField] InputActionReference upShiftAction;
    [SerializeField] InputActionReference downShiftAction;

    public bool overrideSteering = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        accelerationAction.action.Enable();
        accelerationAction.action.performed += (_) =>
        {
            float throttle = _.ReadValue<float>();
            if (controlledCar!=null)
            {
                controlledCar.SetThrottleValue(throttle);
            }
        };
        accelerationAction.action.canceled += (_) =>
        {
            if (controlledCar != null)
            {
                controlledCar.SetThrottleValue(0);
            }
        };

        brakingAction.action.Enable();
        brakingAction.action.performed += (_) =>
        {
            float braking = _.ReadValue<float>();
            if (controlledCar!=null)
            {
                controlledCar.SetBrakingValue(braking);
            }
        };
        brakingAction.action.canceled += (_) => 
        {
            if (controlledCar != null)
            {
                controlledCar.SetBrakingValue(0);
            }
        };

        steerAction.action.Enable();
        steerAction.action.performed += (_) =>
        {
            if (!overrideSteering)
            {
                float steer = _.ReadValue<Vector2>().x;
                if (controlledCar != null)
                {
                    SetSteeringValue(steer);
                }
            }
        };
        steerAction.action.canceled += (_) =>
        {
            if (!overrideSteering)
            {
                if (controlledCar != null)
                {
                    SetSteeringValue(0);
                }
            }
        };

        upShiftAction.action.Enable();
        upShiftAction.action.performed += (_) =>
        {
            if (controlledCar != null)
            {
                controlledCar.UpShift();
            }
        };

        downShiftAction.action.Enable();
        downShiftAction.action.performed += (_) =>
        {
            if (controlledCar != null)
            {
                controlledCar.DownShift();
            }
        };
    }

    public void SetSteeringValue(float value)
    {
        controlledCar.SetSteerInput(value);
    }
}
