using System.Collections.Generic;
using EditorTools;
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
    [SerializeField] private Placement _defaultPlacement = Placement.HORIZONTAL & Placement.VERTICAL & Placement.DIAGONAL_DOWN & Placement.DIAGONAL_UP;
    [SerializeField] private bool _defaultAllowBackwards = false;
    [SerializeField, Range(0f, 1f)] private float _backwardsChance = 0.33f;
    [SerializeField] private int _maxPlacementAttempts = 100;

    [Header("Debug")]
    [SerializeField] private List<string> _remainingWords;
    [SerializeField] private List<Letter> _letterPool;

    [System.Flags]
    public enum Placement
    {
        HORIZONTAL = 1 << 0,
        VERTICAL = 1 << 1,
        DIAGONAL_UP = 1 << 2,
        DIAGONAL_DOWN = 1 << 3,
        ALL = ~0
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
            PopulateLetterPool(_boardSize.x * _boardSize.y);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_generateOnStart && _wordList)
            GenerateBoard();
    }

    [ContextMenu("Generate Board")]
    public void GenerateBoard() => GenerateBoard(_defaultWordCount);
    public void GenerateBoard(int wordCount)
        => GenerateBoard(
            _wordList.GetWords(wordCount, _boardSize.x > _boardSize.y ? _boardSize.x : _boardSize.y),
            _boardSize,
            _defaultPlacement,
            _defaultAllowBackwards);
            
    public void GenerateBoard(string[] words) => GenerateBoard(words, _boardSize, _defaultPlacement, _defaultAllowBackwards);
    public void GenerateBoard(string[] words, Vector2Int boardSize) => GenerateBoard(words, boardSize, _defaultPlacement, _defaultAllowBackwards);
    public void GenerateBoard(string[] words, Vector2Int boardSize, Placement placementFilter, bool allowBackwards)
    {
        int index = 0;
        char[,] boardChars;        

        int addedWords;
        int attempts = 0;
        do
        {
            boardChars = new char[boardSize.x, boardSize.y];
            addedWords = 0;
            attempts++;

            int[] orientationsCount = new int[4];

            foreach (string word in words)
            {
                if (FitWord(word, allowBackwards, ref boardChars, ref orientationsCount))
                {
                    addedWords++;
                }
            }

        }
        while (addedWords < words.Length && attempts < _maxPlacementAttempts);
        
        if (addedWords < words.Length)
            this.Log($"Added {addedWords} / {words.Length} words");

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

        foreach (Letter letter in _letterPool)
            letter.gameObject.SetActive(false);
    }
    private Letter GenerateLetter()
    {
        Letter letterInstance = Instantiate(_letterPrefab, _letterContainer);
        _letterPool.Add(letterInstance);
        return letterInstance;
    }
    // Written myself but with heavy reference
    private int GetWeightedRandom(int[] counts)
    {
        float totalWeight = 0f;
        float[] weights = new float[counts.Length];

        for (int i = 0; i < counts.Length; i++)
        {
            weights[i] = 1f / (counts[i] + 1f);
            totalWeight += weights[i];
        }

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < counts.Length; i++)
        {
            cumulative += weights[i];
            if (random <= cumulative)
                return i;
        }

        this.Log("Random failed, returning 0");
        return 0;
    }
    private string ReverseString(string word)
    {
        string output = string.Empty;
        for (int i = word.Length - 1; i >= 0; i--)
            output += word[i];

        return output;
    }
    private bool FitWord(string word, bool allowBackwards, ref char[,] grid, ref int[] orientationsCount)
    {
        // Horizontal: 0, Vertical: 1, Diagonal Up: 2, Diagonal Down: 3
        int fitLimit = orientationsCount.Length;
        int fitType = GetWeightedRandom(orientationsCount);

        if (allowBackwards ? Random.Range(0f, 1f) <= _backwardsChance : false)
            word = ReverseString(word);

        int orientationAttempts = 0;
        bool success = false;

        while (orientationAttempts <= fitLimit)
        {
            if (success = PlaceWord(word, fitType, ref grid))
            {
                success = true;
                orientationsCount[fitType]++;
                break;
            }

            orientationAttempts++;
            fitType = (int)Mathf.Repeat(fitType + 1, fitLimit);
        }

        return success;
    }
    private bool PlaceWord(string word, int type, ref char[,] grid)
    {
        if (word.Length > grid.GetLength(1))
            return false;

        int xIncrement, yIncrement, maxX, maxY, minY;

        switch (type)
        {
            case 0:
                xIncrement = 1;
                yIncrement = 0;
                maxX = grid.GetLength(0) - word.Length + 1;
                minY = 0;
                maxY = grid.GetLength(1);
                break;

            case 1:
                xIncrement = 0;
                yIncrement = 1;
                maxX = grid.GetLength(0);
                minY = 0;
                maxY = grid.GetLength(1) - word.Length + 1;
                break;

            case 2:
                xIncrement = 1;
                yIncrement = -1;
                maxX = grid.GetLength(0) - word.Length + 1;
                minY = word.Length;
                maxY = grid.GetLength(1);
                break;

            case 3:
                xIncrement = 1;
                yIncrement = 1;
                maxX = grid.GetLength(0) - word.Length + 1;
                minY = 0;
                maxY = grid.GetLength(1) - word.Length + 1;
                break;

            default:
                this.LogError("Unhandled type: " + type);
                return false;
        }

        bool safe = true;

        for (int x1 = Random.Range(0, maxX), x2 = 0, x; x2 < maxX; x2++)
        {
            x = x1 + x2;
            x = x >= maxX ? x - maxX : x;

            for (int y1 = Random.Range(0, maxY - minY), y2 = minY, y; y2 < maxY; y2++)
            {
                y = y1 + y2;
                y = y >= maxY ? y - maxY + minY : y;

                safe = true;

                for (int i = 0; i < word.Length; i++)
                {
                    try
                    {
                        if (grid[x + (xIncrement * i), y + (yIncrement * i)] != '\0')
                        {
                            if (grid[x + (xIncrement * i), y + (yIncrement * i)] != word[i])
                            {
                                safe = false;
                                break;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        this.LogError(
                            $"Something went wrong placing the word \"{word}\" in orientation {type}\n Position: {x}, {y}\n i: {i}, Word Length: {word.Length}" +
                            $"\n X Limits: {maxX}\n Y Limits: {minY}, {maxY} \n\n{e}");
                        return false;
                    }
                }

                if (safe)
                {
                    for (int i = 0; i < word.Length; i++)
                    {
                        grid[x + (xIncrement * i), y + (yIncrement * i)] = word[i];
                    }
                    break;
                }
            }
            if (safe)
                break;
        }

        return safe;
    }

    public struct Word
    {
        public string Value;
        public Vector2Int Position;
        public Placement Placement;
    }
}