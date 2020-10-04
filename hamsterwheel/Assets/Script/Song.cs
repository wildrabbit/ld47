using System;
using System.Collections.Generic;
using UnityEngine;

using System.Text.RegularExpressions;

public struct BeatEntry
{
    public float Beat;
    public float BeatDuration;
    public NoteType ExpectedNote;
    
    public static BeatEntry NoBeat = new BeatEntry
    {
        Beat = -1, BeatDuration = 0.0f, ExpectedNote = NoteType.Down
    };
}

[System.Serializable]
public class Song
{
    public AudioClip songResource;
    public double Offset;
    public int bpm;
    public TextAsset songSheet;
    public int signatureHi = 4;
    public int signatureLo = 4;

    public List<BeatEntry> beats = new List<BeatEntry>();

    public void LoadSheet()
    {
        string[] lines = Regex.Split(songSheet.text, "\n|\r|\r\n");
        foreach (string l in lines)
        {
            string[] tokens = l.Split(' ');
            if (tokens.Length != 3)
            {
                continue;
            }
            BeatEntry b = new BeatEntry();
            float.TryParse(tokens[0], out b.Beat);
            float.TryParse(tokens[1], out b.BeatDuration);
            int type;
            System.Int32.TryParse(tokens[2], out type);
            b.ExpectedNote= (NoteType)type;
            beats.Add(b);
        }
        beats.Sort((b1, b2) => b1.Beat.CompareTo(b2.Beat));
    }

    public bool TryGetBeat(int idx, out BeatEntry entry)
    {
        bool valid = idx >= 0 && idx < beats.Count;
        if (valid)
        {
            entry = beats[idx];
        }
        else
        {
            entry = BeatEntry.NoBeat;
        }
        return valid;
    }

    public List<BeatEntry> GetBeatsInRange(double elapsedTime, float incomingThreshold, float pastThreshold = -1.0f) // -1: Reuse,  0: Not added.
    {
        List<BeatEntry> result = new List<BeatEntry>();
        foreach (BeatEntry entry in beats)
        {
            double time = entry.Beat * 60 / bpm;
            double delta = time - elapsedTime;
            // Max range:
            double minThreshold = pastThreshold < 0.0f
                ? incomingThreshold
                : Mathf.Approximately(pastThreshold, 0.0f)
                    ? elapsedTime
                    : pastThreshold;
            if (delta > incomingThreshold && delta < minThreshold) // Not there yet
                continue;
            result.Add(entry);
        }
        return result;
    }

    public List<BeatEntry> GetUpcomingBeats(float limit, float start = 0)
    {
        List<BeatEntry> result = new List<BeatEntry>();
        foreach (BeatEntry beat in beats)
        {
            if (beat.Beat >= start && beat.Beat <= start + limit)
            {
                result.Add(beat);
            }
        }
        return result;
    }

    public List<float> GetBeats()
    {
        List<float> beatResult = new List<float>();
        foreach (BeatEntry beat in beats)
        {
            beatResult.Add(beat.Beat);
        }
        return beatResult;
    }
}
