using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CarBehaviour : MonoBehaviour
{
    //adjustable params
    public CarData specs;

    //car status data
    Rigidbody CarRigidbody;
    public float airspeedKM;

    //motor status data
    public float currentTorqueOut;//i presume this is in nm? yeap..yet another arbitrary value...
    public float currentHorsepowerOut;//caluclate from torks
    public float engineRPM;
    public float drivetrainRPM;//target RPM for wheels
    public int gear;
    
    public float debugValue1;
    public float debugValue2;
    public float debugValue3;
    public TireBehavior[] wheels;

    //inputs
    public float steeringInput;
    public float throttleInput;
    public float brakeInput;
    public float clutchInput;
    public int shiftInput;
    public bool shifting;


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
        CollectInputs();
        //airspeed kmh
        airspeedKM = CarRigidbody.velocity.magnitude * 3.6f;
        currentHorsepowerOut = (currentTorqueOut * engineRPM) / 7127;


        currentTorqueOut = EngineBehavior();
        drivetrainRPM = engineRPM * (specs.finalDrive * specs.gears[gear]);

        if (Input.GetKey("t"))
        {
            CarRigidbody.AddForceAtPosition(gameObject.transform.right * 300, gameObject.transform.position, ForceMode.Force);
        }
        if (Input.GetKey("n"))
        {
            CarRigidbody.AddForceAtPosition(-gameObject.transform.right * 300, gameObject.transform.position, ForceMode.Force);
        }

    }

    //make inputs into a function so to make future whatever multi input
    //support easier to implement..
    //and if you want to give FFB a go, this would likely be the place
    //to cram that code into.
    void CollectInputs()
    {
        throttleInput = Input.GetAxis("Throttle");
        brakeInput = Input.GetAxis("Brake");
        steeringInput = Input.GetAxis("Steering");
        clutchInput = Input.GetAxis("Clutch");

        //gears - currently only paddles.
        //adding shifter code should be simple enough later on
        //handle the actual gear variable change in the gearbox itself
        if (Input.GetAxis("ShiftUp") == 1 && !shifting)
        {
            shifting = true;
            shiftInput = 1;
        }
        if (Input.GetAxis("ShiftDown") == 1 && !shifting)
        {
            shifting = true;
            shiftInput = -1;
        }
    }


    //take user input and increase/decrease engine rpm
    float EngineBehavior()
    {
        if (engineRPM < specs.engineIdle)
        {
            //idle
            engineRPM += (specs.engineTorque.Evaluate(engineRPM) / specs.engineInertia) * Time.fixedDeltaTime;
            return specs.engineTorque.Evaluate(engineRPM) * 0.2f;
        }
        //throttle
        engineRPM += throttleInput * (specs.engineTorque.Evaluate(engineRPM) / specs.engineInertia) * Time.fixedDeltaTime;
        engineRPM -= (1 - throttleInput) * specs.engineDecelMap.Evaluate(engineRPM) * 10 / specs.engineInertia * Time.fixedDeltaTime;
        return (specs.engineTorque.Evaluate(engineRPM) * throttleInput) - (specs.engineDecelMap.Evaluate(engineRPM) * (1 - throttleInput));

    }

    void Gearbox()
    {
        if(shiftInput > 0 && shifting)
        {
            gear += shiftInput;
            shiftInput = 0;
            StartCoroutine(ShifterDelay());
        }
        if (shiftInput < 0 && shifting)
        {
            gear += shiftInput;
            shiftInput = 0;
            StartCoroutine(ShifterDelay());
        }

    }
    IEnumerator ShifterDelay()
    {
        yield return new WaitForSeconds(specs.shifterDelay);
        shifting = false;
    }

}