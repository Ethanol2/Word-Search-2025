using EditorTools;
using TMPro;
using UnityEngine;

public class Letter : MonoBehaviour
{
    private const string HIGHLIGHT_ANIM = "highlight";

    [SerializeField] private TMP_Text _text;
    [SerializeField] private string _letter = "A";
    [SerializeField] private Vector2Int _coordinates = new Vector2Int();

    [Space]
    [SerializeField] private Animation _animation;
    [SerializeField] private AnimationClip _highlightAnim;

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
    void Start()
    {
        _animation.AddClip(_highlightAnim, HIGHLIGHT_ANIM);
    }

    public void SetCoordinates(int x, int y) => _coordinates.Set(x, y);
    public void PlayHighlightAnimation(float delay = 0f)
    {
        if (delay > 0f)
        {
            Invoke(nameof(PlayHighlightAnimation), delay);
            return;
        }

        _animation.Play(HIGHLIGHT_ANIM);
    }
    
}
