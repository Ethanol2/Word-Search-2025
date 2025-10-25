using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text[] _timerTextObjs = new TMP_Text[0];

    [Space]
    [SerializeField] private uint _timer = 0;
    [SerializeField] private bool _paused = false;

    public bool Paused { get => _paused; set { _paused = value; UpdateText(); } }

    private float _accumulator = 0f;

    void OnEnable()
    {

    }
    void OnDisable()
    {

    }

    void Update()
    {
        if (_paused) return;

        _accumulator += Time.deltaTime;
        if (_accumulator >= 1f)
        {
            _accumulator = 0f;
            _timer++;
            UpdateText();
        }
    }
    private void UpdateText()
    {
        int minutes = Mathf.FloorToInt(_timer / 60);
        int seconds = Mathf.FloorToInt(_timer % 60);

        string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (_paused)
        {
            foreach (TMP_Text textObj in _timerTextObjs)
                textObj.text = "PAUSED " + timeString;
        }
        else
        {
            foreach (TMP_Text textObj in _timerTextObjs)
                textObj.text = timeString;
        }
    }

    public void Reset()
    {
        _timer = 0;
        _accumulator = 0f;
        Paused = false;
        UpdateText();
    }
    public void Pause() => Paused = true;
    public void Unpause() => Paused = false;

}
