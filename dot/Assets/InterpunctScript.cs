using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class InterpunctScript : MonoBehaviour
{

    public KMBombInfo bomb;
    public KMAudio audio;
    public KMRuleSeedable Ruleseed;
    public KMSelectable[] buttons;
    public TextMesh displayText;
    public TextMesh[] buttonTexts;
    public Material[] ledColors;
    public GameObject[] ledBulbs;
    public GameObject[] pointLights;


    static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved, solving;
    int stage = 1;
    int displayIndex;
    int positionPressed;
    string displaySymbol;
    string answerSymbol;
    string symbolPressed;
    List<string> buttonSymbols = new List<string>();
    static readonly string[] allSymbols = {
        //Default table
        "(",   ",",   ">",   "/",   "}",
        "]",   "_",   "-",   "\"",  "|",
        "»",   ":",   ".",   "{",   "<",
        "”",   "«",   "`",   "[",   "?",
        ")",   "!",   "\\",   "'",  ";",
        //Ruleseed symbols
        "&", "#", "^", "+", "~", "¡", "‽", "*", "°"
        };
    string[] symbols;
    List<string> symbolsList = new List<string>();
    List<string> possibleAnswers = new List<string>();
    bool isAnimating;
    bool[] ledStates = new bool[3];

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonPress(button); return false; };
    }
    void Start()
    {
        SetUpTable();
        GenerateStage();
    }
    void GenerateStage()
    {
        symbolsList = symbols.ToList();
        ClearInfo();
        GetDisplay();
        GetAnswer();
        GetButtons();
        DoLogging();
        StartCoroutine(DisplayInfo());
    }

    void SetUpTable()
    {
        var rng = Ruleseed.GetRNG();
        if (rng.Seed != 1)
            rng.ShuffleFisherYates(allSymbols);
        symbols = allSymbols.Take(25).ToArray();
        Debug.Log(symbols.Join());
    }

    void ClearInfo()
    {
        possibleAnswers.Clear();
        buttonSymbols.Clear();
    }
    void GetDisplay()
    {
        displayIndex = UnityEngine.Random.Range(0, 25);
        displaySymbol = symbols[displayIndex];
    }
    void GetAnswer()
    {
        if (displayIndex > 4) // if not on top edge, add symbol above
            possibleAnswers.Add(symbols[displayIndex - 5]);
        if (displayIndex < 20) //if not on bottom edge, add symbol below
            possibleAnswers.Add(symbols[displayIndex + 5]);
        if (!(displayIndex % 5 == 0)) //if not on left edge, add symbol to left
            possibleAnswers.Add(symbols[displayIndex - 1]);
        if (!(displayIndex % 5 == 4)) //if not on right edge, add symbol to right
            possibleAnswers.Add(symbols[displayIndex + 1]);

        answerSymbol = possibleAnswers[UnityEngine.Random.Range(0, possibleAnswers.Count)];
        buttonSymbols.Add(answerSymbol);
        symbolsList.Remove(answerSymbol);
    }
    void GetButtons()
    {
        symbolsList.Remove(answerSymbol); // this line also is at line 102. Why does it clear it twice? because fuck you.
        foreach (string symbol in possibleAnswers) //Removes any symbols that could possible be correct from the list of symbols.
        {
            symbolsList.Remove(symbol);
        }
        symbolsList.Shuffle();
        symbolsList.Remove(answerSymbol);
        buttonSymbols.Add(symbolsList[0]);
        buttonSymbols.Add(symbolsList[1]);   //Takes 2 random symbols and puts them on random buttons.
        buttonSymbols.Shuffle(); 
    }
    void DoLogging()
    {
        Debug.LogFormat("[Interpunct #{0}] Stage {1}: The symbol on the display is {2}.", moduleId, stage, displaySymbol);
        Debug.LogFormat("[Interpunct #{0}] The symbols on the buttons are {1} {2} {3}", moduleId, buttonSymbols[0], buttonSymbols[1], buttonSymbols[2]);
        Debug.LogFormat("[Interpunct #{0}] The correct button to press is {1}", moduleId, answerSymbol);
    }

    void LightOn(int number)
    {
        ledBulbs[number].GetComponent<MeshRenderer>().material = ledColors[1];
        pointLights[number].SetActive(true);
        ledStates[number] = true;
    }
    void LightOff(int number)
    {
        ledBulbs[number].GetComponent<MeshRenderer>().material = ledColors[0];
        pointLights[number].SetActive(false);
        ledStates[number] = false;
    }
    void ToggleLight(int number)
    {
        if (ledStates[number]) LightOff(number); 
        else  LightOn(number);
        //If the light is on, turn it off. If the light is off, turn it on.
    }

    void ButtonPress(KMSelectable button)
    {
        if (isAnimating)
            return;
        button.AddInteractionPunch(1.5f);
        GetComponent<KMAudio>().PlaySoundAtTransform("bulbPressSFX", transform);
        if (moduleSolved)
        {
            ToggleLight(Array.IndexOf(buttons, button));
            return;
        }


        positionPressed = Array.IndexOf(buttons, button);
        symbolPressed = buttonSymbols[positionPressed];

        Debug.LogFormat("[Interpunct #{0}] You pressed button {1}, which had the symbol of {2}", moduleId, positionPressed + 1, symbolPressed);

        if (symbolPressed == answerSymbol)
        {
            Debug.LogFormat("[Interpunct #{0}] That was correct.\n", moduleId);
            StartCoroutine(StageAnim());
        }
        else
        {
            Debug.LogFormat("[Interpunct #{0}] That was incorrect. Strike.\n", moduleId);
            GetComponent<KMBombModule>().HandleStrike();
            displayText.text = string.Empty;
            GenerateStage();
        }
    }

    IEnumerator StageAnim()
    {
        isAnimating = true;
        solving = stage == 3;
        displayText.text = string.Empty;
        for (int i = 0; i < 3; i++)
            buttonTexts[i].text = string.Empty;

        for (int i = 0; i < UnityEngine.Random.Range(8, 12); i++) //Toggles the light a number of times between 8 and 11.
        {
            ToggleLight(stage - 1);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.05f, 0.40f)); //Takes a random float to determine how long to keep the light color for each flicker.
        }
        LightOn(stage - 1); //Finally makes sure that the light ends up on.
        isAnimating = false;
        if (stage == 3)
        {
            Debug.LogFormat("[Interpunct #{0}] Module solved.", moduleId);
            moduleSolved = true;
            GetComponent<KMAudio>().PlaySoundAtTransform("solveSoundSFX", transform);
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        { 
            stage++;
            GenerateStage();
        }
    }
    IEnumerator DisplayInfo()
    {
        isAnimating = true;
        for (int i = 0; i < 3; i++)
            buttonTexts[i].text = buttonSymbols[i];
        yield return new WaitForSeconds(0.5f);
        displayText.text = displaySymbol;
        isAnimating = false;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use [!{0} press 1/2/3] to press that button from left to right. "; 
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        Match m = Regex.Match(command.Trim().ToUpperInvariant(), @"^(?:PRESS\s+|SUBMIT\s+)?([1-3])$");
        if (m.Success)
        {
            yield return null;
            buttons[m.Groups[1].Value[0] - '1'].OnInteract();
        }
        if (solving)
            yield return "solve";
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            while (isAnimating)
                yield return true;
            buttons[Array.IndexOf(buttonSymbols.ToArray(), answerSymbol)].OnInteract();
        }
    }
}
