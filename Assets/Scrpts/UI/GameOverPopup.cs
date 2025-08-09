using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class GameOverPopup : MonoBehaviour
{
    public static event Action OnNewGameSelected; 
    
    [SerializeField] private GameObject popup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private TMP_Text _bestScoreText;
    [SerializeField] private Button _restartButton;

    // Текстовые константы для разных типов проигрыша
    private const string OUT_OF_LETTERS_TITLE = "Буквы закончились!";
    private const string OUT_OF_LETTERS_MESSAGE = "У вас не осталось букв для составления слов.";
    private const string LOW_SCORE_TITLE = "Не хватило очков!";
    private const string LOW_SCORE_MESSAGE = "Вы не набрали достаточно очков для прохождения раунда.";

    private void Awake()
    {
        Debug.Log("Game Over Popup Awake");
        _restartButton.onClick.AddListener(RestartGame);
        // popup.SetActive(false);
    }

    public void Show(bool isOutOfLetters, int bestScore)
    {
        Debug.Log($"Game Over Popup: {isOutOfLetters}");
        popup.SetActive(true);
        // Устанавливаем соответствующий текст в зависимости от причины проигрыша
        if (isOutOfLetters)
        {
            _titleText.text = OUT_OF_LETTERS_TITLE;
            _messageText.text = OUT_OF_LETTERS_MESSAGE;
        }
        else
        {
            _titleText.text = LOW_SCORE_TITLE;
            _messageText.text = LOW_SCORE_MESSAGE;
        }
        _bestScoreText.text = $"Лучший счет: {bestScore}";
    }

    private void RestartGame()
    {
        OnNewGameSelected?.Invoke();
        popup.SetActive(false);
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}