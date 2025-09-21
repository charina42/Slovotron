using System.Collections.Generic;
using Scrpts;
using UnityEngine;
using YG;

namespace YG
{
    public partial class SavesYG
    {
        // Ваши данные для сохранения
        public int currentRound = 0;
        public int confirmedWordsCount = 0;
        public int roundScore = 0;
    }
}

public class RoundManager
{
    public enum RoundState
    {
        InProgress,    
        Success,       
        Failed,       
        Win          
    }
    
    private UIRoundScore _uiRoundScore;
    
    private int _wordsPerRound; 
    private int [] _rounds;
    private int _maxRounds;
    
    public void Initialize(MetaGameData metaGameData, UIRoundScore uiRoundScore)
    {
        _wordsPerRound = MetaGameData.WORDS_PER_ROUND;
        _uiRoundScore = uiRoundScore;
        _rounds = MetaGameData.Rounds;
        _maxRounds = MetaGameData.ROUNDS_COUNT;
        
        SetRoundPanelData();
    }

    public void SetRoundPanelData()
    {
        _uiRoundScore.SetValue(YG2.saves.TotalScore, YG2.saves.roundScore, _rounds[YG2.saves.currentRound], 
            YG2.saves.currentRound, YG2.saves.confirmedWordsCount);
    }
    
    public float CalculateWordContributionPercentage(int wordScore)
    {
        // Получаем текущий раунд и необходимые для него очки
        int currentRoundIndex = YG2.saves.currentRound;
        int requiredScoreForRound = _rounds[currentRoundIndex];
    
        // Если раунд уже завершен или слово не принесло очков, возвращаем 0
        if (requiredScoreForRound <= 0 || wordScore <= 0)
        {
            return 0f;
        }
    
        // Рассчитываем процент вклада
        float contributionPercentage = (float)wordScore / requiredScoreForRound * 100f;
    
        return contributionPercentage;
    }

    public List<ImprovementRarity> GetRoundCompletionRarities()
    {
        Debug.Log("Getting round completion rarities");
        int currentRoundIndex = YG2.saves.currentRound;
        int requiredScoreForRound = _rounds[currentRoundIndex];
        int actualScore = YG2.saves.roundScore;

        // Рассчитываем коэффициент превышения требуемых очков
        float excessRatio = (float)actualScore / requiredScoreForRound;

        List<ImprovementRarity> rarities = new List<ImprovementRarity>();

        if (excessRatio <= 1.0f)
        {
            // Просто прошли раунд без превышения
            rarities.AddRange(new[]
            {
                ImprovementRarity.Common,
                ImprovementRarity.Common,
                ImprovementRarity.Common
            });
        }
        else if (excessRatio > 1.0f && excessRatio <= 1.5f)
        {
            // Небольшое превышение (1-1.5x)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Common,
                ImprovementRarity.Common,
                ImprovementRarity.Rare
            });
        }
        else if (excessRatio > 1.5f && excessRatio <= 2.0f)
        {
            // Хорошее превышение (1.5-2x)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Common,
                ImprovementRarity.Rare,
                ImprovementRarity.Rare
            });
        }
        else if (excessRatio > 2.0f && excessRatio <= 3.0f)
        {
            // Отличное превышение (2-3x)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Rare,
                ImprovementRarity.Rare,
                ImprovementRarity.Rare
            });
        }
        else if (excessRatio > 3.0f && excessRatio <= 4.0f)
        {
            // Выдающееся превышение (3-4x)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Rare,
                ImprovementRarity.Rare,
                ImprovementRarity.Epic
            });
        }
        else if (excessRatio > 4.0f)
        {
            // Феноменальное превышение (4x+)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Rare,
                ImprovementRarity.Epic,
                ImprovementRarity.Epic
            });
        }

        return rarities;
    }

    public RoundState  HandleWordConfirmed(int score)
    {
        YG2.saves.confirmedWordsCount++;
        YG2.saves.roundScore += score;
        Debug.Log(YG2.saves.confirmedWordsCount);
        
        if (YG2.saves.confirmedWordsCount >= _wordsPerRound)
        {
            if (YG2.saves.roundScore >= _rounds[YG2.saves.currentRound])
            {
                // Успешное завершение раунда
                if (YG2.saves.currentRound == _maxRounds-1)
                {
                    return RoundState.Win;
                }
                YG2.saves.confirmedWordsCount = 0;
                YG2.saves.currentRound++;
                YG2.saves.roundScore = 0;

                SetRoundPanelData();
                
                return RoundState.Success;
            }
            else
            {
                // Неудачное завершение раунда
                return RoundState.Failed;
            }
        }

        // Раунд еще продолжается
        SetRoundPanelData();
        
        return RoundState.InProgress;
    }
    
}