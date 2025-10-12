using System.Collections.Generic;
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
    [SerializeField] private bool _allowDiagonalWords = true;
    [SerializeField] private bool _allowBackwardWords = true;
    [SerializeField] private int _maxPlacementAttempts = 100;

    [Header("Debug")]
    [SerializeField] private List<string> _remainingWords;
    [SerializeField] private List<Letter> _letterPool;

    void OnValidate()
    {
        if (_letterContainer && _letterPrefab && _letterPool.Count < _boardSize.x * _boardSize.y)
        {
            int targetCount = (int)(_boardSize.x * _boardSize.y);
            int currentCount = _letterPool.Count;
            int toAdd = targetCount - currentCount;

            for (int i = 0; i < toAdd; i++)
            {
                GenerateLetter();
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_generateOnStart)
            GenerateBoard();
    }

    public void GenerateBoard() => GenerateBoard(_wordList, _boardSize, _allowDiagonalWords, _allowBackwardWords);
    public void GenerateBoard(WordList words, Vector2Int boardSize, bool allowDiagonals, bool allowBackwards)
    {
        int index = 0;
        for (int x = 0; x < boardSize.x; x++)
        {
            for (int y = 0; y < boardSize.y; y++)
            {
                Letter letter;
                if (index >= _letterPool.Count)
                    letter = GenerateLetter();
                else
                    letter = _letterPool[index];

                letter.gameObject.SetActive(true);
                letter.String = ((char)Random.Range(65, 91)).ToString();
                SetAnchors(letter.transform as RectTransform, x, y, boardSize.x, boardSize.y, 0f);

                index++;
            }
        }
    }

    // Utility
    public static void SetAnchors(RectTransform rect, int x, int y, int xCount, int yCount, float padding)
    {
        rect.anchorMin = new Vector2(x * (1f / xCount), y * (1f / yCount)) + (Vector2.one * padding);
        rect.anchorMax = new Vector2((x + 1) * (1f / xCount), (y + 1) * (1f / yCount)) - (Vector2.one * padding);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    private Letter GenerateLetter()
    {
        Letter letterInstance = Instantiate(_letterPrefab, _letterContainer);
        letterInstance.gameObject.SetActive(false);
        _letterPool.Add(letterInstance);
        return letterInstance;
    }
}
