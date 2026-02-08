using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using KModkit;
using Rnd = UnityEngine.Random;

public class investigationScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public Text SearchQuery;
    public KMSelectable SearchButton;
    public TextMesh ResultSummary;
    public Text[] EntryTexts;
    public KMSelectable ScrollBar;
    public TextMesh BottomText;

    public TextAsset[] Transcripts;

    int transcriptIx;
    string chosenTranscript;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;
        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
        }
        */

        //button.OnInteract += delegate () { buttonPress(); return false; };

    }

    // Use this for initialization
    void Start () {
        transcriptIx = Rnd.Range(0, Transcripts.Length);
        chosenTranscript = Transcripts[transcriptIx].ToString();
        
        //Debug.Log(Solves[transcriptIx].MissionName);
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
