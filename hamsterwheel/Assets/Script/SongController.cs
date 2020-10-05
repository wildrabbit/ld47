using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    double lastBeatTime;
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
    public NoteMarker noteHelper;

    public event Action<float, bool> BeatExpired;
    //public event Action SongFinished;
    NoteMarker firstNote;

    public bool stopped;

    double _lastClipTime;
    public void Init()
    {
#if true
        ProcMetronome.SetActive(false);
        WebMetronome.SetActive(true);
        Metronome = WebMetronome.GetComponent<IMetronome>();
#else
        ProcMetronome.SetActive(true);
        WebMetronome.SetActive(false);
        Metronome = ProcMetronome.GetComponent<IMetronome>();
#endif

        Metronome.Ticked += OnMetroTicked;

        if (CurrentSong != null && CurrentSong.songSheet != null)
        {
            CurrentSong.LoadSheet();
            //SongPlayer.clip = CurrentSong.songResource;
        }

        notesSpawned = new List<NoteMarker>();
        noteHelper.gameObject.SetActive(false);
    }

    private void OnMetroTicked(int arg1, double arg2, bool arg3)
    {
        if(noteHelper.gameObject.activeInHierarchy)
        {
            noteHelper.transform.DOPunchScale(0.1f * Vector3.one, 0.2f);
        }
    }

    private void OnDestroy()
    {
        Metronome.Ticked -= OnMetroTicked;
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
        
        firstBeatTime = songStartTime + CurrentSong.Offset;

        var lastBeat = CurrentSong.beats[CurrentSong.beats.Count - 1];
        int lastCompass = ((int)lastBeat.Beat) / signatureHi;

        lastBeatTime = signatureHi * (lastCompass + 2) * 60 / bpm;
        if(CurrentSong.songResource != null)
        {
            lastBeatTime = CurrentSong.songResource.length - CurrentSong.Offset;
            SongPlayer.clip = CurrentSong.songResource;
            SongPlayer.PlayScheduled(songStartTime);
        }

        Metronome.SetStartTime(songStartTime); // Reuse for music clip
        nextNoteIdx = 0;
        foundFirst = false;

        float hitFirst = (float)(beatsShownInAdvance - secsPerBeat * hitThreshold) / beatsShownInAdvance;
        float hitLast = (float)(beatsShownInAdvance + secsPerBeat * hitThreshold) / beatsShownInAdvance;
        hitBefore = Vector2.LerpUnclamped(startNote.position, endNote.position, hitFirst);
        hitAfter = Vector2.LerpUnclamped(startNote.position, endNote.position, hitLast);

        float perfectFirst = (float)(beatsShownInAdvance - secsPerBeat * perfectThreshold) / beatsShownInAdvance;
        float perfectLast = (float)(beatsShownInAdvance + secsPerBeat * perfectThreshold) / beatsShownInAdvance;
        perfectBefore = Vector2.LerpUnclamped(startNote.position, endNote.position, perfectFirst);
        perfectAfter = Vector2.LerpUnclamped(startNote.position, endNote.position, perfectLast);

        stopped = false;

    }
    Vector2 hitBefore, hitAfter, perfectBefore, perfectAfter;

    // Update is called once per frame
    void Update()
    {
        if(stopped)
        {
            // Don't stop song playback
            return; 
        }

#if UNITY_EDITOR
        Debug.DrawLine(hitBefore, hitBefore + Vector2.down);
        Debug.DrawLine(hitAfter, hitAfter + Vector2.down);
        Debug.DrawLine(perfectBefore, perfectBefore + Vector2.down, Color.green);
        Debug.DrawLine(perfectAfter, perfectAfter + Vector2.down, Color.green);
#endif

        double now = AudioSettings.dspTime;
        double elapsedTotal = now - songStartTime;
        double elapsedFirstBeat = now - firstBeatTime;
        float currentSongBeats = (float)(elapsedFirstBeat / secsPerBeat);

        if (now > elapsedFirstBeat && !foundFirst)
        {
            Debug.Log("START!!");
            // Show stuff on screen
            foundFirst = true;
        }
        if (now < firstBeatTime) return;

        float lastBeat = (float)( lastBeatTime / secsPerBeat);
        if (currentSongBeats >= lastBeat)
        {
            Debug.Log("LOOP");
            float delta = currentSongBeats - lastBeat;
            ClearNotes(); // just in case
            firstBeatTime = now - delta + CurrentSong.Offset;
            elapsedFirstBeat = now - firstBeatTime;
            currentSongBeats = (float)(elapsedFirstBeat / secsPerBeat);
            var lastBeatInSheet = CurrentSong.beats[CurrentSong.beats.Count - 1];
            int lastCompass = ((int)lastBeatInSheet.Beat) / signatureHi;
            lastBeatTime = signatureHi * (lastCompass + 2) * 60 / bpm;
            if (CurrentSong.songResource != null)
            {
                lastBeatTime = CurrentSong.songResource.length - CurrentSong.Offset;
            }
            nextNoteIdx = 0;
        }
        

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


        noteHelper.gameObject.SetActive(notesSpawned.Count > 0);
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
                Metronome.Ticked -= note.Ticked;

                Destroy(note.gameObject);
                toRemove.Add(note);
            }
        }

        foreach(var remove in toRemove)
        {
            notesSpawned.Remove(remove);
        }
        toRemove.Clear();

        UpdateHelper();
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
                    noteHitType = Math.Abs(beatDiff) <= perfectThreshold ? NoteHitType.Perfect : NoteHitType.OK;
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

    public void GameWon()
    {
        stopped = true;
        ClearNotes();
        noteHelper.gameObject.SetActive(false);

    }

    void ClearNotes()
    {
        foreach (var note in notesSpawned)
        {
            Metronome.Ticked -= note.Ticked;
            Destroy(note.gameObject);
        }
        notesSpawned.Clear();
        firstNote = null;
    }

    public void ProcessNote(NoteMarker note)
    {
        notesSpawned.Remove(note);
        // TODO: Add hit type feedback, perhaps mark + coroutine to avoid processing it before destruction
        Metronome.Ticked -= note.Ticked;
        Destroy(note.gameObject);
        
        var lastFirst = firstNote;
        firstNote = notesSpawned.Count > 0 ? notesSpawned[0] : null;
        if (firstNote != null && firstNote != lastFirst)
        {
            noteHelper.SetContainerSprite(firstNote.note);
        }
    }

    void UpdateHelper()
    {
        bool showHelper = notesSpawned.Count > 0;

        noteHelper.gameObject.SetActive(showHelper);
        if (!showHelper) return;

        var lastFirst = firstNote;
        firstNote = notesSpawned[0];
        if (firstNote != lastFirst)
        {
            noteHelper.SetContainerSprite(firstNote.note);
        }
    }
}
