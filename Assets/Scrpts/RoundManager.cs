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
    // private int _currentRound = 0;
    // private int _confirmedWordsCount = 0;
    // private int _roundScore = 0;
    
    
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