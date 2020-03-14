using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (Input.touchSupported && Input.simulateMouseWithTouches)
        {
            Debug.Log("simulateMouseWithTouches=yes");
        }
        else
        {
            Debug.Log("simulateMouseWithTouches=no");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("touches!");
        }
    }
}
