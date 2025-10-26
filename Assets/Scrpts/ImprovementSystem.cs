using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Scrpts;
using Random = UnityEngine.Random;
using YG;

namespace YG
{
    public partial class SavesYG
    {
        public readonly List<ImprovementOption> CurrentImprovementOptions = new List<ImprovementOption>();
        public List<ImprovementOption> ActiveImprovements = new List<ImprovementOption>();
    }
}

public class ImprovementSystem
{
    private LetterBag _letterBag;
    private Dictionary<LetterData, int> _lettersPool;
    private int _maxMetaImprovements;
    
    private BagImprovementOptionBuilder _bagOptionBuilder; 
    
    private ImprovementData[] _improvementsDataList;
    private ImprovementData[] _bagImprovementsDataList;
    
    public static event Action<bool> OnImprovementSelected;

    private ImprovementChosePopup _improvementPopup;
    private MetaImprovementPopup _metaImprovementPopup; 

    public void Initialize(LetterBag letterBag, ImprovementChosePopup improvementPopup, 
        MetaImprovementPopup metaImprovementPopup)
    {
        LoadImprovements();
        _maxMetaImprovements = MetaGameData.MAX_META_IMPROVEMENTS;
        _letterBag = letterBag;
        _improvementPopup = improvementPopup;
        _metaImprovementPopup = metaImprovementPopup; // Инициализируем новый попап
        
        _bagOptionBuilder = new BagImprovementOptionBuilder(_letterBag, null);
        
        ImprovementChosePopup.OnCardSelected += HandleCardSelection;
        ImprovementChosePopup.OnRerollRequested += HandleRerollRequest;
        
        // Добавляем обработчики для мета-попапа
        // MetaImprovementPopup.OnImprovementSelected += HandleCardSelection;
        _metaImprovementPopup.OnRerollRequested += HandleRerollRequest;
    }

    public List<ImprovementOption> GenerateImprovementOptions(bool isMajor, List<ImprovementRarity> rarities)
    {
        Debug.Log("GenerateImprovementOptions");

        return isMajor ?
            ShowMetaImprovementOptions(rarities) :
            ShowBagImprovementOptions(rarities);
    }

    private List<ImprovementOption> ShowMetaImprovementOptions(List<ImprovementRarity> rarities)
    {
        _lettersPool = _letterBag.AllLetterTiles();
        _bagOptionBuilder.UpdateLettersPool(_lettersPool);

        var existingOptions = new List<ImprovementOption>();
        var finalOptions = new List<ImprovementOption>();

        for (var i = 0; i < 3; i++)
        {
            ImprovementOption option = GenerateRandomImprovement(existingOptions, rarities[i]);
        
            if (option != null)
            {
                finalOptions.Add(option);
                existingOptions.Add(option);
                Debug.Log($"Meta ImprovementOption {option.EffectType}");
            }
        }

        YG2.saves.CurrentImprovementOptions.Clear();
        YG2.saves.CurrentImprovementOptions.AddRange(finalOptions);
        return finalOptions;
    }

    private List<ImprovementOption> ShowBagImprovementOptions(List<ImprovementRarity> rarities)
    {
        _lettersPool = _letterBag.AllLetterTiles();
        _bagOptionBuilder.UpdateLettersPool(_lettersPool);

        var existingOptions = new List<ImprovementOption>();
        var finalOptions = new List<ImprovementOption>();

        for (var i = 0; i < 3; i++)
        {
            ImprovementOption option = GenerateRandomBagImprovement(existingOptions, rarities[i]);
        
            if (option != null)
            {
                finalOptions.Add(option);
                existingOptions.Add(option);
                Debug.Log($"Bag ImprovementOption {option.EffectType}");
            }
        }

        YG2.saves.CurrentImprovementOptions.Clear();
        YG2.saves.CurrentImprovementOptions.AddRange(finalOptions);
        
        return finalOptions;
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
            // if (YG2.saves.ActiveImprovements.Any(imp => imp.EffectType == baseImprovement.effect))
            // {
            //     isUnique = false;
            //     attempts++;
            //     continue;
            // }

            // Проверка 2: Улучшение уже есть в текущем наборе?
            if (existingOptions.Any(opt => opt.EffectType == baseImprovement.effect))
            {
                isUnique = false;
                attempts++;
                continue;
            }

            newOption = CreateOptionFromData(baseImprovement, rarity);

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
            return CreateOptionFromData(baseImprovement, rarity);
        }

        // Если все улучшения уже активны, возвращаем любое
        var fallback = _improvementsDataList[Random.Range(0, _improvementsDataList.Length)];
        return CreateOptionFromData(fallback, rarity);
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
    
    private void HandleRerollRequest()
    {
        Debug.Log("Rerolling improvement options");
        
        // Определяем, из какого попапа пришел запрос
        bool isMetaPopup = _metaImprovementPopup != null && _metaImprovementPopup.IsActive();
        bool isRegularPopup = _improvementPopup != null && _improvementPopup.IsActive();
        
        List<ImprovementOption> currentOptions = null;
        
        if (isMetaPopup)
        {
            currentOptions = _metaImprovementPopup?.GetCurrentOptions();
        }
        else if (isRegularPopup)
        {
            currentOptions = _improvementPopup?.GetCurrentOptions();
        }
        
        if (currentOptions == null || currentOptions.Count == 0)
        {
            Debug.LogWarning("No current options available for reroll");
            return;
        }
        
        // Определяем тип улучшений и редкости на основе текущих карточек
        bool isMajor = currentOptions[0].IsMeta;
        var rarities = currentOptions.Select(opt => opt.Rarity).ToList();
        
        var newOptions = GenerateImprovementOptions(isMajor, rarities);
        
        // Обновляем соответствующий попап
        if (isMetaPopup && _metaImprovementPopup != null)
        {
            _metaImprovementPopup.RerollImprovements(newOptions);
        }
        else if (isRegularPopup && _improvementPopup != null)
        {
            _improvementPopup.RerollCards(newOptions);
        }
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
    
    public bool CanAddMoreImprovements()
    {
        return YG2.saves.ActiveImprovements.Count < _maxMetaImprovements;
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
    
    public void Cleanup()
    {
        ImprovementChosePopup.OnCardSelected -= HandleCardSelection;
        ImprovementChosePopup.OnRerollRequested -= HandleRerollRequest;
        
        // Отписываемся от событий мета-попапа
        // MetaImprovementPopup.OnImprovementSelected -= HandleCardSelection;
        _metaImprovementPopup.OnRerollRequested -= HandleRerollRequest;
    }

    private ImprovementOption CreateOptionFromData(ImprovementData improvementData, ImprovementRarity rarity)
    {
        return new ImprovementOption
        {
            EffectType = improvementData.effect,
            Description = improvementData.description,
            ShortDescription = improvementData.shortDescription,
            Modifier = improvementData.modifier,
            ModifierBonus = improvementData.modifierBonus,
            TargetLetterChar = '0',
            IsMeta = true,
            Rarity = rarity
        };
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
    public string ShortDescription;
    public int Modifier;
    public int ModifierBonus;
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
    public int modifier;
    public int modifierBonus;
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