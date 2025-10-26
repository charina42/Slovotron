using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

public class BagImprovementOptionBuilder
{
    private readonly LetterBag _letterBag;
    private Dictionary<LetterData, int> _lettersPool;
    
    public BagImprovementOptionBuilder(LetterBag letterBag, Dictionary<LetterData, int> lettersPool)
    {
        _letterBag = letterBag;
        _lettersPool = lettersPool;
    }
    
    public void UpdateLettersPool(Dictionary<LetterData, int> lettersPool)
    {
        _lettersPool = lettersPool;
    }
    
    public ImprovementOption BuildFromImprovementData(ImprovementData baseImprovement)
    {
        return baseImprovement.effect switch
        {
            "DoublePoints2" => BuildDoublePoints(baseImprovement, 2),
            "DoublePoints3" => BuildDoublePoints(baseImprovement, 3),
            "DoublePoints45" => BuildDoublePoints(baseImprovement, 4, 5),
            
            "AddOnePointTo4" => BuildAddOnePointToAll(baseImprovement, 4),
            "AddOnePointTo8" => BuildAddOnePointToAll(baseImprovement, 8),
            
            "CapitalLetter" => BuildCapitalLetter(baseImprovement),
            "FinalLetter" => BuildFinalLetter(baseImprovement),
            "AddWildcard" => BuildAddWildcard(baseImprovement),
            
            "DisposableTile" => BuildDisposableTile(baseImprovement),
            "AddDisposableWildcard" => BuildDisposableWildcard(baseImprovement),
            
            "RepeaterLetter" => BuildRepeaterLetter(baseImprovement),
            
            "Merge2Tiles" => BuildMergeTiles(baseImprovement, 2),
            "Merge3Tiles" => BuildMergeTiles(baseImprovement, 3),
            "Merge4Tiles" => BuildMergeTiles(baseImprovement, 4),
            
            "Multiply2Tiles" => BuildMultiplyTiles(baseImprovement, 2),
            "Multiply3Tiles" => BuildMultiplyTiles(baseImprovement, 3),
            "Multiply4Tiles" => BuildMultiplyTiles(baseImprovement, 4),
            
            "NeighborMultiplier" => BuildNeighborMultiplier(baseImprovement), 
            "ReturnLetter" => BuildReturnLetter(baseImprovement),
            
            _ => throw new Exception($"Неизвестный эффект улучшения: {baseImprovement.effect}")
        };
    }


    public void HandleImprovementEffect(ImprovementOption option)
    {
        switch (option.EffectType)
        {
            case "DoublePoints2":
            case "DoublePoints3":
            case "DoublePoints45":
                _letterBag.DoublePointsForOneLetter(option.TargetLetter[0]);
                break;

            case "AddOnePointTo4":
            case "AddOnePointTo8":
                _letterBag.IncreasePointsForWeakestLetter(option.TargetLetter);
                break;

            case "CapitalLetter":
                _letterBag.AddCapitalLetterToPool(option.TargetLetterChar);
                break;

            case "FinalLetter":
                _letterBag.AddFinalLetterToPool(option.TargetLetterChar);
                break;

            case "AddWildcard":
                _letterBag.AddWildSymbolToPool();
                break;
            
            case "DisposableTile":
                _letterBag.AddDisposableTileToPool(option.TargetLetterChar, option.TargetLetterPoints);
                break;
            
            case "AddDisposableWildcard":
                _letterBag.AddDisposableTileToPool(option.TargetLetterChar, option.TargetLetterPoints);
                break;
            
            case "RepeaterLetter":
                _letterBag.AddRepeaterTileToPool(option.TargetLetterChar, option.TargetLetterPoints);
                break;
            
            case "Merge2Tiles":
                _letterBag.MergeTiles(option.TargetLetter, 2);
                break;
            case "Merge3Tiles":
                _letterBag.MergeTiles(option.TargetLetter, 3);
                break;
            case "Merge4Tiles":
               _letterBag.MergeTiles(option.TargetLetter, 4);
                break;

            case "Multiply2Tiles":
                _letterBag.MultiplyTiles(option.TargetLetterChar, 2);
                break;
            case "Multiply3Tiles":
                _letterBag.MultiplyTiles(option.TargetLetterChar, 3);
                break;
            case "Multiply4Tiles":
                _letterBag.MultiplyTiles(option.TargetLetterChar, 4);
                break;
            
            case "NeighborMultiplier":
                _letterBag.AddNeighborMultiplierToPool(option.TargetLetterChar, option.Modifier);
                break;

            case "ReturnLetter":
                _letterBag.AddReturnLetterToPool(option.TargetLetterChar);
                break;
        }
    }

    private ImprovementOption BuildDoublePoints(ImprovementData baseImprovement, int cost, int cost2 = -1)
    {
        var targetLetterTile = GetRandomLetterWithExactCost(cost, cost2);
        
        if (targetLetterTile == null || !HasEnoughLettersInBag(targetLetterTile.LetterChar))
        {
            return null; // Недостаточно букв для улучшения
        }

        var targetLetter = targetLetterTile.LetterChar;
        var currentPoints = targetLetterTile.Points;
        
        var description = string.Format(
            baseImprovement.description,
            targetLetter, currentPoints, currentPoints * 2
        );

        return CreateOption(baseImprovement, targetLetter, 0,new List<LetterData>(){targetLetterTile}, description);
    }
    
    private ImprovementOption BuildAddOnePointToAll(ImprovementData baseImprovement, int count)
    {
        var randomTilesList = GetLettersWithLowestInventoryValue(count);
        
        // var targetLetterTile = randomTile.Key;
        // var targetLetter = randomTile.Key.LetterChar;
        // var currentPoints = randomTile.Key.Points;
        
        var description = string.Format(
            baseImprovement.description
        );

        return CreateOption(baseImprovement, ' ', 0, randomTilesList, description);
    }
    
    private ImprovementOption BuildCapitalLetter(ImprovementData baseImprovement)
    {
        var targetLetter = baseImprovement.allowedLetters[Random.Range(0, baseImprovement.allowedLetters.Length)][0];
        
        var description = string.Format(
            baseImprovement.description,
            targetLetter, _letterBag.LetterBasePoints[char.ToLower(targetLetter)]
        );

        return CreateOption(baseImprovement, targetLetter, 0,null, description);
    }
    
    private ImprovementOption BuildFinalLetter(ImprovementData baseImprovement)
    {
        var targetLetter = baseImprovement.allowedLetters[Random.Range(0, baseImprovement.allowedLetters.Length)][0];
        
        var description = string.Format(
            baseImprovement.description,
            targetLetter, _letterBag.LetterBasePoints[char.ToLower(targetLetter)]
        );

        return CreateOption(baseImprovement, targetLetter, 0,null, description);
    }
    
    private ImprovementOption BuildAddWildcard(ImprovementData baseImprovement)
    {
        var targetLetter = '*';
        var description = baseImprovement.description;

        return CreateOption(baseImprovement, targetLetter, 0, null, description);
    }
    
    private ImprovementOption BuildDisposableTile(ImprovementData baseImprovement)
    {
        var targetLetter = baseImprovement.allowedLetters[Random.Range(0, baseImprovement.allowedLetters.Length)][0];
        
        var maxPoints = _lettersPool.Keys.Max(letter => letter.Points);
        var minPointsForChar = _lettersPool.Keys
            .Where(letter => letter.LetterChar == targetLetter)
            .Min(letter => letter.Points);
        var targetPoints = Math.Min(minPointsForChar + 5, maxPoints);
        
        var description = string.Format(
            baseImprovement.description,
            targetLetter, targetPoints
        );

        return CreateOption(baseImprovement, targetLetter, targetPoints, null, description);
    }
    
    private ImprovementOption BuildDisposableWildcard(ImprovementData baseImprovement)
    {
        var targetLetter = '*';
        var description = string.Format(
            baseImprovement.description,
            targetLetter
        );
        return CreateOption(baseImprovement, targetLetter, 0, null, description);
    }

    private ImprovementOption BuildRepeaterLetter(ImprovementData baseImprovement)
    {
        var targetLetter = baseImprovement.allowedLetters[Random.Range(0, baseImprovement.allowedLetters.Length)][0];
        
        var description = string.Format(
            baseImprovement.description,
            targetLetter, _letterBag.LetterBasePoints[char.ToLower(targetLetter)]
        );

        return CreateOption(baseImprovement, targetLetter, 0,null, description);
    }
    
    private ImprovementOption BuildMergeTiles(ImprovementData baseImprovement, int count)
    {
        var lettersToMerge = FindLettersForMerge(count);
    
        if (lettersToMerge == null || lettersToMerge.Count == 0)
        {
            Debug.LogError($"Не найдены подходящие буквы для слияния {count} штук");
            return null;
        }
        
        if (lettersToMerge.Count == 0 || 
            !HasEnoughLettersInBag(lettersToMerge, count))
        {
            return null; // Недостаточно букв для слияния
        }

        var targetLetter = lettersToMerge[0].LetterChar;
        var totalPoints = lettersToMerge.Sum(letter => letter.Points);
    
        var description = string.Format(
            baseImprovement.description,
            targetLetter
        );

        return CreateOption(baseImprovement, targetLetter, totalPoints, lettersToMerge, description);
    }

    private ImprovementOption BuildMultiplyTiles(ImprovementData baseImprovement, int count)
    {
        var targetLetter = FindSuitableLetterForMultiply();

        if (targetLetter == '\0')
        {
            Debug.LogError($"Не найдена подходящая буква для копирования");
            return null;
        }

        var basePoints = _letterBag.LetterBasePoints[char.ToLower(targetLetter)];

        var description = string.Format(
            baseImprovement.description,
            targetLetter
        );

        return CreateOption(baseImprovement, targetLetter, basePoints, null, description);
    }
    
    private ImprovementOption BuildNeighborMultiplier(ImprovementData baseImprovement)
    {
        var targetLetter = FindSuitableLetterForNeighborMultiplier();
    
        if (targetLetter == '\0')
        {
            Debug.LogError("Не найдена подходящая буква для множителя соседа");
            return null;
        }

        // Определяем направление множителя (левый или правый)
        var multiplierDirection = Random.Range(1, 2); //== 1 ? "Left" : "Right";
        // string modifier = multiplierDirection;
    
        var basePoints = _letterBag.LetterBasePoints[char.ToLower(targetLetter)];
    
        var description = string.Format(
            baseImprovement.description,
            targetLetter,
            multiplierDirection  == 1 ? "левой" : "правой"
        );

        var option = CreateOption(baseImprovement, targetLetter, basePoints, null, description);
        option.Modifier = multiplierDirection; // Сохраняем направление в modifier
        return option;
    }

    private ImprovementOption BuildReturnLetter(ImprovementData baseImprovement)
    {
        var targetLetter = FindSuitableLetterForReturn();
    
        if (targetLetter == '\0')
        {
            Debug.LogError("Не найдена подходящая буква для возвращающейся буквы");
            return null;
        }

        var basePoints = _letterBag.LetterBasePoints[char.ToLower(targetLetter)];
    
        var description = string.Format(
            baseImprovement.description,
            targetLetter
        );

        return CreateOption(baseImprovement, targetLetter, basePoints, null, description);
    }
    


    private static ImprovementOption CreateOption(ImprovementData baseImprovement, char targetLetter, 
        int targetLetterPoints, List<LetterData> targetLetterTile, string description)
    {
        return new ImprovementOption
        {
            EffectType = baseImprovement.effect,
            TargetLetter = targetLetterTile,
            TargetLetterChar = targetLetter,
            TargetLetterPoints = targetLetterPoints,
            Description = description,
            ShortDescription = baseImprovement.shortDescription,
            Modifier = baseImprovement.modifier, // Сохраняем исходный modifier
            IsMeta = false,
            Rarity = ParseRarity(baseImprovement.rarity)
        };
    }
    
    private static ImprovementRarity ParseRarity(string rarityString)
    {
        return Enum.TryParse<ImprovementRarity>(rarityString, out var rarity) 
            ? rarity 
            : ImprovementRarity.Common;
    }
    
    
    private bool HasEnoughLettersInBag(char letterChar, int requiredCount = 1)
    {
        return _lettersPool
            .Where(kv => kv.Key.LetterChar == letterChar && kv.Value > 0)
            .Sum(kv => kv.Value) >= requiredCount;
    }

    private bool HasEnoughLettersInBag(List<LetterData> letters, int requiredCount = 1)
    {
        if (letters == null) return true;
    
        foreach (var letter in letters)
        {
            var available = _lettersPool
                .Where(kv => kv.Key.Equals(letter) && kv.Value > 0)
                .Sum(kv => kv.Value);
            
            if (available < requiredCount)
                return false;
        }
        return true;
    }

    [CanBeNull]
    private List<LetterData> GetLettersWithLowestInventoryValue(int count)
    {
        var result = new List<LetterData>();

        // Получаем все доступные буквы из пула
        var availableLetters = _lettersPool
            .Where(kv => kv.Value > 0)
            .OrderBy(kv => kv.Key.Points) // Сортируем по стоимости
            .ThenBy(kv => kv.Key.LetterChar) // Для детерминированности
            .ToList();

        if (!availableLetters.Any())
            return result;

        // Создаем взвешенный список всех доступных фишек
        var weightedPool = new List<LetterData>();
        foreach (var letterKv in availableLetters)
        {
            for (int i = 0; i < letterKv.Value; i++)
            {
                weightedPool.Add(letterKv.Key);
            }
        }

        // Если в пуле меньше фишек, чем нужно, возвращаем все что есть
        if (weightedPool.Count < count)
        {
            return weightedPool.Take(count).ToList();
        }

        // Группируем по стоимости и сортируем по возрастанию
        var lettersByCost = weightedPool
            .GroupBy(letter => letter.Points)
            .OrderBy(group => group.Key)
            .ToList();

        // Собираем фишки начиная с самых дешевых
        foreach (var costGroup in lettersByCost)
        {
            var lettersInThisCost = costGroup.ToList();

            // Берем столько фишек из этой группы, сколько нужно или сколько есть
            int takeCount = Math.Min(count - result.Count, lettersInThisCost.Count);

            // Перемешиваем фишки в этой группе для случайного выбора
            for (int i = 0; i < takeCount; i++)
            {
                int randomIndex = Random.Range(0, lettersInThisCost.Count);
                result.Add(lettersInThisCost[randomIndex]);
                lettersInThisCost.RemoveAt(randomIndex);
            }

            // Если набрали нужное количество, выходим
            if (result.Count >= count)
                break;
        }

        // Если все равно не набрали (маловероятно, но на всякий случай)
        if (result.Count < count)
        {
            // Добираем случайные фишки из оставшихся
            var remainingLetters = weightedPool.Except(result).ToList();
            while (result.Count < count && remainingLetters.Count > 0)
            {
                int randomIndex = Random.Range(0, remainingLetters.Count);
                result.Add(remainingLetters[randomIndex]);
                remainingLetters.RemoveAt(randomIndex);
            }
        }

        return result.Take(count).ToList(); // Гарантируем ровно count элементов
    }

    [CanBeNull]
    private LetterData GetRandomLetterWithExactCost(int cost, int cost2 = -1)
    {
        if (cost2 == -1) cost2 = cost;
    
        var letters = _lettersPool
            .Where(letter => (letter.Key.Points == cost || letter.Key.Points == cost2) && letter.Value > 0)
            .ToList();

        if (!letters.Any())
            return null;

        var randomLetter = letters[Random.Range(0, letters.Count)];
        return randomLetter.Key;
    }
    
    private char FindSuitableLetterForMultiply()
    {
        var availableLetters = _lettersPool
            .Where(kv => kv.Value > 0)
            .GroupBy(kv => kv.Key.LetterChar)
            .Select(group => new
            {
                Letter = group.Key,
                TotalCount = group.Sum(kv => kv.Value),
                AvgPoints = group.Average(kv => kv.Key.Points),
                MinPoints = group.Min(kv => kv.Key.Points)
            })
            .Where(g => g.MinPoints >= 2 && g.MinPoints <= 8) // Ограничение: не слишком дешевые и не слишком дорогие
            .OrderBy(g => Mathf.Abs((float)g.AvgPoints - 5f)) // Предпочитаем буквы со средней стоимостью ~5
            .ThenByDescending(g => g.TotalCount)
            .ToList();

        if (!availableLetters.Any())
            return '\0';

        // Взвешенный выбор с предпочтением букв со средней стоимостью
        var weightedPool = new List<char>();
        foreach (var letter in availableLetters)
        {
            // Вес обратно пропорционален отклонению от средней стоимости (5)
            float deviation = Mathf.Abs((float)letter.AvgPoints - 5f);
            int weight = Mathf.Max(1, Mathf.RoundToInt(10f / (deviation + 1f)));
        
            for (int i = 0; i < weight; i++)
            {
                weightedPool.Add(letter.Letter);
            }
        }

        return weightedPool[Random.Range(0, weightedPool.Count)];
    }

    private List<LetterData> FindLettersForMerge(int count)
    {
        // Группируем буквы по символу
        var letterGroups = _lettersPool
            .Where(kv => kv.Value > 0)
            .GroupBy(kv => kv.Key.LetterChar)
            .Select(group => new
            {
                LetterChar = group.Key,
                Letters = group.SelectMany(kv => 
                    Enumerable.Repeat(kv.Key, kv.Value)).ToList(),
                TotalCount = group.Sum(kv => kv.Value)
            })
            .Where(g => g.TotalCount >= count) // Нужно как минимум count букв
            .OrderBy(g => g.Letters.Average(l => l.Points)) // Предпочитаем более дешевые буквы
            .ThenByDescending(g => g.TotalCount) // И те, которых больше
            .ToList();

        if (!letterGroups.Any())
            return null;

        // Выбираем группу букв для слияния
        var selectedGroup = letterGroups[0];
    
        // Берем самые дешевые буквы из выбранной группы
        var lettersToMerge = selectedGroup.Letters
            .OrderBy(l => l.Points)
            .Take(count)
            .ToList();

        return lettersToMerge;
    }
    
    private char FindSuitableLetterForNeighborMultiplier()
    {
        var availableLetters = _lettersPool
            .Where(kv => kv.Value > 0 && kv.Key.Type == LetterType.Standard)
            .GroupBy(kv => kv.Key.LetterChar)
            .Select(group => new
            {
                Letter = group.Key,
                TotalCount = group.Sum(kv => kv.Value),
                AvgPoints = group.Average(kv => kv.Key.Points)
            })
            .Where(g => g.AvgPoints >= 3 && g.AvgPoints <= 7) // Средняя стоимость
            .OrderByDescending(g => g.TotalCount)
            .ToList();

        if (!availableLetters.Any())
            return '\0';

        return availableLetters[Random.Range(0, Mathf.Min(availableLetters.Count, 3))].Letter;
    }

    private char FindSuitableLetterForReturn()
    {
        var availableLetters = _lettersPool
            .Where(kv => kv.Value > 0 && kv.Key.Type == LetterType.Standard)
            .GroupBy(kv => kv.Key.LetterChar)
            .Select(group => new
            {
                Letter = group.Key,
                TotalCount = group.Sum(kv => kv.Value),
                AvgPoints = group.Average(kv => kv.Key.Points)
            })
            .Where(g => g.AvgPoints >= 5) // Дорогие буквы
            .OrderByDescending(g => g.AvgPoints)
            .ThenByDescending(g => g.TotalCount)
            .ToList();

        if (!availableLetters.Any())
            return '\0';

        // Взвешенный случайный выбор - чем выше стоимость, тем больше шансов быть выбранной
        var weightedPool = new List<char>();
        foreach (var letter in availableLetters)
        {
            int weight = Mathf.RoundToInt((float)letter.AvgPoints);
            for (int i = 0; i < weight; i++)
            {
                weightedPool.Add(letter.Letter);
            }
        }

        return weightedPool[Random.Range(0, weightedPool.Count)];
    }
}