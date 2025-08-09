using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using YG;

namespace YG
{
    public partial class SavesYG
    {
        // public readonly Dictionary<LetterData, LetterCounters> LetterInventory = new Dictionary<LetterData, LetterCounters>();
        public readonly List<LetterInventoryItem> LetterInventory = new List<LetterInventoryItem>();
    }
}

[System.Serializable]
public class LetterInventoryItem
{
    public LetterData Letter { get; set; }
    public LetterCounters Counters { get; set; }
    
    // Можно добавить конструктор для удобства
    public LetterInventoryItem(LetterData letter, LetterCounters counters)
    {
        Letter = letter;
        Counters = counters;
    }
}


public class LetterBag
{
   
    // Основной словарь: LetterData -> её счётчики в разных местах
    // private readonly Dictionary<LetterData, LetterCounters> _letterInventory = new Dictionary<LetterData, LetterCounters>();
    
    public readonly Dictionary<char, int> LetterBasePoints = new Dictionary<char, int>();
    
    public delegate void LetterReplacedHandler(LetterData oldLetter, LetterData newLetter, int countChanged);
    public event LetterReplacedHandler OnLetterReplaced;
    
    // === Основные методы ===
    public void InitializeFromJson(TextAsset jsonFile)
    {
        var letterCollection = JsonUtility.FromJson<LetterCollection>(jsonFile.text);
        YG2.saves.LetterInventory.Clear();
        // LetterBasePoints.Clear();

        foreach (var letter in letterCollection.letters)
        {
            var letterData = new LetterData(letter.letter[0], letter.points, LetterType.Standard);
            AddLetter(letterData, LetterLocation.InBag, letter.count);
            // LetterBasePoints.Add(letter.letter[0], letter.points);
        }
    }
    
    public void InitializeBasePoints(TextAsset jsonFile)
    {
        var letterCollection = JsonUtility.FromJson<LetterCollection>(jsonFile.text);
        LetterBasePoints.Clear();

        foreach (var letter in letterCollection.letters)
        {
            LetterBasePoints.Add(letter.letter[0], letter.points);
        }
    }
    
    public void DebugPrintLetterInventory()
    {
        Debug.Log("=== DEBUG: Letter Inventory ===");
    
        // foreach (var item in YG2.saves.LetterInventory)
        // {
        //     Debug.Log($"Буква: {item.Letter.LetterChar}\n" +
        //               $"Тип: {item.Letter.Type}\n" +
        //               $"Очки: {item.Letter.Points}\n" +
        //               $"Мешок: {item.Counters.InBag}\n" +
        //               $"Доска: {item.Counters.OnBoard}\n" +
        //               $"Использовано: {item.Counters.Used}\n" +
        //               $"Всего: {item.Counters.TotalCount}\n" +
        //               "---------------------");
        // }
    
        Debug.Log($"Total letters in bag: {GetCountInLocation(LetterLocation.InBag)}");
        Debug.Log($"Total letters on board: {GetCountInLocation(LetterLocation.OnBoard)}");
        Debug.Log($"Total used letters: {GetCountInLocation(LetterLocation.Used)}");
    }
    

    private void AddLetter(LetterData letter, LetterLocation location, int count = 1)
    {
        var item = YG2.saves.LetterInventory.FirstOrDefault(x => x.Letter.Equals(letter));
        if (item == null)
        {
            item = new LetterInventoryItem(letter, new LetterCounters());
            YG2.saves.LetterInventory.Add(item);
        }
        item.Counters.ChangeCount(location, count);
    }

    public void MoveLetter(LetterData letter, LetterLocation from, LetterLocation to, int count = 1)
    {
        var item = YG2.saves.LetterInventory.FirstOrDefault(x => x.Letter.Equals(letter));
        if (item == null)
        {
            Debug.LogError($"Буква {letter.LetterChar} не найдена в инвентаре!");
            return;
        }

        // Проверяем, достаточно ли букв в исходном месте
        int currentCount = item.Counters.GetCount(from);
        if (currentCount < count)
        {
            Debug.LogError($"Недостаточно букв {letter.LetterChar} в {from} (требуется: {count}, доступно: {currentCount})");
            return;
        }

        if (from == to)
        {
            item.Counters.ChangeCount(from, count);
        }
        else
        {
            // Пробуем изменить количество
            item.Counters.ChangeCount(from, -count);
            item.Counters.ChangeCount(to, count);
        }

        // Debug.Log($"Буква {letter.LetterChar} перемещена из {from} в {to}");
    }

    public LetterData DrawLetter()
    {
        var availableLetters = YG2.saves.LetterInventory
            .Where(item => item.Counters.InBag > 0)
            .Select(item => item.Letter)
            .ToList();

        if (availableLetters.Count == 0)
        {
            Debug.LogWarning("Мешочек пуст!");
            return null;
        }

        int totalOnBoard = GetCountInLocation(LetterLocation.OnBoard);
        int vowelsOnBoard = YG2.saves.LetterInventory
            .Where(item => IsVowel(item.Letter.LetterChar) && item.Counters.OnBoard > 0)
            .Sum(item => item.Counters.OnBoard);

        float currentVowelRatio = totalOnBoard > 0 ? (float)vowelsOnBoard / totalOnBoard : 0;
        float targetVowelRatio = 0.4f;
        float vowelRatioThreshold = 0.15f;

        var filteredLetters = availableLetters;
        bool forceVowel = currentVowelRatio < targetVowelRatio - vowelRatioThreshold;
        bool forceConsonant = currentVowelRatio > targetVowelRatio + vowelRatioThreshold;

        // Лимит редких букв
        int rareLettersOnBoard = YG2.saves.LetterInventory
            .Count(item => item.Letter.Points >= 5 && item.Counters.OnBoard > 0);

        if (rareLettersOnBoard >= 2)
        {
            filteredLetters = filteredLetters.Where(l => l.Points < 5).ToList();
            if (filteredLetters.Count == 0)
                filteredLetters = availableLetters;
        }

        List<LetterData> candidates = forceVowel 
            ? filteredLetters.Where(l => IsVowel(l.LetterChar)).ToList()
            : forceConsonant 
                ? filteredLetters.Where(l => !IsVowel(l.LetterChar)).ToList()
                : filteredLetters;

        if (candidates.Count == 0)
            candidates = filteredLetters;

        LetterData selectedLetter = GetWeightedRandomLetter(candidates);
        MoveLetter(selectedLetter, LetterLocation.InBag, LetterLocation.OnBoard);
        return selectedLetter;
    }

// Вспомогательный метод для взвешенного выбора
    private LetterData GetWeightedRandomLetter(List<LetterData> letters)
    {
        var items = YG2.saves.LetterInventory
            .Where(item => letters.Contains(item.Letter))
            .ToList();

        float totalWeight = items.Sum(item => item.Counters.InBag);
        float randomValue = Random.Range(0, totalWeight);
        
        foreach (var item in items)
        {
            if (randomValue < item.Counters.InBag)
                return item.Letter;
            randomValue -= item.Counters.InBag;
        }

        return letters[0];
    }


    private bool IsVowel(char letter)
    {
        return "аеёиоуыэюя".IndexOf(char.ToLower(letter)) >= 0;
    }

    public void ReturnUsedLettersToBag()
    {
        foreach (var item in YG2.saves.LetterInventory.Where(item => item.Counters.Used > 0))
        {
            MoveLetter(item.Letter, LetterLocation.Used, LetterLocation.InBag, item.Counters.Used);
        }
    }
    
    
    // === Методы для модификации букв ===

    public void IncreaseWordPoints(List<LetterData> word)
    {
        foreach (var letter in word)
        {
            IncreasePointsForUsedLetter(letter);
        }
    }

    private void IncreasePointsForUsedLetter(LetterData letter)
    {
        Debug.Log($"Increase Points For {letter?.LetterChar} Letter");
        
        if (letter == null || GetLetterCount(letter, LetterLocation.OnBoard) <= 0)
        {
            Debug.LogError($"Буква {letter?.LetterChar} не найдена на поле!");
            return;
        }

        MoveLetter(letter, LetterLocation.OnBoard, LetterLocation.OnBoard, -1);

        var modifiedLetter = new LetterData(letter.LetterChar, letter.Points + 1, letter.Type);

        AddLetter(modifiedLetter, LetterLocation.Used);
    }

    public void IncreasePointsForWeakestLetter(LetterData letter)
    {
        if (letter == null)
        {
            Debug.LogError("Буква равна null!");
            return;
        }

        var item = YG2.saves.LetterInventory.FirstOrDefault(x => x.Letter.Equals(letter));
        if (item == null)
        {
            Debug.LogError($"Буква {letter.LetterChar} не найдена в инвентаре!");
            return;
        }

        int inBagCount = item.Counters.InBag;
        int onBoardCount = item.Counters.OnBoard;
        int usedCount = item.Counters.Used;

        YG2.saves.LetterInventory.Remove(item);

        var modifiedLetter = new LetterData(letter.LetterChar, letter.Points + 1, letter.Type);
        
        if (inBagCount > 0) AddLetter(modifiedLetter, LetterLocation.InBag, inBagCount);
        if (onBoardCount > 0)
        {
            AddLetter(modifiedLetter, LetterLocation.OnBoard, onBoardCount);
            OnLetterReplaced?.Invoke(letter, modifiedLetter, onBoardCount);
        }
        if (usedCount > 0) AddLetter(modifiedLetter, LetterLocation.Used, usedCount);
    }
    
    public void DoublePointsForOneLetter(LetterData letter)
    {
        if (letter == null)
        {
            Debug.LogError("Буква равна null!");
            return;
        }

        LetterLocation sourceLocation; // Приоритет поиска

        // Ищем букву в порядке приоритета: InBag -> OnBoard -> Used
        if (GetLetterCount(letter, LetterLocation.InBag) > 0)
            sourceLocation = LetterLocation.InBag;
        else if (GetLetterCount(letter, LetterLocation.OnBoard) > 0)
            sourceLocation = LetterLocation.OnBoard;
        else if (GetLetterCount(letter, LetterLocation.Used) > 0)
            sourceLocation = LetterLocation.Used;
        else
        {
            Debug.LogError($"Буква {letter.LetterChar} не найдена нигде!");
            return;
        }

        // Уменьшаем исходную букву
        MoveLetter(letter, sourceLocation, sourceLocation, -1);

        var doubledLetter = new LetterData(letter.LetterChar, letter.Points * 2, letter.Type);

        AddLetter(doubledLetter, sourceLocation);
        
        if(sourceLocation == LetterLocation.OnBoard)
            OnLetterReplaced?.Invoke(letter, doubledLetter, 1);
    }
    
    
    // === Методы для добавления специальных букв ===
    public void AddCapitalLetterToPool(char letterChar)
    {
        var points = LetterBasePoints[char.ToLower(letterChar)];
        var capitalLetter = new LetterData(letterChar, points, LetterType.Capital);
        AddLetter(capitalLetter, LetterLocation.InBag);
    }

    public void AddFinalLetterToPool(char letterChar)
    {
        var points = LetterBasePoints[char.ToLower(letterChar)];
        var finalLetter = new LetterData(letterChar, points, LetterType.Final);
        AddLetter(finalLetter, LetterLocation.InBag);
    }

    public void AddWildSymbolToPool()
    {
        var wildSymbol = new LetterData('*', 0, LetterType.Wild);
        AddLetter(wildSymbol, LetterLocation.InBag);
    }
    
    
    // === Вспомогательные методы ===
    public int GetLetterCount(LetterData letter, LetterLocation location)
    {
        var item = YG2.saves.LetterInventory.FirstOrDefault(x => x.Letter.Equals(letter));
        if (item != null)
        {
            return location switch
            {
                LetterLocation.InBag => item.Counters.InBag,
                LetterLocation.OnBoard => item.Counters.OnBoard,
                LetterLocation.Used => item.Counters.Used,
                _ => 0
            };
        }
        return 0;
    }
    
    public int GetCountInLocation(LetterLocation location)
    {
        return location switch
        {
            LetterLocation.Used => YG2.saves.LetterInventory.Sum(item => item.Counters.Used),
            LetterLocation.OnBoard => YG2.saves.LetterInventory.Sum(item => item.Counters.OnBoard),
            LetterLocation.InBag => YG2.saves.LetterInventory.Sum(item => item.Counters.InBag),
            _ => 0
        };
    }
    
    public LetterData RandomLetter()
    {
        var allLetters = AllLetterTiles().Keys.ToList();
        if (allLetters.Count == 0) return null;
        int randomIndex = Random.Range(0, allLetters.Count);
        return allLetters[randomIndex];
    }
    
    public Dictionary<LetterData, int> AllLetterTiles()
    {
        return YG2.saves.LetterInventory
            .Where(item => item.Letter.Type == LetterType.Standard)
            .ToDictionary(
                item => item.Letter,
                item => item.Counters.TotalCount
            );
    }
    
    public Dictionary<LetterData, int> GetAllLetters()
    {
        return YG2.saves.LetterInventory
            .ToDictionary(
                item => item.Letter,
                item => item.Counters.TotalCount
            );
    }
    
    

    [System.Serializable]
    private class LetterCollection
    {
        public LetterItem[] letters;
        public int totalLetters;
        public string description;
    }

    [System.Serializable]
    private class LetterItem
    {
        public string letter;
        public int count;
        public int points;
    }
   
}
