using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DuplicateLettersAnalyzer : EditorWindow
{
    private TextAsset dictionaryJson;
    private string savePath = "Assets/duplicate_letters_analysis.txt";
    private int minDuplicateCount = 2;

    [MenuItem("Tools/Analyze Duplicate Letters")]
    public static void ShowWindow()
    {
        GetWindow<DuplicateLettersAnalyzer>("Duplicate Letters Analyzer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Duplicate Letters Analysis Tool", EditorStyles.boldLabel);

        dictionaryJson = (TextAsset)EditorGUILayout.ObjectField(
            "Dictionary JSON", 
            dictionaryJson, 
            typeof(TextAsset), 
            false
        );

        minDuplicateCount = EditorGUILayout.IntField("Min duplicate count", minDuplicateCount);
        minDuplicateCount = Mathf.Max(2, minDuplicateCount); // Не меньше 2

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
        var results = AnalyzeDuplicateLetters(data.wordLists, minDuplicateCount);
        SaveResultsToFile(results);
        AssetDatabase.Refresh();
        Debug.Log($"Analysis saved to: {savePath}");
    }

    private DuplicateAnalysisResult AnalyzeDuplicateLetters(WordList[] wordLists, int minCount)
    {
        var result = new DuplicateAnalysisResult();
        
        foreach (var wordList in wordLists)
        {
            foreach (var word in wordList.words)
            {
                if (string.IsNullOrEmpty(word))
                    continue;

                var wordLower = word.ToLower();
                AnalyzeWord(wordLower, minCount, result);
            }
        }

        // Сортируем результаты
        result.wordsWithDuplicates = result.wordsWithDuplicates
            .OrderByDescending(w => w.duplicateCount)
            .ThenBy(w => w.word)
            .ToList();

        result.duplicateStatistics = result.duplicateStatistics
            .OrderByDescending(pair => pair.Value)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        return result;
    }

    private void AnalyzeWord(string word, int minCount, DuplicateAnalysisResult result)
    {
        var letterCounts = new Dictionary<char, int>();
        
        // Считаем количество каждой буквы в слове
        foreach (char c in word)
        {
            if (char.IsLetter(c))
            {
                if (letterCounts.ContainsKey(c))
                    letterCounts[c]++;
                else
                    letterCounts[c] = 1;
            }
        }

        // Находим буквы с повторениями
        var duplicates = letterCounts.Where(pair => pair.Value >= minCount).ToList();
        
        if (duplicates.Count > 0)
        {
            int maxDuplicateCount = duplicates.Max(pair => pair.Value);
            
            var wordInfo = new WordWithDuplicates
            {
                word = word,
                duplicateCount = maxDuplicateCount,
                duplicateLetters = duplicates.Select(pair => $"{pair.Key}({pair.Value})").ToArray()
            };

            result.wordsWithDuplicates.Add(wordInfo);
            result.totalWordsWithDuplicates++;

            // Обновляем статистику
            foreach (var duplicate in duplicates)
            {
                string key = $"{duplicate.Key}_{duplicate.Value}";
                if (result.duplicateStatistics.ContainsKey(key))
                    result.duplicateStatistics[key]++;
                else
                    result.duplicateStatistics[key] = 1;
            }
        }
    }

    private void SaveResultsToFile(DuplicateAnalysisResult result)
    {
        using (StreamWriter writer = new StreamWriter(savePath))
        {
            writer.WriteLine("=== DUPLICATE LETTERS ANALYSIS ===");
            writer.WriteLine($"Total words analyzed: {result.totalWordsAnalyzed}");
            writer.WriteLine($"Words with duplicates: {result.totalWordsWithDuplicates}");
            writer.WriteLine($"Percentage: {(float)result.totalWordsWithDuplicates / result.totalWordsAnalyzed * 100:F2}%\n");

            // Статистика по типам дубликатов
            writer.WriteLine("=== DUPLICATE STATISTICS ===");
            foreach (var stat in result.duplicateStatistics)
            {
                string[] parts = stat.Key.Split('_');
                writer.WriteLine($"Letter '{parts[0]}' repeated {parts[1]} times: {stat.Value} words");
            }

            // Слова с дубликатами
            writer.WriteLine($"\n=== WORDS WITH DUPLICATES (sorted by duplicate count) ===");
            foreach (var wordInfo in result.wordsWithDuplicates)
            {
                writer.WriteLine($"{wordInfo.word} (max: {wordInfo.duplicateCount}x) - {string.Join(", ", wordInfo.duplicateLetters)}");
            }
        }
    }

    // Классы для хранения результатов
    private class DuplicateAnalysisResult
    {
        public int totalWordsAnalyzed = 0;
        public int totalWordsWithDuplicates = 0;
        public List<WordWithDuplicates> wordsWithDuplicates = new List<WordWithDuplicates>();
        public Dictionary<string, int> duplicateStatistics = new Dictionary<string, int>();
    }

    private class WordWithDuplicates
    {
        public string word;
        public int duplicateCount;
        public string[] duplicateLetters;
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