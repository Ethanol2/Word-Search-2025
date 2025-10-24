using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TempWordListSelector : MonoBehaviour
{
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private Button _buttonPrefab;

    [Space]
    [SerializeField] private WordList[] _wordLists;

    [Space]
    public UnityEvent<WordList> OnSelected;

    void Start()
    {
        for (int i = 0; i < _wordLists.Length; i++)
        {
            Button button = GameObject.Instantiate(_buttonPrefab, _buttonParent);
            int index = i;
            button.onClick.AddListener(() => OnButtonClick(index));
            button.GetComponentInChildren<TMP_Text>().text = _wordLists[i].Title;
        }
    }
    private void OnButtonClick(int index)
    {
        OnSelected.Invoke(_wordLists[index]);
    }
}
