﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LetterBagPopup : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject popupPanel;
    public Transform activeLettersContainer;
    public Transform usedLettersContainer;
    public GameObject letterPrefab;
    public Button closeButton;
    public GameObject backgroundBlocker;
    
    [Header("Tab System")]
    public Button activeLettersTab;
    public Button usedLettersTab;
    public GameObject activeLettersContent;
    public GameObject usedLettersContent;

    private PopupAnimator _popupAnimator;

    private LetterBag _letterBag;

    private void Awake()
    {
        _popupAnimator = popupPanel.GetComponent<PopupAnimator>();
        if (_popupAnimator == null)
        {
            _popupAnimator = popupPanel.AddComponent<PopupAnimator>();
        }
        
        // Подписываемся на событие нажатия кнопки закрытия
        closeButton.onClick.AddListener(ClosePopup);
        
        // Подписываемся на события вкладок
        activeLettersTab.onClick.AddListener(ShowActiveLetters);
        usedLettersTab.onClick.AddListener(ShowUsedLetters);
        
        // Создаем блокировщик если он не установлен
        if (backgroundBlocker == null)
        {
            CreateBackgroundBlocker();
        }
    }
    
    public void Initialize(LetterBag letterBag)
    {
        _letterBag = letterBag;
    }
    
    private void CreateBackgroundBlocker()
    {
        // Создаем объект для блокировки взаимодействия
        backgroundBlocker = new GameObject("BackgroundBlocker");
        backgroundBlocker.transform.SetParent(transform.parent);
        backgroundBlocker.transform.SetAsFirstSibling();
        backgroundBlocker.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        backgroundBlocker.AddComponent<Button>().onClick.AddListener(ClosePopup);
        
        // Устанавливаем растяжение на весь экран
        RectTransform rectTransform = backgroundBlocker.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        backgroundBlocker.SetActive(false);
    }

    // Отображение попапа
    public void ShowPopup()
    {
        // Активируем блокировщик и попап
        if (backgroundBlocker != null)
        {
            backgroundBlocker.SetActive(true);
        }
        popupPanel.SetActive(true);
        
        // Показываем анимацию
        if (_popupAnimator != null)
        {
            _popupAnimator.Show();
        }
        
        // Блокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(false);
        ShowActiveLetters();
    }

    // Закрытие попапа
    public void ClosePopup()
    {
        // Скрываем анимацию
        if (_popupAnimator != null)
        {
            _popupAnimator.Hide();
        }
        
        // Деактивируем блокировщик и попап
        if (backgroundBlocker != null)
        {
            backgroundBlocker.SetActive(false);
        }
        popupPanel.SetActive(false);
        
        // Разблокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(true);

        // Очищаем контейнеры
        ClearContainer(activeLettersContainer);
        ClearContainer(usedLettersContainer);
    }

    private void SetInteractableForOtherUI(bool interactable)
    {
        // Находим все кнопки и другие интерактивные элементы кроме наших
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            if (button != closeButton && 
                button != activeLettersTab && 
                button != usedLettersTab && 
                !button.transform.IsChildOf(popupPanel.transform))
            {
                button.interactable = interactable;
            }
        }
    }

    // Показать вкладку активных букв
    private void ShowActiveLetters()
    {
        activeLettersContent.SetActive(true);
        usedLettersContent.SetActive(false);
        
        UpdateTabAppearance(activeLettersTab, true);
        UpdateTabAppearance(usedLettersTab, false);
        
        UpdateActiveLettersDisplay();
    }

    // Показать вкладку использованных букв
    private void ShowUsedLetters()
    {
        activeLettersContent.SetActive(false);
        usedLettersContent.SetActive(true);
        
        UpdateTabAppearance(activeLettersTab, false);
        UpdateTabAppearance(usedLettersTab, true);
        
        UpdateUsedLettersDisplay();
    }

    // Обновление внешнего вида кнопки вкладки
    private void UpdateTabAppearance(Button tabButton, bool isActive)
    {
        ColorBlock colors = tabButton.colors;
        colors.normalColor = isActive ? new Color(0.8f, 0.8f, 0.8f) : Color.white;
        tabButton.colors = colors;
        
        tabButton.interactable = !isActive;
    }

    // Обновление отображения активных букв
    private void UpdateActiveLettersDisplay()
    {
        ClearContainer(activeLettersContainer);
        var activeLetters = GetActiveLetters();
        DisplayLetters(activeLetters, activeLettersContainer);
    }

    // Обновление отображения использованных букв
    private void UpdateUsedLettersDisplay()
    {
        ClearContainer(usedLettersContainer);
        var usedLetters = GetUsedLetters();
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

    private Dictionary<LetterData, int> GetUsedLetters()
    {
        return _letterBag.GetAllLetters()
            .Where(kvp => _letterBag.GetLetterCount(kvp.Key, LetterLocation.Used) > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => _letterBag.GetLetterCount(kvp.Key, LetterLocation.Used)
            );
    }

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
                    
                    var button = draggable.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = false;
                    }
                }
            }
        }
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnDestroy()
    {
        // Убираем блокировщик при уничтожении объекта
        if (backgroundBlocker != null)
        {
            Destroy(backgroundBlocker);
        }
    }
}