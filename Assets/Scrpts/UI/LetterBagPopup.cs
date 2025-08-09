using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LetterBagPopup : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject popupPanel; // Панель, содержащая попап
    public Transform activeLettersContainer; // Контейнер для активных букв
    public Transform usedLettersContainer; // Контейнер для использованных букв
    public GameObject letterPrefab; // Префаб для отображения буквы (текстовый объект)
    public Button closeButton; // Кнопка закрытия попапа

    private LetterBag _letterBag; // Ссылка на экземпляр LetterBag

    private void Awake()
    {
        // Подписываемся на событие нажатия кнопки закрытия
        closeButton.onClick.AddListener(ClosePopup);
    }

    // Инициализация попапа с данными из LetterBag
    public void Initialize(LetterBag letterBag)
    {
        _letterBag = letterBag;
    }

    // Отображение попапа
    public void ShowPopup()
    {
        popupPanel.SetActive(true);
        UpdateLettersDisplay();
    }

    // Закрытие попапа
    public void ClosePopup()
    {
        popupPanel.SetActive(false);

        // Очищаем контейнеры
        ClearContainer(activeLettersContainer);
        ClearContainer(usedLettersContainer);
    }

    // Обновление отображения букв в попапе
    private void UpdateLettersDisplay()
    {
        var activeLetters = GetActiveLetters();
        var usedLetters = GetUsedLetters();

        DisplayLetters(activeLetters, activeLettersContainer);
        DisplayLetters(usedLetters, usedLettersContainer);
    }

    private Dictionary<LetterData, int> GetActiveLetters()
    {
        return _letterBag.GetAllLetters()
            .Where(kvp => 
                _letterBag.GetLetterCount(kvp.Key, LetterLocation.InBag) > 0 
            )
            .ToDictionary(
                kvp => kvp.Key,
                kvp => _letterBag.GetLetterCount(kvp.Key, LetterLocation.InBag) 
            );
    }

    // Получение словаря с использованными буквами и их количеством
    private Dictionary<LetterData, int> GetUsedLetters()
    {
        return _letterBag.GetAllLetters()
            .Where(kvp => _letterBag.GetLetterCount(kvp.Key, LetterLocation.Used) > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => _letterBag.GetLetterCount(kvp.Key, LetterLocation.Used)
            );
    }

    // Отображение букв в заданном контейнере
    private void DisplayLetters(Dictionary<LetterData, int> letters, Transform container)
    {
        foreach (var letterData in letters)
        {
            for (int i = 0; i < letterData.Value; i++)
            {
                GameObject letterObject = Instantiate(letterPrefab, container);

                LetterTile draggable = letterObject.GetComponent<LetterTile>();
                if (draggable != null)
                {
                    draggable.SetText(letterData.Key);
                }
            }
        }
    }

    // Очистка контейнера от всех дочерних объектов
    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}