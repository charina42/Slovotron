using Scrpts;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using YG;
using YG.Utils.LB;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Настройки как в LBExample")]
    [SerializeField] public LeaderboardYG leaderboardYG;
    [SerializeField] private TMP_Text previousRecordText;
    // public InputField scoreInputField;
    public bool timeConverter;

    [Header("Дополнительные настройки")]
    public float requestCooldown = 1.5f; // Минимальный интервал между запросами

    private MetaGameData _metaGameData;
    // private int _currentPlayerScore;
    private float _lastRequestTime;


    public void Initialize(MetaGameData metaGameData)
    {
        _metaGameData = metaGameData;
    }
    
    // private void OnEnable()
    // {
    //     // Подписываемся на события как в CustomLBExample
    //     YG2.onGetLeaderboard += OnLeaderboardUpdate;
    //     
    //     // Загружаем текущий рекорд игрока
    //     // if (YG.SDKEnabled)
    //     // {
    //         YG2.GetLeaderboard(leaderboardYG.nameLB);
    //     // }
    // }
    //
    // private void OnDisable()
    // {
    //     YG2.onGetLeaderboard -= OnLeaderboardUpdate;
    // }

    // // Обработка данных лидерборда
    // private void OnLeaderboardUpdate(LBData lbData)
    // {
    //     if (lbData.technoName == leaderboardYG.nameLB && lbData.currentPlayer != null)
    //     {
    //         // Debug.Log($"");
    //         _metaGameData.currentPlayerRecord = lbData.currentPlayer.score;
    //         previousRecordText.text = _currentPlayerScore.ToString();
    //         Debug.Log($"Текущий рекорд: {_currentPlayerScore}");
    //     }
    // }

    public void TrySetScore(int score)
    {
        if (Time.time - _lastRequestTime < requestCooldown)
        {
            Debug.LogWarning($"Подождите {requestCooldown} сек. перед новым запросом");
            return;
        }

        if (!timeConverter)
        {
            SetRegularScore(score);
        }
        else
        {
            SetTimeScore(score);
        }
    }

    private void SetRegularScore(int newScore)
    {
        if (newScore > _metaGameData.currentPlayerRecord)
        {
            // Вариант из LBExample
            YG2.SetLeaderboard(leaderboardYG.nameLB, newScore);
            _metaGameData.currentPlayerRecord = newScore;
            Debug.Log($"Новый рекорд: {newScore}");
            _lastRequestTime = Time.time;

            // Обновляем данные
            YG2.GetLeaderboard(leaderboardYG.nameLB);
        }
        else
        {
            Debug.Log($"Текущий рекорд {_metaGameData.currentPlayerRecord} лучше чем {newScore}");
        }

    }

    private void SetTimeScore(int newTime)
    {
        int timeInSeconds = Mathf.RoundToInt(newTime);

        if ( _metaGameData.currentPlayerRecord == 0 || timeInSeconds <  _metaGameData.currentPlayerRecord)
        {
            // Вариант из LBExample для времени
            YG2.SetLBTimeConvert(leaderboardYG.nameLB, newTime);
            _metaGameData.currentPlayerRecord = newTime;
            Debug.Log($"Новое время: {newTime} сек.");
            _lastRequestTime = Time.time;

            // Обновляем данные
            YG2.GetLeaderboard(leaderboardYG.nameLB);
        }
        else
        {
            Debug.Log($"Текущее время { _metaGameData.currentPlayerRecord} лучше чем {timeInSeconds}");
        }
    }
}