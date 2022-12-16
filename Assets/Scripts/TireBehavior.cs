using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TireBehavior : MonoBehaviour
{
    GameObject GraphicalWheel;
    GameObject ForwardReference;//this dummy is for the forward vector reference
    //i dont know if this is useful, at least yet, but maybe in the future if i implement suspension geo
    Rigidbody Car;
    public TireData tireData;
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
    float lateralDirection = 0;//0-forward 1-reverse
    public Vector3 longtitudionalVelocity;

    private void Start()
    {
        tireData = new TireData();
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
        CalculateRelaxedRPM();
        SpringBehavior();
        LongtitudionalGrip();
    }

    void CalculateRelaxedRPM()
    {
        //no point in calculating this if the wheel is not in contact with ground i suppose
        if (isGrounded)
        {
            //calculate the tire target rpm but first you have to make sure the lateral velocity does not account for the target wheel speed
            //because, say youre sliding sideways, the wheel longtitudional target speed would be much less because the wheel is travelling at an angle
            longtitudionalVelocity = Vector3.Scale(Car.velocity, Car.transform.forward);
            wheelRelaxedRPM = (wheelRadius * 2) * Mathf.PI * longtitudionalVelocity.magnitude * 3.6f;
            wheelRADs = wheelRPM / 60 * 2 * Mathf.PI;
            if (Vector3.Angle(Car.velocity, Car.transform.forward) > 90)
            {
                lateralDirection = 1;
                wheelRelaxedRPM = wheelRelaxedRPM * -1;
                //assign sign to relaxed rpm so it can be used for reverse
            }
            else
            {
                lateralDirection = 0;
            }
        //calculate the slip ratio
        slipRatio = wheelRPM - wheelRelaxedRPM;
        }

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
            slipRatio = 0;
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
        //just move the isGrounded check somewhere else later
        if (isGrounded)
        {
            if (debugv1 == 0)
            {
                Car.AddForceAtPosition(gameObject.transform.up.normalized * tireData.longtitudionalGrip.Evaluate(slipRatio) * tireData.gripFactor, gameObject.transform.position, ForceMode.Force);
            }
            else
            {
                Car.AddForceAtPosition(gameObject.transform.up.normalized * tireData.longtitudionalGrip.Evaluate(slipRatio) * tireData.gripFactor, gameObject.transform.position, ForceMode.Force);
            }
        }
    }

}

[System.Serializable]
public class TireData
{
    public float gripFactor = 2000;
    public float maxSlip = 150f;//how much slip is allowed till it caps out
    public AnimationCurve longtitudionalGrip = new AnimationCurve(new Keyframe(-110, -0.2f), new Keyframe(-70, -1), new Keyframe(0, 0), new Keyframe(70, 1), new Keyframe(110, 0.2f));
    public AnimationCurve lateralGrip = new AnimationCurve(new Keyframe(0, 0), new Keyframe(70, 50), new Keyframe(110, 20));


}