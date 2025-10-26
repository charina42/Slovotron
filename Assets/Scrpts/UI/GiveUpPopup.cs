using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class GiveUpPopup : MonoBehaviour
{
    public static event Action<bool> OnGiveUpSelected; 
    
    [SerializeField] private GameObject popup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button giveUpButton;
    [SerializeField] private Button continueButton;
    
    private GameObject _backgroundBlocker; 
    private PopupAnimator _popupAnimator;

    private void Awake()
    {
        _popupAnimator = popup.GetComponent<PopupAnimator>();
        if (_popupAnimator == null)
        {
            _popupAnimator = popup.AddComponent<PopupAnimator>();
        }
        
        if (_backgroundBlocker == null)
        {
            CreateBackgroundBlocker();
        }
        
        giveUpButton.onClick.AddListener(EndGame);
        continueButton.onClick.AddListener(Continue);
    }
    
    private void CreateBackgroundBlocker()
    {
        // Создаем объект для блокировки взаимодействия
        _backgroundBlocker = new GameObject("BackgroundBlocker");
        _backgroundBlocker.transform.SetParent(transform.parent);
        _backgroundBlocker.transform.SetAsFirstSibling();
        _backgroundBlocker.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        _backgroundBlocker.AddComponent<Button>().onClick.AddListener(Continue);
        
        // Устанавливаем растяжение на весь экран
        RectTransform rectTransform = _backgroundBlocker.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void Show()
    {
        // Активируем блокировщик и попап
        if (_backgroundBlocker != null)
        {
            _backgroundBlocker.SetActive(true);
        }
        popup.SetActive(true);
        
        // Показываем анимацию
        if (_popupAnimator != null)
        {
            _popupAnimator.Show();
        }
        
        // Блокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(false);
    }

    private void Continue()
    {
        Hide();
    }

    private void Hide()
    {
        // Скрываем анимацию
        if (_popupAnimator != null)
        {
            _popupAnimator.Hide();
        }
        
        // Деактивируем блокировщик и попап
        if (_backgroundBlocker != null)
        {
            _backgroundBlocker.SetActive(false);
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
            if (button != giveUpButton && 
                button != continueButton && 
                button.gameObject != _backgroundBlocker && 
                !button.transform.IsChildOf(popup.transform))
            {
                button.interactable = interactable;
            }
        }
    }

    private void EndGame()
    {
        Debug.Log("GiveUp");
        Hide();
        OnGiveUpSelected?.Invoke(true);
    }

    private void OnDestroy()
    {
        // Убираем блокировщик при уничтожении объекта
        if (_backgroundBlocker != null)
        {
            Destroy(_backgroundBlocker);
        }
    }
}