﻿using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class ImprovementCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Image rarityBackground;
    [SerializeField] private Transform letterTilesContainer;
    [SerializeField] private GameObject letterPrefab;
    [SerializeField] private GameObject arrowSeparatorPrefab; // Префаб для стрелки-разделителя
    
    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.gray;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;
    
    private ImprovementOption _option;
    private System.Action<ImprovementOption> _onSelectCallback;

    public void Initialize(ImprovementOption option, System.Action<ImprovementOption> onSelect)
    {
        _option = option;
        _onSelectCallback = onSelect;
        descriptionText.text = option.Description;
        
        // Устанавливаем редкость
        SetRarityDisplay(option.Rarity);
        
        // Очищаем контейнер от предыдущих тайлов
        foreach (Transform child in letterTilesContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Создаем визуальное представление букв в зависимости от типа улучшения
        CreateLetterTilesVisualization(option);
    }

    private void SetRarityDisplay(ImprovementRarity rarity)
    {
        string rarityString = "";
        Color color = Color.white;
        
        switch (rarity)
        {
            case ImprovementRarity.Common:
                rarityString = "Обычное";
                color = commonColor;
                break;
            case ImprovementRarity.Rare:
                rarityString = "Редкое";
                color = rareColor;
                break;
            case ImprovementRarity.Epic:
                rarityString = "Эпическое";
                color = epicColor;
                break;
            case ImprovementRarity.Legendary:
                rarityString = "Легендарное";
                color = legendaryColor;
                break;
        }
        
        rarityText.text = rarityString;
        rarityText.color = color;
        
        // Опционально: можно изменить фон карточки в зависимости от редкости
        if (rarityBackground != null)
        {
            rarityBackground.color = new Color(color.r, color.g, color.b, 0.1f); // Полупрозрачный фон
        }
    }

    private void CreateLetterTilesVisualization(ImprovementOption option)
    {
        switch (option.EffectType)
        {
            case "DoublePoints2":
            case "DoublePoints3":
            case "DoublePoints45":
                CreateDoublePointsVisualization(option);
                break;
                
            case "AddOnePointTo4":
            case "AddOnePointTo8":
                CreateAddOnePointVisualization(option);
                break;
                
            case "CapitalLetter":
            case "FinalLetter":
                CreateSpecialLetterVisualization(option);
                break;
                
            case "AddWildcard":
                CreateWildcardVisualization(option);
                break;
                
            case "DisposableTile":
                CreateDisposableTileVisualization(option);
                break;
                
            case "AddDisposableWildcard":
                CreateDisposableWildcardVisualization(option);
                break;
                
            case "RepeaterLetter":
                CreateRepeaterVisualization(option);
                break;
                
            case "Merge2Tiles":
            case "Merge3Tiles":
            case "Merge4Tiles":
                CreateMergeTilesVisualization(option);
                break;
                
            case "Multiply2Tiles":
            case "Multiply3Tiles":
            case "Multiply4Tiles":
                CreateMultiplyTilesVisualization(option);
                break;
                
            case "NeighborMultiplier":
                CreateNeighborMultiplierVisualization(option);
                break;
                
            case "ReturnLetter":
                CreateReturnLetterVisualization(option);
                break;
                
            default:
                // Для обычных улучшений без букв
                break;
        }
    }

    private void CreateDoublePointsVisualization(ImprovementOption option)
    {
        if (option.TargetLetter != null && option.TargetLetter.Count > 0)
        {
            var originalLetter = option.TargetLetter[0];
            var doubledLetter = new LetterData(originalLetter.LetterChar, originalLetter.Points * 2, originalLetter.Type);
            
            // Создаем вертикальную группу: исходная буква сверху, стрелка, новая буква снизу
            CreateVerticalGroup(
                () => CreateLetterTile(originalLetter),
                () => CreateLetterTile(doubledLetter)
            );
        }
    }

    private void CreateAddOnePointVisualization(ImprovementOption option)
    {
        if (option.TargetLetter != null && option.TargetLetter.Count > 0)
        {
            // Для AddOnePointTo8 показываем только итоговые буквы
            if (option.EffectType == "AddOnePointTo8")
            {
                CreateHorizontalGroup(option.TargetLetter, 
                    letter => CreateLetterTile(new LetterData(letter.LetterChar, letter.Points + 1, letter.Type)));
            }
            else
            {
                // Для других улучшений показываем обе строки (исходные и улучшенные)
                CreateVerticalGroup(
                    // Верхняя строка: исходные буквы
                    () => CreateHorizontalGroup(option.TargetLetter, CreateLetterTile),
                    // Нижняя строка: улучшенные буквы
                    () => CreateHorizontalGroup(option.TargetLetter, 
                        letter => CreateLetterTile(new LetterData(letter.LetterChar, letter.Points + 1, letter.Type)))
                );
            }
        }
    }

    private void CreateSpecialLetterVisualization(ImprovementOption option)
    {
        var letterType = option.EffectType == "CapitalLetter" ? LetterType.Capital : LetterType.Final;
        var basePoints = 5; // Базовые очки для специальных букв
        
        var specialLetter = new LetterData(option.TargetLetterChar, basePoints, letterType);
        CreateLetterTile(specialLetter);
    }

    private void CreateWildcardVisualization(ImprovementOption option)
    {
        var wildcard = new LetterData('*', 0, LetterType.Wild);
        CreateLetterTile(wildcard);
    }

    private void CreateDisposableTileVisualization(ImprovementOption option)
    {
        var disposableLetter = new LetterData(option.TargetLetterChar, option.TargetLetterPoints, LetterType.Disposable);
        CreateLetterTile(disposableLetter);
    }

    private void CreateDisposableWildcardVisualization(ImprovementOption option)
    {
        var disposableWildcard = new LetterData('*', 0, LetterType.Disposable);
        CreateLetterTile(disposableWildcard);
    }

    private void CreateRepeaterVisualization(ImprovementOption option)
    {
        var repeaterLetter = new LetterData(option.TargetLetterChar, option.TargetLetterPoints, LetterType.Repeater);
        CreateLetterTile(repeaterLetter);
    }

    private void CreateMergeTilesVisualization(ImprovementOption option)
    {
        if (option.TargetLetter != null && option.TargetLetter.Count > 0)
        {
            CreateVerticalGroup(
                // Верхняя строка: исходные буквы
                () => CreateHorizontalGroup(option.TargetLetter, CreateLetterTile),
                // Нижняя строка: результат слияния
                () => CreateLetterTile(new LetterData(
                    option.TargetLetter[0].LetterChar,
                    option.TargetLetter.Sum(l => l.Points),
                    LetterType.Standard
                ))
            );
        }
    }

    private void CreateMultiplyTilesVisualization(ImprovementOption option)
    {
        int multiplier = option.EffectType switch
        {
            "Multiply2Tiles" => 2,
            "Multiply3Tiles" => 3,
            "Multiply4Tiles" => 4,
            _ => 1
        };
        
        var baseLetter = new LetterData(option.TargetLetterChar, option.TargetLetterPoints, LetterType.Standard);
        
        CreateVerticalGroup(
            // Верхняя строка: исходная буква
            () => CreateLetterTile(baseLetter),
            // Нижняя строка: умноженные буквы
            () => CreateHorizontalGroup(multiplier, i => CreateLetterTile(baseLetter))
        );
    }

    private void CreateNeighborMultiplierVisualization(ImprovementOption option)
    {
        var multiplierType = option.modifier == "Left" ? 
            LetterType.NeighborMultiplierLeft : LetterType.NeighborMultiplierRight;
        
        var multiplierLetter = new LetterData(option.TargetLetterChar, 0, multiplierType);
        CreateLetterTile(multiplierLetter);
    }

    private void CreateReturnLetterVisualization(ImprovementOption option)
    {
        var returnLetter = new LetterData(option.TargetLetterChar, option.TargetLetterPoints, LetterType.Return);
        CreateLetterTile(returnLetter);
    }

    // Создает вертикальную группу с разделителем-стрелкой
    private void CreateVerticalGroup(System.Action createTopRow, System.Action createBottomRow)
{
    // var verticalGroup = new GameObject("VerticalGroup");
    // verticalGroup.transform.SetParent(letterTilesContainer, false);
    //
    // // Добавляем и настраиваем RectTransform для растягивания по центру
    // var rectTransform = verticalGroup.AddComponent<RectTransform>();
    // rectTransform.anchorMin = new Vector2(0f, 0f);
    // rectTransform.anchorMax = new Vector2(1f, 1f);
    // rectTransform.pivot = new Vector2(0.5f, 0.5f);
    // rectTransform.sizeDelta = new Vector2(200, 100); // Начальный размер
    //
    // // Добавляем ContentSizeFitter для автоматического размера
    // // var sizeFitter = verticalGroup.AddComponent<ContentSizeFitter>();
    // // sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    // // sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    //
    // var verticalLayout = verticalGroup.AddComponent<VerticalLayoutGroup>();
    // verticalLayout.childAlignment = TextAnchor.MiddleCenter;
    // verticalLayout.spacing = 5f;
    // verticalLayout.childControlHeight = false;
    // verticalLayout.childControlWidth = false;
    // verticalLayout.childForceExpandHeight = false;
    // verticalLayout.childForceExpandWidth = false;
    
    // Верхняя строка
    var topRow = new GameObject("TopRow");
    topRow.transform.SetParent(letterTilesContainer, false);
    var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
    topLayout.childAlignment = TextAnchor.MiddleCenter;
    topLayout.spacing = 2f;
    topLayout.childControlHeight = false;
    topLayout.childControlWidth = false;
    topLayout.childForceExpandHeight = false;
    topLayout.childForceExpandWidth = false;
    
    createTopRow?.Invoke();
    
    // Стрелка-разделитель
    if (arrowSeparatorPrefab != null)
    {
        var arrow = Instantiate(arrowSeparatorPrefab, letterTilesContainer, false);
        arrow.transform.localScale = Vector3.one * 0.8f;
    }
    else
    {
        CreateArrowSeparator(letterTilesContainer);
    }
    
    // Нижняя строка
    var bottomRow = new GameObject("BottomRow");
    bottomRow.transform.SetParent(letterTilesContainer, false);
    var bottomLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
    bottomLayout.childAlignment = TextAnchor.MiddleCenter;
    bottomLayout.spacing = 2f;
    bottomLayout.childControlHeight = false;
    bottomLayout.childControlWidth = false;
    bottomLayout.childForceExpandHeight = false;
    bottomLayout.childForceExpandWidth = false;
    
    createBottomRow?.Invoke();
}

    // Создает горизонтальную группу букв
    private void CreateHorizontalGroup<T>(List<T> items, System.Func<T, GameObject> createItem)
    {
        foreach (var item in items)
        {
            GameObject createdItem = createItem(item);
            
            // if (items.IndexOf(item) < items.Count - 1)
            // {
            //     GameObject plus = CreatePlus();
            // }
        }
    }

    // Создает горизонтальную группу с повторяющимися элементами
    private void CreateHorizontalGroup(int count, System.Func<int, GameObject> createItem)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject createdItem = createItem(i);
            
            // if (i < count - 1)
            // {
            //     GameObject plus = CreatePlus();
            // }
        }
    }

    private GameObject CreateLetterTile(LetterData letterData)
    {
        Debug.Log("Create LetterTile");
        var letterObj = Instantiate(letterPrefab);
        var letterTile = letterObj.GetComponent<LetterTile>();

        if (letterTile == null)
        {
            Debug.LogWarning("LetterTile is null");
        }
        else 
        {
            letterTile.SetText(letterData);
            // Уменьшаем масштаб для лучшего отображения в карточке
            letterObj.transform.localScale = Vector3.one * 0.7f;
        }
        
        // Устанавливаем родителя - находим текущий активный контейнер
        Transform currentContainer = FindCurrentContainer();
        letterObj.transform.SetParent(currentContainer ?? letterTilesContainer, false);
        
        return letterObj;
    }

    // Вспомогательная функция для поиска текущего контейнера
    private Transform FindCurrentContainer()
    {
        // Ищем последний созданный контейнер строки в основном контейнере
        if (letterTilesContainer.childCount > 0)
        {
            Transform lastChild = letterTilesContainer.GetChild(letterTilesContainer.childCount - 1);
            
            // Если это вертикальная группа, ищем в ней строки
            if (lastChild.name == "VerticalGroup" && lastChild.childCount > 0)
            {
                // Возвращаем последнюю строку в вертикальной группе
                Transform lastRow = lastChild.GetChild(lastChild.childCount - 1);
                if (lastRow.name == "TopRow" || lastRow.name == "BottomRow")
                {
                    return lastRow;
                }
            }
            // Если это отдельная строка
            else if (lastChild.name == "TopRow" || lastChild.name == "BottomRow")
            {
                return lastChild;
            }
        }
        return null;
    }

    private GameObject CreatePlus()
    {
        var plus = new GameObject("Plus");
        var text = plus.AddComponent<TextMeshProUGUI>();
        text.text = "+";
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.Center;
        
        // Устанавливаем родителя для плюса
        Transform currentContainer = FindCurrentContainer();
        plus.transform.SetParent(currentContainer ?? letterTilesContainer, false);
        
        return plus;
    }

    private void CreateArrowSeparator(Transform parent)
    {
        var arrow = new GameObject("ArrowSeparator");
        arrow.transform.SetParent(parent, false);
        var text = arrow.AddComponent<TextMeshProUGUI>();
        text.text = "↓"; // Стрелка вниз
        text.fontSize = 16;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.gray;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onSelectCallback?.Invoke(_option);
    }
}