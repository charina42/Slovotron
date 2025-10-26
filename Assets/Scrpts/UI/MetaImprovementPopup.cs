using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class MetaImprovementPopup : MonoBehaviour
{
    public event Action<ImprovementOption> OnImprovementSelected;
    // public event Action<ImprovementOption, ImprovementOption> OnImprovementReplacement;
    public event Action OnRerollRequested;

    [Header("UI Elements")]
    public GameObject popupPanel;
    public GameObject improvementUnitPrefab;
    public Transform improvementsParent;
    public TMP_Text titleText;
    public TMP_Text infoText;
    public Button rerollButton;
    public TMP_Text rerollButtonText;
    public Button cancelButton;
    
    public GameObject backgroundBlocker;
    private PopupAnimator _popupAnimator;

    private List<ImprovementOption> _newOptions;
    private ImprovementOption _selectedImprovement;
    private bool _hasRerollAvailable = true;
    private bool _isActive = false;
    private bool _isReplacementMode = false;

    private void Awake()
    {
        _popupAnimator = popupPanel.GetComponent<PopupAnimator>();
        if (_popupAnimator == null)
        {
            _popupAnimator = popupPanel.AddComponent<PopupAnimator>();
        }
        
        if (backgroundBlocker == null)
        {
            CreateBackgroundBlocker();
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollButtonClicked);
            UpdateRerollButtonState();
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(ClosePopup);
        }
    }
    
    private void CreateBackgroundBlocker()
    {
        backgroundBlocker = new GameObject("BackgroundBlocker");
        backgroundBlocker.transform.SetParent(transform.parent);
        backgroundBlocker.transform.SetAsFirstSibling();
        backgroundBlocker.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        backgroundBlocker.AddComponent<Button>().onClick.AddListener(ClosePopup);
        
        RectTransform rectTransform = backgroundBlocker.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        backgroundBlocker.SetActive(false);
    }

    public void ShowPopup(List<ImprovementOption> newOptions, bool isCanAddMoreImprovements)
    {
        _newOptions = newOptions;
        _selectedImprovement = null;
        _hasRerollAvailable = true;
        _isReplacementMode = false;
        
        if (backgroundBlocker != null)
        {
            // backgroundBlocker.SetActive(true);
        }
        popupPanel.SetActive(true);
        
        if (_popupAnimator != null)
        {
            _popupAnimator.Show();
        }
        
        _isActive = true;
        SetInteractableForOtherUI(false);
        UpdateUI(isCanAddMoreImprovements);
        ShowImprovements(newOptions);
        UpdateRerollButtonState();
    }

    private void UpdateUI(bool isCanAddMoreImprovements)
    {
        if (titleText != null)
        {
            titleText.text = "Выберите улучшение";
        }

        if (infoText == null) return;
        if (!isCanAddMoreImprovements)
        {
            infoText.text = "У вас максимальное количество улучшений. Выберите улучшение для замены или апгрейда существующего.";
        }
        else
        {
            infoText.text = "Выберите улучшение для добавления в панель";
        }
    }

    private void ShowImprovements(List<ImprovementOption> options)
    {
        foreach (Transform child in improvementsParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var option in options)
        {
            GameObject unitInstance = Instantiate(improvementUnitPrefab, improvementsParent);
            var unit = unitInstance.GetComponent<ImprovementUnit>();
        
            if (unit != null)
            {
                unit.Initialize(option);
                
                var button = unitInstance.GetComponent<Button>();
                if (button == null)
                {
                    button = unitInstance.AddComponent<Button>();
                }
                
                button.onClick.AddListener(() =>  OnImprovementSelected?.Invoke(option));
                    // OnImprovementUnitClicked(option, unitInstance));
            }
        }
    }

    // private void OnImprovementUnitClicked(ImprovementOption option, GameObject unitInstance)
    // {
    //     int currentCount = MetaImprovementManager.Instance.GetActiveMetaImprovements().Count;
    //     int maxCount = MetaImprovementManager.Instance.maxMetaImprovements;
    //     
    //     // Если есть свободные слоты - применяем сразу
    //     if (currentCount < maxCount)
    //     {
    //         OnImprovementSelected?.Invoke(option);
    //         ClosePopup();
    //     }
    //     else
    //     {
    //         // Если слотов нет - переходим в режим замены
    //         _selectedImprovement = option;
    //         _isReplacementMode = true;
    //         
    //         // Увеличиваем выбранную карточку
    //         unitInstance.transform.localScale = Vector3.one * 1.1f;
    //         
    //         // Обновляем текст информации
    //         if (infoText != null)
    //         {
    //             infoText.text = "Теперь выберите улучшение для замены на панели";
    //         }
    //         
    //         // Переключаемся на выбор улучшения для замены
    //         SwitchToReplacementMode();
    //     }
    // }

    // private void SwitchToReplacementMode()
    // {
    //     // Активируем все улучшения на панели для выбора замены
    //     var activeImprovements = MetaImprovementManager.Instance.GetActiveMetaImprovements();
    //     
    //     foreach (var improvement in activeImprovements)
    //     {
    //         // Здесь нужно получить GameObject улучшения на панели
    //         // и добавить обработчик клика для замены
    //         // Это зависит от реализации вашей панели улучшений
    //         
    //         // Примерный код:
    //         // improvement.GetGameObject().GetComponent<Button>().onClick.AddListener(() => OnReplacementTargetSelected(improvement));
    //     }
    //     
    //     // Деактивируем кнопку реролла в режиме замены
    //     if (rerollButton != null)
    //     {
    //         rerollButton.interactable = false;
    //     }
    // }
    //
    // // Этот метод будет вызываться при выборе улучшения для замены
    // public void OnReplacementTargetSelected(ImprovementOption targetToReplace)
    // {
    //     if (_isReplacementMode && _selectedImprovement != null)
    //     {
    //         OnImprovementReplacement?.Invoke(_selectedImprovement, targetToReplace);
    //         ClosePopup();
    //     }
    // }

    private void OnRerollButtonClicked()
    {
        if (_hasRerollAvailable && !_isReplacementMode)
        {
            _hasRerollAvailable = false;
            UpdateRerollButtonState();
            OnRerollRequested?.Invoke();
        }
    }

    public void RerollImprovements(List<ImprovementOption> newOptions)
    {
        _newOptions = newOptions;
        _selectedImprovement = null;
        ShowImprovements(newOptions);
    }

    private void UpdateRerollButtonState()
    {
        if (rerollButton != null)
        {
            rerollButton.interactable = _hasRerollAvailable && !_isReplacementMode;
            if (rerollButtonText != null)
            {
                rerollButtonText.text = _hasRerollAvailable ? "Заменить карты" : "Замена использована";
            }
        }
    }

    public void ClosePopup()
    {
        _isActive = false;
        _isReplacementMode = false;
        
        if (_popupAnimator != null)
        {
            _popupAnimator.Hide();
        }
        
        if (backgroundBlocker != null)
        {
            backgroundBlocker.SetActive(false);
        }
        popupPanel.SetActive(false);
        
        SetInteractableForOtherUI(true);
        
        _newOptions = null;
        _selectedImprovement = null;
        _hasRerollAvailable = true;
    }

    private void SetInteractableForOtherUI(bool interactable)
    {
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            if (!button.transform.IsChildOf(popupPanel.transform))
            {
                button.interactable = interactable;
            }
        }
    }

    public List<ImprovementOption> GetCurrentOptions()
    {
        return _newOptions;
    }

    private void OnDestroy()
    {
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