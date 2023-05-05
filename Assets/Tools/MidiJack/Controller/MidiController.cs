using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MidiController : MonoBehaviour
{

    DancerToBoidController cmpDTBController;
    public Gradient boidColors;

    [SerializeField] Light boidLight;

    [SerializeField] Material boidMaterial;
    /// <summary>
    /// Controls
    /// </summary>
    float latefive;
    float latefivetop;
    float latefivemid;
    float latefivebot;
    bool emissive;

    private void Awake()
    {
        cmpDTBController = GameObject.FindObjectOfType<DancerToBoidController>();
    }

    /*private void FixedUpdate()
    {
        
        /// DANCER CONTROLLER
        DancerValuesOnMidi();

        //// FOR BOID MATERIAL
        BoidMaterial();

        /// NOT USED unitl here
        float six = MidiJack.MidiMaster.GetKnob(53);
        boidLight.intensity = six * 50;

        float seven = MidiJack.MidiMaster.GetKnob(57);
        float eight = MidiJack.MidiMaster.GetKnob(61);

    }*/

   /* private void DancerValuesOnMidi()
    {
        float one = MidiJack.MidiMaster.GetKnob(19);
        float two = MidiJack.MidiMaster.GetKnob(23);
        float three = MidiJack.MidiMaster.GetKnob(27);
        float four = MidiJack.MidiMaster.GetKnob(31);

        float master = MidiJack.MidiMaster.GetKnob(62);

        cmpDTBController.targetWeight = master * 500;

        if (cmpDTBController._preset == dancerToBoidPreset.Nothing)
        {
            cmpDTBController.boidSpeed = one * 400;
            cmpDTBController.alignmentWeight = two * 150;
            cmpDTBController.cohesionWeight = three * 75;
            cmpDTBController.separationWeight = four * 150;
        }

    }*/

    /*private void BoidMaterial()
    {
       
        float five = MidiJack.MidiMaster.GetKnob(49);
        float fivetop = MidiJack.MidiMaster.GetKnob(46);
        float fivemid = MidiJack.MidiMaster.GetKnob(47);
        //float fivebot = MidiJack.MidiMaster.GetKnob(48);

        if (MidiJack.MidiMaster.GetKeyDown(MidiJack.MidiChannel.All, 14))
        {

            if (emissive)
            {
                emissive = true;
            }
            else
            {
                emissive = false;
            }
            ChangeColor(five, fivetop, fivemid);
        }

        if (five != latefive || fivetop != latefivetop || fivemid != latefivemid)
        {
            ChangeColor(five, fivetop, fivemid);
        }
    }*/

 
    /// Color / metallic / Smooth
    /*void ChangeColor(float c, float m, float r)
    {
        ChangeMaterial(boidColors.Evaluate(c),m,r,emissive);
        latefive = c;
        latefivetop = m;
        latefivemid = r;
    }

    public void ChangeMaterial(Color c, float met, float smooth, bool emmisive)
    {

        boidMaterial.SetColor("_Color", c);
        boidMaterial.SetFloat("_Metallic", met);
        boidMaterial.SetFloat("_Glossiness", smooth);



        if (emmisive)
        {
            boidMaterial.EnableKeyword("_EMISSION");
            boidMaterial.SetColor("_EmissionColor", c);
        }
        else
        {
            boidMaterial.DisableKeyword("_EMISSION");
        }
    }*/

    /////////////////////////////////////////////////////////////////////////////////Delegates///////////////////////////////////////////////////////////////////////////////
    void NoteOn(MidiJack.MidiChannel channel, int note, float velocity)
    {
        Debug.Log("NoteOn: " + channel + "," + note + "," + velocity);
    }

    void NoteOff(MidiJack.MidiChannel channel, int note)
    {
        Debug.Log("NoteOff: " + channel + "," + note);
    }

    void Knob(MidiJack.MidiChannel channel, int knobNumber, float knobValue)
    {
        switch (knobNumber)
        {
            /////////////////////////////// -----BOID MATERIAL----- ///////////////////////////////////
            case 49:
                boidMaterial.SetColor("_Color", boidColors.Evaluate(knobValue));
                break;

            case 46:
                boidMaterial.SetFloat("_Metallic", knobValue);
                break;

            case 47:
                boidMaterial.SetFloat("_Glossiness", knobValue);
                break;

            case 53:
                boidLight.intensity = knobValue * 50;
                break;

            /////////////////////////////// -----BOID VALUES----- ///////////////////////////////////

            case 19:
                cmpDTBController.boidSpeed = knobValue * 400;
                break;

            case 23:
                cmpDTBController.alignmentWeight = knobValue * 150;
                break;

            case 27:
                cmpDTBController.cohesionWeight = knobValue * 150;
                break;

            case 31:
                cmpDTBController.separationWeight = knobValue * 150;
                break;

            case 62:
                cmpDTBController.targetWeight = knobValue * 500;
                break;

            default:
                break;
        }
    }

    void OnEnable()
    {
        MidiJack.MidiMaster.noteOnDelegate += NoteOn;
        MidiJack.MidiMaster.noteOffDelegate += NoteOff;
        MidiJack.MidiMaster.knobDelegate += Knob;
    }

    void OnDisable()
    {
        MidiJack.MidiMaster.noteOnDelegate -= NoteOn;
        MidiJack.MidiMaster.noteOffDelegate -= NoteOff;
        MidiJack.MidiMaster.knobDelegate -= Knob;
    }


}
