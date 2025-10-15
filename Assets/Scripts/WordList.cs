using System.Collections.Generic;
using System.Linq;
using EditorTools;
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
    
    public string[] GetWords(int count, int maxLength)
    {
        List<string> output = _words.Where((x) => x.Length <= maxLength).ToList();
        output.OrderBy(x => Random.value);

        if (output.Count < count)
            this.Log($"There are fewer words than {count}, equal to or shorter than {maxLength} chars. Returned word count: {output.Count}");
        else
            output = output.GetRange(0, count);
        
        output.Sort((x, y) => x.Length > y.Length ? -1 : 1);

            
        return output.ToArray();
    }
}
