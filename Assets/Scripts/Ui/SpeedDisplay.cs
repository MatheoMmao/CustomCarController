using TMPro;
using UnityEngine;

public class SpeedDisplay : MonoBehaviour
{
    TMP_Text text;

    CarController car;

    private void Start()
    {
        text = GetComponent<TMP_Text>();
        car = FindAnyObjectByType<CarController>();
    }

    private void Update()
    {
        text.text = (int)car.GetSpeed() + " km/h";
    }
}
