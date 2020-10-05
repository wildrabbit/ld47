using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;
using DG.Tweening;

public enum NoteType
{
    Up,
    Down,
    Left,
    Right,
}

public enum NoteHitType
{
    OK,       // Default success frame
    Perfect, // Smaller success frame
    WrongKey,
    WrongTime, // Wrong key or Too early
    Skipped // We were expecting a note and there was nothing
}

public enum GameState
{
    Init,
    Running,
    Won
}

//public class NoteInfo
//{
//    public NoteType id;
//    public InputAction action;
//    public string actionName;
//    public string animatorTrigger;
//    public bool isPressed;
//    public float pressStarted;
//    public bool justPressed;
//}

public class GameController : MonoBehaviour
{
    public static readonly int UpTrigger = Animator.StringToHash("Up");
    public static readonly int DownTrigger = Animator.StringToHash("Down");
    public static readonly int LeftTrigger = Animator.StringToHash("Left");
    public static readonly int RightTrigger = Animator.StringToHash("Right");

    public InputActionAsset Controls;
    public Animator Hamster; // Replace type
    public RotateInfinite Wheel;

    public float DefaultRotationSpeed = 360;

    public float powerBarLevel = 10;
    public float powerBarMaxLevel = 100;

    public float normalHitScore = 3;
    public float perfectHitScore = 8;
    public float wrongHitScore = -5;
    public float skippedHitScore = -2;

    public Image powerBar;
    public RectTransform barContainer;

    public RectTransform noteContainer;
    public Text ok;
    public Text miss;
    public Text perfect;

    public RectTransform victoryContainer;
    public RectTransform startContainer;

    public AudioClip sfxOk;
    public AudioClip sfxOk2;
    public AudioClip sfxPerfect;
    public AudioClip sfxMiss;
    public AudioClip sfxSkip;

    public AudioClip win;
    public AudioClip winGame;

    [SerializeField] AudioSource _sfxPlayer;

    [SerializeField] SongController _songController = null;

    InputAction _up;
    InputAction _down;
    InputAction _left;
    InputAction _right;
    InputAction _restart;

    InputActionMap _map;

    public int songIdx = 0;

    float[] _noteStates;
    float _restartPressed;
    float startPowerLevel;

    public GameState _currentState;

    WaitForSeconds textWait;

    private void Awake()
    {
        startPowerLevel = powerBarLevel;
        textWait = new WaitForSeconds(0.5f);
        _currentState = GameState.Init;
        _map = Controls.FindActionMap("Player");

        _up = _map.FindAction("Up");
        _down = _map.FindAction("Down");
        _left = _map.FindAction("Left");
        _right = _map.FindAction("Right");
        _restart = _map.FindAction("Restart");
    }

    // Use this for initialization
    void Start()
    {
        StartGame(songIdx);
    }

    public void StartGame(int levelIdx)
    {
        _noteStates = new float[]
        {
            -1,-1,-1,-1
        };
        _restartPressed = -1;

        _songController.BeatExpired += OnBeatExpired;

        StartCoroutine(StartRoutine(levelIdx));
    }

    IEnumerator StartRoutine(int songIdx = 0)
    {
        DOTween.Clear();
        _songController.InitLevel(songIdx);
        float bpmRatio = _songController.CurrentSong.bpm / 120f;

        Hamster.speed = bpmRatio;
        Wheel.StartGame(DefaultRotationSpeed * bpmRatio);

        victoryContainer.gameObject.SetActive(false);
        noteContainer.gameObject.SetActive(false);
        barContainer.gameObject.SetActive(true);
        powerBarLevel = startPowerLevel;
        powerBar.fillAmount = powerBarLevel / powerBarMaxLevel;
        StartCoroutine(LevelStartText());

        yield return null;

        _songController.StartGame();
        _currentState = GameState.Running;
    }

    IEnumerator LevelStartText()
    {
        startContainer.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);

        startContainer.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _songController.BeatExpired -= OnBeatExpired;
    }

    public IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(0.1f);
        DOTween.Clear();
        yield return new WaitForSeconds(0.1f);
        Debug.Log("RESTART ALL THE THINGS!");
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    #region input
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
            }
        }
        else
        {
            stateChange = lastPressedTime >= 0;
            if(stateChange)
            {
                lastPressedTime = -1;
            }
        }
        return (pressed, stateChange, elapsed);
    }
    #endregion input


    // Update is called once per frame
    void Update()
    {
        var now = Time.time;
        (bool restartPressed, bool restartJustChanged, float _) = ReadAction(_restart, now, ref _restartPressed);
        if (restartPressed && restartJustChanged)
        {
            StartCoroutine(RestartGame());
        }


        var mappings = new (NoteType, InputAction)[]
        {
           (NoteType.Up, _up),
           (NoteType.Down, _down),
           (NoteType.Left, _left),
           (NoteType.Right, _right)
        };
        bool notePressed;
        bool noteJustChanged;

        if(_currentState != GameState.Running)
        {
            return;
        }

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
        bool validInput = _songController.RecordedNote(note, out var hitType);
        if(validInput)
        {
            NoteHit(hitType);
        }
    }


    private void OnBeatExpired(float arg1, bool arg2)
    {
        NoteHit(NoteHitType.Skipped);
    }

    #region power
    public void NoteHit(NoteHitType hitType)
    {
        AudioClip clip = null;
        switch (hitType)
        {
            case NoteHitType.OK:
                clip = UnityEngine.Random.value > 0.5? sfxOk : sfxOk2;
                powerBarLevel += normalHitScore;
                break;
            case NoteHitType.Perfect:
                clip = sfxPerfect;
                powerBarLevel += perfectHitScore;
                break;
            case NoteHitType.WrongKey:
            case NoteHitType.WrongTime:
                clip = sfxMiss;
                powerBarLevel += wrongHitScore;
                break;
            case NoteHitType.Skipped:
                clip = sfxSkip;
                powerBarLevel += skippedHitScore;
                break;
            default:
                break;
        }

        if (clip != null)
        {
            _sfxPlayer.PlayOneShot(clip, 0.2f);
            
        }
        StartCoroutine(ShowText(hitType));

        powerBarLevel = Mathf.Clamp(powerBarLevel, 0, powerBarMaxLevel);
        powerBar.fillAmount = powerBarLevel / powerBarMaxLevel;

        
        if (Mathf.Approximately(powerBarLevel, 0))
        {
            // Small "oh oh" animation??
            Debug.Log($"<color=cyan>Don't lose hope, keep trying!</color>");
        }
        else if (Mathf.Approximately(powerBarLevel, powerBarMaxLevel))
        {
            _songController.StopPlayback();
            // last song?
            if (_songController.songIdx >= _songController.Songs.Length - 1)
            {
                _sfxPlayer.PlayOneShot(winGame);

                // Next wheel state / next level / game won
                StartCoroutine(VictoryRoutine());
            }
            else
            {                
                _sfxPlayer.PlayOneShot(win);
                StartCoroutine(NextLevelRoutine());
                // Load next song
            }
        }
    }

    private IEnumerator NextLevelRoutine()
    {
        _currentState = GameState.Init;

        yield return new WaitForSeconds(2.0f);

        NextLevel(_songController.songIdx + 1);
    }

    private void NextLevel(int levelIdx)
    {
        // Clean stuff
        StartGame(levelIdx);
    }

    private IEnumerator VictoryRoutine()
    {
        noteContainer.gameObject.SetActive(false);
        victoryContainer.gameObject.SetActive(true);
        barContainer.gameObject.SetActive(false);

        _currentState = GameState.Won;
        _songController.BeatExpired -= OnBeatExpired;
        _songController.GameWon();
        Wheel.Stop();
        Hamster.SetTrigger("Win");
        yield return null;
    }

    private IEnumerator ShowText(NoteHitType hitType)
    {
        noteContainer.gameObject.SetActive(true);
        Text active = null;
        switch (hitType)
        {
            case NoteHitType.OK:
                active = ok;
                ok.gameObject.SetActive(true);
                perfect.gameObject.SetActive(false);
                miss.gameObject.SetActive(false);
                break;
            case NoteHitType.Perfect:
                active = perfect;
                ok.gameObject.SetActive(false);
                perfect.gameObject.SetActive(true);
                miss.gameObject.SetActive(false);
                break;
            case NoteHitType.WrongKey:
            case NoteHitType.WrongTime:
            case NoteHitType.Skipped:
                active = miss;
                ok.gameObject.SetActive(false);
                perfect.gameObject.SetActive(false);
                miss.gameObject.SetActive(true);
                break;
            default:
                break;
        }
        if(active != null)
        {
            var activeColor = active.color;
            activeColor.a = 0.1f;
            active.color = activeColor;
            active.DOFade(1, 0.2f);
            active.transform.DOPunchScale(0.2f * Vector3.one, 0.3f);
        }
        yield return textWait;
        var endColor = active.color;
        endColor.a = 1;
        active.color = endColor;

        noteContainer.gameObject.SetActive(false);
    }


    #endregion


}
