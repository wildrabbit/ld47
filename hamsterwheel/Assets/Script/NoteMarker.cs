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

    [SerializeField] UnityEngine.UI.Image view;

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

    public bool UpdateNote(float currentSongBeat, float beatsPreview)
    {
        float lerpRatio = (beatsPreview - (beat - currentSongBeat)) / beatsPreview;
        transform.position = Vector2.Lerp(startPos, endPos, lerpRatio);
        return lerpRatio > 1;
    }
}
