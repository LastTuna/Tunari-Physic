using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TireBehavior : MonoBehaviour
{
    GameObject GraphicalWheel;
    //i dont know if this is useful, at least yet, but maybe in the future if i implement suspension geo
    GameObject ContactPosTrajectory;
    //contact pos is a game object so that i can use
    //transform.rotateAround because i dont know how to use math
    //to rotate vector in 3d space
    Rigidbody rigidbody;
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
    Vector3 contactPoint;//tire contact point
    public float wheelRelaxedRPM;//wheel's "natural" RPM, use this to calculate slip ratio later
    public float wheelRPM;//current wheel RPM
    public float wheelRADs;//current wheel RAD/sec
    public float longtitudionalSlipRatio;
    public float slipRatio;
    public float slipAngle;
    public float travelSpeed;//how fast is spring moving in arbitrary units
    public Vector3 springForceVector;//how much spring force including damper in a vector
    public float springDisplacement = 0;//1-bottom out 0-fully extended
    public float brakeTorque = 0;
    float steerAngle = 0;
    public Vector3 longtitudionalVelocity;
    public Vector3 lateralVelocity;
    public float currentSteerAngle;
    public float AAAAAAAAAASSSS;


    public Vector3 localForward;
    public Vector3 localRight;
    public Vector3 localUp;


    private void Start()
    {
        tireData = new TireData();
        GraphicalWheel = GameObject.Find(wheelName);
        rigidbody = gameObject.GetComponentInParent<Rigidbody>();
        ContactPosTrajectory = new GameObject();
    }

    private void Update()
    {
        Vector3 graphicsPos = gameObject.transform.position;
        graphicsPos += (-localUp) * ((travelRange * (1 - springDisplacement)) - wheelRadius);
        GraphicalWheel.transform.position = graphicsPos;
        GraphicalWheel.transform.localEulerAngles = new Vector3(GraphicalWheel.transform.localEulerAngles.x, steerAngle - GraphicalWheel.transform.localEulerAngles.z, GraphicalWheel.transform.localEulerAngles.z);
        GraphicalWheel.transform.Rotate(wheelRADs * 360 * Time.deltaTime, 0, 0);
    }

    private void FixedUpdate()
    {
        localForward = gameObject.transform.localRotation * gameObject.transform.forward;
        localForward.Normalize();

        localRight = gameObject.transform.localRotation * gameObject.transform.right;
        localRight.Normalize();

        localUp = gameObject.transform.localRotation * gameObject.transform.up;
        localUp.Normalize();


        SpringBehavior();
        SlipCalc();
        Steer();
        
    }

    public void SlipCalc()
    {
        ContactPosTrajectory.transform.position = contactPoint + rigidbody.velocity;
        ContactPosTrajectory.transform.RotateAround(contactPoint, rigidbody.angularVelocity.normalized, rigidbody.angularVelocity.magnitude);
        //this gets the position delta of where the car is GOING TO BE, calculating the actual velocity and also the angular momentum
        //obviosly this is dumb as fuck but i literaly cant into maths right now and its 6am

        Vector3 direction = ContactPosTrajectory.transform.position - contactPoint;
      
        //calculate slip angle
        slipAngle = Vector3.SignedAngle(localForward, direction, Vector3.up);
        if(slipAngle > 90 || slipAngle < -90)
        {
            slipRatio = (Mathf.Abs(slipAngle) - 90) * 0.01111111111f;//divide by 90
        }
        else
        {
            slipRatio = slipAngle * 0.01111111111f;//divide by 90
        }
        
        //slip ratio is how much of the total momentum is lateral and how much of it is longtitudional
        //probably rename this to something else later

        longtitudionalVelocity = direction * (1 - Mathf.Abs(slipRatio));
        lateralVelocity = direction * Mathf.Abs(slipRatio);

        //calc wheel target rpm
        wheelRelaxedRPM = (wheelRadius * 2) * Mathf.PI * longtitudionalVelocity.magnitude * 3.6f;
        if (slipAngle > 90 || slipAngle < -90)
        {
            wheelRelaxedRPM = wheelRelaxedRPM * -1;
            //get the sign of the relaxed rpm so it also works for reverse because .magnitude always returns positive
        }
        wheelRADs = wheelRPM / 60 * 2 * Mathf.PI;

        longtitudionalSlipRatio = wheelRPM - wheelRelaxedRPM;
        //use the magnitude of the vector for the slip curve ref

        if (isGrounded)
        {
            if (slipAngle < 0)
            {
                rigidbody.AddForceAtPosition(localRight * 100 * tireData.lateralGrip.Evaluate(lateralVelocity.magnitude), gameObject.transform.position, ForceMode.Force);
            }
            else
            {
                rigidbody.AddForceAtPosition(-localRight * 100 * tireData.lateralGrip.Evaluate(lateralVelocity.magnitude), gameObject.transform.position, ForceMode.Force);
            }

            rigidbody.AddForceAtPosition(localForward * 100 * tireData.longtitudionalGrip.Evaluate(longtitudionalSlipRatio), gameObject.transform.position, ForceMode.Force);

        }

        //take rigidbody velocity add it to current wheel pos and then use that to calc slip angle wiht quternion.lookangle or was it vector3.angle
    }

    public void Steer()
    {

    }
    

    Vector3 SpringForce()
    {
        RaycastHit dolor;
        //use transform.forward to get the tangent for the wheel..
        //BUT MAKE SURE TO ROTATE THE PHYSICAL DUMMY 90 DEGREES ON X!!!!!!!
        if (Physics.Raycast(gameObject.transform.position, -localUp, out dolor, travelRange + wheelRadius))
        {
            contactPoint = dolor.point;
            isGrounded = true;
            float displacementDelta = springDisplacement;
            springDisplacement = 1 - ((dolor.distance - wheelRadius) / travelRange);
            travelSpeed = (springDisplacement - displacementDelta) * travelRange;
            float totalForce = springDisplacement * springRate * Time.fixedDeltaTime;
            totalForce += SlowDamper();
            springForceVector = localUp * totalForce;
            
        }
        else
        {
            longtitudionalSlipRatio = 0;
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
        rigidbody.AddForceAtPosition(SpringForce(), gameObject.transform.position, ForceMode.Force);
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

}

[System.Serializable]
public class TireData
{
    public float gripFactor = 2000;
    public float maxSlip = 150f;//how much slip is allowed till it caps out
    public AnimationCurve longtitudionalGrip = new AnimationCurve(new Keyframe(-110, -0.2f), new Keyframe(-70, -1), new Keyframe(0, 0), new Keyframe(70, 1), new Keyframe(110, 0.2f));
    public AnimationCurve lateralGrip = new AnimationCurve(new Keyframe(-110, -0.2f), new Keyframe(-70, -1), new Keyframe(0, 0), new Keyframe(70, 1), new Keyframe(110, 0.2f));


}