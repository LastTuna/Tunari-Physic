using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TireBehavior : MonoBehaviour
{
    GameObject GraphicalWheel;
    GameObject ForwardReference;//this dummy is for the forward vector reference
    //i dont know if this is useful, at least yet, but maybe in the future if i implement suspension geo
    Rigidbody Car;
    public string wheelName = "WHEEL_";
    public float wheelRadius = 0.33f;//NOT diameter

    public float travelRange = 1f;//how much room spring has to travel
    public float springRate = 100;
    public float dampBumpRate = 20;
    public float dampReboundRate = 20;
    public float brakeStrength = 10;
    public float maxSteerLock = 20;
    public float wheelInertia = 0.7f;


    //tire
    public float fz0 = 100f;

    //readonly go below
    public bool isGrounded;//dolor
    public float wheelRelaxedRPM;//wheel's "natural" RPM, use this to calculate slip ratio later
    public float wheelRPM;//current wheel RPM
    public float wheelRADs;//current wheel RAD/sec
    public float slipRatio;
    public float travelSpeed;//how fast is spring moving in arbitrary units
    public Vector3 springForceVector;//how much spring force including damper in a vector
    public float springDisplacement = 0;//1-bottom out 0-fully extended
    public float brakeTorque = 0;
    float steerAngle = 0;

    private void Start()
    {
        GraphicalWheel = GameObject.Find(wheelName);
        Car = gameObject.GetComponentInParent<Rigidbody>();
        ForwardReference = Instantiate(new GameObject(), gameObject.transform.position, new Quaternion(0,0,0,1), Car.gameObject.transform);
    }

    private void Update()
    {
        Vector3 graphicsPos = gameObject.transform.position;
        graphicsPos += gameObject.transform.forward.normalized * ((travelRange * (1 - springDisplacement)) - wheelRadius);
        GraphicalWheel.transform.position = graphicsPos;
        GraphicalWheel.transform.localEulerAngles = new Vector3(GraphicalWheel.transform.localEulerAngles.x, steerAngle - GraphicalWheel.transform.localEulerAngles.z, GraphicalWheel.transform.localEulerAngles.z);
        GraphicalWheel.transform.Rotate(wheelRADs * 360 * Time.deltaTime, 0, 0);
    }

    private void FixedUpdate()
    {
        Steering();
        SpringBehavior();
        //wheel rpm
        wheelRelaxedRPM = (wheelRadius * 2) * Mathf.PI * Car.velocity.magnitude * 3.6f;
        wheelRADs = wheelRPM / 60 * 2 * Mathf.PI;
        
    }

    Vector3 SpringForce()
    {
        RaycastHit dolor;
        //use transform.forward to get the tangent for the wheel..
        //BUT MAKE SURE TO ROTATE THE PHYSICAL DUMMY 90 DEGREES ON X!!!!!!!
        if (Physics.Raycast(gameObject.transform.position, gameObject.transform.forward.normalized, out dolor, travelRange + wheelRadius))
        {
            isGrounded = true;
            float displacementDelta = springDisplacement;
            springDisplacement = 1 - ((dolor.distance - wheelRadius) / travelRange);
            travelSpeed = (springDisplacement - displacementDelta) * travelRange;
            float totalForce = springDisplacement * springRate * Time.fixedDeltaTime;
            totalForce += SlowDamper();
            springForceVector = -gameObject.transform.forward.normalized * totalForce;
        }
        else
        {
            isGrounded = false;
            //raycast didnt hit nothing so safe to say its voided
            travelSpeed = 0;
            springDisplacement = 0;
            //maybe implement here some rebound force with HUB MASS
            springForceVector = new Vector3(0, 0, 0);
        }
        return springForceVector;
    }
    
    //applies spring force to car.
    void SpringBehavior()
    {
        Car.AddForceAtPosition(SpringForce(), gameObject.transform.position, ForceMode.Force);
    }
    
    //slow damper calculation
    float SlowDamper()
    {
        //not sure if these was reversed i wrote this at 4am at 3 beers in
        //dampers have a minimum threshold so to let spring return back to natural position
        //rebound damper
        if (travelSpeed < 0.00002)
        {
        return -dampReboundRate * Time.fixedDeltaTime;
        }

        //bump damper
        if (travelSpeed > -0.00002)
        {
        return dampBumpRate * Time.fixedDeltaTime;
        }
        //wel if nothing else then dont apply damper force...
        return 0;
    }
    
    public void TorqueForce(float engineRPM, float motorTorks)
    {
        //for now just reference cars forward vector for force direction
        //however in the future... if adding geometry deformation,
        //add a script to instantiate a child dummy or something
        //and then calculate its position before taking the forward vector from it
        wheelRPM = engineRPM;
        slipRatio = Mathf.Clamp(wheelRPM / wheelRelaxedRPM, -3, 3);
        

        if (isGrounded)
        {
            TorqueBehavior(motorTorks / slipRatio);
        }
    }

    public void TorqueBehavior(float perro)
    {
        Vector3 motorForce = ForwardReference.transform.forward.normalized * perro * Time.fixedDeltaTime;
        Car.AddForceAtPosition(motorForce, gameObject.transform.position, ForceMode.Force);
    }
    
    //move this to carbehavior
    //unless..
    void Steering()
    {

    }
}