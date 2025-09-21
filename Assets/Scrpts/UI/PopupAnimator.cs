using DG.Tweening;
using UnityEngine;

public class PopupAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float showDuration = 0.3f;
    public float hideDuration = 0.2f;
    public Ease showEase = Ease.OutBack;
    public Ease hideEase = Ease.InBack;
    
    private Vector3 _originalScale;
    private CanvasGroup _canvasGroup;
    
    private void Awake()
    {
        _originalScale = transform.localScale;
        
        // Добавляем CanvasGroup если его нет
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Скрываем попап при старте
        transform.localScale = Vector3.zero;
        _canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }
    
    
    public void Show()
    {
        gameObject.SetActive(true);
        
        // Сбрасываем предыдущие анимации
        transform.DOKill();
        _canvasGroup.DOKill();
        
        // Анимация появления
        transform.localScale = Vector3.zero;
        _canvasGroup.alpha = 0;
        
        Sequence showSequence = DOTween.Sequence();
        showSequence.Join(transform.DOScale(_originalScale, showDuration).SetEase(showEase));
        showSequence.Join(_canvasGroup.DOFade(1, showDuration).SetEase(Ease.OutQuad));
        showSequence.OnComplete(() => {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        });
    }
    
    public void Hide()
    {
        // Сбрасываем предыдущие анимации
        transform.DOKill();
        _canvasGroup.DOKill();
        
        // Отключаем взаимодействие во время анимации
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        
        // Анимация исчезновения
        Sequence hideSequence = DOTween.Sequence();
        hideSequence.Join(transform.DOScale(Vector3.zero, hideDuration).SetEase(hideEase));
        hideSequence.Join(_canvasGroup.DOFade(0, hideDuration).SetEase(Ease.InQuad));
        hideSequence.OnComplete(() => {
            gameObject.SetActive(false);
        });
    }
    
    // Для быстрого скрытия без анимации
    public void HideImmediate()
    {
        transform.DOKill();
        _canvasGroup.DOKill();
        
        gameObject.SetActive(false);
        transform.localScale = Vector3.zero;
        _canvasGroup.alpha = 0;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }
    
    private void OnDestroy()
    {
        transform.DOKill();
    }

   
}