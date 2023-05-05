using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public enum dancerToBoidPreset
{
    fastCirclesInHip, breakCircle, ControlVelocitySlow, Nothing
}

public class DancerToBoidController : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("Dancer to track movement from")]
    [SerializeField] private int dancerToTrack = 0;

    [Header("Bone to target and track movement from")]
    [SerializeField] private int boneToTarget = 0;
    [SerializeField] private bool averageValuesFromAllBones = true;

    [Header("Boid Behaviour from dancer")]
    [SerializeField]public dancerToBoidPreset _preset;
    

    [Header("Boids Values")]
    [SerializeField] public float boidSpeed;
    [SerializeField] public float alignmentWeight;
    [SerializeField] public float separationWeight;
    [SerializeField] public float cohesionWeight;
    [SerializeField] public float targetWeight;

    private float velocity;
    private float acceleration;
    private float jerk;

    bool rt = false;

    private BoidControllerECSJobsFast[] cmpsBoidController;


    [Header("Dancers to get values from")]
    [SerializeField] private DancerToTrack[] dancersToTrackVelocity;


    private Vector3 valuesOfBones = Vector3.zero;
 
    [System.Serializable]
    public struct DancerToTrack
    {
        public Transform[] bonesToTrack;
    }

    private ValuesToTrack[] valuesDancersToTrack;
 
    public struct ValuesToTrack
    {
        public GetValuesFromBone[] vAccJerkFromDancer;
    }

    [Header("Material of Boids")]
    public Material boidMaterial;


    private void Awake()
    {
        cmpsBoidController = GameObject.FindObjectsOfType<BoidControllerECSJobsFast>();

        
        valuesDancersToTrack = new ValuesToTrack[dancersToTrackVelocity.Length];

        int j = 0;
        int y = 0;
        foreach  (DancerToTrack dancer in dancersToTrackVelocity)
        {
            
            valuesDancersToTrack[j].vAccJerkFromDancer = new GetValuesFromBone[dancer.bonesToTrack.Length];
            foreach (Transform bone in dancer.bonesToTrack)
            {
                valuesDancersToTrack[j].vAccJerkFromDancer[y] = bone.gameObject.AddComponent<GetValuesFromBone>();
                y++;
            }

            y = 0;
            j++;
        }
 
    }

    // Update is called once per frame
    void Update()
    {
        if (dancerToTrack > dancersToTrackVelocity.Length - 1)
        {
            dancerToTrack = dancersToTrackVelocity.Length - 1;
        }

        if (boneToTarget >  dancersToTrackVelocity[dancerToTrack].bonesToTrack.Length - 1)
        {
            boneToTarget = dancersToTrackVelocity[dancerToTrack].bonesToTrack.Length - 1;
        }

       
    }

    private void FixedUpdate()
    {
        GetDataFromDancersAndBones();
        CalculateDataToSendOnPlayerMovement();
        SendDataToBoidSystem();
    }

    private void GetDataFromDancersAndBones()
    {
        valuesOfBones = Vector3.zero;
        if (averageValuesFromAllBones)
        {
            
            int f = 0;
            foreach (GetValuesFromBone _cmpGetValuesFromBone in valuesDancersToTrack[dancerToTrack].vAccJerkFromDancer)
            {
                valuesOfBones += _cmpGetValuesFromBone.GetValues();
                f++;
            }

            ///// To get average
            valuesOfBones = valuesOfBones / f;
        }
        else
        {
            valuesOfBones = valuesDancersToTrack[dancerToTrack].vAccJerkFromDancer[boneToTarget].GetValues();
        }
        velocity = valuesOfBones.x;
        acceleration = valuesOfBones.y;
        jerk = valuesOfBones.z;
    }


    void CalculateDataToSendOnPlayerMovement()
    {
        rt = true;
        CalculateDataToSend(rt);        
    }
      

    void SendDataToBoidSystem()
    {
        int a = 0;
        foreach (BoidControllerECSJobsFast cmp in cmpsBoidController)
        {
            cmp.boidSpeed = boidSpeed;
            cmp.alignmentWeight = alignmentWeight;
            cmp.separationWeight = separationWeight;
            cmp.cohesionWeight = cohesionWeight;
            cmp.targetWeight = targetWeight;
            cmp.targetPosition = dancersToTrackVelocity[dancerToTrack].bonesToTrack[boneToTarget].transform.position;
            a++;
        }
    }

    public void ChangeBoidValuesOnce(int index)
    {
        var values = Enum.GetValues(typeof(dancerToBoidPreset));

        int x = 0;
        foreach (dancerToBoidPreset item in values)
        {
            if (x == index)
            {
                _preset = item;
            }
            x++;
        }

        // THIS IS FOR VALUES THAT ARE NOT RELATED TO MOVEMENT
        bool b = false;
        CalculateDataToSend(b);
    }

    private void CalculateDataToSend(bool onRuntime)
    {
        switch (_preset)
        {
            case dancerToBoidPreset.fastCirclesInHip:
                if (onRuntime)
                {
                    boidSpeed = 70 + (velocity * 2.2f);
                    separationWeight = Mathf.Abs(jerk * 10);
                    alignmentWeight = Mathf.Clamp(Mathf.Abs(1 / (jerk * 10)), 0, 500);
                    cohesionWeight = Mathf.Clamp(Mathf.Abs(1 / (jerk * 10)), 0, 250);
                }
                else
                {

                }
                break;

            case dancerToBoidPreset.breakCircle:
                if (onRuntime)
                {
                    float f = Mathf.Abs(jerk * 10);
                    if (f <= 1)
                    {
                        alignmentWeight = 170;
                        cohesionWeight = 120;
                        separationWeight = 5;
                    }
                    else
                    {
                        separationWeight = Mathf.Abs(jerk * 2000);
                        alignmentWeight = 0;
                        cohesionWeight = 0;
                    }
                }
                else
                {
                    boidSpeed = 200;
                   
                }
                break;

            case dancerToBoidPreset.ControlVelocitySlow:
                if (onRuntime)
                {
                    boidSpeed = 5 + velocity;
                }
                else
                {
                    separationWeight = 45;
                    alignmentWeight = 20;
                    cohesionWeight = 5;
                }
                break;

            case dancerToBoidPreset.Nothing:
                break;

            default:
                break;
        }
    }

   
}
