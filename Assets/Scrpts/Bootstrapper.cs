using Scrpts;
using UnityEngine;
using UnityEngine.Serialization;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private UIRoot uiRoot; 
    [SerializeField] private WordGameManager gameManager; 
    [SerializeField] private WordPanelManager wordPanelManager;
    
    [SerializeField] private LetterBagPopup letterBagPopup;
    [SerializeField] private ImprovementChosePopup improvementChosePopup;
    [SerializeField] private UIRoundScore uiRoundScore;
    [SerializeField] private ImprovementPanel improvementPanel;
    [SerializeField] private GameOverPopup gameOverPopup;
    [SerializeField] private GiveUpPopup giveUpPopup;
    [SerializeField] private GameWinPopup gameWinPopup;
    [SerializeField] private LeaderboardManager leaderboardManager;
    [SerializeField] private ScoreAnimationController scoreAnimationController;

    private void Awake()
    {
        
        Debug.Log("Boostrapper Awake");
        // Инициализация сервисов (важен порядок!)
        Services.Score = new ScoreManager();
        Services.UI = new UIManager();
        Services.LetterBag = new LetterBag();
        Services.Round = new RoundManager();
        Services.MetaGameData = new MetaGameData();
        Services.ImprovementSystem = new ImprovementSystem();
        
        
        // Связываем зависимости
        Services.Score.Initialize(Services.LetterBag, Services.ImprovementSystem);
        
        wordPanelManager.Initialize();
        leaderboardManager.Initialize(Services.MetaGameData);
        Services.Round.Initialize(Services.MetaGameData, uiRoundScore);
        Services.ImprovementSystem.Initialize(Services.LetterBag);
        letterBagPopup.Initialize(Services.LetterBag);
        scoreAnimationController.Initialize(uiRoundScore, wordPanelManager, improvementPanel);

        gameManager.Initialize(Services.MetaGameData, Services.LetterBag,  wordPanelManager, letterBagPopup, Services.Round, Services.Score,  
            Services.ImprovementSystem,  improvementPanel, improvementChosePopup, gameOverPopup, giveUpPopup, 
            gameWinPopup, leaderboardManager, scoreAnimationController);
        
        Services.UI.Initialize(uiRoot, gameManager);
        
       

        
        // Services.Game.OnScoreChanged += Services.UI.UpdateScore;
        // _uiRoot.StartButton.onClick.AddListener(Services.Game.StartGame);
    }
}