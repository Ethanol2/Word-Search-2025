using TMPro;
using UnityEngine;

public class Letter : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private string _letter = "A";
    [SerializeField] private Vector2Int _coordinates = new Vector2Int();

    public char Char { get => _letter.Length > 0 ? _letter[0] : '\0'; set => String = value.ToString(); }
    public string String { get => _letter; set { _letter = value; name = value; if (_text) _text.text = _letter; } }
    public RectTransform RectTransform => this.transform as RectTransform;
    public Vector2Int Coordinates => _coordinates;

    void OnValidate()
    {
        _text = GetComponentInChildren<TMP_Text>();
        if (_text)
            _text.text = _letter;
    }
    public void SetCoordinates(int x, int y) => _coordinates.Set(x, y);
}
