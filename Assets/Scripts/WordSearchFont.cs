using EditorTools;
using UnityEngine;

[CreateAssetMenu(fileName = "WordSearchFont", menuName = "Scriptable Objects/WordSearchFont")]
public class WordSearchFont : ScriptableObject
{
    [SerializeField] private Sprite[] _sprites;

    public Sprite GetLetter(char letter)
    {
        int index = (int)letter - 65;

// #if UNITY_EDITOR
//         if (!Application.isPlaying)
//             this.Log($"Char: {letter}, Index: {index}");
//         #endif

        if (index >= 0 && index < _sprites.Length)
            return _sprites[index];
        return null;
    }
}
