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
    public KMSelectable[] buttons;
    public TextMesh displayText;
    public TextMesh[] buttonTexts;
    public Material[] ledColors;
    public GameObject[] ledBulbs;
    public GameObject[] pointLights;


    static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;
    int stage = 1;
    int displayIndex;
    int positionPressed;
    string displaySymbol;
    string answerSymbol;
    string symbolPressed;
    List<string> buttonSymbols = new List<string>();
    static string[] symbols = new string[]
    {
    @"(",   @",",   @">",   @"/",   @"}",
    @"]",   @"_",   @"-",   "\"",   @"|",
    @"»",   @":",   @".",   @"{",   @"<",
    @"”",   @"«",   @"`",   @"[",   @"?",
    @")",   @"!",   @"\",   @"'",   @";",
    };
    List<string> symbolsList = new List<string>();
    List<string> possibleAnswers = new List<string>();
    bool isAnimating;
    bool[] ledStates = new bool[3];

    string buttonToPress;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable button in buttons)
        {
            button.OnInteract += delegate () { ButtonPress(button); return false; };
        }

    }
    void Start()
    {
        
        symbolsList = symbols.ToList();
        ClearInfo();
        GetDisplay();
        GetAnswer();
        GetButtons();
        DoLogging();
        StartCoroutine(DisplayInfo());
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
        {
            possibleAnswers.Add(symbols[displayIndex - 5]);
        }
        if (displayIndex < 20) //if not on bottom edge, add symbol below
        {
            possibleAnswers.Add(symbols[displayIndex + 5]);
        }
        if (!(displayIndex % 5 == 0)) //if not on left edge, add symbol to left
        {
            possibleAnswers.Add(symbols[displayIndex - 1]);
        }
        if (!(displayIndex % 5 == 4)) //if not on right edge, add symbol to right
        {
            possibleAnswers.Add(symbols[displayIndex + 1]);
        }

        answerSymbol = possibleAnswers[UnityEngine.Random.Range(0, possibleAnswers.Count)];
        buttonSymbols.Add(answerSymbol);
        symbolsList.Remove(answerSymbol);
    }
    void GetButtons()
    {
        symbolsList.Remove(answerSymbol);
        foreach (string symbol in possibleAnswers)
        {
            symbolsList.Remove(symbol);
        }
        symbolsList.Shuffle();
        buttonSymbols.Add(symbolsList[0]);
        buttonSymbols.Add(symbolsList[1]);
        buttonSymbols.Shuffle();
    }
    void DoLogging()
    {
        Debug.LogFormat("[Interpunct #{0}] Stage {1}: The symbol on the display is {2}.", moduleId, stage, displaySymbol);
        Debug.LogFormat("[Interpunct #{0}] The symbols on the buttons are {1} {2} {3}", moduleId, buttonSymbols[0], buttonSymbols[1], buttonSymbols[2]);
        Debug.LogFormat("[Interpunct #{0}] The correct button to press is {1}", moduleId, answerSymbol);
    }

    void LightOn()
    {
        ledBulbs[stage - 1].GetComponent<MeshRenderer>().material = ledColors[1];
        pointLights[stage - 1].SetActive(true);
        ledStates[stage - 1] = true;
    }
    void LightOff()
    {
        ledBulbs[stage - 1].GetComponent<MeshRenderer>().material = ledColors[0];
        pointLights[stage - 1].SetActive(false);
        ledStates[stage - 1] = false;
    }

    void ToggleLight()
    {
        if (ledStates[stage - 1] == true)
        {
            LightOff();
        }
        else
        {
            LightOn();
        }
    }

    void ButtonPress(KMSelectable button)
    {
        if (moduleSolved || isAnimating)
        {
            return;
        }
        button.AddInteractionPunch(1.5f);
        GetComponent<KMAudio>().PlaySoundAtTransform("bulbPressSFX", transform);
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
            Start();
        }
    }

    IEnumerator StageAnim()
    {
        isAnimating = true;
        displayText.text = string.Empty;
        for (int i = 0; i < 3; i++)
        {
            buttonTexts[i].text = string.Empty;
        }

        for (int i = 0; i < UnityEngine.Random.Range(8, 12); i++)
        {
            ToggleLight();
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.05f, 0.40f));
        }
        LightOn();
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
            Start();
        }
    }
    IEnumerator DisplayInfo()
    {
        isAnimating = true;
        for (int i = 0; i < 3; i++)
        {
            buttonTexts[i].text = buttonSymbols[i];
        }
        yield return new WaitForSeconds(0.5f);
        displayText.text = displaySymbol;
        isAnimating = false;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use [!{0} press 1] to press the leftmost button. ";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        string[] parameters = Command.Split(' ');
        if (parameters.Length > 2)
        {
            yield return null;
        }
        else
        {
            if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                buttonToPress = parameters[1];
            }
            else
            {
                buttonToPress = parameters[0];
            }
            if (!"123".Contains(buttonToPress))
            {
                yield return null;
            }
            else
            {
                buttons[int.Parse(buttonToPress) - 1].OnInteract();
                yield return "solve";
            }
        }   
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            while (isAnimating)
            {
                yield return true;
            }
            for (int i = 0; i < 3; i++)
            {
                if (buttonSymbols[i] == answerSymbol)
                {
                    yield return new WaitForSeconds(0.1f);
                    buttons[i].OnInteract();
                    break;
                }
            }
        }
    }
}
