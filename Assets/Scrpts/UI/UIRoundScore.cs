using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UIRoundScore : MonoBehaviour
{
    public TextMeshProUGUI scoreText; 
    // public Slider slider; 
    public TextMeshProUGUI text;
    public TextMeshProUGUI roundText;
    // public Toggle[] wordToggles; 
    private const string TextFormat = "{0}/{1}";

    public void SetValue(float score, float value, float maxValue,int currentRound, int wordsConfirmed)
    {
        scoreText.text = $"Очки {score}";
        // var currentValue = Mathf.Clamp01(value / maxValue);

        // slider.value = currentValue;
        roundText.text = "Раунд " + (currentRound+1).ToString();

        
        text.text = string.Format(TextFormat, value, maxValue);
        
        // UpdateWordToggles(wordsConfirmed);
    }
    
    // private void UpdateWordToggles(int wordsConfirmed)
    // {
    //     // Проверяем, что массив тогглов инициализирован и имеет правильный размер
    //     if (wordToggles == null || wordToggles.Length != 4)
    //     {
    //         Debug.LogWarning("Word toggles array is not properly set up!");
    //         return;
    //     }
    //
    //     // Активируем тогглы в соответствии с количеством подтвержденных слов
    //     for (int i = 0; i < wordToggles.Length; i++)
    //     {
    //         if (wordToggles[i] != null)
    //         {
    //             wordToggles[i].isOn = i < wordsConfirmed;
    //         }
    //     }
    // }
}