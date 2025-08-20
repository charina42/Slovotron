using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LetterFrequencyEditorAnalyzer : EditorWindow
{
    private TextAsset dictionaryJson;
    private string savePath = "Assets/letter_frequency_analysis.txt";

    [MenuItem("Tools/Analyze Letter Frequency")]
    public static void ShowWindow()
    {
        GetWindow<LetterFrequencyEditorAnalyzer>("Letter Frequency Analyzer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Letter Frequency Analysis Tool", EditorStyles.boldLabel);

        dictionaryJson = (TextAsset)EditorGUILayout.ObjectField(
            "Dictionary JSON", 
            dictionaryJson, 
            typeof(TextAsset), 
            false
        );

        if (GUILayout.Button("Analyze and Save to File"))
        {
            if (dictionaryJson == null)
            {
                Debug.LogError("Dictionary JSON not assigned!");
                return;
            }

            AnalyzeAndSave();
        }
    }

    private void AnalyzeAndSave()
    {
        var data = JsonUtility.FromJson<DictionaryData>(dictionaryJson.text);
        var frequency = AnalyzeLetterFrequency(data.wordLists);
        SaveResultsToFile(frequency);
        AssetDatabase.Refresh();
        Debug.Log($"Analysis saved to: {savePath}");
    }

    private Dictionary<char, int> AnalyzeLetterFrequency(WordList[] wordLists)
    {
        Dictionary<char, int> frequency = new Dictionary<char, int>();

        foreach (var wordList in wordLists)
        {
            foreach (var word in wordList.words)
            {
                if (string.IsNullOrEmpty(word))
                    continue;

                foreach (char c in word.ToLower())
                {
                    if (char.IsLetter(c)) // Игнорируем цифры, символы и т.д.
                    {
                        if (frequency.ContainsKey(c))
                            frequency[c]++;
                        else
                            frequency[c] = 1;
                    }
                }
            }
        }

        return frequency;
    }

    private void SaveResultsToFile(Dictionary<char, int> frequency)
    {
        using (StreamWriter writer = new StreamWriter(savePath))
        {
            writer.WriteLine("=== Letter Frequency Analysis ===");
            writer.WriteLine($"Total letters: {frequency.Values.Sum()}\n");

            // Сортируем по убыванию частотности
            var sorted = frequency.OrderByDescending(pair => pair.Value);

            foreach (var pair in sorted)
            {
                float percentage = (float)pair.Value / frequency.Values.Sum() * 100;
                writer.WriteLine($"{pair.Key}: {pair.Value} ({percentage:F2}%)");
            }
        }
    }

    [System.Serializable]
    private class DictionaryData
    {
        public WordList[] wordLists;
    }

    [System.Serializable]
    private class WordList
    {
        public int length;
        public string[] words;
    }
}
