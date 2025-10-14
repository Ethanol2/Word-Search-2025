using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [SerializeField] private WordList _wordList;
    [SerializeField] private bool _generateOnStart = true;

    [Space]
    [SerializeField] private Vector2Int _boardSize = new Vector2Int(10, 10);
    [SerializeField] private Letter _letterPrefab;
    [SerializeField] private RectTransform _letterContainer;

    [Space]
    [SerializeField] private int _defaultWordCount = 10;
    [SerializeField] private bool _allowDiagonalWords = true;
    [SerializeField] private bool _allowBackwardWords = true;
    [SerializeField, Range(0f, 1f)] private float _backwardsChance = 0.33f;
    [SerializeField] private int _maxPlacementAttempts = 100;

    [Header("Debug")]
    [SerializeField] private List<string> _remainingWords;
    [SerializeField] private List<Letter> _letterPool;

    void OnValidate()
    {
        PopulateLetterPool(_boardSize.x * _boardSize.y);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_generateOnStart && _wordList)
            GenerateBoard(_defaultWordCount);
    }

    public void GenerateBoard(int wordCount, bool random = true) => GenerateBoard(_wordList.GetWords(wordCount, random), _boardSize, _allowDiagonalWords, _allowBackwardWords);
    public void GenerateBoard(string[] words) => GenerateBoard(words, _boardSize, _allowDiagonalWords, _allowBackwardWords);
    public void GenerateBoard(string[] words, Vector2Int boardSize) => GenerateBoard(words, boardSize, _allowDiagonalWords, _allowBackwardWords);
    public void GenerateBoard(string[] words, Vector2Int boardSize, bool allowDiagonals, bool allowBackwards)
    {
        int index = 0;

        char[,] boardChars = new char[boardSize.x, boardSize.y];
        foreach (string word in words)
            boardChars = FitWord(word, boardChars, allowDiagonals, allowBackwards);

        PopulateLetterPool(boardSize.x * boardSize.y);

        for (int y = 0; y < boardSize.y; y++)
        {
            for (int x = 0; x < boardSize.x; x++)
            {
                Letter letter = _letterPool[index];

                letter.gameObject.SetActive(true);
                letter.Char = boardChars[x, y] == '\0' ? '\0' /*(char)Random.Range(65, 91)*/ : boardChars[x, y];
                SetAnchors(letter.transform as RectTransform, x, y, boardSize.x, boardSize.y, 0f);

                index++;
            }
        }
    }

    // Utility
    public static void SetAnchors(RectTransform rect, int x, int y, int xCount, int yCount, float padding)
    {
        // To flip the y placement
        y = yCount - y - 1;

        rect.anchorMin = new Vector2(x * (1f / xCount), y * (1f / yCount)) + (Vector2.one * padding);
        rect.anchorMax = new Vector2((x + 1) * (1f / xCount), (y + 1) * (1f / yCount)) - (Vector2.one * padding);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    private void PopulateLetterPool(int count)
    {
        if (_letterContainer && _letterPrefab && _letterPool.Count < count)
        {
            int toAdd = count - _letterPool.Count;

            for (int i = 0; i < toAdd; i++)
            {
                GenerateLetter();
            }
        }
    }
    private Letter GenerateLetter()
    {
        Letter letterInstance = Instantiate(_letterPrefab, _letterContainer);
        letterInstance.gameObject.SetActive(false);
        _letterPool.Add(letterInstance);
        return letterInstance;
    }
    private char[,] FitWord(string word, char[,] grid, bool allowDiagonal, bool allowBackwards)
    {
        // Horizontal: 0, Vertical: 1, Diagonal Up: 2, Diagonal Down: 3
        int fitLimit = allowDiagonal ? 4 : 3;
        int fitType = Random.Range(0, fitLimit);

        if (allowBackwards ? Random.Range(0f, 1f) <= _backwardsChance : false)
            word = word.Reverse().ToString();

        int orientationAttempts = 0;
        bool success = false;

        while (orientationAttempts < fitLimit && !success)
        {
            switch (fitType)
            {
                case 0:
                    success = FitWordStraight(word, word.Length, grid.GetLength(1), true, ref grid);
                    break;
                case 1:
                    success = FitWordStraight(word, grid.GetLength(0), word.Length, false, ref grid);
                    break;
            }

            orientationAttempts++;
            fitType = (int)Mathf.Repeat(fitType + 1, fitLimit);
        }

        if (!success)
            Debug.Log("Failed to use word: " + word);

        return grid;
    }
    bool FitWordStraight(string word, int maxX, int maxY, bool horizontal, ref char[,] grid)
    {
        if (word.Length > grid.GetLength(1))
            return false;

        Debug.Log("Adding word: " + word);

        int xIncrement, yIncrement;
        if (horizontal)
        {
            xIncrement = 1;
            yIncrement = 0;
        }
        else
        {
            xIncrement = 0;
            yIncrement = 1;
        }

        bool safe = true;

        for (int x1 = Random.Range(0, maxX), x2 = 0, x; x2 < maxX; x2++)
        {
            x = x1 + x2;
            x = x >= maxX ? x - maxX : x;

            for (int y1 = Random.Range(0, maxY), y2 = 0, y; y2 < maxY; y2++)
            {
                y = y1 + y2;
                y = y >= maxY ? y - maxY : y;

                safe = true;

                for (int i = 0; i < word.Length; i++)
                {
                    try
                    {
                        if (grid[x + xIncrement, y + yIncrement] != '\0')
                        {
                            if (grid[x + xIncrement, y + yIncrement] != word[i])
                            {
                                safe = false;
                                break;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Something went wrong. X: {x}, Y: {y}, i: {i}, Word Length: {word.Length}\n\n{e}");
                    }
                }

                if (safe)
                {
                    for (int i = 0; i < word.Length; i++)
                    {
                        grid[x + xIncrement, y + yIncrement] = word[i];
                    }
                    break;
                }
            }
            if (safe)
                break;
        }

        return safe;
    }

    // bool FitWordDiagonal(string word, ref bool diaDown, bool backwards, ref Vector2Int startPos)
    // {
    //     int startRange = gridSize - word.Length;
    //     int dirCount = 0;
    //     int startCount = 0;
    //     bool safe;

    //     do
    //     {
    //         safe = true;

    //         // Diagonal Down
    //         if (diaDown)
    //         {
    //             do
    //             {
    //                 safe = true;
    //                 startCount++;
    //                 startPos.x = Random.Range(0, startRange);
    //                 startPos.y = Random.Range(0, startRange);
    //                 for (int k = 0; k < word.Length; k++)
    //                 {
    //                     if (CheckFilled(new Vector2Int(startPos.x + k, startPos.y + k)))
    //                     {
    //                         if (grid[startPos.x + k, startPos.y + k] != word[k] && !backwards)
    //                         {
    //                             safe = false;
    //                         }
    //                         else if (grid[startPos.x + k, startPos.y + k] != word[word.Length - 1 - k] && backwards)
    //                         {
    //                             safe = false;
    //                         }
    //                     }
    //                 }
    //             }
    //             while (!safe && startCount <= startRange * startRange);

    //             if (startCount >= startRange * startRange && !safe)
    //             {
    //                 dirCount++;
    //                 diaDown = !diaDown;
    //                 startCount = 0;
    //             }
    //         }
    //         // Diagonal Up
    //         else if (!diaDown)
    //         {
    //             do
    //             {
    //                 safe = true;
    //                 startCount++;
    //                 startPos.x = Random.Range(0, startRange);
    //                 startPos.y = Random.Range(gridSize - startRange, gridSize);
    //                 for (int k = 0; k < word.Length; k++)
    //                 {
    //                     if (CheckFilled(new Vector2Int(startPos.x + k, startPos.y - k)))
    //                     {
    //                         if (grid[startPos.x + k, startPos.y - k] != word[k] && !backwards)
    //                         {
    //                             safe = false;
    //                         }
    //                         else if (grid[startPos.x + k, startPos.y - k] != word[word.Length - 1 - k] && backwards)
    //                         {
    //                             safe = false;
    //                         }
    //                     }
    //                 }
    //             }
    //             while (!safe && startCount <= startRange * startRange && !safe);

    //             if (startCount >= startRange * startRange && !safe)
    //             {
    //                 dirCount++;
    //                 diaDown = !diaDown;
    //                 startCount = 0;
    //             }
    //         }
    //     } while (!safe && dirCount <= 1);

    //     if (dirCount >= 2)
    //     {
    //         return false;
    //     }
    //     return true;
    // }
}
