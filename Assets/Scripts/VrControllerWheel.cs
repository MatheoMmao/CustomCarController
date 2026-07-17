using UnityEngine;

public class VrControllerWheel : MonoBehaviour
{
    [SerializeField] GameObject leftController;
    [SerializeField] GameObject rightController;

    RegisterInputs inputs;

    private void Start()
    {
       inputs = FindAnyObjectByType<RegisterInputs>();
    }

    private void Update()
    {
        if (leftController.activeInHierarchy && rightController.activeInHierarchy)
        {
            inputs.overrideSteering = true;

            Vector3 gap = rightController.transform.localPosition - leftController.transform.localPosition;

            float angle = Mathf.Atan2(gap.y, gap.x) * Mathf.Rad2Deg;

            angle /= 90f;

            angle = Mathf.Clamp(angle, -1, 1);

            inputs.SetSteeringValue(angle*-1f);
        }
    }
}
