using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class TutorialPopup : MonoBehaviour, IPointerClickHandler
{
    public event Action OnContinueSelected; 
    
    [SerializeField] private GameObject popup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button continueButton;
    private GameObject _backgroundBlocker; 
    
    [SerializeField]  private PopupAnimator popupAnimator; 
    

    private void Awake()
    {
        // popupAnimator = popup.GetComponent<PopupAnimator>();
        // if (popupAnimator == null)
        // {
        //     popupAnimator = popup.AddComponent<PopupAnimator>();
        // }
        
        // Создаем блокировщик фона если он не установлен
        if (_backgroundBlocker == null)
        {
            CreateBackgroundBlocker();
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Hide);
        }
    }
    

    private void CreateBackgroundBlocker()
    {
        // Создаем объект для блокировки взаимодействия
        _backgroundBlocker = new GameObject("BackgroundBlocker");
        _backgroundBlocker.transform.SetParent(transform.parent);
        _backgroundBlocker.transform.SetAsFirstSibling();
        _backgroundBlocker.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        _backgroundBlocker.AddComponent<Button>().onClick.AddListener(Hide);
        
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
        
        Debug.Log("TutorialPopup.Show");
        popupAnimator.Show();
        
        // Блокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(false);
    }

    private void Hide()
    {
       
        popupAnimator.Hide();
        // Деактивируем блокировщик и попап
        if (_backgroundBlocker != null)
        {
            _backgroundBlocker.SetActive(false);
        }
        
        // Разблокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(true);
        OnContinueSelected?.Invoke();
    }

    private void SetInteractableForOtherUI(bool interactable)
    {
        // Находим все кнопки и другие интерактивные элементы кроме наших
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            if (button != continueButton && 
                button.gameObject != _backgroundBlocker && 
                !button.transform.IsChildOf(popup.transform))
            {
                button.interactable = interactable;
            }
        }
    }

    // Обработчик клика по попапу
    public void OnPointerClick(PointerEventData eventData)
    {
        Hide();
    }

    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    public void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    public string GetMessage()
    {
        return messageText != null ? messageText.text : string.Empty;
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