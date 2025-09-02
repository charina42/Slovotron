
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Rendering;

public class LetterTile : MonoBehaviour//, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public LetterData Letter;
    
    public Slot HomeSlot; 
    private Image _image;
    private Canvas _canvas;
    public TextMeshProUGUI letterText;
    public TextMeshProUGUI pointsText;
    
    private const float MoveDuration = 0.3f;
    private const float ScaleDuration = 0.2f;
    
    private WordPanelManager _wordPanelManager;

    // Вызывается при создании буквы (из фабрики или менеджера)
    public void Initialize(WordPanelManager manager)
    {
        _wordPanelManager = manager;
    }
  
    private void Start()
    {
        _image = GetComponent<Image>();
    
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnLetterClick);
        }
    
        // Не ищем Canvas здесь, а отложим до момента использования
    }

    private void EnsureCanvas()
    {
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                _canvas = FindAnyObjectByType<Canvas>();
                if (_canvas == null)
                {
                    Debug.LogError("Canvas not found!", this);
                    return;
                }
            }
        }
    }
    
    
    public void SetLetter(LetterData newLetter)
    {
        Letter = newLetter;
    }

    public void SetText(LetterData letter)
    {
        this.Letter = letter;
        letterText.text  = letter.LetterChar.ToString().ToUpper();
        pointsText.text = letter.Points.ToString();
        letterText.text = TrimSymbols(letterText.text);
        
        switch (Letter.Type)
        {
            case LetterType.Capital:
                letterText.color = Color.red;
                break;
            case LetterType.Final:
                letterText.color = Color.black;
                letterText.text +=  ".";
                break;
            case LetterType.Disposable:
                letterText.color = Color.green;
                break;
            case LetterType.Repeater:
                letterText.color = Color.magenta;
                break;
            case LetterType.NeighborMultiplierLeft:
                letterText.color = Color.black;
                letterText.text =  "<" + letterText.text;
                break;
            case LetterType.NeighborMultiplierRight:
                letterText.color = Color.black;
                letterText.text += ">";
                break;
            case LetterType.Return:
                letterText.color = Color.blue;
                break;
            
            case LetterType.Standard:
            case LetterType.Wild:
            default:
                letterText.color = Color.black ;
                break;
        }
    }
    public void UpdatePoints()
    {
        pointsText.text = Letter.Points.ToString();
    }

    private string TrimSymbols(string text)
    {
        text = text.TrimEnd('.');
        text = text.TrimEnd('>');
        text = text.TrimStart('<');
        return text;
    }
    
    public void SetScale()
    {
        Slot currentParentSlot = transform.parent != null ? transform.parent.GetComponent<Slot>() : null; 
        var isGridSlot = currentParentSlot != null && currentParentSlot.IsGridSlot;
        
        float targetScale = isGridSlot ? 0.9f : 1f;
        transform.localScale = Vector3.one * targetScale;
    }

    // public void OnBeginDrag(PointerEventData eventData)
    // {
    //     _image.raycastTarget = false;
    //     transform.SetParent(_canvas.transform);
    // }
    //
    // public void OnDrag(PointerEventData eventData)
    // {
    //     transform.position = eventData.position;
    // }
    //
    // public void OnEndDrag(PointerEventData eventData)
    // {
    //     Debug.Log("End Drag: ");
    //
    //     RectTransformUtility.ScreenPointToWorldPointInRectangle(
    //         _canvas.GetComponent<RectTransform>(),
    //         eventData.position,
    //         _canvas.worldCamera,
    //         out Vector3 worldPoint
    //     );
    //     Vector2 dropPosition = worldPoint;
    //
    //     Collider2D[] colliders = Physics2D.OverlapPointAll(dropPosition);
    //
    //     
    //     Slot newSlot = null;
    //     foreach (var collider in colliders)
    //     {
    //         newSlot = collider.GetComponent<Slot>();
    //         if (newSlot != null)
    //             break;
    //     }
    //     
    //     if (newSlot != null)
    //     {
    //         Debug.Log("LetterChar found a slot: " + newSlot.SlotID);
    //         
    //         if (newSlot.IsGridSlot == false && newSlot.transform.childCount == 0)
    //         {
    //             _wordGameManager.AddLetterToWordSlot(transform.gameObject, newSlot.SlotID);
    //         }
    //         else
    //         {
    //             ReturnHome();
    //         }
    //     }
    //     else
    //     {
    //         // Возвращаем в исходный слот, если кликнули мимо
    //         ReturnHome();
    //     }
    //     _image.raycastTarget = true;
    // }

    public void ReturnHome()
    {
        if (HomeSlot != null)
        {
            // transform.SetParent(HomeSlot.transform);
            // transform.localPosition = Vector3.zero;
            // _wordPanelManager.RemoveLetterFromWordSlot(transform.gameObject);
            MoveToSlot(HomeSlot.transform, true, () => {
                _wordPanelManager.RemoveLetterFromWordSlot(transform.gameObject);
            });
        }
        else
        {
            //  Удаляем, если HomeSlot не назначен (ошибка или перемещение из пула)
            transform.DOScale(0, 0.2f).OnComplete(() => {
                Destroy(gameObject);
                Debug.LogWarning("LetterTile returned, but no HomeSlot found. LetterTile destroyed.");
            });
        }
    }

    private void OnLetterClick()
    {
        Slot currentParentSlot = transform.parent != null ? transform.parent.GetComponent<Slot>() : null; // Безопасная проверка
       
        if (currentParentSlot != null && currentParentSlot.IsGridSlot)
        {
            _wordPanelManager.MoveToFirstFreeWordSlot(transform.gameObject);
        }
        // Иначе (если буква в слоте слова), возвращаем в домашний слот (грид)
        else
        {
            ReturnHome();
        }
    }
    
    public void MoveToSlot(Transform slotTransform, bool isGridSlot, Action onComplete = null)
    {
        // Отключаем кнопку во время анимации
        EnsureCanvas();
        
        var button = GetComponent<Button>();
        if (button) button.interactable = false;
         
        transform.SetParent(_canvas.GetComponent<RectTransform>());
        

        // Последовательность анимаций
        Sequence sequence = DOTween.Sequence();
        
        // Анимация перемещения
        sequence.Append(transform.DOMove(slotTransform.position, MoveDuration)
            .SetEase(Ease.OutQuad));
        
        // Анимация масштаба
        sequence.Join(transform.DOScale(isGridSlot ? Vector3.one * 0.9f : Vector3.one, ScaleDuration)
            .SetEase(Ease.OutBack));
        
        // После завершения
        sequence.OnComplete(() => {
            transform.SetParent(slotTransform);
            if (button) button.interactable = true;
            transform.localPosition = Vector3.zero;
            onComplete?.Invoke();
            
        });
    }
    
    public void PlayJumpAnimation(float jumpPower = 20f, float scalePower = 0.1f, float duration = 0.4f, Action onComplete = null)
    {
        var button = GetComponent<Button>();
        if (button != null) button.interactable = false;

        RectTransform rect = GetComponent<RectTransform>();
        Vector2 startPos = rect.anchoredPosition;
        Vector3 startScale = transform.localScale;

        // Последовательность: прыжок + масштаб
        Sequence jump = DOTween.Sequence();
    
        // Вверх с увеличением
        jump.Join(rect.DOAnchorPosY(startPos.y + jumpPower, duration * 0.5f).SetEase(Ease.OutBack));
        jump.Join(transform.DOScale(startScale * (1 + scalePower), duration * 0.5f).SetEase(Ease.OutQuad));
    
        // Обратно вниз с уменьшением
        jump.Append(rect.DOAnchorPosY(startPos.y, duration * 0.5f).SetEase(Ease.InOutBounce));
        jump.Join(transform.DOScale(startScale, duration * 0.5f).SetEase(Ease.InQuad));
    
        jump.OnComplete(() => {
            if (button != null) button.interactable = true;
            onComplete?.Invoke();
        });
    }

   
}