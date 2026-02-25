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

    public KMAudio Audio;
    public KMSelectable ModuleSelectable;
    public KMBombModule Module;

    public Text SearchQuery;
    public KMSelectable SearchButton;
    public TextMesh ResultSummary;
    public MeshRenderer[] EntryMeshes;
    public Material[] EntryMats;
    public Text[] EntryTexts;
    public GameObject ScrollBarObj;
    public KMSelectable ScrollBarSel;
    public KMSelectable[] ScrollBarAreaSels;
    public KMSelectable BottomButton;
    public TextMesh BottomText;

    public TextAsset TranscriptNames;
    public TextAsset[] Transcripts;
    public TextAsset ExampleTranscript;
    public bool CheckIfYouAreBlan;

    int transcriptIx;
    string bombName;
    string defusedBy;
    string[] chosenTranscript;
    List<string> bombList;

    private bool focused = false;
    private bool shifting = false;
    private KeyCode[] ControlKeys = { KeyCode.Backspace, KeyCode.PageUp, KeyCode.PageDown, KeyCode.Return, KeyCode.UpArrow, KeyCode.KeypadEnter, KeyCode.DownArrow };
    private KeyCode[] Keys = { KeyCode.BackQuote, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals, KeyCode.KeypadDivide, KeyCode.KeypadMultiply, KeyCode.KeypadMinus, KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.LeftBracket, KeyCode.RightBracket, KeyCode.Backslash, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.KeypadPlus, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon, KeyCode.Quote, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period, KeyCode.Question, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Space, KeyCode.Keypad0, KeyCode.KeypadPeriod };
    static readonly string Unshifted = "`1234567890-=/*-qwertyuiop[]\\789+asdfghjkl;'456zxcvbnm,./123 0.";
    static readonly string Shifted = "~!@#$%^&*()_+/*-QWERTYUIOP{}|789+ASDFGHJKL:\"456ZXCVBNM<>?123 0.";

    private bool _scrollBarHeld = false;
    private int _currentScrollBarPos = 0;
    private Coroutine _scrollBarAnimation;

    string query = "";
    string[] results = { "", "", "", "", "", "", "", "", "", "" };
    bool phaseTwo = false;

    //Logging
    private int moduleId;
    private static int moduleIdCounter = 1;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        if (Application.isEditor) { focused = true; }
        ModuleSelectable.OnFocus += delegate () { focused = true; };
        ModuleSelectable.OnDefocus += delegate () { focused = false; };

        SearchButton.OnInteract += delegate () { 
            SearchButton.AddInteractionPunch();
            if (!phaseTwo) { SubmitQuery(); } 
            return false; 
        };

        BottomButton.OnInteract += delegate () { 
            BottomButton.AddInteractionPunch();
            BottomButtonPress(); 
            return false; 
        };

        ScrollBarSel.OnInteract += ScrollBarPress;
        ScrollBarSel.OnInteractEnded += ScrollBarRelease;
        for (int i = 0; i < ScrollBarAreaSels.Length; i++)
        {
            ScrollBarAreaSels[i].OnHighlight += ScrollAreaHighlight(i);
            ScrollBarAreaSels[i].OnInteract += UnityEditorOnlyScrollAreaPress(i);
        }
    }

    // Use this for initialization
    void Start()
    {
        string[] nameFile = TranscriptNames.ToString().Split('\n');
        bombList = nameFile.ToList();

        transcriptIx = Rnd.Range(0, Transcripts.Length);
        string[] wholeFile = (Application.isEditor && !CheckIfYouAreBlan) ? 
                             ExampleTranscript.ToString().Split('\n') : 
                             Transcripts[transcriptIx].ToString().Split('\n');
        bombName = wholeFile[0].Trim();
        defusedBy = wholeFile[1].Trim();
        chosenTranscript = wholeFile[2].Trim().Split(' ');
        Debug.LogFormat("[Investigation #{0}] Chosen transcript: {1} defused by {2}", moduleId, bombName, defusedBy);
    }

    //This gets called every frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            shifting = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            shifting = false;
        }

        for (int k = 0; k < Keys.Length; k++)
        {
            if (Input.GetKeyDown(Keys[k]))
            {
                HandleKey(shifting ? Shifted[k] : Unshifted[k]);
            }
        }

        for (int q = 0; q < ControlKeys.Length; q++)
        {
            if (Input.GetKeyDown(ControlKeys[q]))
            {
                HandleControlKey(q);
            }
        }
    }

    void HandleKey (char c)
    {
        ModuleSelectable.AddInteractionPunch(0.25f);
        if (focused && !moduleSolved)
        {
            query = query + c;
            SearchQuery.text = query.Replace("  ", " ");
            if (phaseTwo) { PhaseTwoUpdate(); }
        }
    }

    void HandleControlKey(int j)
    {
        ModuleSelectable.AddInteractionPunch(0.25f);
        if (!focused || moduleSolved) { return; }
        switch (j)
        {
            case 0: //backspace
                if (query.Length != 0) {
                    query = query.Substring(0, query.Length - 1);
                    SearchQuery.text = query;
                    if (phaseTwo) { PhaseTwoUpdate(); }
                }
            break;
            case 1: case 4: //up
                if (_currentScrollBarPos != 0)
                {
                    _scrollBarAnimation = StartCoroutine(AnimateScrollBarMovement(_currentScrollBarPos - 1, false));
                }
            break; 
            case 2: case 6: //down
                if (_currentScrollBarPos != 9)
                {
                    _scrollBarAnimation = StartCoroutine(AnimateScrollBarMovement(_currentScrollBarPos + 1, false));
                }
            break; 
            case 3: case 5: //enter
                if (!phaseTwo) { SubmitQuery(); } else { SubmitBomb(); }
            break;
        }
    }

    void SubmitQuery()
    {
        if (moduleSolved) { return; }

        if (query.Trim().Length == 0)
        {
            ResultSummary.text = "Enter word(s)";
            return;
        }

        string[] splitQuery = query.Trim().Split(' ');
        if (splitQuery.Length > 5)
        {
            ResultSummary.text = "Too many words";
            return;
        }

        int sideWords = (9 - splitQuery.Length) / 2;
        List<int> foundIxs = new List<int> { };

        for (int w = 0; w < chosenTranscript.Length; w++)
        {
            if (w + splitQuery.Length > chosenTranscript.Length) { break; }
            bool match = true;
            for (int v = 0; v < splitQuery.Length; v++)
            {
                if (Regex.Replace(chosenTranscript[w+v].ToUpper(), @"\p{P}", "") != Regex.Replace(splitQuery[v].ToUpper(), @"\p{P}", ""))
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                foundIxs.Add(w);
                if (foundIxs.Count() == 10) { break; }
            }
        }

        for (int f = 0; f < foundIxs.Count(); f++)
        {
            int number = foundIxs[f];

            int first = Math.Max(0, number - sideWords);
            int last = Math.Min(chosenTranscript.Length - 1, number + (splitQuery.Length - 1) + sideWords);

            List<string> lineWords = new List<string> { };
            int index = first;
            while (index <= last)
            {
                lineWords.Add(chosenTranscript[index]);
                index++;
            }
            results[f] = lineWords.Join(" ");
        }

        for (int h = 9; h >= foundIxs.Count(); h--)
        {
            results[h] = "";
        }

        _scrollBarAnimation = StartCoroutine(AnimateScrollBarMovement(0, false));

        ResultSummary.text = (foundIxs.Count() == 10 ? "Max" : foundIxs.Count().ToString()) + " result" + (foundIxs.Count() == 1 ? "" : "s") + " found";
        Debug.LogFormat("[Investigation #{0}] Searching \"{1}\" yielded {2} result{3}", moduleId, query.Trim(), foundIxs.Count(), foundIxs.Count() == 1 ? "" : "s");
    }

    void BottomButtonPress()
    {
        if (moduleSolved) { return; }
        if (!phaseTwo)
        {
            PhaseTwoStart();
        } else
        {
            SubmitBomb();
        }
    }

    void PhaseTwoStart()
    {
        phaseTwo = true;
        query = "";
        SearchQuery.text = "";
        ResultSummary.text = "Enter bomb name";
        _scrollBarAnimation = StartCoroutine(AnimateScrollBarMovement(0, true));
        BottomText.text = "Submit";
    }

    void PhaseTwoUpdate()
    {
        List<string> bombsThatFit = new List<string> { };

        bool actualQuery = true;
        if (query.Trim().Length == 0) { 
            actualQuery = false;
            goto NoQuery;
        }

        for (int b = 0; b < bombList.Count(); b++)
        {
            if (bombList[b].ToUpper().Replace("Ü", "U").Contains(query.Trim().ToUpper()))
            {
                bombsThatFit.Add(bombList[b].Trim());
            }
        }

        bombsThatFit = bombsThatFit.OrderBy(n => n.Length).ToList();

        for (int z = 0; z < Math.Min(10, bombsThatFit.Count()); z++)
        {
            results[z] = bombsThatFit[z];
        }

        NoQuery:
        for (int x = 9; x >= bombsThatFit.Count(); x--)
        {
            results[x] = "";
        }

        for (int v = 0; v < 10; v++)
        {
            if (v - _currentScrollBarPos < 4 && v - _currentScrollBarPos > -1)
            {
                EntryTexts[v - _currentScrollBarPos].text = results[v];
            }
        }

        ResultSummary.text = actualQuery ? (bombsThatFit.Count() >= 10 ? "Max" : bombsThatFit.Count().ToString()) + " bomb" + (bombsThatFit.Count() == 1 ? "" : "s") + " found" : "Enter bomb name";
    }

    void SubmitBomb()
    {
        _scrollBarAnimation = StartCoroutine(AnimateScrollBarMovement(0, true));
        SearchQuery.text = "";
        if (results[0] == bombName)
        {
            Module.HandlePass();
            Debug.LogFormat("[Investigation #{0}] Submitted \"{1}\", which is correct, module solved!", moduleId, bombName);
            moduleSolved = true;
            SearchQuery.fontStyle = UnityEngine.FontStyle.Bold;
            ResultSummary.text = "";
            BottomText.text = "";
        } else
        {
            Module.HandleStrike();
            Debug.LogFormat("[Investigation #{0}] Submitted \"{1}\", which is incorrect. Strike.", moduleId, results[0]);
            phaseTwo = false;
            query = "";
            ResultSummary.text = "Input pending";
            BottomText.text = "Continue";
            EntryTexts[0].fontStyle = UnityEngine.FontStyle.Normal;
        }
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
            _scrollBarAnimation = StartCoroutine(AnimateScrollBarMovement(_currentScrollBarPos, false));
        };
    }

    private IEnumerator AnimateScrollBarMovement(int goalPos, bool clear)
    {
        if (moduleSolved) { yield break; }

        var startScrollBarPos = ScrollBarObj.transform.localPosition;
        float goalScrollBarPosZ = -0.0125f * (goalPos - 3);
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            ScrollBarObj.transform.localPosition = new Vector3(startScrollBarPos.x, startScrollBarPos.y, Mathf.Lerp(startScrollBarPos.z, goalScrollBarPosZ, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        ScrollBarObj.transform.localPosition = new Vector3(startScrollBarPos.x, startScrollBarPos.y, goalScrollBarPosZ);
        _currentScrollBarPos = goalPos;

        for (int e = 0; e < 4; e++)
        {
            EntryTexts[e].text = clear ? "" : results[e + goalPos];
            EntryMeshes[e].material = EntryMats[(e + goalPos) % 2];
        }
        if (phaseTwo)
        {
            EntryTexts[0].fontStyle = goalPos == 0 ? UnityEngine.FontStyle.Bold : UnityEngine.FontStyle.Normal;
        }
        if (moduleSolved)
        {
            EntryTexts[0].text = bombName;
            EntryTexts[1].text = defusedBy;
            EntryTexts[1].fontStyle = UnityEngine.FontStyle.Italic;
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
            _scrollBarAnimation = StartCoroutine(AnimateScrollBarMovement(_currentScrollBarPos, false));
            return false;
        };
    }
}
