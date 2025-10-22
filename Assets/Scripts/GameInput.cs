using System.Collections.Generic;
using System.Linq;
using EditorTools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private GameManager _manager;
    [SerializeField] private RectTransform _selectionImagePrefab;
    [SerializeField] private RectTransform _lettersParent;

    [Header("Debug")]
    [SerializeField] private RectTransform _activeSelection;
    [SerializeField] private Letter _startLetter;
    [SerializeField] private Vector3 _endPosition;
    [SerializeField] Vector3 _diagonalVector;
    [SerializeField] private List<RectTransform> _selections = new List<RectTransform>();

    [Space]
    [SerializeField] private List<Letter> _selectedLetters = new List<Letter>();
    [SerializeField] private string _selectedLettersString = string.Empty;

    [Space]
    [SerializeField] private Transform _debugMarker;

    private Rect _selectionArea;

    void Start()
    {
        _selectionArea = _lettersParent.rect;
        Vector2 position = _selectionArea.position;
        _selectionArea.xMin *= 0.95f;
        _selectionArea.xMax *= 0.95f;
        _selectionArea.yMin *= 0.95f;
        _selectionArea.yMax *= 0.95f;
    }
    void OnEnable()
    {
        _manager.OnBoardGenerated += OnBoardGenerated;
    }
    void OnDisable()
    {
        _manager.OnBoardGenerated -= OnBoardGenerated;

        if (_activeSelection)
            Destroy(_activeSelection.gameObject);
    }

    private void OnBoardGenerated(GameManager.Word[] _)
    {
        _diagonalVector = (_manager.CurrentGrid[1, 1].transform as RectTransform).localPosition - (_manager.CurrentGrid[0, 0].transform as RectTransform).localPosition;
        _diagonalVector = _diagonalVector.normalized;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_activeSelection) return;

        _endPosition = ConvertPosition(eventData.position);

        Vector3 direction = (_endPosition - _startLetter.RectTransform.localPosition).normalized;
        float distance = Vector3.Distance(_startLetter.RectTransform.localPosition, _endPosition);

        float absDirX = Mathf.Abs(direction.x);
        float letterDist;
        float selectionWidth;

        if (absDirX < _diagonalVector.x / 2f)
        {
            absDirX = 0f;
            direction.y = direction.y < 0f ? -1f : 1f;

            selectionWidth = _manager.CurrentLetterSize.x;
            letterDist = _manager.CurrentLetterSize.y;
        }
        else if (absDirX > _diagonalVector.x / 2f && absDirX < ((1f - _diagonalVector.x) / 2f) + _diagonalVector.x)
        {
            absDirX = _diagonalVector.x;
            direction.y = direction.y < 0f ? _diagonalVector.y : -_diagonalVector.y;

            selectionWidth = Mathf.Lerp(_manager.CurrentLetterSize.x, _manager.CurrentLetterSize.y, 0.5f);
            letterDist = _manager.CurrentLetterSize.magnitude;
        }
        else
        {
            absDirX = 1f;
            direction.y = 0f;

            selectionWidth = _manager.CurrentLetterSize.y;
            letterDist = _manager.CurrentLetterSize.x;
        }

        direction.x = direction.x < 0f ? -absDirX : absDirX;
        _activeSelection.right = direction;

        float offset = distance % letterDist;
        distance = offset < letterDist / 2f ? distance - offset : distance + (letterDist - offset);
        _endPosition = _startLetter.RectTransform.localPosition + (distance * direction);

        Vector3 size = new Vector2(distance + _manager.CurrentLetterSize.x, selectionWidth);
        size.x = Mathf.Clamp(size.x, _manager.CurrentLetterSize.x, float.MaxValue);
        size.y = Mathf.Clamp(size.y, _manager.CurrentLetterSize.y, float.MaxValue);

        _activeSelection.sizeDelta = size;

        _activeSelection.localPosition = _startLetter.RectTransform.localPosition + ((_endPosition - _startLetter.RectTransform.localPosition) / 2f);


        // Get Selected Letters
        List<Letter> newSelection = new List<Letter>() { _startLetter };
        _selectedLettersString = _startLetter.String;

        Vector2Int intDirection = Vector2Int.RoundToInt(direction);
        intDirection.y = -intDirection.y;

        Vector2Int coord;
        for (int i = 1; i < (distance / letterDist) + 1; i++)
        {
            coord = _startLetter.Coordinates + (intDirection * i);

            if (coord.x >= _manager.CurrentGrid.GetLength(0) || coord.x < 0 || coord.y >= _manager.CurrentGrid.GetLength(1) || coord.y < 0)
                break;

            Letter letter = _manager.CurrentGrid[coord.x, coord.y];
            newSelection.Add(letter);
            _selectedLettersString += letter.String;

            if (!_selectedLetters.Contains(letter))
                letter.PlayHighlightAnimation();
        }
        _selectedLetters = newSelection;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector3 localPos = ConvertPosition(eventData.position);

        if (_debugMarker)
            _debugMarker.localPosition = localPos;

        if (_manager.GetLetterAtPosition(localPos, out _startLetter))
        {
            _activeSelection = GameObject.Instantiate(_selectionImagePrefab, this.transform);

            Vector2 size = _startLetter.RectTransform.rect.size;
            if (size.x < size.y)
                size.y = size.x;
            else
                size.x = size.y;

            _activeSelection.sizeDelta = size;

            OnDrag(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_activeSelection)
        {
            if (_manager.CheckInputSelection(_selectedLettersString, _startLetter.Coordinates))
            {
                _selections.Add(_activeSelection);
                _activeSelection.gameObject.GetComponent<Image>().color = _manager.CorrectSelectionColour;
                _activeSelection = null;
            }
            else
            {
                Destroy(_activeSelection.gameObject);
            }
        }
    }

    private Vector3 ConvertPosition(Vector2 screenPosition) =>
        GetPointOrClosestPerimeterPoint(_selectionArea, _lettersParent.InverseTransformPoint(screenPosition));

    // Gemini Method
    /// <summary>
    /// Returns the input point if it's inside the Rect, otherwise returns the closest point on the Rect's perimeter.
    /// </summary>
    /// <param name="rect">The Rect to check against.</param>
    /// <param name="point">The point to evaluate.</param>
    /// <returns>The point itself if inside the Rect, or the closest point on the Rect's perimeter.</returns>
    public static Vector2 GetPointOrClosestPerimeterPoint(Rect rect, Vector2 point)
    {
        if (rect.Contains(point))
        {
            return point;
        }

        // If the point is outside, find the closest point on the perimeter.
        float closestX = Mathf.Clamp(point.x, rect.xMin, rect.xMax);
        float closestY = Mathf.Clamp(point.y, rect.yMin, rect.yMax);

        // Determine which edge is closest if the point is outside the clamped region
        // This logic handles points directly on the corners or edges correctly.
        if (point.x < rect.xMin) closestX = rect.xMin;
        else if (point.x > rect.xMax) closestX = rect.xMax;

        if (point.y < rect.yMin) closestY = rect.yMin;
        else if (point.y > rect.yMax) closestY = rect.yMax;

        // If the point is outside the rectangle's bounds,
        // one of the clamped coordinates will be on an edge.
        // If it's a corner, both will be on an edge.
        // If it's directly outside an edge, one will be clamped to that edge.
        return new Vector2(closestX, closestY);
    }
}
