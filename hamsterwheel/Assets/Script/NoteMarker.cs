using UnityEngine;
using System.Collections;
using System;
using DG.Tweening;

public class NoteMarker : MonoBehaviour
{
    public Sprite up;
    public Sprite down;
    public Sprite left;
    public Sprite right;

    public Sprite upC;
    public Sprite downC;
    public Sprite leftC;
    public Sprite rightC;

    Vector3 startPos;
    Vector3 endPos;
    public float beat;
    public NoteType note;

    [SerializeField] UnityEngine.UI.Image view = null;

    Sprite[] mappings;
    Sprite[] containerMappings;

    private void Awake()
    {
        mappings = new Sprite[]
        {
            up, down, left, right
        };
        containerMappings = new Sprite[]
        {
            upC, downC, leftC, rightC
        };
    }

    public void SetContainerSprite(NoteType note)
    {
        view.sprite = containerMappings[(int)note];
    }

    public void Init(NoteType note, float beat, Transform root, Vector3 startPos, Vector3 endPos)
    {
        transform.SetParent(root, false);
        transform.SetSiblingIndex(0);
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

    internal void Ticked(int arg1, double arg2, bool arg3)
    {
        transform.DOPunchScale(0.1f * Vector3.one, 0.2f);
    }
}
