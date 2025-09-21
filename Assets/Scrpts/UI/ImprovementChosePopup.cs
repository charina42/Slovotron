using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class ImprovementChosePopup : MonoBehaviour
{
    public static event Action<ImprovementOption> OnCardSelected; 

    [Header("UI Elements")] 
    public GameObject popupPanel;
    public GameObject CardPrefab;
    public Transform CardsParent;
    public TMP_Text wordText;
    public TMP_Text scoreInfoText;
    public GameObject backgroundBlocker;
    [SerializeField] private PopupAnimator popupAnimator;

    private List<ImprovementOption> _currentOptions;
    private string _lastWord;
    private ScoreManager.ScoreResult _lastScoreResult;

    private void Awake()
    {
        // Создаем блокировщик если он не установлен
        if (backgroundBlocker == null)
        {
            CreateBackgroundBlocker();
        }
    }
    
    public void Initialize(PopupAnimator popupAnimator)
    {
        this.popupAnimator = popupAnimator;
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

    public void ShowPopup(List<ImprovementOption> options, string word, ScoreManager.ScoreResult scoreResult)
    {
        _currentOptions = options;
        _lastWord = word;
        _lastScoreResult = scoreResult;
        
        // Активируем блокировщик и попап
        if (backgroundBlocker != null)
        {
            backgroundBlocker.SetActive(true);
        }
        popupPanel.SetActive(true);
        
        // Показываем анимацию
        if (popupAnimator != null)
        {
            popupAnimator.Show();
        }
        
        // Блокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(false);
        UpdateWordAndScoreInfo();
        ShowOptions(options);
    }

    private void UpdateWordAndScoreInfo()
    {
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
        popupPanel.SetActive(false);
        
        // Разблокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(true);
        
        _lastWord = null;
        _lastScoreResult = null;
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
}