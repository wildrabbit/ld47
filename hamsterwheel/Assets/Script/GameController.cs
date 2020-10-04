﻿using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System;

public enum NoteType
{
    Up,
    Down,
    Left,
    Right,
}

//public class NoteData
//{
//    public NoteType id;
//    public InputAction action;
//    public string actionName;
//    public string animatorTrigger;
//}

public class GameController : MonoBehaviour
{
    public static readonly int UpTrigger = Animator.StringToHash("Up");
    public static readonly int DownTrigger = Animator.StringToHash("Down");
    public static readonly int LeftTrigger = Animator.StringToHash("Left");
    public static readonly int RightTrigger = Animator.StringToHash("Right");

    public InputActionAsset Controls;
    public Animator Hamster; // Replace type


    InputAction _up;
    InputAction _down;
    InputAction _left;
    InputAction _right;
    InputAction _restart;

    InputActionMap _map;

    float[] _noteStates;
    float _restartPressed;

    // Use this for initialization
    void Start()
    {
        _map = Controls.FindActionMap("Player");

        _up = _map.FindAction("Up");
        _down = _map.FindAction("Down");
        _left = _map.FindAction("Left");
        _right = _map.FindAction("Right");
        _restart = _map.FindAction("Restart");
        
        _noteStates = new float[]
        {
            -1,-1,-1,-1
        };
        _restartPressed = -1;
    }

    
    private void OnRestart(InputAction.CallbackContext obj)
    {
        RestartGame();
    }

    public void RestartGame()
    {
        Debug.Log("RESTART ALL THE THINGS!");
    }

    private void OnDestroy()
    {
    }

    private (bool, bool, float) ReadNote(NoteType note, float now, InputAction action)
    {
        int idx = (int)note;
        return ReadAction(action, now, ref _noteStates[idx]);
    }

    private (bool, bool, float) ReadAction(InputAction action, float now, ref float lastPressedTime)
    {
        float elapsed = lastPressedTime >= 0 ? Time.time - lastPressedTime : 0;
        bool pressed = action.ReadValue<float>() > 0;
        bool stateChange = false;
        if (pressed)
        {
            stateChange = lastPressedTime < 0;
            if(stateChange)
            {
                lastPressedTime = now;
                Debug.Log($"First press for {action}");
            }
        }
        else
        {
            stateChange = lastPressedTime >= 0;
            if(stateChange)
            {
                lastPressedTime = -1;
                Debug.Log($"First release for {action}. Time spent pressed was {elapsed}");
            }
        }
        return (pressed, stateChange, elapsed);
    }


    // Update is called once per frame
    void Update()
    {
        var now = Time.time;
        var mappings = new (NoteType, InputAction)[]
        {
           (NoteType.Up, _up),
           (NoteType.Down, _down),
           (NoteType.Left, _left),
           (NoteType.Right, _right)
        };
        bool notePressed;
        bool noteJustChanged;

        for(int i = 0; i < mappings.Length; ++i)
        {
            NoteType note = mappings[i].Item1;
            InputAction action = mappings[i].Item2;
            (notePressed, noteJustChanged, _) = ReadNote(note, now, action);
            if(notePressed && noteJustChanged)
            {
                NotePressed(mappings[i].Item1);
            }
        }

        (bool restartPressed, bool restartJustChanged, float _) = ReadAction(_restart, now, ref _restartPressed);
        if(restartPressed && restartJustChanged)
        {
            RestartGame();
        }
    }

    public void NotePressed(string noteName)
    {
        if(Enum.TryParse<NoteType>(noteName, out var note))
        {
            NotePressed(note);
        }       
    }

    public void NotePressed(NoteType note)
    {
        int idx = (int)note;
        int[] triggerMappings = new int[]
        {
            UpTrigger,
            DownTrigger,
            LeftTrigger,
            RightTrigger            
        };

        Hamster.SetTrigger(triggerMappings[idx]);
    }

}