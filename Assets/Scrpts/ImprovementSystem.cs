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

    public void Initialize(LetterBag letterBag)
    {
        LoadImprovements();
        _letterBag = letterBag;
        
        _bagOptionBuilder = new BagImprovementOptionBuilder(_letterBag, null);
        
        ImprovementChosePopup.OnCardSelected += HandleCardSelection;
    }


    public List<ImprovementOption> ShowImprovementOptions(bool isMajor, List<ImprovementRarity> rarities)
    {
        Debug.Log("ShowImprovementOptions");

        _lettersPool = _letterBag.AllLetterTiles();
        _bagOptionBuilder.UpdateLettersPool(_lettersPool);

        var existingOptions = new List<ImprovementOption>();
        var finalOptions = new List<ImprovementOption>();

        for (var i = 0; i < 3; i++)
        {
            ImprovementOption option;
            int attempt = 0;
    
            do
            {
                if (isMajor)
                {
                    option = GenerateRandomImprovement(existingOptions, rarities[i]);
                }
                else
                {
                    option = GenerateRandomBagImprovement(existingOptions, rarities[i]);
                }
        
                attempt++;
        
                // Если после 5 попыток не нашли валидное улучшение, используем fallback
                if (option == null && attempt >= 5)
                {
                    if (isMajor)
                    {
                        option = GetFallbackMetaImprovement(rarities[i]);
                    }
                    else
                    {
                        option = GetFallbackImprovement(rarities[i]);
                    }
                }
        
            } while (option == null && attempt < 10); // Максимум 10 попыток

            if (option != null)
            {
                finalOptions.Add(option);
                existingOptions.Add(option);
                Debug.Log($"ImprovementOption {option.EffectType}");
            }
        }

        YG2.saves.CurrentImprovementOptions.Clear();
        YG2.saves.CurrentImprovementOptions.AddRange(finalOptions);
        Debug.Log("ImprovementOptions have been added");
        return finalOptions;
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
        ImprovementOption newOption = null;
        bool isUnique = false;
        int attempts = 0;
        const int maxAttempts = 50;
        const int fallbackAttempts = 30;

        do
        {
            var baseImprovement = GetRandomImprovementByRarity(_bagImprovementsDataList.ToList(), rarity);
        
            if (baseImprovement != null)
            {
                newOption = _bagOptionBuilder.BuildFromImprovementData(baseImprovement);
            
                if (newOption == null && attempts > fallbackAttempts)
                {
                    // Пробуем улучшения, которые добавляют буквы (не требуют существующих)
                    var additiveImprovements = _bagImprovementsDataList
                        .Where(imp => IsAdditiveImprovement(imp.effect))
                        .ToList();
                    
                    if (additiveImprovements.Any())
                    {
                        baseImprovement = GetRandomImprovementByRarity(additiveImprovements, rarity);
                        newOption = _bagOptionBuilder.BuildFromImprovementData(baseImprovement);
                    }
                }

                if (newOption != null)
                {
                    newOption.Rarity = rarity;

                    // Проверка на уникальность
                    isUnique = existingOptions.All(option => 
                        newOption.EffectType != option.EffectType || 
                        newOption.TargetLetterChar != option.TargetLetterChar);
                }
            }

            attempts++;
            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("Не удалось сгенерировать улучшение после " + maxAttempts + " попыток.");
                return GetFallbackImprovement(rarity); // Запасной вариант
            }
        } while (!isUnique || newOption == null);

        return newOption;
    }

    private ImprovementOption GenerateRandomImprovement(List<ImprovementOption> existingOptions, ImprovementRarity rarity)
{
    ImprovementData baseImprovement;
    ImprovementOption newOption = null;
    bool isUnique;
    int attempts = 0;
    const int maxAttempts = 50;
    const int fallbackAttempts = 30; // После этого количества попыток начинаем пробовать другие редкости

    do
    {
        isUnique = true;
        
        // После fallbackAttempts попыток пробуем улучшения других редкостей
        if (attempts > fallbackAttempts)
        {
            var allRarities = Enum.GetValues(typeof(ImprovementRarity)).Cast<ImprovementRarity>().ToList();
            var randomRarity = allRarities[Random.Range(0, allRarities.Count)];
            baseImprovement = GetRandomImprovementByRarity(_improvementsDataList.ToList(), randomRarity);
        }
        else
        {
            baseImprovement = GetRandomImprovementByRarity(_improvementsDataList.ToList(), rarity);
        }

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
            return GetFallbackMetaImprovement(rarity); // Запасной вариант для мета-улучшений
        }
    } while (!isUnique);

    return newOption;
}

// Запасное мета-улучшение
    private ImprovementOption GetFallbackMetaImprovement(ImprovementRarity rarity)
    {
        // Ищем любое улучшение, которое еще не активно
        var availableImprovements = _improvementsDataList
            .Where(imp => !YG2.saves.ActiveImprovements.Any(active => active.EffectType == imp.effect))
            .ToList();

        if (availableImprovements.Any())
        {
            var baseImprovement = availableImprovements[Random.Range(0, availableImprovements.Count)];
            return new ImprovementOption
            {
                EffectType = baseImprovement.effect,
                Description = baseImprovement.description,
                shortDescription = baseImprovement.shortDescription,
                modifier = baseImprovement.modifier,
                TargetLetterChar = '0',
                IsMeta = true,
                Rarity = rarity
            };
        }

        // Если все улучшения уже активны, возвращаем любое
        var fallback = _improvementsDataList[Random.Range(0, _improvementsDataList.Length)];
        return new ImprovementOption
        {
            EffectType = fallback.effect,
            Description = fallback.description,
            shortDescription = fallback.shortDescription,
            modifier = fallback.modifier,
            TargetLetterChar = '0',
            IsMeta = true,
            Rarity = rarity
        };
    }

    // Проверяет, добавляет ли улучшение новые буквы (не требует существующих)
    private bool IsAdditiveImprovement(string effectType)
    {
        var additiveEffects = new[]
        {
            "CapitalLetter", "FinalLetter", "AddWildcard", "AddDisposableWildcard",
            "DisposableTile", "RepeaterLetter", "NeighborMultiplier", "ReturnLetter"
        };
    
        return additiveEffects.Contains(effectType);
    }

// Запасное улучшение, которое всегда доступно
    private ImprovementOption GetFallbackImprovement(ImprovementRarity rarity)
    {
        // Улучшения, которые всегда можно применить
        var fallbackEffects = new[]
        {
            "AddWildcard", "AddDisposableWildcard", "CapitalLetter", "FinalLetter"
        };
    
        var availableEffects = _bagImprovementsDataList
            .Where(imp => fallbackEffects.Contains(imp.effect))
            .ToList();
        
        if (availableEffects.Any())
        {
            var baseImprovement = GetRandomImprovementByRarity(availableEffects, rarity);
            return _bagOptionBuilder.BuildFromImprovementData(baseImprovement);
        }
    
        // Если ничего не найдено, создаем базовое улучшение
        return new ImprovementOption
        {
            EffectType = "AddWildcard",
            TargetLetterChar = '*',
            Description = "Добавляет wildcard-символ в сумку",
            IsMeta = false,
            Rarity = rarity
        };
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
    public int TargetLetterPoints;
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