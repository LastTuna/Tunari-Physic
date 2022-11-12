using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TireBehavior : MonoBehaviour
{
    GameObject GraphicalWheel;
    GameObject ForwardReference;//this dummy is for the forward vector reference
    //i dont know if this is useful, at least yet, but maybe in the future if i implement suspension geo
    Rigidbody Car;
    public TireData jamal;
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
    public float debugv1;

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
        jamal = new TireData();
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
        SpringBehavior();
        //wheel rpm
        wheelRelaxedRPM = (wheelRadius * 2) * Mathf.PI * Car.velocity.magnitude * 3.6f;
        wheelRADs = wheelRPM / 60 * 2 * Mathf.PI;
        slipRatio = Mathf.Clamp(wheelRPM / wheelRelaxedRPM - 1, 0, 100);

        LongtitudionalGrip();
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

    void LongtitudionalGrip()
    {
        //i dont know why what or how everything works
        //presumably i need to make fz0 the current tire load
        //in newtons.. that means calculating the current weight load
        //and also the rotational force load? i dont know
        debugv1 = jamal.LongtitudionalForce(fz0, slipRatio);

    }
    

}

[System.Serializable]
public class TireData
{
    //honestly i doubt i need this at all ill just rip something out of my ass
    //because i dont understand none of this, let alone how to apply
    //these forces to the physics engine
    //this does not include lateral force..this is only longtitudional so far
    //the reference i used https://www.edy.es/dev/docs/pacejka-94-parameters-explained-a-comprehensive-guide/
    public float b0 = 1.5f; //shape factor
    public float b1 = 0; //Load influence on longitudinal friction coefficient
    public float b2 = 1100; //Longitudinal friction coefficient
    public float b3 = 0; //Curvature factor of stiffness/load
    public float b4 = 300; //Change of stiffness with slip
    public float b5 = 1f; //Change of progressivity of stiffness/load
    public float b6 = 0; //Curvature change with load^2
    public float b7 = 0; //Curvature change with load
    public float b8 = -2; //Curvature factor
    public float b9 = 0; //Load influence on horizontal shift
    public float b10 = 0; //Horizontal shift
    public float b11 = 0; //Vertical shift
    public float b12 = 0; //Vertical shift at load
    public float b13 = 0f; //Curvature shift
    

    //i am certain that a lot of this maths can be simplified
    //unfortunately for you and me i literaly cant maths even if i tried
    //so get bent
    public float LongtitudionalForce(float fz, float slipratio)
    {
        //precalculate watever these are
        float c = b0;
        float d = fz * (b1 * fz + b2);
        float h = b9 * fz + b10;
        float e = (b6 * (fz * fz) + b7 * fz + b8) * (1 - b13 * Mathf.Sign(slipratio + h));
        float bcd = (b3 * (fz * fz) + b4 * fz) * e * ((-b5 * fz) * (-b5 * fz));//missing e^(-b5*fz). no idea what e is.
        float b = bcd / (c * d);
        float v = b11 * fz + b12;
        float bx1 = b * (slipratio + h);
        return d * Mathf.Sin(c * Mathf.Atan(bx1 - e * (bx1 - Mathf.Atan(bx1)))) + v;


    }

}