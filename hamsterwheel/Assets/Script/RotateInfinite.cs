using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateInfinite : MonoBehaviour
{

    public float DegreesPerSecond = 360;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.forward, DegreesPerSecond * Time.deltaTime);
        
    }
}
