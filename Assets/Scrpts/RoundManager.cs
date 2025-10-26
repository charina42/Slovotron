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
    
    // private int _wordsPerRound; 
    private int [] _rounds;
    private int _maxRounds;
    
    public void Initialize(UIRoundScore uiRoundScore)
    {
        // _wordsPerRound = MetaGameData.WORDS_PER_ROUND;
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

    public float CalculateRoundExcessRatio()
    {
        Debug.Log("Getting round completion rarities");
        int currentRoundIndex = YG2.saves.currentRound;
        int requiredScoreForRound = _rounds[currentRoundIndex];
        int actualScore = YG2.saves.roundScore;

        // Рассчитываем коэффициент превышения требуемых очков
        return (float)actualScore / requiredScoreForRound;
    }

    public RoundState  HandleWordConfirmed(int score)
    {
        YG2.saves.confirmedWordsCount++;
        YG2.saves.roundScore += score;
        Debug.Log(YG2.saves.confirmedWordsCount);


        if (YG2.saves.roundScore >= _rounds[YG2.saves.currentRound])
        {
            // Успешное завершение раунда
            // if (YG2.saves.currentRound == _maxRounds - 1)
            // {
            //     return RoundState.Win;
            // }

            YG2.saves.confirmedWordsCount = 0;
            YG2.saves.currentRound++;
            YG2.saves.roundScore = 0;

            SetRoundPanelData();

            return RoundState.Success;
        }
        else
        {
            SetRoundPanelData();
            return RoundState.InProgress;
            // return RoundState.Failed;
        }


        // // Раунд еще продолжается
        // SetRoundPanelData();
        //
        // return RoundState.InProgress;
    }
    
}