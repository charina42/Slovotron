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
        Debug.Log("IncreaseWordPoints");
        var lettersToReturn = new List<LetterData>();
    
        foreach (var letter in word)
        {
            switch (letter.Type)
            {
                case LetterType.Disposable:
                    // Удаляем одноразовую букву
                    Debug.Log("Disposable letter");
                    MoveLetter(letter, LetterLocation.OnBoard, LetterLocation.OnBoard);
                    break;
                
                case LetterType.Return:
                    // Помечаем для возврата на поле
                    lettersToReturn.Add(letter);
                    MoveLetter(letter, LetterLocation.OnBoard, LetterLocation.Used);
                    break;
                
                case LetterType.Wild when letter.LetterChar == '*':
                    MoveLetter(letter, LetterLocation.OnBoard, LetterLocation.Used);
                    break;
                
                case LetterType.NeighborMultiplierLeft:
                case LetterType.NeighborMultiplierRight:
                    // Множители не приносят очков, просто убираются
                    MoveLetter(letter, LetterLocation.OnBoard, LetterLocation.Used);
                    break;
                
                default:
                    IncreasePointsForUsedLetter(letter);
                    break;
            }
        }
    
        // Возвращаем буквы типа Return на поле
        foreach (var returnLetter in lettersToReturn)
        {
            MoveLetter(returnLetter, LetterLocation.Used, LetterLocation.OnBoard);
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

    public void IncreasePointsForWeakestLetter(List<LetterData> letterList)
    {
        if (letterList == null || letterList.Count == 0)
        {
            Debug.LogError("Список букв равен null или пуст!");
            return;
        }

        foreach (var letter in letterList)
        {
            Debug.Log($"IncreasePoints For {letter?.LetterChar} Letter");
        
            if (letter == null)
            {
                Debug.LogError("Одна из букв в списке равна null!");
                continue;
            }

            var item = YG2.saves.LetterInventory.FirstOrDefault(x => x.Letter.Equals(letter));
            if (item == null)
            {
                Debug.LogError($"Буква {letter.LetterChar} не найдена в инвентаре!");
                continue;
            }
            
            if (item.Counters.InBag <= 0)
            {
                Debug.LogWarning($"Буква {letter.LetterChar} отсутствует в мешке!");
                continue;
            }
            
            var modifiedLetter = new LetterData(letter.LetterChar, letter.Points + 1, letter.Type);
            MoveLetter(letter, LetterLocation.InBag, LetterLocation.InBag, -1);
            
            AddLetter(modifiedLetter, LetterLocation.InBag);
        
            if (item.Counters.TotalCount <= 0)
            {
                YG2.saves.LetterInventory.Remove(item);
            }
        }
    }
    
    public void DoublePointsForOneLetter(LetterData letter)
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

        // Проверяем, есть ли буква в мешке
        if (item.Counters.InBag <= 0)
        {
            Debug.LogWarning($"Буква {letter.LetterChar} отсутствует в мешке!");
            return;
        }

        // Уменьшаем количество исходной буквы в мешке на 1
        MoveLetter(letter, LetterLocation.InBag, LetterLocation.InBag, -1);

        var doubledLetter = new LetterData(letter.LetterChar, letter.Points * 2, letter.Type);

        // Добавляем новую букву с удвоенными очками в мешок
        AddLetter(doubledLetter, LetterLocation.InBag);
    
        // Если исходной буквы больше не осталось в инвентаре, удаляем запись
        if (item.Counters.TotalCount <= 0)
        {
            YG2.saves.LetterInventory.Remove(item);
        }
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
    
    public void AddDisposableTileToPool(char letterChar, int points)
    {
        // var points = LetterBasePoints[char.ToLower(letterChar)] + 5;
        var letter = new LetterData(letterChar, points, LetterType.Disposable);
        AddLetter(letter, LetterLocation.InBag);
    }

    public void AddRepeaterTileToPool(char letterChar, int points)
    {
        var letter = new LetterData(letterChar, points, LetterType.Repeater);
        AddLetter(letter, LetterLocation.InBag);
    }
    
    public void AddNeighborMultiplierToPool(char letterChar, string direction)
    {
        var points = 0; // Сама по себе приносит 0 очков
    
        LetterType multiplierType = direction.ToLower() == "left" 
            ? LetterType.NeighborMultiplierLeft 
            : LetterType.NeighborMultiplierRight;
    
        var multiplierLetter = new LetterData(letterChar, points, multiplierType);
        AddLetter(multiplierLetter, LetterLocation.InBag);
    
        Debug.Log($"Добавлен множитель {direction} для буквы '{letterChar}'");
    }

    public void AddReturnLetterToPool(char letterChar)
    {
        var points = LetterBasePoints[char.ToLower(letterChar)];
        var returnLetter = new LetterData(letterChar, points, LetterType.Return);
        AddLetter(returnLetter, LetterLocation.InBag);
    }
    
    public void MergeTiles(List<LetterData> lettersToMerge, int count)
    {
        if (lettersToMerge == null || lettersToMerge.Count != count)
        {
            Debug.LogError($"Неверный список букв для слияния: ожидалось {count}, получено {lettersToMerge?.Count}");
            return;
        }

        var targetChar = lettersToMerge[0].LetterChar;
        var totalPoints = lettersToMerge.Sum(letter => letter.Points);

        // Удаляем все указанные буквы
        foreach (var letter in lettersToMerge)
        {
            RemoveLetterFromAllLocations(letter, 1);
        }

        // Добавляем новую букву с суммой очков
        var mergedLetter = new LetterData(targetChar, totalPoints, LetterType.Standard);
        AddLetter(mergedLetter, LetterLocation.InBag);
    
        Debug.Log($"Слияние {count} букв '{targetChar}': создана буква стоимостью {totalPoints} очков");
    }

    public void MultiplyTiles(char letterChar, int multiplier)
    {
        // Найти самую дешевую версию этой буквы
        var cheapestLetter = YG2.saves.LetterInventory
            .Where(item => item.Letter.LetterChar == letterChar)
            .OrderBy(item => item.Letter.Points)
            .FirstOrDefault();

        if (cheapestLetter == null)
        {
            Debug.LogError($"Буква '{letterChar}' не найдена");
            return;
        }

        // Удаляем одну букву
        RemoveLetterFromAllLocations(cheapestLetter.Letter, 1);

        // Добавляем multiplier новых букв с той же стоимостью
        for (int i = 0; i < multiplier; i++)
        {
            AddLetter(cheapestLetter.Letter, LetterLocation.InBag);
        }
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
    
    private void RemoveLetterFromAllLocations(LetterData letter, int count)
    {
        var item = YG2.saves.LetterInventory.FirstOrDefault(x => x.Letter.Equals(letter));
        if (item == null) return;

        // Пытаемся удалить сначала из мешка, потом с доски, потом из использованных
        int remaining = count;
    
        if (item.Counters.InBag > 0)
        {
            int removeFromBag = Mathf.Min(remaining, item.Counters.InBag);
            MoveLetter(letter, LetterLocation.InBag, LetterLocation.InBag, -removeFromBag);
            remaining -= removeFromBag;
        }

        if (remaining > 0 && item.Counters.OnBoard > 0)
        {
            int removeFromBoard = Mathf.Min(remaining, item.Counters.OnBoard);
            MoveLetter(letter, LetterLocation.OnBoard, LetterLocation.OnBoard, -removeFromBoard);
            remaining -= removeFromBoard;
        }

        if (remaining > 0 && item.Counters.Used > 0)
        {
            int removeFromUsed = Mathf.Min(remaining, item.Counters.Used);
            MoveLetter(letter, LetterLocation.Used, LetterLocation.Used, -removeFromUsed);
            remaining -= removeFromUsed;
        }

        // Если букв не осталось, удаляем запись
        if (item.Counters.TotalCount <= 0)
        {
            YG2.saves.LetterInventory.Remove(item);
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"Не удалось удалить {remaining} букв '{letter.LetterChar}' - недостаточно экземпляров");
        }
    }
    
    // public LetterData RandomLetter()
    // {
    //     var allLetters = AllLetterTiles().Keys.ToList();
    //     if (allLetters.Count == 0) return null;
    //     int randomIndex = Random.Range(0, allLetters.Count);
    //     return allLetters[randomIndex];
    // }
    
    public Dictionary<LetterData, int> AllLetterTiles()
    {
        // return YG2.saves.LetterInventory
        //     .Where(item => item.Letter.Type == LetterType.Standard)
        //     .ToDictionary(
        //         item => item.Letter,
        //         item => item.Counters.TotalCount
        //     );
        
        return YG2.saves.LetterInventory
            .Where(item => item.Letter.Type == LetterType.Standard && item.Counters.InBag > 0)
            .ToDictionary(
                item => item.Letter,
                item => item.Counters.InBag // ← Только в сумке!
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
