using System;
using System.Collections.Generic;
using UnityEngine;

public interface IMetronome
{
    void SetStartTime(double time);
    void Reset(double time);
    void Stop();

    double BPM
    { get; set; }

    double TimeBetweenBeats
    {
        get;
    }

    int NumPendingBeats
    {
        get;
    }
    void ResetBeatCount();
    void SetSignature(int hi, int lo);

    double GetTotalElapsed();
    double GetElapsedFromLastTick();
    double GetTimeFromPrevTick();
    double GetTimeForTick(int tick);
    double GetRemainingTimeToTick(int tick);
    double GetTimeToNextTick();

    event Action<int, double, bool> Ticked;
}