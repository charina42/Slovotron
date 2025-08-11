using UnityEngine;
// using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class ImprovementChosePopup : MonoBehaviour
{
    public static event Action<ImprovementOption> OnCardSelected; 

    [Header("UI Elements")] 
    public GameObject popupPanel;
    public GameObject CardPrefab;
    public Transform CardsParent;
    public TMP_Text wordText; // Поле для отображения слова
    public TMP_Text scoreInfoText; // Поле для информации об очках

    private List<ImprovementOption> _currentOptions;
    private string _lastWord;
    private ScoreManager.ScoreResult _lastScoreResult;

    public void ShowPopup(List<ImprovementOption> options, string word, ScoreManager.ScoreResult scoreResult)
    {
        _currentOptions = options;
        _lastWord = word;
        _lastScoreResult = scoreResult;
        
        popupPanel.SetActive(true);
        UpdateWordAndScoreInfo();
        ShowOptions(options);
    }

    private void UpdateWordAndScoreInfo()
    {
        // // Отображаем слово
        // wordText.text = $"Слово: <color=#FFD700>{_lastWord}</color>";
        //
        // // Формируем информацию об очках
        // string scoreInfo = $"Всего очков: <color=#FFD700>{_lastScoreResult.TotalScore}</color>\n";
        // scoreInfo += $"Базовые очки: {_lastScoreResult.BaseScore}\n";
        //
        // foreach (var bonus in _lastScoreResult.Bonuses)
        // {
        //     scoreInfo += $"+{bonus.Amount} ({bonus.Description})\n";
        // }
        
        scoreInfoText.text = _lastScoreResult.GetFullDescription(_lastWord);
    }

    private void ShowOptions(List<ImprovementOption> options)
    {
        // Очищаем предыдущие карточки
        foreach (Transform child in CardsParent)
        {
            Destroy(child.gameObject);
        }

        // Создаем новые карточки улучшений
        for (int i = 0; i < Mathf.Min(options.Count, 3); i++)
        {
            GameObject cardInstance = Instantiate(CardPrefab, CardsParent);
            var card = cardInstance.GetComponent<ImprovementCard>();
        
            if (card != null)
            {
                card.Initialize(options[i], (option) => {
                    OnCardSelected?.Invoke(option);
                    ClosePopup();
                });
            }
        }
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
        _lastWord = null;
        _lastScoreResult = null;
    }
}