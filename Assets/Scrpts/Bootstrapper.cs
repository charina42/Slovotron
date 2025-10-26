﻿using Scrpts;
using UnityEngine;
using UnityEngine.Serialization;

public class Bootstrapper : MonoBehaviour
{
    [Header("Core Managers")]
    [SerializeField] private UIRoot uiRoot;
    [SerializeField] private WordGameManager gameManager;
    [SerializeField] private WordPanelManager wordPanelManager;
    
    [Header("Popup References")]
    [SerializeField] private LetterBagPopup letterBagPopup;
    [SerializeField] private ImprovementChosePopup improvementChosePopup;
    [SerializeField] private MetaImprovementPopup metaImprovementPopup;
    [SerializeField] private GameOverPopup gameOverPopup;
    [SerializeField] private GiveUpPopup giveUpPopup;
    [SerializeField] private GameWinPopup gameWinPopup;
    [SerializeField] private TutorialPopup tutorialPopup;
    
    [Header("UI Components")]
    [SerializeField] private UIRoundScore uiRoundScore;
    [SerializeField] private ImprovementPanel improvementPanel;
    [SerializeField] private LeaderboardManager leaderboardManager;
    [SerializeField] private ScoreAnimationController scoreAnimationController;
    // [SerializeField] private PopupAnimator popupAnimator;
    
    private void Awake()
    {
        Debug.Log("Bootstrapper Awake");
        
        InitializeServices();
        InitializePopups();
        InitializeManagers();
        LinkDependencies();
    }

    private void InitializeServices()
    {
        // Инициализация сервисов (важен порядок!)
        Services.Score = new ScoreManager();
        Services.UI = new UIManager();
        Services.LetterBag = new LetterBag();
        Services.Round = new RoundManager();
        Services.MetaGameData = new MetaGameData();
        Services.ImprovementSystem = new ImprovementSystem();
        Services.TutorialManager = new TutorialManager();
        Services.DictionaryManager = new DictionaryManager();
        Services.MetaImprovementManager = new MetaImprovementManager();
    }

    private void InitializePopups()
    {
        letterBagPopup.Initialize(Services.LetterBag);
        // improvementChosePopup.Initialize(popupAnimator);
        // gameOverPopup.Initialize(popupAnimator);
        // giveUpPopup.Initialize(popupAnimator);
        // gameWinPopup.Initialize(popupAnimator);
        // tutorialPopup.Initialize(popupAnimator);
    }

    private void InitializeManagers()
    {
        // Инициализация менеджеров
        wordPanelManager.Initialize();
        leaderboardManager.Initialize(Services.MetaGameData);
        Services.Round.Initialize(uiRoundScore);
        Services.ImprovementSystem.Initialize( Services.LetterBag, improvementChosePopup, metaImprovementPopup);
        scoreAnimationController.Initialize(uiRoundScore, wordPanelManager, improvementPanel);
        Services.TutorialManager.Initialize(tutorialPopup);
        Services.MetaImprovementManager.Initialize(Services.ImprovementSystem, metaImprovementPopup, improvementPanel);
    }

    private void LinkDependencies()
    {
        // Связывание основных зависимостей
        gameManager.Initialize(
            Services.MetaGameData, 
            Services.LetterBag, 
            wordPanelManager, 
            letterBagPopup, 
            Services.Round, 
            Services.Score,  
            Services.ImprovementSystem, 
            improvementPanel, 
            improvementChosePopup, 
            metaImprovementPopup,
            gameOverPopup, 
            giveUpPopup, 
            gameWinPopup, 
            leaderboardManager, 
            scoreAnimationController, 
            Services.TutorialManager, 
            Services.DictionaryManager
        );
        
        Services.UI.Initialize(uiRoot, gameManager);
    }
}