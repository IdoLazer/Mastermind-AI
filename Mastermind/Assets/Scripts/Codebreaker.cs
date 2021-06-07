using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Codebreaker : MonoBehaviour
{

    [SerializeField]
    public AudioClip shriekSound;

    [SerializeField]
    public AudioClip keyboardTypingSound;

    private const int DIGITS_NUM = 10;

    private List<int[]> possibleCombinations = new List<int[]>();
    private HashSet<int[]> possibleSolutions = new HashSet<int[]>();
    private int[] lastGuess;
    private Animator anim;
    private AudioSource audioSource;

    void Awake()
    {
        CreateCombinations(0, new int[GameManager.MAX_DIGITS]);
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.5f;
    }

    public void ChallengePlayer()
    {
        anim.SetTrigger("Start");
        audioSource.PlayOneShot(shriekSound);
    }

    public void StartTyping()
    {
        audioSource.clip = keyboardTypingSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    // Create all possible combinations of 4 distinct digits
    private void CreateCombinations(int depth, int[] digits)
    {
        if (depth == GameManager.MAX_DIGITS)
        {
            int[] newCombination = new int[GameManager.MAX_DIGITS];
            for (int i = 0; i < GameManager.MAX_DIGITS; i++)
            {
                newCombination[i] = digits[i];
            }
            possibleCombinations.Add(newCombination);
            possibleSolutions.Add(newCombination);
            return;
        }
        for (int digit = 0; digit < DIGITS_NUM; digit++)
        {
            bool digitAlreadyUsed = false;
            for (int i = 0; i < depth; i++)
            {
                if (digit == digits[i])
                    digitAlreadyUsed = true;
            }
            if (digitAlreadyUsed)
                continue;
            digits[depth] = digit;
            CreateCombinations(depth + 1, digits);
        }
    }

    // To be called when the game starts
    public int[] FirstGuess()
    {
        int[] guess = possibleCombinations[0];
        possibleCombinations.RemoveAt(0);
        possibleSolutions.Remove(guess);
        lastGuess = guess;
        return guess;
    }

    public void Reset()
    {
        anim.SetTrigger("Finish");
        audioSource.Stop();
        possibleCombinations = new List<int[]>();
        possibleSolutions = new HashSet<int[]>();
        CreateCombinations(0, new int[GameManager.MAX_DIGITS]);
    }

    // To be called if the last guess was wrong
    public int[] GuessAgain(int[] response)
    {
        ReducePossibleSolutions(response);
        List<int[]> bestGuesses = MinMaxGuesses();
        return PickGuess(bestGuesses);
    }

    // Reduce the possible solutions based on the Donald Knuth algorithm
    private void ReducePossibleSolutions(int[] response)
    {
        HashSet<int[]> newPossibleSolutions = new HashSet<int[]>();
        foreach (int[] possibleSolution in possibleSolutions)
        {
            if (GameManager.ComparePegs(CheckCode(lastGuess, possibleSolution), response))
            {
                // A code is a possible solution only if it recieves the same response our last guess received when compared to it
                newPossibleSolutions.Add(possibleSolution);
            }
        }
        possibleSolutions = newPossibleSolutions;
    }

    // Use a min-max algorithm to determine the next best guesses, based on the Donald Knuth algorithm
    private List<int[]> MinMaxGuesses()
    {
        Dictionary<int[], int> minimumScores = new Dictionary<int[], int>();
        Dictionary<int[], int> combinationScores = new Dictionary<int[], int>();
        int maxMinScore = 0;
        // For each combination we haven't tried yet, we find the minimum number of possible solutions it may remove from the current set, which will be its score
        foreach(int[] combination in possibleCombinations)
        {
            int maxCombinationScore = 0;
            foreach(int[] possibleSolution in possibleSolutions)
            {
                int[] response = CheckCode(combination, possibleSolution);
                if (combinationScores.ContainsKey(response))
                {
                    combinationScores[response] += 1;
                }
                else
                {
                    combinationScores[response] = 1;
                }
                if (combinationScores[response] > maxCombinationScore)
                {
                    maxCombinationScore = combinationScores[response];
                }
            }
            combinationScores.Clear();
            minimumScores[combination] = possibleSolutions.Count - maxCombinationScore;
            
            // We keep track of the maximum possible score
            if (minimumScores[combination] > maxMinScore)
            {
                maxMinScore = minimumScores[combination];
            }
        }
        
        List<int[]> bestGuesses = new List<int[]>();
        foreach (int[] possibleCombination in possibleCombinations)
        {
            if (minimumScores[possibleCombination] == maxMinScore)
            {
                bestGuesses.Add(possibleCombination);
            }
        }

        return bestGuesses;
    }

    private int[] PickGuess(List<int[]> bestGuesses)
    {
        foreach (int[] guess in bestGuesses)
        {
            if (possibleSolutions.Contains(guess))
            {
                lastGuess = guess;
                possibleSolutions.Remove(guess);
                possibleCombinations.Remove(guess);
                return guess;
            }
        }
        lastGuess = bestGuesses[0];
        possibleCombinations.Remove(bestGuesses[0]);
        return bestGuesses[0];
    }

    public static int[] CheckCode(int[] correct, int[] possibleSolution)
    {
        int[] response = new int[GameManager.MAX_DIGITS];
        int pegIndex = 0;
        for (int i = 0; i < GameManager.MAX_DIGITS; i++)
        {
            if (possibleSolution[i] == correct[i])
            {
                response[pegIndex] = GameManager.BLACK_PEG;
                pegIndex++;
            }
        }
        for (int i = 0; i < GameManager.MAX_DIGITS; i++)
        {
            if (possibleSolution[i] == correct[i])
                continue;
            for (int j = 0; j < GameManager.MAX_DIGITS; j++)
            {
                if (j != i && possibleSolution[i] == correct[j])
                {
                    response[pegIndex] = GameManager.WHITE_PEG;
                    pegIndex++;
                }
            }

        }
        return response;
    }
}
