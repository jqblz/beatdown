using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum NoteType { down = 0, right = 1, up = 2, left = 3 };

[System.Serializable]
public class NoteData
{
    public double beat;
    public NoteType direction;

    public NoteData(double beat_, NoteType direction_)
    {
        beat = beat_;
        direction = direction_;
    }
}

public class RhythmController : MonoBehaviour {
    public AudioSource audioSource;

    public string path;
    public string simfileName;

    public int strongBeatLog;
    public double strongBeatSpeed { get; private set; }

    public int subBeatLog;
    public double subBeatSpeed { get; private set; }

    [SerializeField] private NoteBoard[] boards;
    
    public BeatLerper beatLerper { get; private set; }

    public bool playing = false;
    public bool ready = false;

    private Song song;
    private IEnumerable<NoteData> notes_iter;

    private double old_beat;
    private double song_start_time;

    public delegate void OnBeat(double beat);
    private OnBeat onStrongBeat, onSubBeat;

    public Renderer bg_render;

    // Use this for initialization
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        song = BeatFile.ReadStepfile("Assets/Resources/music/" + path + "/" + simfileName);
        string audio_path = "music/" + path + "/" + System.IO.Path.GetFileNameWithoutExtension(song.metadata["MUSIC"]);
        Debug.Log(audio_path);
        audioSource.clip = Resources.Load<AudioClip>(audio_path);

        /*try
        {
            string bg_path = "music/" + path + "/" + System.IO.Path.GetFileNameWithoutExtension(song.metadata["BACKGROUND"]);
            Debug.Log(bg_path);
            bg_render.material.mainTexture = Resources.Load<Texture>(bg_path);
        }
        catch { }*/

        beatLerper = new BeatLerper(song.bpmEvents, song.offset);
        notes_iter = song.notes.OrderBy(note => note.beat);
        strongBeatSpeed = System.Math.Pow(2, strongBeatLog);
        subBeatSpeed = System.Math.Pow(2, subBeatLog);
        ready = true;

        foreach (NoteBoard board in boards)
        {
            board.Initialize(this, notes_iter);
        }

        Debug.Log("Ready.");
    }

    public void AddStrongBeatCallback(OnBeat callback)
    {
        onStrongBeat += callback;
    }

    public void AddSubBeatCallback(OnBeat callback)
    {
        onSubBeat += callback;
    }

    public void PlaySong()
    {
        Debug.Log(audioSource.clip);
        audioSource.Play(0);
        song_start_time = AudioSettings.dspTime;
        playing = true;
        Debug.Log("Playing!");
    }

    public void Stop()
    {
        audioSource.Stop();
        playing = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (playing && SongTime() > song.offset)
        {
            double new_beat = SongBeat();
            //Debug.Log("beat " + old_beat + "->" + new_beat + ", " + old_beat * strongBeatSpeed + "->" + new_beat * strongBeatSpeed);
            if (onStrongBeat != null && System.Math.Truncate(new_beat * strongBeatSpeed) != System.Math.Truncate(old_beat * strongBeatSpeed))
            {
                onStrongBeat(new_beat);
                //Debug.Log("executing strong beat callbacks");
            }
            if (onSubBeat != null && System.Math.Truncate(new_beat * subBeatSpeed) != System.Math.Truncate(old_beat * subBeatSpeed))
            {
                onSubBeat(new_beat);
                //Debug.Log("executing sub beat callbacks");
            }
            old_beat = new_beat;
        }
    }
    
    public double SongTime ()
    {
        return AudioSettings.dspTime - song_start_time;
    }

    public double SongBeat()
    {
        return beatLerper.BeatFromTime(SongTime());
    }

    public double TimeUntilBeat(double beat)
    {
        return beatLerper.TimeFromBeat(beat) - SongTime();
    }

    public double TimeToNextStrongBeat()
    {
        return TimeUntilBeat(System.Math.Ceiling(SongBeat() * strongBeatSpeed) / strongBeatSpeed);
    }

    public double TimeToNextSubBeat()
    {
        return TimeUntilBeat(System.Math.Ceiling(SongBeat() * subBeatSpeed) / subBeatSpeed);
    }
}

public enum BPMEventType { BPMChange, Stop };

[System.Serializable]
public class BPMEvent
{
    public BPMEventType type;
    public double beat;
    public double newBPM;
    public double stopDuration;

    public BPMEvent(BPMEventType type, double beat_, double param)
    {
        beat = beat_;
        if (type == BPMEventType.BPMChange)
        {
            newBPM = param;
        }
        else
        {
            stopDuration = param;
        }
    }
}

// DO NOT MESS WITH THIS CLASS FOR THE LOVE OF PALUTENA
public class BeatLerper
{
    [System.Serializable]
    public class BeatLine
    {
        public double start, end, bps, intercept, offset;

        public BeatLine(double start_, double end_, double bps_, double intercept_, double offset_)
        {
            start = start_; end = end_; bps = bps_; intercept = intercept_; offset = offset_;
        }

        public double BeatFromTime(double time)
        {
            return bps * (time + offset) + intercept;
        }

        public double TimeFromBeat(double beat)
        {
            return (beat - intercept) / bps - offset;
        }

        public bool TimeWithinBounds(double time)
        {
            return (time - offset) > start && (time - offset) <= end;
        }
    }

    private BeatLine[] lines;

    public BeatLerper(BPMEvent[] events_arr, double offset)
    {
        IEnumerable<BPMEvent> events = events_arr.OrderBy(ev => ev.beat);
        List<BeatLine> lines_l = new List<BeatLine>();

        BPMEvent ev1 = events.First();
        if (ev1.type == BPMEventType.Stop)
        {
            Debug.LogException(new System.Exception("First BPM event must be a BPM change"));
        }
        lines_l.Add(new BeatLine(0, double.PositiveInfinity, ev1.newBPM / 60, 0, offset));
        
        foreach (BPMEvent ev in events.Skip(1))
        {
            BeatLine left = lines_l.Last();
            left.end = left.TimeFromBeat(ev.beat);
            lines_l[lines_l.Count - 1] = left;

            if (ev.type == BPMEventType.BPMChange)
            {
                double new_bps = ev.newBPM / 60;
                lines_l.Add(new BeatLine(left.end, double.PositiveInfinity, new_bps, ev.beat - new_bps * left.end, offset));
            }
            else // it's a stop
            {
                lines_l.Add(new BeatLine(left.end, left.end + ev.stopDuration, 0, ev.beat, offset));
                lines_l.Add(new BeatLine(left.end + ev.stopDuration, double.PositiveInfinity, left.bps, ev.beat - left.bps * (left.end + ev.stopDuration), offset));
            }
        }

        lines = lines_l.ToArray();
    }

    public double BeatFromTime(double time)
    {
        return lines.AsEnumerable().Where( line => line.TimeWithinBounds( time ) )
                                   .First( )
                                   .BeatFromTime( time );
    }

    public double TimeFromBeat(double beat)
    {
        return lines.AsEnumerable().Where( line => line.TimeWithinBounds( line.TimeFromBeat( beat ) ) )
                                   .First( )
                                   .TimeFromBeat( beat );
    }
}