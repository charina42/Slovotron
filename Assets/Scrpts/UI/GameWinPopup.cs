using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameWinPopup : MonoBehaviour
{
    public static event Action OnNewGameSelected;

    [SerializeField] private GameObject _popup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private TMP_Text _bestScoreText;
    [SerializeField] private Button _restartButton;

    private void Awake()
    {
        _restartButton.onClick.AddListener(RestartGame);
        _popup.SetActive(false);
    }

    public void Show(int bestScore)
    {
        _bestScoreText.text = $"Ваш счет: {bestScore}";
        _popup.SetActive(true);
    }

    private void RestartGame()
    {
        _popup.SetActive(false);
        OnNewGameSelected?.Invoke();
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}