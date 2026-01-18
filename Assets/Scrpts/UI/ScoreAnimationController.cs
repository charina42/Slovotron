using DG.Tweening;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;
// using UnityEngine.UI;

public class ScoreAnimationController : MonoBehaviour
{
    private UIRoundScore _uiRoundScore;
    private WordPanelManager _wordPanelManager;
    private ImprovementPanel _improvementPanel;
    
    [FormerlySerializedAs("ScorePanel")]
    [Header("References")]
    [SerializeField] private GameObject scorePanel;
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private TMP_Text scoreText;
    
    [Header("Animation Settings")]
    [SerializeField] private float letterJumpDuration = 0.3f;
    [SerializeField] private float bonusHighlightDuration = 0.5f;
    [SerializeField] private const float MinWidth = 20f;
    [SerializeField] private float maxWidth = 500f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float overshootDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;
    
    private float _totalScoreWidthRatio;
    private int _accumulatedScore;
    private int _currentScore;
    private float _currentTime;
    private float _accumulatedWidth;
    private Sequence _animationSequence;
    
    public event System.Action OnAnimationStart;
    public event System.Action OnAnimationComplete;

    public void Initialize(UIRoundScore uiRoundScore, WordPanelManager wordPanelManager, ImprovementPanel improvementPanel)
    {
        _uiRoundScore = uiRoundScore;
        _wordPanelManager = wordPanelManager;
        _improvementPanel = improvementPanel;
        scorePanel.SetActive(false);
    }

    public void StartAnimation(ScoreManager.ScoreResult scoreData)
    {
        Debug.Log($"StartAnimation ");
    
        // Очищаем предыдущую анимацию
        if (_animationSequence != null && _animationSequence.IsActive())
        {
            _animationSequence.Kill();
            _animationSequence = null;
        }
    
        InitializeAnimation(scoreData);
    
        AnimateLetters(scoreData);
        AnimateSpecialLetterBonuses(scoreData);
        AnimateImprovementBonuses(scoreData);
    
        CompleteAnimation();
    }

    private void InitializeAnimation(ScoreManager.ScoreResult scoreData)
    {
        scorePanel.SetActive(true);
        OnAnimationStart?.Invoke();
        ResetBar();

        _totalScoreWidthRatio = (maxWidth - MinWidth) / scoreData.WordScore;
        _accumulatedScore = 0;
        _currentScore = 0;
        _currentTime = 0f; // Сбрасываем время
        _accumulatedWidth = MinWidth;
    
        // Создаем новую последовательность
        _animationSequence = DOTween.Sequence();
    
        scoreText.text = "0";
    }

    private void AnimateLetters(ScoreManager.ScoreResult scoreData)
    {
        Debug.Log($"AnimateLetters ");
        // Сначала собираем информацию о мультипликаторах соседей
        var neighborMultipliers = new Dictionary<int, (bool isLeft, int multiplierIndex)>();
        
        for (int i = 0; i < scoreData.LetterScores.Count; i++)
        {
            var letterData = scoreData.LetterScores[i];
            
            // Проверяем, является ли буква мультипликатором соседа
            if (letterData.IsNeighborMultiplierLeft && i > 0)
            {
                neighborMultipliers[i - 1] = (true, i);
            }
            else if (letterData.IsNeighborMultiplierRight && i < scoreData.LetterScores.Count - 1)
            {
                neighborMultipliers[i + 1] = (false, i);
            }
        }

        // Анимируем буквы с учетом мультипликаторов
        for (int i = 0; i < scoreData.LetterScores.Count; i++)
        {
            var letterData = scoreData.LetterScores[i];
            
            // Пропускаем мультипликаторы соседей - они будут анимированы вместе с целевыми буквами
            if (letterData.IsNeighborMultiplierLeft || letterData.IsNeighborMultiplierRight)
                continue;
            
            if (neighborMultipliers.ContainsKey(i))
            {
                // Анимируем букву вместе с ее мультипликатором
                var (isLeft, multiplierIndex) = neighborMultipliers[i];
                var multiplierData = scoreData.LetterScores[multiplierIndex];
                AnimateLetterWithMultiplier(letterData, i, multiplierData, multiplierIndex, scoreData);
            }
            else if (letterData.IsRepeater)
            {
                AnimateRepeaterLetter(letterData, i, scoreData);
            }
            else
            {
                AnimateRegularLetter(letterData, i);
            }
        }
    }

    private void AnimateLetterWithMultiplier(ScoreManager.LetterScore targetLetter, int targetIndex, 
                                           ScoreManager.LetterScore multiplierLetter, int multiplierIndex,
                                           ScoreManager.ScoreResult scoreData)
    {
        int letterPoints = targetLetter.Points;
        _accumulatedScore += letterPoints;

        // Анимируем целевую букву и мультипликатор одновременно
        AddLetterAnimation(targetLetter.Letter, targetIndex, letterPoints);
        AddSimultaneousLetterAnimation(multiplierLetter.Letter, multiplierIndex, 0); // 0 очков для мультипликатора
        
        AnimateProgressBar(_currentTime, letterJumpDuration);

        _currentTime += letterJumpDuration;
    }

    private void AnimateRegularLetter(ScoreManager.LetterScore letterData, int index)
    {
        int letterPoints = letterData.Points;
        _accumulatedScore += letterPoints;

        AddLetterAnimation(letterData.Letter, index, letterPoints);
        AnimateProgressBar(_currentTime, letterJumpDuration);

        _currentTime += letterJumpDuration;
    }

    private void AnimateRepeaterLetter(ScoreManager.LetterScore repeaterData, int repeaterIndex, ScoreManager.ScoreResult scoreData)
    {
        var sameLetters = scoreData.LetterScores
            .Where(l => l.Letter.LetterChar == repeaterData.Letter.LetterChar)
            .ToList();
        int repeaterPoints = repeaterData.Points;

        // Анимируем сам повторитель
        _accumulatedScore += repeaterPoints;
        AddLetterAnimation(repeaterData.Letter, repeaterIndex, repeaterPoints);
        AnimateProgressBar(_currentTime, letterJumpDuration);

        // Анимируем все одинаковые буквы одновременно с повторителем
        foreach (var sameLetter in sameLetters)
        {
            if (sameLetter != repeaterData)
            {
                var sameLetterIndex = scoreData.LetterScores.IndexOf(sameLetter);
                AddSimultaneousLetterAnimation(sameLetter.Letter, sameLetterIndex, repeaterPoints);
                
                var simultaneousTime = _currentTime - letterJumpDuration;
                AnimateProgressBar(simultaneousTime, letterJumpDuration);
            }
        }

        _currentTime += letterJumpDuration;
    }
    

    private void AddLetterAnimation(LetterData letter, int position, int points)
    {
        _animationSequence.InsertCallback(_currentTime, () => 
            AnimateLetter(letter, position, letterJumpDuration));
        
        _animationSequence.InsertCallback(_currentTime, () => 
            UpdateScoreDisplay(points));
    }

    private void AddSimultaneousLetterAnimation(LetterData letter, int position, int points)
    {
        var simultaneousTime = _currentTime - letterJumpDuration;
        
        _animationSequence.InsertCallback(simultaneousTime, () => 
            AnimateLetter(letter, position, letterJumpDuration));
        
        _animationSequence.InsertCallback(simultaneousTime, () => 
            UpdateScoreDisplay(points));
    }

    private void AnimateProgressBar(float startTime, float animDuration)
    {
        var targetWidth = MinWidth + (_accumulatedScore * _totalScoreWidthRatio);
        
        _animationSequence.Insert(startTime,
            DOTween.To(() => _accumulatedWidth,
                    UpdateProgressBarWidth,
                    targetWidth,
                    animDuration)
                .SetEase(easeType));
        
        _accumulatedWidth = targetWidth;
    }

    private void UpdateProgressBarWidth(float width)
    {
        _accumulatedWidth = width;
        fillRect.sizeDelta = new Vector2(width, fillRect.sizeDelta.y);
    }

    private void UpdateScoreDisplay(int points)
    {
        _currentScore += points;
        scoreText.text = _currentScore.ToString();
    }

    private void AnimateSpecialLetterBonuses(ScoreManager.ScoreResult scoreData)
    {
        // Debug.Log($"AnimateSpecialLetterBonuses ");
        foreach (var bonus in scoreData.BonusScores.Where(bonus => IsSpecialLetterBonus(bonus)))
        {
            AnimateSpecialLetterBonus(bonus, scoreData);
        }
    }

    private static bool IsSpecialLetterBonus(ScoreManager.BonusScore bonus)
    {
        return !bonus.IsFromImprovement && 
               (bonus.Description == "CapitalLetter" || bonus.Description == "FinalLetter");
    }

    private void AnimateSpecialLetterBonus(ScoreManager.BonusScore bonus, ScoreManager.ScoreResult scoreData)
    {
        var letterScore = bonus.Description == "CapitalLetter"
            ? scoreData.LetterScores.FirstOrDefault(l => l.IsCapital)
            : scoreData.LetterScores.FirstOrDefault(l => l.IsFinal);

        if (letterScore == null) return;
        var letterIndex = scoreData.LetterScores.IndexOf(letterScore);
        var bonusPoints = bonus.Points;

        AddSpecialLetterAnimation(letterScore.Letter, letterIndex);
        UpdateScoreWithBonus(bonusPoints);
        AnimateProgressBar(_currentTime, letterJumpDuration);

        _currentTime += letterJumpDuration;
    }

    private void AddSpecialLetterAnimation(LetterData letter, int position)
    {
        _animationSequence.InsertCallback(_currentTime, () =>
            _wordPanelManager.PlayLetterAnimation(letter, position, letterJumpDuration));
    }

    private void AnimateImprovementBonuses(ScoreManager.ScoreResult scoreData)
    {
        
        // Debug.Log($"AnimateImprovementBonuses ");
        foreach (var bonus in scoreData.BonusScores.Where(bonus => !IsSpecialLetterBonus(bonus)))
        {
            AnimateImprovementBonus(bonus);
        }
        // Debug.Log($"AnimateImprovementBonuses complete");
    }

    private void AnimateImprovementBonus(ScoreManager.BonusScore bonus)
    {
        var bonusType = bonus.IsFromImprovement 
            ? bonus.SourceImprovement.EffectType 
            : bonus.Description;

        var bonusPoints = bonus.Points;
        _accumulatedScore += bonusPoints;

        AddBonusHighlight(bonusType);
        UpdateScoreWithBonus(bonusPoints);
        AnimateProgressBar(_currentTime, bonusHighlightDuration);

        _currentTime += bonusHighlightDuration;
    }

    private void AddBonusHighlight(string bonusType)
    {
        _animationSequence.InsertCallback(_currentTime, () => 
            ShowBonus(bonusType)); // Points передаются отдельно
    }

    private void UpdateScoreWithBonus(int bonusPoints)
    {
        _animationSequence.InsertCallback(_currentTime, () => 
            UpdateScoreDisplay(bonusPoints));
    }

    private void CompleteAnimation()
    {
        if (_animationSequence == null) return;
    
        _animationSequence.OnComplete(() =>
        {
            Debug.Log("Animation Complete");
            scoreText.text = _accumulatedScore.ToString();
            OnAnimationComplete?.Invoke();
            scorePanel.SetActive(false);
        
            // Очищаем последовательность после завершения
            _animationSequence = null;
        });
    }

    private void ResetBar()
    {
        fillRect.DOKill();
        fillRect.sizeDelta = new Vector2(0f, fillRect.sizeDelta.y);
        _accumulatedScore = 0;
        _currentScore = 0;
        scoreText.text = "0";
    
        // Сбрасываем accumulatedWidth
        _accumulatedWidth = MinWidth;
    }

    private void AnimateLetter(LetterData letter, int position, float duration)
    {
        _wordPanelManager.PlayLetterAnimation(letter, position, duration);
    }

    private void ShowBonus(string bonusType)
    {
        _improvementPanel.HighlightImprovement(bonusType, bonusHighlightDuration);
    }
}