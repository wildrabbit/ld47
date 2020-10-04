using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SimpleMetronome: MonoBehaviour, IMetronome
{
    public AudioClip metroAccent;
    public AudioClip metroDefault;

    public event Action<int, double, bool> Ticked;

    double bpm = 140.0F;

    public int signatureHi = 4;
    public int signatureLo = 4;

    private double nextTick = 0.0F;
    private double startTick = 0.0f;
    private bool running = false;
    private double sampleRate = 0.0F;
    public int pendingBeats = 0;
    private bool init = false;
    public int curAccentMarker = 0;
    AudioSource source;

    double lastTickTime = 0.0f;

    public double BPM
    {
        get { return bpm; }
        set { bpm = value; }
    }

    public int NumPendingBeats
    {
        get { return pendingBeats;  }
    }

    public void SetSignature(int hi, int lo)
    {
        signatureHi = hi;
        signatureLo = lo;
    }

    public void ResetBeatCount()
    {
        pendingBeats = 0;
    }

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        sampleRate = AudioSettings.outputSampleRate;
        running = false;
    }

    public void SetStartTime(double time)
    {
        startTick = time;
        lastTickTime = time;
        nextTick = time;
        pendingBeats = 0;
        init = true;
        running = AudioSettings.dspTime >= startTick;
        Debug.LogFormat("Starting tracker!! Now: {0}, Expected start: {1} Next: {2}", AudioSettings.dspTime, time, nextTick);
    }

    void Update()
    {
        if (!init) { return; }
        if (!running)
        {
            Debug.LogFormat("Now: {0}, Expected start: {1}", AudioSettings.dspTime, startTick);
            if (AudioSettings.dspTime >= startTick)
            {
                Debug.Log("LET'S GO");
                running = true;
            }
            else return;
        }
        double now = AudioSettings.dspTime;
        double delta = now;
        bool willPlay = false;
        int skipped = 0;
        bool accent = (curAccentMarker % signatureHi) == 0;

        while (delta >= nextTick)
        {
            delta -= TimeBetweenBeats;
            willPlay = true; 
            pendingBeats++;
            skipped++;
            lastTickTime = now;
            Ticked?.Invoke(pendingBeats, GetTotalElapsed(), accent);
        }
        if (willPlay)
        {
            nextTick += skipped * TimeBetweenBeats;
            curAccentMarker  += skipped;
            source.PlayOneShot(accent ? metroAccent : metroDefault);
        }
    }

    public double GetTotalElapsed()
    {
        return AudioSettings.dspTime - startTick;
    }

    public double GetElapsedFromLastTick()
    {
        return AudioSettings.dspTime - lastTickTime;
    }


    public double TimeBetweenBeats
    {
        get
        {
            return 60.0f / bpm;
        }
    }
    public double GetTimeFromPrevTick()
    {
        double prev = lastTickTime - TimeBetweenBeats;
        return AudioSettings.dspTime - prev;
    }

    public double GetTimeForTick(int tick)
    {
        return tick * TimeBetweenBeats;
    }

    public double GetRemainingTimeToTick(int tick)
    {
        return GetTimeForTick(tick) - GetTotalElapsed();
    }

    public double GetTimeToNextTick()
    {
        double next = lastTickTime + TimeBetweenBeats;
        return next - AudioSettings.dspTime;
    }

    
    public void Reset(double time)
    {
        running = false;
        SetStartTime(time);
    }

    public void Stop()
    {
        running = false;
        init = false;
    }
}


