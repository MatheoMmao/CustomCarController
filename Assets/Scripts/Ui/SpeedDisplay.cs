using TMPro;
using UnityEngine;

public class SpeedDisplay : MonoBehaviour
{
    TMP_Text text;

    [SerializeField]
    Rigidbody car;

    private void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        text.text = car.linearVelocity.magnitude.ToString() + " km/h";
    }
}
