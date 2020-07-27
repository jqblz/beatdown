using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Flags]
public enum InputChord
{
    None = 0,
    Left = 1,
    Down = 2,
    Up = 4,
    Right = 8
}
public static class ICExtension
{
    public static string FriendlyRepr(this InputChord ic)
    {
        return ((ic & InputChord.Left) != 0  ? "<" : "")
             + ((ic & InputChord.Down) != 0  ? "v" : "")
             + ((ic & InputChord.Up) != 0    ? "^" : "")
             + ((ic & InputChord.Right) != 0 ? ">" : "");
    }
}

public class InputHandler : MonoBehaviour
{    

    public double inputLatency;

    public string upButton;
    public string downButton;
    public string leftButton;
    public string rightButton;

    public FightController fightController;
    public RhythmController rhythmController;

    private List<InputChord> inputs;
    private InputChord chordBuffer;

    // Start is called before the first frame update
    void Start()
    {
        chordBuffer = InputChord.None;
        rhythmController.AddSubBeatCallback(OnSubBeat);
        inputs = new List<InputChord>();
    }

    void OnSubBeat(double beat)
    {
        inputs.Add(chordBuffer);
        chordBuffer = InputChord.None;
    }

    public List<InputChord> GetInputs()
    {
        var temp = inputs;
        inputs = new List<InputChord>();
        return temp;
    }

    // Update is called once per frame
    void Update()
    {
        bool up = Input.GetButtonDown(upButton);
        bool down = Input.GetButtonDown(downButton);
        bool left = Input.GetButtonDown(leftButton);
        bool right = Input.GetButtonDown(rightButton);

        if (left)  chordBuffer |= InputChord.Left;
        if (down)  chordBuffer |= InputChord.Down;
        if (up)    chordBuffer |= InputChord.Up;
        if (right) chordBuffer |= InputChord.Right;
    }
}
