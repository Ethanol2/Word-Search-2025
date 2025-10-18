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
    [SerializeField] private List<Image> _selections = new List<Image>();

    [Space]
    [SerializeField] private List<Letter> _selectedLetters = new List<Letter>();
    [SerializeField] private string _selectedLettersString = string.Empty;

    [Space]
    [SerializeField] private Transform _debugMarker;

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

    private void OnBoardGenerated()
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

        if (absDirX < _diagonalVector.x / 2f)
        {
            absDirX = 0f;
            direction.y = direction.y < 0f ? -1f : 1f;

            letterDist = _manager.CurrentLetterSize.y;
        }
        else if (absDirX > _diagonalVector.x / 2f && absDirX < ((1f - _diagonalVector.x) / 2f) + _diagonalVector.x)
        {
            absDirX = _diagonalVector.x;
            direction.y = direction.y < 0f ? _diagonalVector.y : -_diagonalVector.y;

            letterDist = _manager.CurrentLetterSize.magnitude;
        }
        else
        {
            absDirX = 1f;
            direction.y = 0f;

            letterDist = _manager.CurrentLetterSize.x;
        }

        direction.x = direction.x < 0f ? -absDirX : absDirX;

        _activeSelection.right = direction;
        _endPosition = _startLetter.RectTransform.localPosition + (distance * direction);

        Vector3 size = new Vector2(distance + _manager.CurrentLetterSize.x, _activeSelection.sizeDelta.y);
        size.x = Mathf.Clamp(size.x, _manager.CurrentLetterSize.x, float.MaxValue);
        size.y = Mathf.Clamp(size.y, _manager.CurrentLetterSize.y, float.MaxValue);

        _activeSelection.sizeDelta = size;

        _activeSelection.localPosition = _startLetter.RectTransform.localPosition + ((_endPosition - _startLetter.RectTransform.localPosition) / 2f);


        // Get Selected Letters
        _selectedLetters.Clear();
        _selectedLetters.Add(_startLetter);
        _selectedLettersString = _startLetter.String;

        Vector2Int intDirection = Vector2Int.RoundToInt(direction);
        intDirection.y = -intDirection.y;
        
        Vector2Int coord;
        for (int i = 1; i < distance / letterDist; i++)
        {
            coord = _startLetter.Coordinates + (intDirection * i);

            if (coord.x > _manager.CurrentGrid.GetLength(0) || coord.x < 0 || coord.y > _manager.CurrentGrid.GetLength(1) || coord.y < 0)
                break;

            _selectedLetters.Add(_manager.CurrentGrid[coord.x, coord.y]);
            _selectedLettersString += _selectedLetters.Last().String;
        }
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
        _manager.CheckInputSelection(_selectedLettersString, _startLetter.Coordinates);

        if (_activeSelection)
            Destroy(_activeSelection.gameObject);
    }

    private Vector3 ConvertPosition(Vector2 screenPosition) => _lettersParent.InverseTransformPoint(screenPosition);
}
