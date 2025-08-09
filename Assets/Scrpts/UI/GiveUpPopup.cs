using System;
using UnityEngine;
using UnityEngine.UI;
// using UnityEngine.SceneManagement;
using TMPro;

public class GiveUpPopup : MonoBehaviour
{
    public static event Action<bool> OnGiveUpSelected; 
    
    [SerializeField] private GameObject _popup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _giveupButton;
    [SerializeField] private Button _continueButton ;
    
    private void Awake()
    {
        _giveupButton.onClick.AddListener(EndGame);
        _continueButton.onClick.AddListener(Continue);
        // _popup.SetActive(false);
    }

    public void Show()
    {
        _popup.SetActive(true);
    }

    private void Continue()
    {
        _popup.SetActive(false);
        
    }

    private void EndGame()
    {
        Debug.Log("GiveUp");
        _popup.SetActive(false);
        OnGiveUpSelected?.Invoke(true);
        
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
