using System.Collections.Generic;
using EditorTools;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    #region Inspector

    [Header("Game Settings")]
    [SerializeField] private Color _correctSelectionColour = Color.green;

    [Header("Generation Settings")]
    [SerializeField] private WordList _wordList;
    [SerializeField] private bool _generateOnStart = true;

    [Space]
    [SerializeField] private Vector2Int _boardSize = new Vector2Int(10, 10);
    [SerializeField] private Letter _letterPrefab;
    [SerializeField] private RectTransform _letterContainer;

    [Space]
    [SerializeField] private int _defaultWordCount = 10;
    [SerializeField]
    private PlacementSettings _defaultPlacementSettings = new PlacementSettings()
    {
        Horizontal = true,
        Vertical = true,
        DiagonalUp = true,
        DiagonalDown = true
    };
    [SerializeField, Range(0f, 1f)] private float _backwardsChance = 0.33f;
    [SerializeField] private int _maxPlacementAttempts = 100;

    [Header("Debug")]
    [SerializeField] private List<Word> _currentWords;
    [SerializeField] private Letter[,] _currentGrid;
    [SerializeField] private Vector2Int _currentGridSize;
    [SerializeField] private List<Letter> _letterPool;

    private Vector2 _currentLetterSize;

    public Word[] CurrentWords => _currentWords.ToArray();
    public Letter[,] CurrentGrid => _currentGrid;
    public Vector2 CurrentLetterSize => _currentLetterSize;

    public Color CorrectSelectionColour => _correctSelectionColour;

    #endregion

    #region Events

    [Space]
    public UnityEvent<Word[]> OnGenerated;
    public event System.Action<Word[]> OnBoardGenerated;
    public UnityEvent<string> OnWordFound;
    public event System.Action<string> OnWordDiscovered;
    public UnityEvent OnAllWordsFound;
    public event System.Action OnAllWordsDiscovered;

    #endregion

    #region LifeCycle
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

    #endregion

    #region Public Methods

    [ContextMenu("Generate Board")]
    // Only for a temp button
    public void Generate() => GenerateBoard();
    public List<Word> GenerateBoard() => GenerateBoard(_defaultWordCount);
    public List<Word> GenerateBoard(int wordCount)
        => GenerateBoard(
            _wordList.GetWords(wordCount, _boardSize.x > _boardSize.y ? _boardSize.x : _boardSize.y),
            _boardSize,
            _defaultPlacementSettings);

    public List<Word> GenerateBoard(string[] words) => GenerateBoard(words, _boardSize, _defaultPlacementSettings);
    public List<Word> GenerateBoard(string[] words, Vector2Int boardSize) => GenerateBoard(words, boardSize, _defaultPlacementSettings);
    public List<Word> GenerateBoard(string[] words, Vector2Int boardSize, PlacementSettings placementSettings)
    {
        int index = 0;
        char[,] boardChars;
        List<Word> wordObjects = new List<Word>();

        int attempts = 0;
        do
        {
            boardChars = new char[boardSize.x, boardSize.y];
            wordObjects.Clear();
            attempts++;

            Dictionary<Placement, int> orientationsCount = placementSettings.GetDictionary();

            foreach (string word in words)
            {
                if (FitWord(word, placementSettings.Backwards, ref boardChars, ref orientationsCount, out Word wordObj))
                {
                    if (wordObj == null)
                    {
                        this.LogError("Null word out from FitWord");
                        break;
                    }
                    wordObjects.Add(wordObj);
                }
            }

        }
        while (wordObjects.Count < words.Length && attempts < _maxPlacementAttempts);

        if (wordObjects.Count < words.Length)
            this.Log($"Added {wordObjects.Count} / {words.Length} words");

        PopulateLetterPool(boardSize.x * boardSize.y);
        _currentGrid = new Letter[boardSize.x, boardSize.y];

        for (int y = 0; y < boardSize.y; y++)
        {
            for (int x = 0; x < boardSize.x; x++)
            {
                Letter letter = _letterPool[index];
                _currentGrid[x, y] = letter;
                letter.SetCoordinates(x, y);

                letter.gameObject.SetActive(true);
                letter.Char = boardChars[x, y] == '\0' ? (char)Random.Range(65, 91) : boardChars[x, y];
                SetAnchors(letter.transform as RectTransform, x, y, boardSize.x, boardSize.y, 0f);

                index++;
            }
        }

        _currentWords = wordObjects;
        _currentGridSize = boardSize;
        _currentLetterSize = ((RectTransform)_currentGrid[0, 0].transform).rect.size;

        if (_currentWords.Contains(null))
        {
            this.LogError("An null word is in the list");
            _currentWords.RemoveAll(null);
        }

        var wordsArr = _currentWords.ToArray();
        OnBoardGenerated?.Invoke(wordsArr);
        OnGenerated.Invoke(wordsArr);
        return wordObjects;
    }

    public static void SetAnchors(RectTransform rect, int x, int y, int xCount, int yCount, float padding)
    {
        // To flip the y placement
        y = yCount - y - 1;

        rect.anchorMin = new Vector2(x * (1f / xCount), y * (1f / yCount)) + (Vector2.one * padding);
        rect.anchorMax = new Vector2((x + 1) * (1f / xCount), (y + 1) * (1f / yCount)) - (Vector2.one * padding);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    public Vector2Int GetCoords(Vector3 localPosition)
    {
        Rect letterRect = (_currentGrid[0, 0].transform as RectTransform).rect;

        localPosition.x += _currentGridSize.x * (letterRect.size.x / 2f);
        localPosition.y += _currentGridSize.y * (letterRect.size.y / 2f);

        return new Vector2Int()
        {
            x = Mathf.FloorToInt(localPosition.x / letterRect.size.x),
            y = _currentGridSize.y - Mathf.FloorToInt(localPosition.y / letterRect.size.y) - 1
        };
    }
    public bool GetLetterAtPosition(Vector3 localPosition, out Letter letter)
    {
        Vector2Int coordinates = GetCoords(localPosition);

        if (coordinates.x >= 0 && coordinates.x < _currentGridSize.x && coordinates.y >= 0 && coordinates.y < _currentGridSize.y)
        {
            letter = _currentGrid[coordinates.x, coordinates.y];
            return true;
        }

        letter = null;
        return false;
    }

    public bool CheckInputSelection(string word, Vector2Int coordinates)
    {
        foreach (Word wordObj in _currentWords)
        {
            if (wordObj.Value == word && wordObj.Position == coordinates)
            {
                if (wordObj.Found)
                    return false;

                wordObj.Found = true;

                OnWordFound.Invoke(word);
                OnWordDiscovered?.Invoke(word);

                if (AllWordsFound())
                {
                    OnAllWordsDiscovered?.Invoke();
                    OnAllWordsFound.Invoke();
                }

                return true;
            }
        }

        return false;
    }
    public bool AllWordsFound()
    {
        bool output = true;
        foreach (Word word in _currentWords)
            output = output && word.Found;
        return output;
    }

    #endregion

    #region Private Methods

    private void PopulateLetterPool(int count)
    {
        if (_letterContainer && _letterPrefab && _letterPool.Count < count)
        {
            int toAdd = count - _letterPool.Count;

            for (int i = 0; i < toAdd; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    var newLetter = UnityEditor.PrefabUtility.InstantiatePrefab(_letterPrefab, _letterContainer) as Letter;
                    _letterPool.Add(newLetter);
                }
                else
#endif
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
    private Placement GetWeightedRandom(Dictionary<Placement, int> counts)
    {
        float totalWeight = 0f;
        Dictionary<Placement, float> weights = new Dictionary<Placement, float>();

        foreach (Placement key in counts.Keys)
        {
            weights.Add(key, 1f / (counts[key] + 1f));
            totalWeight += weights[key];
        }

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (Placement key in counts.Keys)
        {
            cumulative += weights[key];
            if (random <= cumulative)
                return key;
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
    private bool FitWord(string word, bool allowBackwards, ref char[,] grid, ref Dictionary<Placement, int> orientationsCount, out Word wordObject)
    {
        wordObject = null;

        int fitLimit = orientationsCount.Keys.Count;
        Placement placement = GetWeightedRandom(orientationsCount);

        if (allowBackwards ? Random.Range(0f, 1f) <= _backwardsChance : false)
            word = ReverseString(word);

        int orientationAttempts = 0;
        bool success = false;

        while (orientationAttempts <= fitLimit)
        {
            if (orientationsCount.ContainsKey(placement))
            {
                if (success = PlaceWord(word, placement, ref grid, out wordObject))
                {
                    orientationsCount[placement]++;
                    break;
                }

                orientationAttempts++;
            }

            placement = (int)placement + 1 > 3 ? Placement.HORIZONTAL : placement + 1;
        }

        return success;
    }
    private bool PlaceWord(string word, Placement type, ref char[,] grid, out Word wordObject)
    {
        wordObject = null;

        if (word.Length > grid.GetLength(1))
            return false;
        if (word.Length == 0)
            return false;

        int xIncrement, yIncrement, maxX, maxY, minY;

        switch (type)
        {
            case Placement.HORIZONTAL:
                xIncrement = 1;
                yIncrement = 0;
                maxX = grid.GetLength(0) - word.Length + 1;
                minY = 0;
                maxY = grid.GetLength(1);
                break;

            case Placement.VERTICAL:
                xIncrement = 0;
                yIncrement = 1;
                maxX = grid.GetLength(0);
                minY = 0;
                maxY = grid.GetLength(1) - word.Length + 1;
                break;

            case Placement.DIAGONAL_UP:
                xIncrement = 1;
                yIncrement = -1;
                maxX = grid.GetLength(0) - word.Length + 1;
                minY = word.Length;
                maxY = grid.GetLength(1);
                break;

            case Placement.DIAGONAL_DOWN:
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

                    wordObject = new Word()
                    {
                        Value = word,
                        Position = new Vector2Int(x, y),
                        Placement = type,
                    };

                    break;
                }
            }
            if (safe)
                break;
        }

        return safe;
    }

    #endregion

    #region Support Objects

    [System.Serializable]
    public class Word
    {
        public string Value;
        public Vector2Int Position;
        public Placement Placement;
        public bool Found = false;

        public override string ToString()
        {
            if (Found)
                return "<s>" + Value + "</s>";
            return Value;
        }
    }
    [System.Serializable]
    public struct PlacementSettings
    {
        public bool Horizontal;
        public bool Vertical;
        public bool DiagonalUp;
        public bool DiagonalDown;
        public bool Backwards;

        public Dictionary<Placement, int> GetDictionary()
        {
            Dictionary<Placement, int> output = new Dictionary<Placement, int>();
            if (Horizontal)
                output.Add(Placement.HORIZONTAL, 0);
            if (Vertical)
                output.Add(Placement.VERTICAL, 0);
            if (DiagonalUp)
                output.Add(Placement.DIAGONAL_UP, 0);
            if (DiagonalDown)
                output.Add(Placement.DIAGONAL_DOWN, 0);

            return output;
        }
    }

    public enum Placement
    {
        HORIZONTAL = 0,
        VERTICAL = 1,
        DIAGONAL_UP = 2,
        DIAGONAL_DOWN = 3
    }

    #endregion
}