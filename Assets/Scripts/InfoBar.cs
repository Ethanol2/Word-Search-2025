using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoBar : MonoBehaviour
{
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private TMP_Text _batteryText;
    [SerializeField] private Image _batteryLevel;

    void Update()
    {
        _timeText.text = System.DateTime.Now.ToString("h:mm tt");
    }
    void FixedUpdate()
    {
        _batteryText.text = (SystemInfo.batteryLevel * 100f) + "%";
        _batteryLevel.fillAmount = SystemInfo.batteryLevel;
    }
}
