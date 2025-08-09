using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRoot : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI scoreText; 
    [SerializeField] private Button submitButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button latterBagButton;
    // ... другие ссылки на UI-элементы

    // Публичные свойства для доступа
    public TextMeshProUGUI ScoreText => scoreText;
    public Button SubmitButton => submitButton;
    public Button BackButton => backButton;
    public Button LatterBagButton => latterBagButton;
}