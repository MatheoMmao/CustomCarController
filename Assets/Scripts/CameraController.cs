using Unity.XR.OpenVR;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class CameraController : MonoBehaviour
{
    [SerializeField] Transform carTransform;

    [SerializeField] Vector3 offset;
    [SerializeField] Vector3 angleBase;

    // TODO : Replace this controller with a cinemachine camera for better control

    private void OnDrawGizmos()
    {
        float angleX = carTransform.eulerAngles.x;
        angleX = (angleX + 90) % 360;
        if (angleX > 180)
        {
            angleX -= 360;
        }
        angleX = (Mathf.Abs(angleX) - 90);
        angleX /= 3;

        Vector3 targetPosition = carTransform.position + Quaternion.Euler(-angleX * 0.3f, carTransform.eulerAngles.y, 0) * offset;
        Vector3 gapPosition = targetPosition - transform.position;
        Vector3 directionRay = (targetPosition - carTransform.position).normalized;

        Handles.DrawLine(carTransform.position + directionRay * 3f, carTransform.position + directionRay * offset.magnitude);

        Gizmos.DrawWireSphere(carTransform.position + directionRay * 3f, 0.1f);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (!carTransform)
            {
                return;
            }


            float angleZ = carTransform.eulerAngles.z;
            angleZ = (angleZ + 90) % 360f;
            if (angleZ > 180)
            {
                angleZ -= 360;
            }
            angleZ = (Mathf.Abs(angleZ) - 90) / 3;

            float angleX = carTransform.eulerAngles.x;
            angleX = (angleX + 90) % 360;
            if (angleX > 180)
            {
                angleX -= 360;
            }
            angleX = (Mathf.Abs(angleX) - 90);
            angleX /= 3;

            transform.position = carTransform.position + Quaternion.Euler(-angleX * 0.3f, carTransform.eulerAngles.y, 0) * offset;

            Vector3 targetAngle = new Vector3(angleBase.x + angleX, angleBase.y + carTransform.eulerAngles.y, angleBase.z + angleZ);
            transform.rotation = Quaternion.Euler(targetAngle);
        }
    }

    private void FixedUpdate()
    {
        if (!carTransform)
        {
            return;
        }


        float angleZ = carTransform.eulerAngles.z;
        angleZ = (angleZ + 90) % 360f;
        if (angleZ > 180)
        {
            angleZ -= 360;
        }
        angleZ = (Mathf.Abs(angleZ) - 90) / 3;

        float angleX = carTransform.eulerAngles.x;
        angleX = (angleX + 90) % 360;
        if (angleX > 180)
        {
            angleX -= 360;
        }
        angleX = (Mathf.Abs(angleX) - 90);
        angleX /= 3;

        Vector3 targetPosition = carTransform.position + Quaternion.Euler(-angleX * 0.3f, carTransform.eulerAngles.y, 0) * offset;
        Vector3 gapPosition = targetPosition - transform.position;
        Vector3 directionRay = (targetPosition - carTransform.position).normalized;


        if (Physics.Raycast(carTransform.position + directionRay * 3f, directionRay, out RaycastHit item, offset.magnitude - 3, 1))
        {
            gapPosition = (carTransform.position + ((item.distance * 0.8f) + 3) * directionRay) - transform.position;
        }
        Collider[] collider = Physics.OverlapSphere(carTransform.position + directionRay * 3f, 0.1f);
        if (collider.Length > 0)
        {
            gapPosition = (carTransform.position + 3 * directionRay) - transform.position;
        }

        transform.position += gapPosition * Time.fixedDeltaTime * 10;

        Vector3 targetAngle = new Vector3(angleBase.x + angleX, angleBase.y + carTransform.eulerAngles.y, angleBase.z + angleZ);
        Vector3 actualAngle = transform.eulerAngles;
        Vector3 gapAngle = targetAngle - actualAngle;

        if (gapAngle.x > 180f)
        {
            actualAngle.x += 360f;
        }
        if (gapAngle.x < -180f)
        {
            actualAngle.x -= 360f;
        }

        if (gapAngle.y > 180f)
        {
            actualAngle.y += 360f;
        }
        if (gapAngle.y < -180f)
        {
            actualAngle.y -= 360f;
        }

        if (gapAngle.z > 180f)
        {
            actualAngle.z += 360f;
        }
        if (gapAngle.z < -180f)
        {
            actualAngle.z -= 360f;
        }
        gapAngle = targetAngle - actualAngle;

        transform.Rotate(gapAngle * Time.fixedDeltaTime * 10);
    }
}
