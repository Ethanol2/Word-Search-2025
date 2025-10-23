using System.Collections.Generic;
using System.Linq;
using EditorTools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private GameManager _manager;
    [SerializeField] private TMP_Text _selectionPreview;

    [Space]
    [SerializeField] private RectTransform _selectionImagePrefab;

    [Header("Debug")]
    [SerializeField] private Selection _activeSelection;
    [SerializeField] private List<Selection> _selections = new List<Selection>();

    [Space]
    [SerializeField] private Transform _debugMarker;

    private Rect _selectionArea;
    private Vector3 _diagonalVector;

    void Start()
    {
        _selectionArea = _manager.LettersPanel.rect;
        _selectionArea.xMin *= 0.95f;
        _selectionArea.xMax *= 0.95f;
        _selectionArea.yMin *= 0.95f;
        _selectionArea.yMax *= 0.95f;
    }
    void OnEnable()
    {
        _manager.OnBoardGenerated += OnBoardGenerated;
        _manager.OnWindowResized += OnWindowResize;

        _selectionPreview.text = string.Empty;
    }
    void OnDisable()
    {
        _manager.OnBoardGenerated -= OnBoardGenerated;
        _manager.OnWindowResized -= OnWindowResize;

        if (_activeSelection && _activeSelection.RectTransform)
            Destroy(_activeSelection.GameObject);
        
        _selectionPreview.text = string.Empty;
    }

    private void OnWindowResize()
    {
        _diagonalVector = (_manager.CurrentGrid[1, 1].transform.localPosition - _manager.CurrentGrid[0, 0].transform.localPosition).normalized;

        foreach (Selection selection in _selections)
            ResizeSelection(selection);
    }
    private void OnBoardGenerated(GameManager.Word[] _)
    {
        foreach (Selection selection in _selections)
        {
            Destroy(selection.GameObject);
        }
        _selections.Clear();

        OnWindowResize();
    }
    private (float, Vector3) ResizeSelection(Selection selection)
    {
        Vector3 direction = selection.Direction();
        float distance = selection.Length();

        float absDirX = Mathf.Abs(direction.x);

        float selectionWidth, letterDist;

        if (selection.Active)
        {
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

            float offset = distance % letterDist;
            distance = offset < letterDist / 2f ? distance - offset : distance + (letterDist - offset);
            selection.EndPosition = selection.StartPosition + (distance * direction);
        }
        else
        {
            if (direction.x == 0)
                selectionWidth = _manager.CurrentLetterSize.x;
            else if (direction.y == 0)
                selectionWidth = _manager.CurrentLetterSize.y;
            else
                selectionWidth = Mathf.Lerp(_manager.CurrentLetterSize.x, _manager.CurrentLetterSize.y, 0.5f);
        }
        
        selection.RectTransform.right = direction;

        Vector3 size = new Vector2(distance + _manager.CurrentLetterSize.x, selectionWidth);
        size.x = Mathf.Clamp(size.x, _manager.CurrentLetterSize.x, float.MaxValue);
        size.y = Mathf.Clamp(size.y, _manager.CurrentLetterSize.y, float.MaxValue);

        selection.RectTransform.sizeDelta = size;

        selection.RectTransform.localPosition = selection.StartPosition + ((selection.EndPosition - selection.StartPosition) / 2f);

        return (distance, direction);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_activeSelection) return;

        _activeSelection.EndPosition = ConvertPosition(eventData.position);

        float distance;
        Vector3 direction;

        (distance, direction) = ResizeSelection(_activeSelection);

        // Get Selected Letters
        List<Letter> newSelection = new List<Letter>() { _activeSelection.StartLetter };
        _activeSelection.String = _activeSelection.StartLetter.String;

        Vector2Int intDirection = Vector2Int.RoundToInt(direction);
        intDirection.y = -intDirection.y;

        int letterCount;
        if (intDirection.x != 0 && intDirection.y != 0)
            letterCount = Mathf.RoundToInt(distance / _manager.CurrentLetterSize.magnitude) + 1;
        else if (intDirection.x == 0)
            letterCount = Mathf.RoundToInt(distance / _manager.CurrentLetterSize.y) + 1;
        else
            letterCount = Mathf.RoundToInt(distance / _manager.CurrentLetterSize.x) + 1;

        Vector2Int coord;
        for (int i = 1; i < letterCount; i++)
        {
            coord = _activeSelection.StartLetter.Coordinates + (intDirection * i);

            if (coord.x >= _manager.CurrentGrid.GetLength(0) || coord.x < 0 || coord.y >= _manager.CurrentGrid.GetLength(1) || coord.y < 0)
                break;

            Letter letter = _manager.CurrentGrid[coord.x, coord.y];
            newSelection.Add(letter);
            _activeSelection.String += letter.String;

            if (!_activeSelection.Letters.Contains(letter))
                letter.PlayHighlightAnimation();
        }
        _activeSelection.Letters = newSelection.ToArray();
        _selectionPreview.text = _activeSelection.String;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector3 localPos = ConvertPosition(eventData.position);

        if (_debugMarker)
            _debugMarker.localPosition = localPos;

        if (_manager.GetLetterAtPosition(localPos, out Letter letter))
        {
            _activeSelection = new Selection()
            {
                StartLetter = letter,
                RectTransform = GameObject.Instantiate(_selectionImagePrefab, this.transform)
            };

            Vector2 size = letter.RectTransform.rect.size;
            if (size.x < size.y)
                size.y = size.x;
            else
                size.x = size.y;

            _activeSelection.RectTransform.sizeDelta = size;

            OnDrag(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _selectionPreview.text = string.Empty;

        if (_activeSelection)
        {
            if (_manager.CheckInputSelection(_activeSelection.String, _activeSelection.StartLetter.Coordinates))
            {
                _activeSelection.GameObject.GetComponent<Image>().color = _manager.CorrectSelectionColour;
                _activeSelection.SetActive(false);

                _selections.Add(_activeSelection);
                _activeSelection = null;
            }
            else
            {
                Destroy(_activeSelection.GameObject);
            }
        }
    }

    private Vector3 ConvertPosition(Vector2 screenPosition) =>
        GetPointOrClosestPerimeterPoint(_selectionArea, _manager.LettersPanel.InverseTransformPoint(screenPosition));

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

    [System.Serializable]
    public class Selection
    {
        public string String;
        public Letter[] Letters;
        public RectTransform RectTransform;
        public Letter StartLetter;
        public Vector3 EndPosition;

        private bool active = true;
        public bool Active => active;

        public void SetActive(bool value) => active = value;

        public Vector3 StartPosition => StartLetter.transform.localPosition;
        public GameObject GameObject => RectTransform.gameObject;
        public Vector3 Direction()
        {
            if (active)
                return (EndPosition - StartLetter.RectTransform.localPosition).normalized;
            else
                return (Letters.Last().transform.localPosition - StartLetter.transform.localPosition).normalized;
        }
        public float Length()
        {
            if (active)
                return Vector3.Distance(StartLetter.transform.localPosition, EndPosition);
            else
                return Vector3.Distance(StartLetter.transform.localPosition, Letters.Last().transform.localPosition);
        }

        public static implicit operator bool(Selection selection) => selection != null;
    }
}
