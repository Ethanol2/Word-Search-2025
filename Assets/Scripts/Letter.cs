using TMPro;
using UnityEngine;

public class Letter : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private string _letter = "A";

    public char Char { get => _letter.Length > 0 ? _letter[0] : '\0'; set => String = value.ToString(); }
    public string String { get => _letter; set { _letter = value; name = value; if (_text) _text.text = _letter; } }
    void OnValidate()
    {
        _text = GetComponentInChildren<TMP_Text>();
        if (_text)
            _text.text = _letter;
    }
}
