using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongController : MonoBehaviour
{
    public Song CurrentSong;
    public AudioSource SongPlayer;
    public GameObject WebMetronome;
    public GameObject ProcMetronome;

    public Transform startNote;
    public Transform endNote;
    public Transform container;

    public NoteMarker NotePrefab;

    double songStartTime;
    double firstBeatTime;
    double bpm;
    double secsPerBeat;

    public double hitThreshold = 0.3f;
    public double perfectThreshold = 0.1f;
    public float expireRatio = 1.2f;

    int signatureHi;
    int signatureLo;

    int nextNoteIdx;

    float beatsShownInAdvance = 3;
    bool foundFirst;

    IMetronome Metronome;

    List<NoteMarker> notesSpawned;

    public event Action<float, bool> BeatExpired;

    public void Init()
    {
        ProcMetronome.SetActive(true);
        WebMetronome.SetActive(false);
        Metronome = ProcMetronome.GetComponent<IMetronome>();


        if (CurrentSong != null && CurrentSong.songSheet != null)
        {
            CurrentSong.LoadSheet();
            //SongPlayer.clip = CurrentSong.songResource;
        }

        notesSpawned = new List<NoteMarker>();
    }

    private void Awake()
    {
        Init();    
    }
    // Start is called before the first frame update
    void Start()
    {
        bpm = CurrentSong.bpm;
        secsPerBeat = 60 / bpm;
        signatureHi = CurrentSong.signatureHi;
        signatureLo = CurrentSong.signatureLo;

        Metronome.BPM = CurrentSong.bpm; // Signature should be fetched from the song too!
        Metronome.SetSignature(signatureHi, signatureLo);


        songStartTime = AudioSettings.dspTime;
        firstBeatTime = songStartTime + CurrentSong.Offset; // + level offset

        Metronome.SetStartTime(songStartTime); // Reuse for music clip
        nextNoteIdx = 0;
        foundFirst = false;

        float hitFirst = (float)(beatsShownInAdvance - secsPerBeat * hitThreshold) / beatsShownInAdvance;
        float hitLast = (float)(beatsShownInAdvance + secsPerBeat * hitThreshold) / beatsShownInAdvance;
        //1 => ahora == actual

        hitBefore = Vector2.LerpUnclamped(startNote.position, endNote.position, hitFirst);
        hitAfter = Vector2.LerpUnclamped(startNote.position, endNote.position, hitLast);

        float perfectFirst = (float)(beatsShownInAdvance - secsPerBeat * perfectThreshold) / beatsShownInAdvance;
        float perfectLast = (float)(beatsShownInAdvance + secsPerBeat * perfectThreshold) / beatsShownInAdvance;
        perfectBefore = Vector2.LerpUnclamped(startNote.position, endNote.position, perfectFirst);
        perfectAfter = Vector2.LerpUnclamped(startNote.position, endNote.position, perfectLast);

    }
    Vector2 hitBefore, hitAfter, perfectBefore, perfectAfter;

    // Update is called once per frame
    void Update()
    {
        Debug.DrawLine(hitBefore, hitBefore + Vector2.down);
        Debug.DrawLine(hitAfter, hitAfter + Vector2.down);
        Debug.DrawLine(perfectBefore, perfectBefore + Vector2.down, Color.green);
        Debug.DrawLine(perfectAfter, perfectAfter + Vector2.down, Color.green);
        double now = AudioSettings.dspTime;
        double elapsedTotal = now - songStartTime;
        double elapsedFirstBeat = now - firstBeatTime;
        if(now > elapsedFirstBeat && !foundFirst)
        {
            Debug.Log("START!!");
            foundFirst = true;
        }
        if (now < firstBeatTime) return;

        float currentSongBeats = (float)(elapsedFirstBeat / secsPerBeat);

        var beats = CurrentSong.beats;

        if(nextNoteIdx < beats.Count)
        {
            var nextBeatInfo = beats[nextNoteIdx];
            if (nextNoteIdx < beats.Count && nextBeatInfo.Beat < currentSongBeats + beatsShownInAdvance)
            {
                GenerateNextNote(nextBeatInfo);
                nextNoteIdx++;
            }
        }
        // Wait for end of the song, then notify so we can update the playlist / loop the current song
        UpdateNotes(currentSongBeats, beatsShownInAdvance);
    }

    void UpdateNotes(float currentBeats, float beatsInAdvance)
    {
        var toRemove = new List<NoteMarker>();
        foreach(var note in notesSpawned)
        {           
            note.UpdateNote(currentBeats, beatsInAdvance, out float ratio);
            bool remove = ratio > expireRatio;
            if(remove)
            {
                BeatExpired?.Invoke(note.beat, note.gameObject.activeInHierarchy);
                Destroy(note.gameObject);
                toRemove.Add(note);
            }
        }
        foreach(var remove in toRemove)
        {
            notesSpawned.Remove(remove);
        }
        toRemove.Clear();
    }

    private void GenerateNextNote(BeatEntry beatInfo)
    {
        var instance = Instantiate(NotePrefab);
        instance.Init(beatInfo.ExpectedNote, beatInfo.Beat, container, startNote.position, endNote.position);
        notesSpawned.Add(instance);
    }

    public bool RecordedNote(NoteType note, out NoteHitType noteHitType)
    {
        double now = AudioSettings.dspTime;
        double elapsedTotal = now - songStartTime;
        double elapsedFirstBeat = now - firstBeatTime;
        float nowBeats = (float)(elapsedFirstBeat / secsPerBeat);

        noteHitType = NoteHitType.Skipped;

        if(notesSpawned.Count > 0)
        {
            var currentNote = notesSpawned[0];

            if(currentNote.note != note)
            {
                noteHitType = NoteHitType.WrongKey;
                ProcessNote(currentNote);
            }
            else
            {
                // Current note
                double beatDiff = nowBeats - notesSpawned[0].beat;
                if (Math.Abs(beatDiff) <= hitThreshold)
                {
                    noteHitType = beatDiff <= perfectThreshold ? NoteHitType.Perfect : NoteHitType.OK;
                    ProcessNote(currentNote);
                }
                else
                {
                    noteHitType = NoteHitType.WrongTime;
                    ProcessNote(currentNote);
                }
            }

            return true;
        }
        return false;
    }

    public void ProcessNote(NoteMarker note)
    {
        notesSpawned.Remove(note);
        // TODO: Add hit type feedback, perhaps mark + coroutine to avoid processing it before destruction
        Destroy(note.gameObject);
    }
}
