using System.Collections.Generic;
using EditorTools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoardInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private BoardGenerator _board;
    [SerializeField] private RectTransform _selectionImagePrefab;
    [SerializeField] private RectTransform _lettersParent;

    [Header("Debug")]
    [SerializeField] private RectTransform _activeSelection;
    [SerializeField] private RectTransform _startPosition;
    [SerializeField] private Vector2Int _startCoords;
    [SerializeField] private Vector3 _endPosition;
    [SerializeField] Vector3 _diagonalVector;
    [SerializeField] private List<Image> _selections = new List<Image>();

    [Space]
    [SerializeField] private Transform _debugMarker;

    void OnEnable()
    {
        _board.OnBoardGenerated += OnBoardGenerated;
    }
    void OnDisable()
    {
        _board.OnBoardGenerated -= OnBoardGenerated;

        if (_activeSelection)
            Destroy(_activeSelection.gameObject);
    }

    private void OnBoardGenerated()
    {
        _diagonalVector = (_board.CurrentGrid[1, 1].transform as RectTransform).localPosition - (_board.CurrentGrid[0, 0].transform as RectTransform).localPosition;
        _diagonalVector = _diagonalVector.normalized;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_activeSelection) return;

        _endPosition = ConvertPosition(eventData.position);

        Vector3 direction = (_endPosition - _startPosition.localPosition).normalized;
        float distance = Vector3.Distance(_startPosition.localPosition, _endPosition);

        float absDirX = Mathf.Abs(direction.x);

        if (absDirX < _diagonalVector.x / 2f)
        {
            absDirX = 0f;
            direction.y = direction.y < 0f ? -1f : 1f;
        }
        else if (absDirX > _diagonalVector.x / 2f && absDirX < ((1f - _diagonalVector.x) / 2f) + _diagonalVector.x)
        {
            absDirX = _diagonalVector.x;
            direction.y = direction.y < 0f ? _diagonalVector.y : -_diagonalVector.y;
        }
        else
        {
            absDirX = 1f;
            direction.y = 0f;
        }

        direction.x = direction.x < 0f ? -absDirX : absDirX;

        _activeSelection.right = direction;
        _endPosition = _startPosition.localPosition + (distance * direction);

        Vector3 size = new Vector2(distance + _board.CurrentLetterSize.x, _activeSelection.sizeDelta.y);
        size.x = Mathf.Clamp(size.x, _board.CurrentLetterSize.x, float.MaxValue);
        size.y = Mathf.Clamp(size.y, _board.CurrentLetterSize.y, float.MaxValue);

        _activeSelection.sizeDelta = size;

        _activeSelection.localPosition = _startPosition.localPosition + ((_endPosition - _startPosition.localPosition) / 2f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector3 localPos = ConvertPosition(eventData.position);

        if (_debugMarker)
        _debugMarker.localPosition = localPos;
        
        if (_board.GetLetterAtPosition(localPos, out Letter letter, out Vector2Int coords))
        {
            _startPosition = letter.transform as RectTransform;
            _startCoords = coords;
            _activeSelection = GameObject.Instantiate(_selectionImagePrefab, this.transform);
            _activeSelection.sizeDelta = _startPosition.rect.size;
            
            OnDrag(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_activeSelection)
            Destroy(_activeSelection.gameObject);
    }

    private Vector3 ConvertPosition(Vector2 screenPosition) => _lettersParent.InverseTransformPoint(screenPosition);
}
