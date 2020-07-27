using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour {
    [SerializeField] private Sprite quarterSprite;
    [SerializeField] private Sprite eighthSprite;

    private RhythmController rhythm;
    private NoteBoard noteBoard;
    private double beat;
    private NoteType type;

    private bool added = false;

    private double visualOffset;

    private new SpriteRenderer renderer;

	// Use this for initialization
	void Start () {
        renderer = GetComponent<SpriteRenderer>();
        renderer.enabled = false;
	}

    public void Initialize(RhythmController c, NoteBoard n, double b, NoteType t)
    {
        renderer = GetComponent<SpriteRenderer>();
        rhythm = c;
        noteBoard = n;
        beat = b;
        type = t;
        visualOffset = n.visualOffset;

        renderer.sprite = DoubleIsInteger(beat) ? quarterSprite : eighthSprite;
        renderer.color = new Color(1, 1, 1, 0.5f); // half transparency
        transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 90 * (int)type));
    }

    public static bool DoubleIsInteger(double x)
    {
        return (x - System.Math.Truncate(x)) < float.Epsilon;
    }

	// Update is called once per frame
	void Update () {
        
        if(beat == (Mathf.Floor((float)rhythm.SongBeat()) + 1) && !added)
        {
            noteBoard.AddCurrentNote(type);
            added = true;
        }
        double until = rhythm.beatLerper.TimeFromBeat(beat) - rhythm.SongTime() + visualOffset;
        if (until < 0)
        {
            //Debug.Log("Note destroying self");
            Destroy(gameObject);
            return;
        }

        //renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, (float)(1.0 - until));

        transform.position = Vector2.Lerp(noteBoard.GetStartPos(type), noteBoard.GetEndPos(type), (float)(1 - until / noteBoard.noteTime));
        if (!renderer.enabled && until <= noteBoard.noteTime)
        {
            renderer.enabled = true;
        }
    }
}
