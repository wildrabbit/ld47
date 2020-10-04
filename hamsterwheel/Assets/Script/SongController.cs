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

    int signatureHi;
    int signatureLo;

    int nextNoteIdx;

    float beatsShownInAdvance = 3;

    IMetronome Metronome;

    List<NoteMarker> notesSpawned;

    bool started;

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
        // Sync

        started = false;
    }

    // Update is called once per frame
    void Update()
    {
        double now = AudioSettings.dspTime;
        double elapsedTotal = now - songStartTime;
        double elapsedFirstBeat = now - firstBeatTime;
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
            bool remove = note.UpdateNote(currentBeats, beatsInAdvance);
            if(remove)
            {
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

        noteHitType = NoteHitType.Skipped;

        if(notesSpawned.Count > 0)
        {
            var currentNote = notesSpawned[0];

            if(currentNote.note != note)
            {
                noteHitType = NoteHitType.Missed;
            }
            else
            {
                // Current note
                double beatDiff = now - notesSpawned[0].beat;
                if (Math.Abs(beatDiff) <= hitThreshold)
                {
                    noteHitType = NoteHitType.OK;
                }
                else
                {
                    noteHitType = NoteHitType.Missed;
                }
            }

            return true;
        }
        return false;
    }
}
