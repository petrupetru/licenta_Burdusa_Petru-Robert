using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class MinerAgent : Agent
{
    public Rigidbody rBody;
    float rechargeTime = 1f;
    float timeLastHit = 0;
    int score = 0;
    public GameObject sensor;
    RayPerceptionSensorComponent3D rayPerceptionSensor;
    int normMaxHits = 1; //used for normalising the observations
    int normMaxCoins = 1; //used for normalising the observations
    int normMaxTotalCoins = 1; ////used for normalising the rewards


    // Start is called before the first frame update
    void Start()
    {
        timeLastHit = 0;
        rBody = GetComponent<Rigidbody>();
        rBody.freezeRotation = true;
    }

    public override void Initialize()
    {
        this.rayPerceptionSensor = sensor.GetComponent<RayPerceptionSensorComponent3D>();
        this.MaxStep = 5000;
    }

    public override void OnEpisodeBegin()
    {
        //RandomiseParameters();
        score = 0;
        // If the Agent fell, zero its momentum
        if (this.transform.localPosition.y < 0)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }


    }
    public override void CollectObservations(VectorSensor sensor)
    {
        var rayObservations  = RayCastInfo(rayPerceptionSensor);

        //DEBUG//DEBUG//DEBUG//DEBUG//DEBUG//DEBUG//DEBUG
        /*string debug = "";
        for (int i = 0; i < rayObservations.hitsNeededObs.Count; ++i)
        {
            debug += rayObservations.numOfCoinsObs[i].ToString() + " # ";
        }
        Debug.Log(debug);*/
        //Debug.Log(normMaxHits.ToString() + " # " + normMaxCoins.ToString());
        //DEBUG//DEBUG//DEBUG//DEBUG//DEBUG//DEBUG//DEBUG

        //Agent position
        sensor.AddObservation(this.transform.localPosition);
        //Agent rotation
        sensor.AddObservation(this.transform.rotation);
        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);

        // Ray Sensor
        sensor.AddObservation(rayObservations.hitsNeededObs);
        sensor.AddObservation(rayObservations.numOfCoinsObs);
        


    }

    public float forceMultiplier = 1f;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);

        // Fell off platform
        if (this.transform.localPosition.y < 0)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        /*var actionForward = act[0];
        
        var actionRight = act[1];
        var actionLeft = act[2];*/
        if(act[0] == 1)
        {
            dirToGo = transform.forward * 1f;
        }
        if(act[1] == 1)
        {
            rotateDir = transform.up * 1f;
        }
        if(act[2] == 1)
        {
            rotateDir = transform.up * -1f;
        }
        
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 150f);
        rBody.AddForce(dirToGo * 0.5f, ForceMode.VelocityChange);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Coin")
        {
            Destroy(other.gameObject);
            //Debug.Log(1f / (float)normMaxTotalCoins);
            AddReward(1f / (float)normMaxTotalCoins);
            score++;
            //Debug.Log("score: " + score.ToString());
        }
    }

    void OnCollisionStay(Collision other)
    {
        if (other.gameObject.tag == "Platform")
        {
            Platform platform = other.transform.GetComponent<Platform>();
            if (Time.time - timeLastHit > rechargeTime)
            {
                platform.Hit();
                timeLastHit = Time.time;
            }
        }
    }

    private (List<float> hitsNeededObs, List<float> numOfCoinsObs) RayCastInfo(RayPerceptionSensorComponent3D rayComponent)
    {
        /*Aceasta functie este inspirata din: https://forum.unity.com/threads/helper-function-to-get-the-rayperceptionsensorcomponent3d-hit-distance-to-gameobject.1240648/ (accesare aprilie 2023)*/
        List<float> rayObservationsHitsNeeded = new List<float>();
        List<float> rayObservationsNumOfCoins = new List<float>();

        var rayOutputs = RayPerceptionSensor
                .Perceive(rayComponent.GetRayPerceptionInput())
                .RayOutputs;

        if (rayOutputs != null)
        {
            var lengthOfRayOutputs = RayPerceptionSensor
                    .Perceive(rayComponent.GetRayPerceptionInput())
                    .RayOutputs
                    .Length;
            for (int i = 0; i < lengthOfRayOutputs; i++)
            {
                float hitsNeeded = 100f * normMaxHits;
                float numOfCoins = -100f * normMaxCoins;
                GameObject goHit = rayOutputs[i].HitGameObject;
                /*
                 tag = goHit.tag
                distance = rayHitDistance
                hitsNedded = goHit.hitsNeeded
                potentialReward = goHit.numOfCoins
                 */
                if (goHit != null)
                {
                    // Found some of this code to Denormalized length
                    // calculation by looking trough the source code:
                    // RayPerceptionSensor.cs in Unity Github. (version 2.2.1)
                    
                    var rayDirection = rayOutputs[i].EndPositionWorld - rayOutputs[i].StartPositionWorld;
                    var scaledRayLength = rayDirection.magnitude;
                    float rayHitDistance = rayOutputs[i].HitFraction * scaledRayLength;
                    
                    if(goHit.tag == "Platform")
                    {
                        Platform platform = goHit.GetComponent<Platform>();
                        hitsNeeded = platform.getHits();
                        numOfCoins = platform.getNumOfCoins();
                    }

                    
                }
                rayObservationsHitsNeeded.Add(hitsNeeded / normMaxHits);
                rayObservationsNumOfCoins.Add(numOfCoins / normMaxCoins);
            }
        }

        return (rayObservationsHitsNeeded, rayObservationsNumOfCoins);
    }

    public void setNormHitsAndCoins(int maxHits, int maxCoins)
    {
        //Debug.Log("DEBUG: " + maxHits.ToString());
        this.normMaxHits = maxHits;
        this.normMaxCoins = maxCoins;
    }

    public void setNormTotalCoins(int totalCoins)
    {
        this.normMaxTotalCoins = totalCoins;
    }





}



