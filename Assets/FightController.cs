using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FightController : MonoBehaviour
{
    public InputHandler p1Input, p2Input;

    private RhythmController rhythm;

    // Start is called before the first frame update
    void Start()
    {
        rhythm = GetComponent<RhythmController>();
        rhythm.AddStrongBeatCallback(OnStrongBeat);
        rhythm.AddSubBeatCallback(OnSubBeat);
    }

    

    void OnStrongBeat(double beat)
    {
        /*Debug.Log("pulled command " + */p1Input.FinalizeCommand()/*.Stringify())*/;
    }

    void OnSubBeat(double beat)
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
