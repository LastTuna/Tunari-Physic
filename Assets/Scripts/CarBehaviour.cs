using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CarBehaviour : MonoBehaviour
{
    Rigidbody CarRigidbody;

    //public CarData carData;

    public float airspeedKM;
    public float engineRPM;
    public float debugValue1;
    public float debugValue2;
    public float debugValue3;
    public TireBehavior[] wheels;

    void Start()
    {
        //Time.timeScale = 0.1f;
        CarRigidbody = gameObject.GetComponent<Rigidbody>();
        //TODO add instantiation during runtime
        wheels = new TireBehavior[4];
        wheels[0] = GameObject.Find("WHEEL_LF").GetComponent<TireBehavior>();
        wheels[1] = GameObject.Find("WHEEL_RF").GetComponent<TireBehavior>();
        wheels[2] = GameObject.Find("WHEEL_LR").GetComponent<TireBehavior>();
        wheels[3] = GameObject.Find("WHEEL_RR").GetComponent<TireBehavior>();
        CarRigidbody.centerOfMass = GameObject.Find("CenterOfGravy").transform.localPosition;
    }

    void FixedUpdate()
    {
        //go through every wheel n calculate spring
        foreach(TireBehavior ww in wheels)
        {
            ww.SpringBehavior();
        }
        
        if (Input.GetKey("n"))
        {
            wheels[2].motorTorque = 5;
            wheels[3].motorTorque = 5;
        }
        else
        {
            wheels[2].motorTorque = 0;
            wheels[3].motorTorque = 0;
        }

        //airspeed kmh
        airspeedKM = CarRigidbody.velocity.magnitude * 3.6f;



        
    }

    void Update()
    {



    }



    void EngineBehavior()
    {




    }
    


}