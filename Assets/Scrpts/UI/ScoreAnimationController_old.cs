using DG.Tweening;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ScoreAnimationController_ : MonoBehaviour
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
    [SerializeField] private const float MaxWidth = 500f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float overshootDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;
    
    private float _totalScoreWidthRatio;
    private int _accumulatedScore; // Накопленный счет
    private int _currentScore;
    
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
        scorePanel.SetActive(true);
        OnAnimationStart?.Invoke();
        ResetBar();

        _totalScoreWidthRatio = (MaxWidth - MinWidth) / scoreData.WordScore;
        _accumulatedScore = 0;
        scoreText.text = "0";

        Sequence animationSequence = DOTween.Sequence();
        float currentTime = 0.3f;
        float accumulatedWidth = MinWidth;

        // Сначала анимируем все буквы
        for (int i = 0; i < scoreData.LetterScores.Count; i++)
        {
            var letterData = scoreData.LetterScores[i];
            int index = i;
            _currentScore = 0;
            int letterPoints = letterData.Points;
            _accumulatedScore += letterData.Points;

            animationSequence.InsertCallback(currentTime,
                () => AnimateLetter(letterData.Letter, index, letterJumpDuration));

            animationSequence.InsertCallback(currentTime, () =>
            {
                _currentScore += letterPoints;
                scoreText.text = _currentScore.ToString();
            });

            float targetWidth = MinWidth + (_accumulatedScore * _totalScoreWidthRatio);

            // Анимация шкалы
            animationSequence.Insert(currentTime,
                DOTween.To(() => accumulatedWidth,
                        x =>
                        {
                            accumulatedWidth = x;
                            fillRect.sizeDelta = new Vector2(x, fillRect.sizeDelta.y);
                        },
                        targetWidth,
                        letterJumpDuration)
                    .SetEase(easeType));

            currentTime += letterJumpDuration;
            accumulatedWidth = targetWidth;
        }

        // Затем анимируем бонусы за особые буквы (Capital и Final)
        foreach (var bonus in scoreData.BonusScores)
        {
            // Обрабатываем только базовые бонусы за особые буквы
            if (!bonus.IsFromImprovement &&
                (bonus.Description == "CapitalLetter" || bonus.Description == "FinalLetter"))
            {
                // Находим соответствующую букву для анимации
                var letterScore = bonus.Description == "CapitalLetter"
                    ? scoreData.LetterScores.FirstOrDefault(l => l.IsCapital)
                    : scoreData.LetterScores.FirstOrDefault(l => l.IsFinal);

                if (letterScore != null)
                {
                    int letterIndex = scoreData.LetterScores.IndexOf(letterScore);
                    int bonusPoints = bonus.Points;

                    animationSequence.InsertCallback(currentTime, () =>
                    {
                        // Анимация прыжка буквы
                        _wordPanelManager.PlayLetterAnimation(letterScore.Letter, letterIndex, letterJumpDuration);

                        // // Подсветка бонуса
                        // _improvementPanel.HighlightImprovement(bonus.Description, bonusHighlightDuration);
                    });

                    _accumulatedScore += bonusPoints;

                    animationSequence.InsertCallback(currentTime, () =>
                    {
                        _currentScore += bonusPoints;
                        scoreText.text = _currentScore.ToString();
                    });

                    float targetWidth = MinWidth + (_accumulatedScore * _totalScoreWidthRatio);

                    animationSequence.Insert(currentTime,
                        DOTween.To(() => accumulatedWidth,
                                x =>
                                {
                                    accumulatedWidth = x;
                                    fillRect.sizeDelta = new Vector2(x, fillRect.sizeDelta.y);
                                },
                                targetWidth,
                                letterJumpDuration)
                            .SetEase(easeType));

                    currentTime += letterJumpDuration;
                    accumulatedWidth = targetWidth;
                }
            }
        }

        // Затем анимируем остальные бонусы (улучшения)
        foreach (var bonus in scoreData.BonusScores)
        {
            // Пропускаем уже обработанные базовые бонусы
            if (!bonus.IsFromImprovement &&
                (bonus.Description == "CapitalLetter" || bonus.Description == "FinalLetter"))
                continue;

            var improvementType = bonus.IsFromImprovement ? bonus.SourceImprovement.EffectType : bonus.Description;
            animationSequence.InsertCallback(currentTime, () => ShowBonus(improvementType, bonus.Points));

            _accumulatedScore += bonus.Points;
            int bonusPoints = bonus.Points;

            animationSequence.InsertCallback(currentTime, () =>
            {
                _currentScore += bonusPoints;
                scoreText.text = _currentScore.ToString();
            });

            float targetWidth = MinWidth + (_accumulatedScore * _totalScoreWidthRatio);

            animationSequence.Insert(currentTime,
                DOTween.To(() => accumulatedWidth,
                        x =>
                        {
                            accumulatedWidth = x;
                            fillRect.sizeDelta = new Vector2(x, fillRect.sizeDelta.y);
                        },
                        targetWidth,
                        bonusHighlightDuration)
                    .SetEase(easeType));

            currentTime += bonusHighlightDuration;
            accumulatedWidth = targetWidth;
        }

        animationSequence.OnComplete(() =>
        {
            scoreText.text = _accumulatedScore.ToString();
            OnAnimationComplete?.Invoke();
            scorePanel.SetActive(false);
        });
    }

    private void ResetBar()
    {
        fillRect.DOKill();
        fillRect.sizeDelta = new Vector2(0f, fillRect.sizeDelta.y);
        _accumulatedScore = 0;
        scoreText.text = "0";
    }

    private void AnimateLetter(LetterData letter, int position, float duration)
    {
        _wordPanelManager.PlayLetterAnimation(letter, position, duration);
    }

    private void ShowBonus(string bonusType, int points)
    {
        _improvementPanel.HighlightImprovement(bonusType, bonusHighlightDuration);
    }
}