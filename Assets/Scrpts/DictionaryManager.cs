using System.Collections.Generic;
using UnityEngine;

public class DictionaryManager
{
    // [SerializeField] private string wordsJson;
    private readonly Dictionary<int, List<string>> _wordsByLengthDictionary = new Dictionary<int, List<string>>();

    public void Initialize(string dictionaryJson)
    {
        LoadDictionary(dictionaryJson);
    }

    private void LoadDictionary(string dictionaryJson)
    {
        TextAsset dictFile = Resources.Load<TextAsset>(dictionaryJson);
        var data = JsonUtility.FromJson<SerializableDictionary>(dictFile.text);
        
        foreach (var wordList in data.wordLists)
        {
            _wordsByLengthDictionary[wordList.length] = wordList.words;
        }
    }

    public bool CheckRegularWord(string word)
    {
        return _wordsByLengthDictionary.ContainsKey(word.Length) && 
               _wordsByLengthDictionary[word.Length].Contains(word);
    }

    public bool CheckWordWithWildcards(string pattern, List<LetterData> letterList)
    {
        int length = pattern.Length;
        if (!_wordsByLengthDictionary.ContainsKey(length))
            return false;
        
        foreach (string word in _wordsByLengthDictionary[length])
        {
            if (MatchesWildcardPattern(word, pattern))
            {
                return true;
            }
        }
        return false;
    }

    private bool MatchesWildcardPattern(string word, string pattern)
    {
        for (int i = 0; i < word.Length; i++)
        {
            if (pattern[i] != '*' && pattern[i] != word[i])
                return false;
        }
        return true;
    }
}

[System.Serializable]
public class SerializableDictionary
{
    public List<WordList> wordLists = new List<WordList>();

    public SerializableDictionary(Dictionary<int, List<string>> dict)
    {
        foreach (var kvp in dict)
        {
            wordLists.Add(new WordList(kvp.Key, kvp.Value));
        }
    }
}

[System.Serializable]
public class WordList
{
    public int length;
    public List<string> words;

    public WordList(int length, List<string> words)
    {
        this.length = length;
        this.words = words;
    }
}