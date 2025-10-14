using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "WordList", menuName = "Scriptable Objects/WordList")]
public class WordList : ScriptableObject
{
    [SerializeField] private string _name = "New Word List";

    [Tooltip("Paste the words, don't type them. Words must be seperated by")]
    [SerializeField, TextArea] private string _wordsQuickAdd = string.Empty;
    
    [SerializeField] private List<string> _words;

    public List<string> Words => _words;

    void OnValidate()
    {
        List<string> wordsList = new List<string>(_words);

        if (_wordsQuickAdd != string.Empty)
        {
            wordsList.AddRange(_wordsQuickAdd.Split(',', System.StringSplitOptions.RemoveEmptyEntries));

            for (int i = 0; i < wordsList.Count; i++) wordsList[i] = wordsList[i].Trim().ToUpper();

            _wordsQuickAdd = string.Empty;
        }

        _words = wordsList.Distinct().ToList();
    }
    
    public string[] GetWords(int count, bool random = true)
    {
        List<string> selectedWords = new List<string>();

        if (random)
        {
            List<int> selectedIndexes = new List<int>();

            while (selectedWords.Count < count)
            {
                int index;
                do
                    index = Random.Range(0, _words.Count);
                while (selectedIndexes.Contains(index));

                selectedWords.Add(_words[index]);
                selectedIndexes.Add(index);
            }
        }
        else
        {
            selectedWords = _words.GetRange(0, count);
        }

        selectedWords.Sort((x, y) => x.Length > y.Length ? -1 : 1);
        return selectedWords.ToArray();
    }
}
