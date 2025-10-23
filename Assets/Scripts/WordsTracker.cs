using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Word = GameManager.Word;

public class WordsTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager _manager;
    [SerializeField] private TMP_Text _wordsText;
    [SerializeField] private Color _foundWordColour = Color.grey;

    void OnEnable()
    {
        _manager.OnBoardGenerated += UpdateWordsBox;
        _manager.OnWordDiscovered += UpdateWordsBox;
    }
    void OnDisable()
    {
        _manager.OnBoardGenerated -= UpdateWordsBox;
        _manager.OnWordDiscovered -= UpdateWordsBox;
    }

    private void UpdateWordsBox(string _) => UpdateWordsBox(_manager.CurrentWords);
    private void UpdateWordsBox(Word[] words)
    {
        if (words.Length == 0)
        {
            _wordsText.text = string.Empty;
            return;
        }

        words = new List<Word>(words).OrderBy((x) => !x.Found).ToArray();

        string output = "";
        bool foundFinished = false;

        if (words[0].Found)
        {
            output += $"<color=#{ColorUtility.ToHtmlStringRGBA(_foundWordColour)}><size=80%>";
            foundFinished = true;
        }

        foreach (Word word in words)
        {
            if (!word.Found && foundFinished)
            {
                output += "</size></color>\n";
                foundFinished = false;
            }
            output += word + " ";
        }

        _wordsText.text = output;
    }
}
