using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WordList", menuName = "Scriptable Objects/WordList")]
public class WordList : ScriptableObject
{
    [SerializeField] private string _name = "New Word List";
    [SerializeField, TextArea] private string _wordsQuickAdd = string.Empty;
    [SerializeField] private string[] _words;

    public string[] Words => _words;

    void OnValidate()
    {
        List<string> wordsList = new List<string>(_words);

        if (_wordsQuickAdd != string.Empty)
        {
            wordsList.AddRange(_wordsQuickAdd.Split(',', System.StringSplitOptions.RemoveEmptyEntries));

            for (int i = 0; i < wordsList.Count; i++) wordsList[i] = wordsList[i].Trim();

            _wordsQuickAdd = string.Empty;
        }

        wordsList.Sort((x, y) => x.Length > y.Length ? -1 : 1);
        _words = wordsList.ToArray();
    }
    
    public string[] GetWords(int count, bool random = true)
    {
        if (random)
        {
            List<string> selectedWords = new List<string>();
            List<int> selectedIndexes = new List<int>();

            while (selectedWords.Count < count)
            {
                int index;
                do
                    index = Random.Range(0, _words.Length);
                while (selectedIndexes.Contains(index));

                selectedWords.Add(_words[index]);
                selectedIndexes.Add(index);
            }

            return selectedWords.ToArray();
        }
        
        return _words[..count];
    }
}
