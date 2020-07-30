using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public static string Stringify(this InputChord ic)
    {
        return ((ic & InputChord.Left) != 0  ? "<" : "_")
             + ((ic & InputChord.Down) != 0  ? "v" : "_")
             + ((ic & InputChord.Up) != 0    ? "^" : "_")
             + ((ic & InputChord.Right) != 0 ? ">" : "_");
    }
}

public class Command
{
    public double timingScore;
    public int syncCount;
    public InputChord input1, input2;

    public Command()
    {
        timingScore = 0;
        syncCount = 0;
        input1 = input2 = InputChord.None;
    }

    public Command(double t, int s, InputChord i1, InputChord i2)
    {
        timingScore = t;
        syncCount = s;
        input1 = i1;
        input2 = i2;
    }

    public string Stringify()
    {
        if (input2 == InputChord.None) {
            return input1.Stringify();
        }
        return input1.Stringify() + "|" + input2.Stringify();
    }
}

public class InputHandler : MonoBehaviour
{    

    public double inputLatency;

    public string upButton;
    public string downButton;
    public string leftButton;
    public string rightButton;

    [SerializeField] private FightController fightController;
    [SerializeField] private RhythmController rhythmController;
    private NoteBoard noteBoard;

    private List<InputChord> inputs;
    private List<double> inputTimes;
    private InputChord chordBuffer;
    private Queue<NoteData> notes;
    private List<NoteData> currentNotes; // the notes that are in the current sub-beat
    private double totalTimingGap;
    private int singleInputCount;
    private Command command;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("InputHandler Start() was called");
        noteBoard = GetComponent<NoteBoard>();
        chordBuffer = InputChord.None;
        rhythmController.AddSubBeatCallback(OnSubBeat);
        rhythmController.AddOnPlayCallback(OnPlay);
        inputs = new List<InputChord>();
        notes = new Queue<NoteData>(rhythmController.GetAllNotes());

        currentNotes = new List<NoteData>();
        totalTimingGap = 0;
        singleInputCount = 0;
        command = new Command();
    }

    private void OnPlay()
    {
        PopulateCurrentNotes();
    }

    private void PopulateCurrentNotes()
    {
        if (notes.Count == 0) { return; }
        
        double target = rhythmController.NextSubBeat();
        //Debug.Log("current beat is " + rhythmController.SongBeat() + ", target beat is " + target);
        while (notes.Peek().beat < target - 1f / rhythmController.subBeatSpeed) {
            //Debug.Log("note beat is " + notes.Peek().beat + ", discarding");
            notes.Dequeue();
        }
        while (notes.Peek().beat < target) {
            //Debug.Log("note beat is " + notes.Peek().beat + ", adding");
            currentNotes.Add(notes.Dequeue());
        }    
    }

    void OnSubBeat(double beat)
    {
        inputs.Add(chordBuffer);
        //Debug.Log("registered chord " + chordBuffer.Stringify());
        chordBuffer = InputChord.None;
        currentNotes.Clear();
        PopulateCurrentNotes();
    }

    // Only to be called by FightController in its strong beat callback
    public Command FinalizeCommand()
    {
        //Debug.Log(command.Stringify());
        //Debug.Log("inputs list " + string.Join(" ", inputs.Select(i => i.Stringify())));
        //command.timingScore = totalTimingGap / singleInputCount;
        totalTimingGap = 0;
        singleInputCount = 0;
        if (inputs.Count == 0) {
            command.input1 = command.input2 = InputChord.None;
        } else if (inputs.Count == 1) {
            command.input1 = inputs[0];
            command.input2 = InputChord.None;
        } else if (inputs[0] == InputChord.None) {
            command.input1 = inputs[1];
            command.input2 = InputChord.None;
        } else {
            command.input1 = inputs[0];
            command.input2 = inputs[1];
        }
        inputs.Clear();

        var temp = command;
        command = new Command();
        return temp;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown(leftButton))
        {
            RegisterSingleInput(NoteType.Left, InputChord.Left);
        }
        if (Input.GetButtonDown(rightButton))
        {
            RegisterSingleInput(NoteType.Right, InputChord.Right);
        }
        if (Input.GetButtonDown(upButton))
        {
            RegisterSingleInput(NoteType.Up, InputChord.Up);
        }
        if (Input.GetButtonDown(downButton))
        {
            RegisterSingleInput(NoteType.Down, InputChord.Down);
        }
        //Debug.Log("here again " + command.Stringify());
    }

    private void RegisterSingleInput(NoteType dir, InputChord alsoDir)
    {
        chordBuffer |= alsoDir;
        singleInputCount++;
        var match = GetNextNote(dir);
        if (match != null) {
            totalTimingGap += rhythmController.TimeUntilBeat(match.beat);
            command.syncCount++;
            noteBoard.DeleteNote(match);
        }
    }

    private NoteData GetNextNote(NoteType dir)
    {
        for (int i=0; i < currentNotes.Count; i++) {
            if (currentNotes[i].direction == dir) {
                var match = currentNotes[i];
                currentNotes.RemoveAt(i);
                return match;
            }
        }
        return null;
    }
}
