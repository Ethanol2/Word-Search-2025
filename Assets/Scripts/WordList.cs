using UnityEngine;

[CreateAssetMenu(fileName = "WordList", menuName = "Scriptable Objects/WordList")]
public class WordList : ScriptableObject
{
    [SerializeField] private string[] _words;

    public string[] Words => _words;
}
