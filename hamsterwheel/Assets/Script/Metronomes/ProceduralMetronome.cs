using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// The code example shows how to implement a metronome that procedurally generates the click sounds via the OnAudioFilterRead callback.
// While the game is paused or the suspended, this time will not be updated and sounds playing will be paused. Therefore developers of music scheduling routines do not have to do any rescheduling after the app is unpaused

[RequireComponent(typeof(AudioSource))]
public class ProceduralMetronome : MonoBehaviour, IMetronome
{
    public double bpm = 140.0F;
    public float gain = 0.5F;
    public int signatureHi = 4;
    public int signatureLo = 4;
    private double nextTick = 0.0F;
    private double startTick = 0.0f;
    private float amp = 0.0F;
    private float phase = 0.0F;
    private double sampleRate = 0.0F;
    private int accent;
    private bool running = false;
    public int pendingBeats;
    AudioSource source;

    double lastTickTime = 0.0f;

    public event Action<int, double, bool> Ticked;

    public void SetSignature(int hi, int lo)
    {
        signatureHi = hi;
        signatureLo = lo;
    }
    public double BPM
    {
        get
        {
            return bpm;
        }
        set
        {
            bpm = value;
        }
    }

    public int NumPendingBeats
    {
        get { return pendingBeats; }
    }
    
    private void Awake()
    {
        source = GetComponent<AudioSource>();
        pendingBeats = 0;
        accent = signatureHi;
        sampleRate = AudioSettings.outputSampleRate;
        running = false;
    }

    public void ResetBeatCount()
    {
        pendingBeats = 0;
    }

    public void SetStartTime(double time)
    {
        startTick = time;
        source.PlayScheduled(startTick);
        nextTick = time * sampleRate;
        running = true;        
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

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!running)
        {
            return;
        }

        double samplesPerTick = sampleRate * TimeBetweenBeats * 4.0F / signatureLo;
        double sample = AudioSettings.dspTime * sampleRate;
        int dataLen = data.Length / channels;
        int n = 0;
        while (n < dataLen)
        {
            float x = gain * amp * Mathf.Sin(phase);
            int i = 0;
            while (i < channels)
            {
                data[n * channels + i] += x;
                i++;
            }
            while (sample + n >= nextTick)
            {
                nextTick += samplesPerTick;
                amp = 1.0F;
                bool accented = ++accent > signatureHi;
                if (accented)
                {
                    accent = 1;
                    amp *= 2.0F;
                }

                //Debug.Log("Tick #" + totalTicks + ": " + accent + " / " + signatureHi);
                pendingBeats++;
                Ticked?.Invoke(pendingBeats, GetTotalElapsed(), accented);

                lastTickTime = AudioSettings.dspTime;
            }
            phase += amp * 0.3F;
            amp *= 0.993F;
            n++;
        }
    }

    public void Reset(double time)
    {
        source.Stop();
        SetStartTime(time);
    }

    public void Stop()
    {
        source.Stop();
    }
}

