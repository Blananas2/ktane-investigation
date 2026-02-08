using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using KModkit;
using Rnd = UnityEngine.Random;

public class investigationScript : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public Text SearchQuery;
    public KMSelectable SearchButton;
    public TextMesh ResultSummary;
    public Text[] EntryTexts;
    public GameObject ScrollBarObj;
    public KMSelectable ScrollBarSel;
    public KMSelectable[] ScrollBarAreaSels;
    public TextMesh BottomText;

    public TextAsset[] Transcripts;

    int transcriptIx;
    string chosenTranscript;

    //Logging
    private int moduleId;
    private static int moduleIdCounter = 1;
    private bool moduleSolved;

    private bool _scrollBarHeld = false;
    private int _currentScrollBarPos = 0;
    private Coroutine _scrollBarAnimation;
    private static readonly float[] _scrollBarPositions = new float[]
    {
        0.0375f, 0.0225f, 0.0075f, -0.0075f, -0.0225f, -0.0375f
    };

    void Awake()
    {
        moduleId = moduleIdCounter++;
        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
        }
        */

        //button.OnInteract += delegate () { buttonPress(); return false; };
        ScrollBarSel.OnInteract += ScrollBarPress;
        ScrollBarSel.OnInteractEnded += ScrollBarRelease;
        for (int i = 0; i < ScrollBarAreaSels.Length; i++)
        {
            ScrollBarAreaSels[i].OnHighlight += ScrollAreaHighlight(i);
            ScrollBarAreaSels[i].OnInteract += UnityEditorOnlyScrollAreaPress(i);
        }
    }

    private KMSelectable.OnInteractHandler UnityEditorOnlyScrollAreaPress(int i)
    {
        return delegate ()
        {
            if (!Application.isEditor)
                return false;
            _currentScrollBarPos = i;
            if (_scrollBarAnimation != null)
                StopCoroutine(_scrollBarAnimation);
            _scrollBarAnimation = StartCoroutine(AnimateScrollBarMovement(_currentScrollBarPos));
            return false;
        };
    }

    // Use this for initialization
    void Start()
    {
        return;
        transcriptIx = Rnd.Range(0, Transcripts.Length);
        chosenTranscript = Transcripts[transcriptIx].ToString();



    }

    private bool ScrollBarPress()
    {
        _scrollBarHeld = true;
        return false;
    }

    private void ScrollBarRelease()
    {

        _scrollBarHeld = false;

    }

    private Action ScrollAreaHighlight(int i)
    {
        return delegate ()
        {
            if (!_scrollBarHeld)
                return;
            _currentScrollBarPos = i;
            if (_scrollBarAnimation != null)
                StopCoroutine(_scrollBarAnimation);
            _scrollBarAnimation = StartCoroutine(AnimateScrollBarMovement(_currentScrollBarPos));
        };
    }

    private IEnumerator AnimateScrollBarMovement(int goalPos)
    {
        var startScrollBarPos = ScrollBarObj.transform.localPosition;
        var goalScrollBarPosZ = _scrollBarPositions[goalPos];
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            ScrollBarObj.transform.localPosition = new Vector3(startScrollBarPos.x, startScrollBarPos.y, Mathf.Lerp(startScrollBarPos.z, goalScrollBarPosZ, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        ScrollBarObj.transform.localPosition = new Vector3(startScrollBarPos.x, startScrollBarPos.y, goalScrollBarPosZ);
    }

    /*
    void keypadPress(KMSelectable object) {
        
    }
    */

    /*
    void buttonPress() {

    }
    */
}
