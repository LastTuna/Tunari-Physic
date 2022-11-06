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

    //readonly go below
    public bool isGrounded;//dolor
    public float wheelMomentum;//current wheel momentum in ??? units
    public float wheelRPM;//current wheel RPM
    public float motorTorque = 0;//current torque applied to the wheel, dictated by carbehavior
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
        GraphicalWheel.transform.Rotate(wheelRPM / 60 * 360 * Time.deltaTime, 0, 0);
    }

    private void FixedUpdate()
    {
        if (isGrounded)
        {
            MotorBehavior();
            BrakeBehavior();
        }
        Steering();
        //wheel rpm
        wheelRPM = (wheelRadius * 2) * Mathf.PI * Car.velocity.magnitude * 3.6f;
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

    //this is called from the car fixed update
    //calculates and applies spring force to car.
    public void SpringBehavior()
    {
        Car.AddForceAtPosition(SpringForce(), gameObject.transform.position, ForceMode.Acceleration);
    }


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
    

    public void MotorBehavior()
    {
        //for now just reference cars forward vector for force direction
        //however in the future... if adding geometry deformation,
        //add a script to instantiate a child dummy or something
        //and then calculate its position before taking the forward vector from it

        Vector3 motorForce = ForwardReference.transform.forward.normalized * motorTorque;
        Car.AddForceAtPosition(motorForce, gameObject.transform.position, ForceMode.Acceleration);

    }
    
    public void BrakeBehavior()
    {
        //we can just call this form the local fixed update
        //check if user is braking
        //and also calculate brake cooling etc...

        if (Input.GetKey("t"))
        {
            brakeTorque = brakeStrength;
            //apply force
            Vector3 brakeForce = ForwardReference.transform.forward.normalized * -brakeTorque;
            Car.AddForceAtPosition(brakeForce, gameObject.transform.position, ForceMode.Acceleration);
        }
        else
        {
            brakeTorque = 0;
        }
    }
    void Steering()
    {
        if (Input.GetKey("v") && steerAngle < maxSteerLock)
        {
            steerAngle += 1;

        }
        else if (Input.GetKey("b") && steerAngle > -maxSteerLock)
        {
            steerAngle -= 1;
        }

    }
}