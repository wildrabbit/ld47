using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateInfinite : MonoBehaviour
{
    bool stopped;
    public float DegreesPerSecond = 360;
    // Start is called before the first frame update
    void Awake()
    {
        stopped = true;
    }

    public void StartGame(float degreesPerSec)
    {
        DegreesPerSecond = degreesPerSec;
        stopped = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (stopped) return;
        transform.Rotate(Vector3.forward, DegreesPerSecond * Time.deltaTime);
        
    }

    public void Stop()
    {
        stopped = true;
    }
}
