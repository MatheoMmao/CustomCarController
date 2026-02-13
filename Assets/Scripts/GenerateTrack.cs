using UnityEngine;

public class GenerateTrack : MonoBehaviour
{
    [SerializeField] GameObject prefab;
    [SerializeField] Vector3 direction;

    CarPhysics car;

    private void Start()
    {
        car = FindAnyObjectByType<CarPhysics>();
    }

    private void Update()
    {
        if (Vector3.Distance(car.transform.position, transform.position)>20)
        {
            //Destroy(transform.parent.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Collider[] hit = Physics.OverlapSphere(transform.position + direction, 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

        foreach(Collider hit2 in hit)
        {
            if (hit2.gameObject.GetComponentInChildren<GenerateTrack>()!=null)
            {
                return;
            }
        }

        Instantiate(prefab, transform.position + direction, Quaternion.identity).name="Ground";
    }
}
