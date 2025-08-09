using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Random = UnityEngine.Random;
using YG;

namespace YG
{
    public partial class SavesYG
    {
        public readonly List<ImprovementOption> CurrentImprovementOptions = new List<ImprovementOption>();
        public readonly List<ImprovementOption> ActiveImprovements = new List<ImprovementOption>();
    }
}

public class ImprovementSystem
{
    private LetterBag _letterBag;
    private Dictionary<LetterData, int> _lettersPool;
    
    private ImprovementData[] _improvementsDataList;
    private ImprovementData[] _bagImprovementsDataList;
    
    // private readonly List<ImprovementOption> _currentImprovementOptions = new List<ImprovementOption>();
    // public readonly List<ImprovementOption> ActiveImprovements = new List<ImprovementOption>();

    public static event Action<bool> OnImprovementSelected;


    private readonly Dictionary<string, int> _rarityWeights = new Dictionary<string, int>
    {
        { "Common", 70 }, // 70% шанс
        { "Rare", 25 }, // 25% шанс
        { "Epic", 10 } // 5% шанс
    };

    public void Initialize(LetterBag letterBag)
    {
        LoadImprovements();
        _letterBag = letterBag;
        
        ImprovementChosePopup.OnCardSelected += HandleCardSelection;
    }


    public List<ImprovementOption> ShowImprovementOptions(bool isMajor)
    {
        _lettersPool = _letterBag.AllLetterTiles();
        var existingOptions = new List<ImprovementOption>();

        for (var i = 0; i < 3; i++)
        {
            ImprovementOption option;
            if (isMajor)
            {
                option = GenerateRandomImprovement(existingOptions);
                if (option == null) // Если не удалось создать уникальное улучшение
                {
                    option = new ImprovementOption
                    {
                        EffectType = "Fallback",
                        Description = "Дополнительные очки", // Запасной вариант
                        IsMeta = true
                    };
                }
            }
            else
            {
                option = GenerateRandomBagImprovement(existingOptions);
            }

            YG2.saves.CurrentImprovementOptions.Add(option);
            existingOptions.Add(option);
        }

        return YG2.saves.CurrentImprovementOptions;
    }

    private ImprovementData WeightedRandomSelection(List<ImprovementData> improvements)
    {
        // 1. Создаём список с учётом весов
        List<ImprovementData> weightedList = new List<ImprovementData>();
        foreach (var imp in improvements)
        {
            int weight = _rarityWeights[imp.rarity];
            for (int i = 0; i < weight; i++)
            {
                weightedList.Add(imp); // Добавляем N раз в зависимости от веса
            }
        }

        return weightedList[Random.Range(0, weightedList.Count)];
    }

    private ImprovementOption GenerateRandomBagImprovement(List<ImprovementOption> existingOptions)
    {
        ImprovementData baseImprovement;
        ImprovementOption newOption;
        bool isUnique;
        int attempts = 0;
        const int maxAttempts = 50; // Защита от бесконечного цикла

        do
        {
            isUnique = true;
            baseImprovement = WeightedRandomSelection(_bagImprovementsDataList.ToList());

            char targetLetter;
            int currentPoints;
            string description;
            LetterData targetLetterTile;

            switch (baseImprovement.effect)
            {
                case "DoublePoints":
                case "AddOnePointToAll":
                    var randomTile = DrawRandomWeakestTile();
                    targetLetterTile = randomTile.Key;
                    targetLetter = randomTile.Key.LetterChar;
                    currentPoints = randomTile.Key.Points;
                    description = string.Format(
                        baseImprovement.description,
                        targetLetter, currentPoints,
                        baseImprovement.effect == "DoublePoints" ? currentPoints * 2 : currentPoints + 1
                    );
                    break;

                case "CapitalLetter":
                case "FinalLetter":
                    targetLetter =
                        baseImprovement.allowedLetters[Random.Range(0, baseImprovement.allowedLetters.Length)][0];
                    targetLetterTile = null;
                    description = string.Format(
                        baseImprovement.description,
                        targetLetter, _letterBag.LetterBasePoints[char.ToLower(targetLetter)]
                    );
                    break;

                case "AddWildcard":
                    targetLetter = '*';
                    targetLetterTile = null;
                    description = baseImprovement.description;
                    break;

                default:
                    throw new System.Exception("Неизвестный эффект улучшения!");
            }

            newOption = new ImprovementOption
            {
                EffectType = baseImprovement.effect,
                TargetLetter = targetLetterTile,
                TargetLetterChar = targetLetter,
                Description = description,
                IsMeta = false
            };

            // Проверка на уникальность
            foreach (var option in existingOptions)
            {
                if (newOption.EffectType == "AddWildcard" && option.EffectType == "AddWildcard")
                {
                    isUnique = false;
                    break;
                }

                if (newOption.EffectType == option.EffectType &&
                    newOption.TargetLetterChar == option.TargetLetterChar)
                {
                    isUnique = false;
                    break;
                }
            }

            attempts++;
            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("Не удалось сгенерировать уникальное улучшение после " + maxAttempts + " попыток.");
                return newOption; // Возвращаем даже если не уникально, чтобы избежать бесконечного цикла
            }
        } while (!isUnique);

        return newOption;
    }

    private ImprovementOption GenerateRandomImprovement(List<ImprovementOption> existingOptions)
    {
        ImprovementData baseImprovement;
        ImprovementOption newOption = null;
        bool isUnique;
        int attempts = 0;
        const int maxAttempts = 50;

        do
        {
            isUnique = true;
            baseImprovement = WeightedRandomSelection(_improvementsDataList.ToList());

            // Проверка 1: Улучшение уже активно?
            if (YG2.saves.ActiveImprovements.Any(imp => imp.EffectType == baseImprovement.effect))
            {
                isUnique = false;
                attempts++;
                continue;
            }

            // Проверка 2: Улучшение уже есть в текущем наборе?
            if (existingOptions.Any(opt => opt.EffectType == baseImprovement.effect))
            {
                isUnique = false;
                attempts++;
                continue;
            }

            newOption = new ImprovementOption
            {
                EffectType = baseImprovement.effect,
                Description = baseImprovement.description,
                shortDescription = baseImprovement.shortDescription,
                modifier = baseImprovement.modifier,
                TargetLetterChar = '0',
                IsMeta = true
            };

            attempts++;
            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("Не удалось сгенерировать уникальное улучшение.");
                return null; // или запасной вариант
            }
        } while (!isUnique);

        return newOption;
    }

    private void HandleCardSelection(ImprovementOption option)
    {
        // Обработка выбранной опции
        Debug.Log($"Selected: {option.Description}");

        if (option.IsMeta)
        {
            YG2.saves.ActiveImprovements.Add(option);
            YG2.saves.CurrentImprovementOptions.Clear();
            OnImprovementSelected?.Invoke(true);
        }
        else
        {
            switch (option.EffectType)
            {
                case "DoublePoints":
                    _letterBag.DoublePointsForOneLetter(option.TargetLetter);
                    break;

                case "AddOnePointToAll":
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
            }

            YG2.saves.CurrentImprovementOptions.Clear();
            OnImprovementSelected?.Invoke(false); //isMajor = false
        }
    }

    private void LoadImprovements()
    {
        // Для обычных улучшений
        TextAsset jsonFile = Resources.Load<TextAsset>("Improvements");
        Wrapper improvementsWrapper = JsonUtility.FromJson<Wrapper>(jsonFile.text);
        _improvementsDataList = improvementsWrapper.improvements;
        Debug.Log(_improvementsDataList);

        // Для улучшений сумки
        TextAsset newJsonFile = Resources.Load<TextAsset>("BagImprovements");
        Wrapper bagImprovementsWrapper = JsonUtility.FromJson<Wrapper>(newJsonFile.text);
        _bagImprovementsDataList = bagImprovementsWrapper.improvements;
        Debug.Log(_bagImprovementsDataList);
    }
    
    
    private KeyValuePair<LetterData, int> DrawRandomWeakestTile()
    {
        var randomLetter = _letterBag.RandomLetter();
       
        var tilesForLetter = _lettersPool
            .Where(tile => tile.Key.LetterChar == randomLetter.LetterChar)
            .ToList();

        if (!tilesForLetter.Any())
        {
            Debug.LogWarning("No tiles for this letter");
            return default;
        }

        var minPoints = tilesForLetter.Min(tile => tile.Key.Points);
        var weakestTiles = tilesForLetter
            .Where(tile => tile.Key.Points == minPoints)
            .ToList();

        
        var selectedTile = weakestTiles[Random.Range(0, weakestTiles.Count)];
       
        return selectedTile;
    }
    
}

// Вспомогательный класс для хранения вариантов улучшений
public class ImprovementOption
{
    public string EffectType;
    public LetterData TargetLetter;
    public char TargetLetterChar;
    public string Description;
    public string shortDescription;
    public string modifier;
    public bool IsMeta;
}

[System.Serializable]
public class ImprovementData
{
    public int id;
    public string name;
    public string description; // Шаблон с {0}, {1}, {2} для подстановки буквы и очков
    public string shortDescription;
    public string modifier;
    public string effect;
    public string rarity;
    public string[] allowedLetters; // Новое поле (опционально)
    public bool isMeta;
}
[System.Serializable]
public class Wrapper
{
    public ImprovementData[] improvements;
}




