using DG.Tweening;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class ScoreAnimationController : MonoBehaviour
{
    
    private UIRoundScore _uiRoundScore;
    private WordPanelManager _wordPanelManager;
    private ImprovementPanel _improvementPanel;
    
    [Header("References")]
    [SerializeField] private RectTransform scoreBar;
    [SerializeField] private GameObject scoreSegmentPrefab;
    
    [Header("Animation Settings")]
    [SerializeField] private float letterJumpDuration = 0.3f;
    [SerializeField] private float segmentFillDuration = 0.4f;
    [SerializeField] private float scoreIncrementDuration = 0.8f;
    [SerializeField] private float bonusHighlightDuration = 0.5f;
    
    private List<Image> _scoreSegments = new List<Image>();
    private int _currentSegmentIndex = 0;
    
    public event System.Action OnAnimationStart;
    public event System.Action OnAnimationComplete;

    public void Initialize(UIRoundScore uiRoundScore, WordPanelManager wordPanelManager, ImprovementPanel improvementPanel)
    {
        _uiRoundScore = uiRoundScore;
        _wordPanelManager = wordPanelManager;
        _improvementPanel = improvementPanel;
    }

    public void StartAnimation(ScoreManager.ScoreResult scoreData)
    {
        OnAnimationStart?.Invoke();
        
        // Инициализация полоски счета
        InitializeScoreBar(scoreData);
        
        // Запуск последовательности анимаций
        Sequence animationSequence = DOTween.Sequence();
        
        // Анимация букв и добавление сегментов
        foreach (var letterData in scoreData.LetterScores)
        {
            animationSequence.AppendCallback(() => AnimateLetter(letterData.Letter));
            animationSequence.AppendInterval(letterJumpDuration);
        }
        
        // Анимация бонусов
        foreach (var bonus in scoreData.BonusScores)
        {
            animationSequence.AppendCallback(() => ShowBonus(bonus.Description, bonus.Points));
            animationSequence.AppendInterval(bonusHighlightDuration);
        }
        
        // Итоговый счет
        animationSequence.AppendCallback(() => AnimateTotalScore(scoreData.TotalScore));
        
        animationSequence.OnComplete(() => OnAnimationComplete?.Invoke());
    }
    
    private void InitializeScoreBar(ScoreManager.ScoreResult scoreData)
    {
        // Очистка предыдущих сегментов
        foreach (var segment in _scoreSegments)
        {
            Destroy(segment.gameObject);
        }
        _scoreSegments.Clear();
        _currentSegmentIndex = 0;
        
        // Создание сегментов
        int totalSegments = scoreData.LetterScores.Count + scoreData.BonusScores.Count;
        for (int i = 0; i < totalSegments; i++)
        {
            GameObject segment = Instantiate(scoreSegmentPrefab, scoreBar);
            Image segmentImage = segment.GetComponent<Image>();
            segmentImage.fillAmount = 0;
            _scoreSegments.Add(segmentImage);
        }
    }
    
    public void AnimateLetter(LetterData letter)
    {
        // Анимация буквы в WordPanelManager
        _wordPanelManager.PlayWordJumpAnimation();
        
        // Добавление сегмента
        if (_currentSegmentIndex < _scoreSegments.Count)
        {
            Image segment = _scoreSegments[_currentSegmentIndex];
            segment.DOFillAmount(1, segmentFillDuration);
            segment.color = GetLetterSegmentColor(letter);
            _currentSegmentIndex++;
        }
    }
    
    public void ShowBonus(string bonusType, int points)
    {
        // Подсветка улучшения
        _improvementPanel.HighlightImprovement(bonusType, bonusHighlightDuration);
        
        // Добавление бонусного сегмента
        if (_currentSegmentIndex < _scoreSegments.Count)
        {
            Image segment = _scoreSegments[_currentSegmentIndex];
            segment.DOFillAmount(1, segmentFillDuration);
            segment.color = GetBonusSegmentColor(bonusType);
            _currentSegmentIndex++;
        }
    }
    
    private void AnimateTotalScore(int totalScore)
    {
        // Анимация плавного увеличения общего счета
        // _uiRoundScore.AnimateScoreChange(totalScore, scoreIncrementDuration);
    }
    
    private Color GetLetterSegmentColor(LetterData letter)
    {
        // Цвета для разных типов букв
        return letter.Type switch
        {
            LetterType.Capital => new Color(1f, 0.5f, 0.5f), // Красноватый
            LetterType.Final => new Color(0.5f, 0.5f, 1f),   // Голубоватый
            _ => new Color(0.8f, 0.8f, 0.2f)                // Желтый (обычные буквы)
        };
    }
    
    private Color GetBonusSegmentColor(string bonusType)
    {
        // Цвета для разных типов бонусов
        return bonusType switch
        {
            "VowelFirstBonus" => new Color(0.2f, 0.8f, 0.2f), // Зеленый
            "ConsonantComboBonus" => new Color(0.8f, 0.2f, 0.8f), // Фиолетовый
            _ => new Color(0.2f, 0.8f, 0.8f) // Голубой (по умолчанию)
        };
    }
}
