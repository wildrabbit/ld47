using UnityEngine;
using System.Collections;

public class NoteMarker : MonoBehaviour
{
    public Sprite up;
    public Sprite down;
    public Sprite left;
    public Sprite right;

    Vector3 startPos;
    Vector3 endPos;
    public float beat;
    public NoteType note;

    [SerializeField] UnityEngine.UI.Image view = null;

    Sprite[] mappings;

    private void Awake()
    {
        mappings = new Sprite[]
        {
            up, down, left, right
        };
    }

    public void Init(NoteType note, float beat, Transform root, Vector3 startPos, Vector3 endPos)
    {
        transform.SetParent(root, false);
        transform.position = startPos;
        transform.localScale = Vector3.one;
        view.sprite = mappings[(int)note];

        this.note = note;
        this.beat = beat;
        this.startPos = startPos;
        this.endPos = endPos;
    }

    public void UpdateNote(float currentSongBeat, float beatsPreview, out float ratio)
    {
        ratio = (beatsPreview - (beat - currentSongBeat)) / beatsPreview;

        transform.position = Vector2.LerpUnclamped(startPos, endPos, ratio); // We will allow overshoot
    }
}
