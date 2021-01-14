using System;
using UnityEngine;
using UnityEngine.Rendering;

public class Rotator : MonoBehaviour
{
    [SerializeField]
    public float yDegreesPerSecond = 180;
    DateTime prev = DateTime.Now;
    double[] difs = new double[1000];
    int count = 0;
    // Update is called once per frame
    void Update()
    {
        var now = DateTime.Now;
        var dif = now - prev;
        transform.localRotation *= Quaternion.Euler(0, yDegreesPerSecond * Time.deltaTime, 0);
        prev = now;
        if (count < difs.Length)
        {
            difs[count] = dif.TotalMilliseconds;
        }
        else
        {
            Debug.Log("DIFS==================");
            for (int i = 0; i < difs.Length; i++)
            {
                Debug.Log(difs[i]+"\n");
            }
        }

        count++;
    }
}
