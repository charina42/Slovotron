﻿using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;
using YG;

public class GameWinPopup : MonoBehaviour
{
    public static event Action OnNewGameSelected;

    [SerializeField] private GameObject popup;
     [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private TMP_Text _bestScoreText;
    [SerializeField] private TMP_Text _statisticsText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private LeaderboardYG _leaderboard;
    [SerializeField] private GameObject backgroundBlocker;
    [SerializeField] private PopupAnimator popupAnimator;

    private void Awake()
    {
        popupAnimator = popup.GetComponent<PopupAnimator>();
        if (popupAnimator == null)
        {
            popupAnimator = popup.AddComponent<PopupAnimator>();
        }
        
        if (backgroundBlocker == null)
        {
            CreateBackgroundBlocker();
        }
        
        _restartButton.onClick.AddListener(RestartGame);
        popup.SetActive(false);
    }
    
    private void CreateBackgroundBlocker()
    {
        // Создаем объект для блокировки взаимодействия
        backgroundBlocker = new GameObject("BackgroundBlocker");
        backgroundBlocker.transform.SetParent(transform.parent);
        backgroundBlocker.transform.SetAsFirstSibling();
        backgroundBlocker.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        backgroundBlocker.AddComponent<Button>().onClick.AddListener(Hide);
        
        // Устанавливаем растяжение на весь экран
        RectTransform rectTransform = backgroundBlocker.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void Show(int bestScore, GameStatistics statistics = null)
    {
        // Активируем блокировщик и попап
        if (backgroundBlocker != null)
        {
            backgroundBlocker.SetActive(true);
        }
        popup.SetActive(true);
        
        // Показываем анимацию
        if (popupAnimator != null)
        {
            popupAnimator.Show();
        }
        
        // Блокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(false);
        
        _bestScoreText.text = $"Ваш счет: {bestScore}";
        
        // Показываем статистику, если она предоставлена
        if (statistics != null)
        {
            _statisticsText.text = GetStatisticsText(statistics);
        }
        else
        {
            _statisticsText.text = "Статистика недоступна";
        }
        
        // Обновляем лидерборд
        if (_leaderboard != null)
        {
            YG2.GetLeaderboard(_leaderboard.nameLB);
        }
    }

    private void Hide()
    {
        // Скрываем анимацию
        if (popupAnimator != null)
        {
            popupAnimator.Hide();
        }
        
        // Деактивируем блокировщик и попап
        if (backgroundBlocker != null)
        {
            backgroundBlocker.SetActive(false);
        }
        
        // Разблокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(true);
    }

    private void SetInteractableForOtherUI(bool interactable)
    {
        // Находим все кнопки и другие интерактивные элементы кроме наших
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            if (button != _restartButton && 
                button.gameObject != backgroundBlocker && 
                !button.transform.IsChildOf(popup.transform))
            {
                button.interactable = interactable;
            }
        }
    }

    private string GetStatisticsText(GameStatistics statistics)
    {
        string stats = "=== СТАТИСТИКА ИГРЫ ===\n";
        stats += $"Всего слов: {statistics.totalWords}\n";
        stats += $"Общий счет: {statistics.totalScore}\n";
        
        if (statistics.totalWords > 0)
        {
            stats += $"Самое длинное слово: {statistics.longestWord} ({statistics.longestWordLength} букв)\n";
            stats += $"Лучшее слово: {statistics.highestScoringWord} ({statistics.highestWordScore} очков)\n";
        }
        else
        {
            stats += "Слова не были составлены\n";
        }
        
        return stats;
    }

    private void RestartGame()
    {
        Hide();
        OnNewGameSelected?.Invoke();
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