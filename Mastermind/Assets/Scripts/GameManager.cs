using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* This class deals with the game progression, including input handling, animations, audio etc.
 * The main method here is the PlayGame Coroutine, which asks the codemaster for guesses, and
 * continues until he manages to crack the code
 */
public class GameManager : MonoBehaviour
{
    [SerializeField]
    GameObject code;
    [SerializeField]
    GameObject pressSpaceInstruction;
    [SerializeField]
    GameObject enterDigitsInstruction;
    [SerializeField]
    Text enemyBreakingCodeText;
    [SerializeField]
    GameObject[] guesses;
    [SerializeField]
    Codebreaker codebreaker;
    [SerializeField]
    AudioClip keyPressSound;
    [SerializeField]
    AudioClip blackPegSound;
    [SerializeField]
    AudioClip whitePegSound;
    [SerializeField]
    AudioClip codeCrackedSound;

    public const int MAX_DIGITS = 4;
    public const int NO_PEG = 0;
    public const int WHITE_PEG = 1;
    public const int BLACK_PEG = 2;
    public int[] WINNING_PEGS = { BLACK_PEG, BLACK_PEG, BLACK_PEG, BLACK_PEG};

    private int[] digits = new int[MAX_DIGITS];
    private Text[] codeDigits = new Text[MAX_DIGITS];
    private int digitsNum = 0;
    private Coroutine gameCoroutine = null;
    private Coroutine enemyBreakingTextCoroutine = null;
    private bool started = false;
    private List<GameObject> objectsToTurnOff = new List<GameObject>();

    // Update is called once per frame
    void Update()
    {
        if (!started && Input.GetKeyDown(KeyCode.Space))
        {
            Camera.main.GetComponent<AudioSource>().PlayOneShot(keyPressSound);
            codebreaker.ChallengePlayer();
            Camera.main.GetComponent<Animator>().SetTrigger("Start");
            pressSpaceInstruction.SetActive(false);
            StartCoroutine(StartedGame());
        }
        if (!started)
            return;
        if (digitsNum == MAX_DIGITS && gameCoroutine == null)
        {
            gameCoroutine = StartCoroutine(PlayGame());
        }
        else if (digitsNum < MAX_DIGITS && gameCoroutine == null)
        {
            ProcessDigits();
        }
    }

    public IEnumerator StartedGame()
    {
        yield return new WaitForSeconds(5f);
        enterDigitsInstruction.SetActive(true);
        int i = 0;
        foreach(Transform digit in code.transform)
        {
            codeDigits[i] = digit.GetComponent<Text>();
            codeDigits[i].text = "_";
            i++;
        }
        code.SetActive(true);
        objectsToTurnOff.Add(code);
        started = true;
    }

    private IEnumerator PlayGame()
    {
        enterDigitsInstruction.SetActive(false);
        enemyBreakingTextCoroutine = StartCoroutine(EnemyText());

        int[] guess; 
        int guessIndex = 0;
        GameObject guessLine;
        Transform pegs;
        int[] response = new int[MAX_DIGITS];

        do
        {
            yield return new WaitForSeconds(1f);
            
            guess = guessIndex == 0 ? codebreaker.FirstGuess() : codebreaker.GuessAgain(response);
            guessLine = guesses[guessIndex];
            PresentGuessToMonitor(guess, guessLine);
            objectsToTurnOff.Add(guessLine);
            response = Codebreaker.CheckCode(digits, guess);
            
            pegs = guessLine.transform.GetChild(MAX_DIGITS);
            int pegIndex = 0;
            for (int i = 0; i < MAX_DIGITS; i++)
            {
                int digit = guess[i];
                if (digit == digits[i])
                {
                    yield return new WaitForSeconds(1f);
                    StartCoroutine(FlashLetter(guessLine.transform.GetChild(i).GetComponent<Text>(), Color.black));
                    StartCoroutine(FlashLetter(code.transform.GetChild(i).GetComponent<Text>(), Color.black));
                    Transform peg = pegs.GetChild(pegIndex);
                    peg.GetComponent<Text>().color = Color.black;
                    yield return new WaitForSeconds(0.25f);
                    Camera.main.GetComponent<AudioSource>().PlayOneShot(blackPegSound);
                    peg.gameObject.SetActive(true);
                    objectsToTurnOff.Add(peg.gameObject);
                    pegIndex++;
                    continue;
                }
                for (int j = 0; j < MAX_DIGITS; j++)
                {
                    if (j != i && digit == digits[j])
                    {
                        yield return new WaitForSeconds(1f);
                        StartCoroutine(FlashLetter(guessLine.transform.GetChild(i).GetComponent<Text>(), Color.white));
                        StartCoroutine(FlashLetter(code.transform.GetChild(j).GetComponent<Text>(), Color.white));
                        Transform peg = pegs.GetChild(pegIndex);
                        peg.GetComponent<Text>().color = Color.white;
                        Camera.main.GetComponent<AudioSource>().PlayOneShot(whitePegSound);
                        yield return new WaitForSeconds(0.25f);
                        peg.gameObject.SetActive(true);
                        objectsToTurnOff.Add(peg.gameObject);
                        pegIndex++;
                        continue;
                    }
                }
            }
            guessIndex++;
        } while (!ComparePegs(response, WINNING_PEGS) && guessIndex < guesses.Length - 1);
        
        StartCoroutine(ResetGame(ComparePegs(response, WINNING_PEGS)));
    }

    public static bool ComparePegs(int[] pegs1, int[] pegs2)
    {
        for (int i = 0; i < MAX_DIGITS; i++)
        {
            if (pegs1[i] != pegs2[i])
                return false;
        }
        return true;
    }

    private IEnumerator FlashLetter(Text text, Color color)
    {
        Color startColor = text.color;
        float startTime = Time.time;
        float timeDiff = Time.time - startTime;
        while (timeDiff < 0.25f)
        {
            text.color = ((timeDiff / 0.25f) * color + ((0.25f - timeDiff) / 0.25f) * startColor);
            yield return new WaitForEndOfFrame();
            timeDiff = Time.time - startTime;
        }
        yield return new WaitForSeconds(0.5f);
        startTime = Time.time;
        timeDiff = Time.time - startTime;
        while (timeDiff < 0.25f)
        {
            text.color = ((timeDiff / 0.25f) * startColor + ((0.25f - timeDiff) / 0.25f) * color);
            yield return new WaitForEndOfFrame();
            timeDiff = Time.time - startTime;
        }
    }

    private static void PresentGuessToMonitor(int[] guess, GameObject guessLine)
    {
        for (int i = 0; i < MAX_DIGITS; i++)
        {
            guessLine.transform.GetChild(i).GetComponent<Text>().text = guess[i].ToString();
        }
        guessLine.SetActive(true);
    }

    private IEnumerator EnemyText()
    {
        enemyBreakingCodeText.text = "Enemy Cracking Code";
        enemyBreakingCodeText.gameObject.SetActive(true);
        objectsToTurnOff.Add(enemyBreakingCodeText.gameObject);
        while (started)
        {
            enemyBreakingCodeText.text = "Enemy Cracking Code";
            yield return new WaitForSeconds(0.2f);
            enemyBreakingCodeText.text = "Enemy Cracking Code.";
            yield return new WaitForSeconds(0.2f);
            enemyBreakingCodeText.text = "Enemy Cracking Code..";
            yield return new WaitForSeconds(0.2f);
            enemyBreakingCodeText.text = "Enemy Cracking Code...";
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator ResetGame(bool enemyWon)
    {
        StopCoroutine(enemyBreakingTextCoroutine);
        enemyBreakingTextCoroutine = null;
        started = false;
        Camera.main.GetComponent<AudioSource>().PlayOneShot(codeCrackedSound);
        if (enemyWon)
        {
            enemyBreakingCodeText.text = "CODE CRACKED!!!";
        }
        else
        {
            enemyBreakingCodeText.text = "CODE SAFE ";
        }
        digits = new int[MAX_DIGITS];
        codeDigits = new Text[MAX_DIGITS];
        digitsNum = 0;
        gameCoroutine = null;
        codebreaker.Reset();
        yield return new WaitForSeconds(2f);
        Camera.main.GetComponent<Animator>().SetTrigger("Finish");
        yield return new WaitForSeconds(2f);
        foreach (GameObject objectToTurnOff in objectsToTurnOff)
        {
            objectToTurnOff.SetActive(false);
        }
        objectsToTurnOff.Clear();
        pressSpaceInstruction.SetActive(true);
        started = false;
    }

    private void ProcessDigits()
    {
        foreach (char c in Input.inputString)
        {
            int digit = c - '0';
            if (0 <= digit && digit <= 9)
            {
                if (DigitUsedAlready(digit))
                    continue;
                digits[digitsNum] = digit;
                Camera.main.GetComponent<AudioSource>().PlayOneShot(keyPressSound);
                codeDigits[digitsNum].text = c.ToString();
                digitsNum++;
                if (digitsNum == MAX_DIGITS)
                    break;
            }
        }
    }

    private bool DigitUsedAlready(int digit)
    {
        for (int i = 0; i < digitsNum; i++)
        {
            if (digit == digits[i])
            {
                return true;
            }
        }
        return false;
    }
}
