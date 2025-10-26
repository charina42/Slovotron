using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ImprovementChosePopup : MonoBehaviour
{
    public static event Action<ImprovementOption> OnCardSelected; 
    public static event Action OnRerollRequested;

    [Header("UI Elements")] 
    public GameObject popupPanel;
    public GameObject cardPrefab;
    public Transform cardsParent;
    // public TMP_Text wordText;
    // public TMP_Text scoreInfoText;
    public Button rerollButton;
    public TMP_Text rerollButtonText;
    
    public GameObject backgroundBlocker;
    private PopupAnimator _popupAnimator;

    private List<ImprovementOption> _currentOptions;
    // private string _lastWord;
    // private ScoreManager.ScoreResult _lastScoreResult;
    private bool _hasRerollAvailable = true;
    private bool _isActive = false;

    private void Awake()
    {
        _popupAnimator = popupPanel.GetComponent<PopupAnimator>();
        if (_popupAnimator == null)
        {
            _popupAnimator = popupPanel.AddComponent<PopupAnimator>();
        }
        
        // Создаем блокировщик если он не установлен
        if (backgroundBlocker == null)
        {
            CreateBackgroundBlocker();
        }

        // Настраиваем кнопку реролла
        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollButtonClicked);
            UpdateRerollButtonState();
        }
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

    public void ShowPopup(List<ImprovementOption> options)
    {
        _currentOptions = options;
        // _lastWord = word;
        // _lastScoreResult = scoreResult;
        _hasRerollAvailable = true; // Сбрасываем доступность реролла при каждом новом показе
        _isActive = true;
        
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
        // UpdateWordAndScoreInfo();
        ShowOptions(options);
        UpdateRerollButtonState();
    }

    // private void UpdateWordAndScoreInfo()
    // {
    //     scoreInfoText.text = _lastScoreResult.GetFullDescription(_lastWord);
    // }

    private void ShowOptions(List<ImprovementOption> options)
    {
        // Очищаем предыдущие карточки
        foreach (Transform child in cardsParent)
        {
            Destroy(child.gameObject);
        }

        // Создаем новые карточки улучшений
        for (int i = 0; i < Mathf.Min(options.Count, 3); i++)
        {
            GameObject cardInstance = Instantiate(cardPrefab, cardsParent);
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

    private void OnRerollButtonClicked()
    {
        if (_hasRerollAvailable)
        {
            _hasRerollAvailable = false;
            UpdateRerollButtonState();
            OnRerollRequested?.Invoke();
        }
    }

    private void UpdateRerollButtonState()
    {
        if (rerollButton != null)
        {
            rerollButton.interactable = _hasRerollAvailable;
            if (rerollButtonText != null)
            {
                rerollButtonText.text = _hasRerollAvailable ? "Заменить карты" : "Замена использована";
            }
        }
    }

    public void RerollCards(List<ImprovementOption> newOptions)
    {
        _currentOptions = newOptions;
        ShowOptions(newOptions);
    }

    public List<ImprovementOption> GetCurrentOptions()
    {
        return _currentOptions;
    }

    private void ClosePopup()
    {
        _isActive = true;
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
        
        // _lastWord = null;
        // _lastScoreResult = null;
        _hasRerollAvailable = true; // Сбрасываем при закрытии
    }

    private void SetInteractableForOtherUI(bool interactable)
    {
        // Находим все кнопки и другие интерактивные элементы кроме наших
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            if (!button.transform.IsChildOf(popupPanel.transform))
            {
                button.interactable = interactable;
            }
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
    
    public bool IsActive()
    {
        return _isActive;
    }
}