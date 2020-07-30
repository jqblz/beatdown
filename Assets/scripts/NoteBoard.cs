using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NoteBoard : MonoBehaviour
{
    [SerializeField] private BoxCollider2D noteBox;
    [SerializeField] private LineRenderer topLine, bottomLine;
    [SerializeField] private GameObject arrowHolder;
    [SerializeField] private Note note_prefab;

    [SerializeField] private bool reversed;

    public double visualOffset;

    public double noteTime;
    public double speedMultiplier
    {
        get { return 100f / noteTime; }
        set { noteTime = 100f / value; }
    }

    private const double note_pre_time = 0.5;

    private RhythmController controller;
    private List<NoteData> notes;

    private float left_x, start_y, end_y, note_separation;

    private readonly int[] distances = { 1, 3, 2, 0 };

    //public AttackHandler attackHandler;
    //public AttackHandler.PlayerNum player;

    // Start is called before the first frame update
    void Start()
    {
        Vector2 worldPos = noteBox.transform.position;
        Vector2 bl = worldPos + noteBox.offset - noteBox.size / 2;
        Vector2 tr = worldPos + noteBox.offset + noteBox.size / 2;

        float note_size = arrowHolder.transform.Find("ArrowR").GetComponent<SpriteRenderer>().size.x;

        topLine.SetPositions(   new[] { new Vector3(bl.x, tr.y), new Vector3(tr.x, tr.y) });
        bottomLine.SetPositions(new[] { new Vector3(bl.x, bl.y), new Vector3(tr.x, bl.y) });
        float line_left = topLine.GetPosition(0).x;
        float line_right = topLine.GetPosition(1).x;
        left_x = bl.x + note_size;
        note_separation = (tr.x - bl.x - 2*note_size) / 3;
        start_y = bl.y;
        end_y = tr.y;

        // Set up the target arrows
        arrowHolder.transform.Find("ArrowR").transform.position = new Vector2(GetNoteX(NoteType.Right), end_y);
        arrowHolder.transform.Find("ArrowU").transform.position = new Vector2(GetNoteX(NoteType.Up), end_y);
        arrowHolder.transform.Find("ArrowD").transform.position = new Vector2(GetNoteX(NoteType.Down), end_y);
        arrowHolder.transform.Find("ArrowL").transform.position = new Vector2(GetNoteX(NoteType.Left), end_y);
    }

    public void Initialize(RhythmController controller_, IEnumerable<NoteData> notes_)
    {
        controller = controller_;
        notes = (reversed ? notes_.Select(ReverseNote) : notes_).ToList();
        controller.AddSubBeatCallback(SpawnNotes);
        controller.AddStrongBeatCallback(AnimateLines);
        Debug.Log("Note controller " + gameObject.name + " ready with " + notes.Count() + " notes");
        //Debug.Log("Beat multiplier = " + controller.strongBeatSpeed);
    }

    public static NoteData ReverseNote(NoteData note)
    {
        switch (note.direction)
        {
            case NoteType.Right:
                return new NoteData(note.beat, NoteType.Left);
            case NoteType.Left:
                return new NoteData(note.beat, NoteType.Right);
            default:
                return note;
        }
    }

    public static int NoteSubdivision(double beat)
    {
        if (Note.DoubleIsInteger(beat))
        {
            return 1;
        } else
        {
            return System.Convert.ToInt32(System.Math.Round(1f / (beat - System.Math.Truncate(beat))));
        }
    }

    private void SpawnNotes(double current_beat)
    {   
        foreach (NoteData note in notes.Where(note => ShouldSpawnNote(note.beat)))
        {
            //Debug.Log(controller.beatMultiplier + " " + note.beat);
            Instantiate(note_prefab).Initialize(controller, this, note.beat, note.direction);
        }
        
    }

    private bool ShouldSpawnNote(double beat)
    {
        double until = controller.TimeUntilBeat(beat);
        return until > 0
            && until < noteTime + note_pre_time
            /*&& Note.DoubleIsInteger(beat * controller.subBeatSpeed)*/;
    }

    private void AnimateLines(double _)
    {
        StartCoroutine(LineBulge());
    }

    private IEnumerator LineBulge()
    {
        topLine.widthMultiplier = .2f;
        bottomLine.widthMultiplier = .2f;
        yield return new WaitForSeconds(0.06f);
        topLine.widthMultiplier = .1f;
        bottomLine.widthMultiplier = .1f;
    }

    public void DeleteNote(NoteData target)
    {
        Debug.Log("deleteNote called, target note is " + target.direction + " on beat " + target.beat);
        foreach (var note in FindObjectsOfType<Note>()) {
            Debug.Log("checking " + note.type + " on beat " + note.beat);
            if (note.type == target.direction && note.beat == target.beat) {
                Destroy(note);
                Debug.Log("found a match. deleting");
                return;
            }
            Debug.Log("no match");
        }
    }

    public float GetNoteX(NoteType type)
    {
        return left_x + note_separation * distances[(int)type];
    }

    public Vector2 GetStartPos(NoteType type)
    {
        return new Vector2(GetNoteX(type), start_y);
    }

    public Vector2 GetEndPos(NoteType type)
    {
        return new Vector2(GetNoteX(type), end_y);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddCurrentNote(NoteType noteType)
    {
        //attackHandler.AddNote(player, noteType);
    }
}
