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
    [SerializeField] private GameObject backgroundBlocker; 
    [SerializeField] private PopupAnimator popupAnimator; 
    

    private void Awake()
    {
        // Создаем блокировщик фона если он не установлен
        if (backgroundBlocker == null)
        {
            CreateBackgroundBlocker();
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Hide);
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
        backgroundBlocker.AddComponent<Button>().onClick.AddListener(Hide);
        
        // Устанавливаем растяжение на весь экран
        RectTransform rectTransform = backgroundBlocker.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void Show()
    {
        // Активируем блокировщик и попап
        if (backgroundBlocker != null)
        {
            backgroundBlocker.SetActive(true);
        }
        popup.SetActive(true);
        
        popupAnimator.Show();
        
        // Блокируем взаимодействие с другими элементами
        SetInteractableForOtherUI(false);
    }

    private void Hide()
    {
       
        popupAnimator.Hide();
        // Деактивируем блокировщик и попап
        if (backgroundBlocker != null)
        {
            backgroundBlocker.SetActive(false);
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
                button.gameObject != backgroundBlocker && 
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
        if (backgroundBlocker != null)
        {
            Destroy(backgroundBlocker);
        }
    }

   
}