#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DictionaryPreprocessor : EditorWindow
{
    [MenuItem("Tools/Preprocess Combined Dictionary")]
    public static void PreprocessCombinedDictionary()
    {
        // Пути к трём исходным файлам словарей
        string[] dictionaryPaths = new string[]
        {
            "Assets/Dictionaries/rus_custom_15kfun.txt",
            "Assets/Dictionaries/rus_custom_clearmeanings.txt",
            "Assets/Dictionaries/rus_nouns_freq_56k.txt"
        };

        // Сюда будем собирать все уникальные слова
        HashSet<string> allWords = new HashSet<string>();

        // Читаем все файлы
        foreach (string path in dictionaryPaths)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Файл не найден: {path}");
                continue;
            }

            string[] wordsInFile = File.ReadAllLines(path);
            foreach (string word in wordsInFile)
            {
                string trimmedWord = word.Trim().ToLower();
                if (!string.IsNullOrEmpty(trimmedWord))
                {
                    allWords.Add(trimmedWord);
                }
            }
        }

        Debug.Log($"Найдено уникальных слов: {allWords.Count}");

        // Группируем слова по длине
        Dictionary<int, List<string>> wordsByLength = new Dictionary<int, List<string>>();
        foreach (string word in allWords)
        {
            int length = word.Length;
            if (!wordsByLength.ContainsKey(length))
            {
                wordsByLength[length] = new List<string>();
            }
            wordsByLength[length].Add(word);
        }

        // Сортируем слова по алфавиту для каждого размера
        foreach (var length in wordsByLength.Keys.ToList())
        {
            wordsByLength[length].Sort();
        }

        // Сохраняем в JSON
        string outputPath = "Assets/Dictionaries/combined_dictionary.json";
        string json = JsonUtility.ToJson(new SerializableDictionary(wordsByLength), true);
        File.WriteAllText(outputPath, json);

        Debug.Log($"Объединённый словарь сохранён: {outputPath}");
        AssetDatabase.Refresh();
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
}
#endif