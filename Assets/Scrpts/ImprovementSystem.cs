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
    
    private BagImprovementOptionBuilder _bagOptionBuilder; 
    
    private ImprovementData[] _improvementsDataList;
    private ImprovementData[] _bagImprovementsDataList;
    
    public static event Action<bool> OnImprovementSelected;
    
    // private readonly Dictionary<string, int> _rarityWeights = new Dictionary<string, int>
    // {
    //     { "Common", 70 }, // 70% шанс
    //     { "Rare", 25 }, // 25% шанс
    //     { "Epic", 10 } // 5% шанс
    // };

    public void Initialize(LetterBag letterBag)
    {
        LoadImprovements();
        _letterBag = letterBag;
        
        _bagOptionBuilder = new BagImprovementOptionBuilder(_letterBag, null);
        
        ImprovementChosePopup.OnCardSelected += HandleCardSelection;
    }


    public List<ImprovementOption> ShowImprovementOptions(bool isMajor, List<ImprovementRarity> rarities )
    {
        Debug.Log("ShowImprovementOptions");
        if (rarities == null)
        {
            Debug.LogWarning("Rarities are null");
            rarities = new List<ImprovementRarity> { 
                ImprovementRarity.Common, 
                ImprovementRarity.Rare, 
                ImprovementRarity.Epic 
            };
        }
        else
        {
            Debug.Log($"{rarities[0]} + {rarities[1]} + {rarities[2]}");
        }
        
        _lettersPool = _letterBag.AllLetterTiles();
        _bagOptionBuilder.UpdateLettersPool(_lettersPool);
        var existingOptions = new List<ImprovementOption>();

        for (var i = 0; i < 3; i++)
        {
            ImprovementOption option;
            if (isMajor)
            {
                option = GenerateRandomImprovement(existingOptions, rarities[i]);
            }
            else
            {
                option = GenerateRandomBagImprovement(existingOptions, rarities[i]);
            }
            
            Debug.Log($"ImprovementOption {option.EffectType}");
            
            YG2.saves.CurrentImprovementOptions.Add(option);
            existingOptions.Add(option);
        }

        return YG2.saves.CurrentImprovementOptions;
    }

    public List<ImprovementOption> ShowBagImprovementOptions(float contributionPercentage)
    {
        return YG2.saves.CurrentImprovementOptions;
    }

    private ImprovementData GetRandomImprovementByRarity(List<ImprovementData> improvements, ImprovementRarity rarity)
    {
        // Конвертируем enum в строку для сравнения
        string rarityString = rarity.ToString();
        
        // Фильтруем улучшения по нужной редкости
        var filteredImprovements = improvements
            .Where(imp => imp.rarity == rarityString)
            .ToList();

        if (!filteredImprovements.Any())
        {
            Debug.LogWarning($"No improvements found for rarity: {rarity}");
            return improvements[Random.Range(0, improvements.Count)]; // Fallback
        }

        return filteredImprovements[Random.Range(0, filteredImprovements.Count)];
    }

    private ImprovementOption GenerateRandomBagImprovement(List<ImprovementOption> existingOptions, ImprovementRarity rarity)
    {
        ImprovementOption newOption;
        bool isUnique;
        int attempts = 0;
        const int maxAttempts = 50; // Защита от бесконечного цикла

        do
        {
            var baseImprovement = GetRandomImprovementByRarity(_bagImprovementsDataList.ToList(), rarity);
            Debug.Log($"Random base Improvement {baseImprovement.effect}");
            
            newOption = _bagOptionBuilder.BuildFromImprovementData(baseImprovement);
            newOption.Rarity = rarity;

            // Проверка на уникальность
            isUnique = existingOptions.All(option => newOption.EffectType != option.EffectType 
                                                     || newOption.TargetLetterChar != option.TargetLetterChar);

            attempts++;
            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("Не удалось сгенерировать уникальное улучшение после " + maxAttempts + " попыток.");
                return newOption; // Возвращаем даже если не уникально, чтобы избежать бесконечного цикла
            }
        } while (!isUnique);

        return newOption;
    }

    private ImprovementOption GenerateRandomImprovement(List<ImprovementOption> existingOptions, ImprovementRarity rarity)
    {
        ImprovementData baseImprovement;
        ImprovementOption newOption = null;
        bool isUnique;
        int attempts = 0;
        const int maxAttempts = 50;

        do
        {
            isUnique = true;
            baseImprovement = GetRandomImprovementByRarity(_improvementsDataList.ToList(), rarity);

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
                IsMeta = true,
                Rarity = rarity
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
            _bagOptionBuilder.HandleImprovementEffect(option);
            // switch (option.EffectType)
            // {
            //     case "DoublePoints":
            //         _letterBag.DoublePointsForOneLetter(option.TargetLetter);
            //         break;
            //
            //     case "AddOnePointToAll":
            //         _letterBag.IncreasePointsForWeakestLetter(option.TargetLetter);
            //         break;
            //
            //     case "CapitalLetter":
            //         _letterBag.AddCapitalLetterToPool(option.TargetLetterChar);
            //         break;
            //
            //     case "FinalLetter":
            //         _letterBag.AddFinalLetterToPool(option.TargetLetterChar);
            //         break;
            //
            //     case "AddWildcard":
            //         _letterBag.AddWildSymbolToPool();
            //         break;
            // }

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

        // Для улучшений сумки
        TextAsset newJsonFile = Resources.Load<TextAsset>("BagImprovements");
        Wrapper bagImprovementsWrapper = JsonUtility.FromJson<Wrapper>(newJsonFile.text);
        _bagImprovementsDataList = bagImprovementsWrapper.improvements;
    }
    
    
    // private KeyValuePair<LetterData, int> DrawRandomWeakestTile()
    // {
    //     var randomLetter = _letterBag.RandomLetter();
    //    
    //     var tilesForLetter = _lettersPool
    //         .Where(tile => tile.Key.LetterChar == randomLetter.LetterChar)
    //         .ToList();
    //
    //     if (!tilesForLetter.Any())
    //     {
    //         Debug.LogWarning("No tiles for this letter");
    //         return default;
    //     }
    //
    //     var minPoints = tilesForLetter.Min(tile => tile.Key.Points);
    //     var weakestTiles = tilesForLetter
    //         .Where(tile => tile.Key.Points == minPoints)
    //         .ToList();
    //
    //     
    //     var selectedTile = weakestTiles[Random.Range(0, weakestTiles.Count)];
    //    
    //     return selectedTile;
    // }
    
    public List<ImprovementRarity> GetWordRarities(float contributionPercentage)
    {
        List<ImprovementRarity> rarities = new List<ImprovementRarity>();

        if (contributionPercentage <= 25f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Common, ImprovementRarity.Common, ImprovementRarity.Common });
        }
        else if (contributionPercentage >= 26f && contributionPercentage <= 35f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Common, ImprovementRarity.Common, ImprovementRarity.Rare });
        }
        else if (contributionPercentage >= 36f && contributionPercentage <= 50f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Rare, ImprovementRarity.Rare, ImprovementRarity.Rare });
        }
        else if (contributionPercentage >= 51f && contributionPercentage <= 75f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Rare, ImprovementRarity.Rare, ImprovementRarity.Epic });
        }
        else if (contributionPercentage >= 76f && contributionPercentage <= 100f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Rare, ImprovementRarity.Epic, ImprovementRarity.Epic });
        }
        else if (contributionPercentage > 100f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Epic, ImprovementRarity.Epic, ImprovementRarity.Epic });
        }

        return rarities;
    }   
}

// Вспомогательный класс для хранения вариантов улучшений
public class ImprovementOption
{
    public string EffectType;
    public List<LetterData> TargetLetter;
    public char TargetLetterChar;
    public char TargetLetterPoints;
    public string Description;
    public string shortDescription;
    public string modifier;
    public bool IsMeta;
    public ImprovementRarity Rarity;
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
    public string[] allowedLetters; 
    // public bool isMeta;
}
[System.Serializable]
public class Wrapper
{
    public ImprovementData[] improvements;
}

public enum ImprovementRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}