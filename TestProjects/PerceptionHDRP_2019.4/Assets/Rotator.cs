using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField]
    public float yDegreesPerSecond = 180;

    // Update is called once per frame
    void Update()
    {
        transform.localRotation *= Quaternion.Euler(0, yDegreesPerSecond * Time.deltaTime, 0);
    }
}
