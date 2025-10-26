using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Scrpts;
using UnityEngine.Serialization;
using YG;
using YG.Utils.LB;

namespace YG
{
    public partial class SavesYG
    {
        public bool IsTutorialCompleted { get; set; }
        public int TotalScore { get; set; }
        public GameStatistics GameStatistics { get; set; } = new GameStatistics();
        public bool IsImprovementPopupOpen { get; set; }
        public string LastValidatedWord { get; set; }
        public ScoreManager.ScoreResult LastScoreResult { get; set; }
        public bool IsMetaImprovementPopupOpen { get; set; } // Новый флаг для мета-попапа
    }
}

public class WordGameManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int poolSize = 20;
    [SerializeField] public int wordsPerRound = 3;
    [SerializeField] public int minWordLength = 3;

    [Header("Слоты")] 
    public Transform letterSlotsGread;
    public Slot[] letterSlots; 
    public Slot[] wordSlots; 
    
    [Header("Dictionary")]
    [SerializeField] private string wordsJson;
    [SerializeField] private TextAsset lettersJson;

    [Header("References")]
    [SerializeField] private GameObject letterPrefab;
    [SerializeField] private Transform lettersGrid;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button shuffleButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button letterBagButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button eraseButton;
    [SerializeField] public LeaderboardYG leaderboardYG;

    private bool _isTutorialShow = true;

    private readonly Queue<GameObject> _letterObjectsPool = new Queue<GameObject>();
    private readonly Dictionary<int, List<string>> _wordsByLengthDictionary = new Dictionary<int, List<string>>();
    
    private readonly List<GameObject> _allInGameLetters = new List<GameObject>();

    private LetterBag _letterBag;
    private WordPanelManager _wordPanelManager;
    private LetterBagPopup _letterBagPopup;
    private RoundManager _roundManager;
    private ScoreManager _scoreManager;
    private ImprovementSystem _improvementSystem;
    
    private ImprovementPanel _improvementPanel;
    private ImprovementChosePopup _improvementChosePopup;
    private MetaImprovementPopup _metaImprovementPopup; // Новый попап для мета-улучшений
    private GameOverPopup _gameOverPopup;
    private GiveUpPopup _giveUpPopup;
    private GameWinPopup _gameWinPopup;
    private LeaderboardManager _leaderboardManager;
    private ScoreAnimationController _scoreAnimationController;
    private TutorialManager _tutorialManager;
    private DictionaryManager _dictionaryManager;

    private MetaGameData _metaGameData;

    // ========== INITIALIZATION ==========
    public void Initialize(MetaGameData metaGameData, LetterBag letterBag, WordPanelManager wordPanelManager,
        LetterBagPopup letterBagPopup, RoundManager roundManager, ScoreManager scoreManager, 
        ImprovementSystem improvementSystem, ImprovementPanel improvementPanel,
        ImprovementChosePopup improvementChosePopup, MetaImprovementPopup metaImprovementPopup, // Добавлен новый параметр
        GameOverPopup gameOverPopup, GiveUpPopup giveUpPopup, 
        GameWinPopup gameWinPopup, LeaderboardManager leaderboardManager,
        ScoreAnimationController scoreAnimationController, TutorialManager tutorialManager,
        DictionaryManager dictionaryManager)
    {
        _metaGameData = metaGameData; 
        _letterBag = letterBag;
        _letterBagPopup = letterBagPopup;
        _roundManager = roundManager;
        _scoreManager = scoreManager;
        _improvementSystem = improvementSystem;
        _improvementPanel = improvementPanel;
        _improvementChosePopup = improvementChosePopup;
        _metaImprovementPopup = metaImprovementPopup; // Инициализируем новый попап
        _wordPanelManager = wordPanelManager;
        _gameOverPopup = gameOverPopup;
        _giveUpPopup = giveUpPopup;
        _gameWinPopup = gameWinPopup;
        _leaderboardManager = leaderboardManager;
        _scoreAnimationController = scoreAnimationController;
        _tutorialManager = tutorialManager;
        _dictionaryManager = dictionaryManager;
         
        submitButton.onClick.AddListener(CheckWord);
        refreshButton.onClick.AddListener(Refresh);
        letterBagButton.onClick.AddListener(ShowLetterBagPopup);
        shuffleButton.onClick.AddListener(ShuffleLetters);
        newGameButton.onClick.AddListener(LoadNewGame);
        eraseButton.onClick.AddListener(EraseWord);
        
        ImprovementSystem.OnImprovementSelected += ImprovementSelectedProceed;
        MetaImprovementManager.OnImprovementSelected += ImprovementSelectedProceed;
        _letterBag.OnLetterReplaced += HandleLetterReplaced;
        GiveUpPopup.OnGiveUpSelected += GameOver;
        GameOverPopup.OnNewGameSelected += LoadNewGame;
        GameWinPopup.OnNewGameSelected += LoadNewGame;
        WordPanelManager.OnRemoveInGameLetter += RemoveInGameLetterObject;
        YG2.onDefaultSaves += StartNewGame;
        YG2.onGetLeaderboard += OnLeaderboardUpdate;
        OnInitialized += StartGame;
        _tutorialManager.OnTutorialStepCompleted += ProcessPendingWord;
        
        YG2.GetLeaderboard(leaderboardYG.nameLB);
        
        Debug.Log("WordGameManager initialized");
        OnInitialized?.Invoke();
    }

    private void StartGame()
    {
        Debug.Log("StartGame");
        InitializePool();
        _dictionaryManager.Initialize(wordsJson);
        _letterBag.InitializeBasePoints(lettersJson);
        
        // Проверяем, нужно ли восстановить попап улучшений
        if (YG2.saves.IsImprovementPopupOpen && YG2.saves.CurrentImprovementOptions != null)
        {
            RestoreImprovementPopup();
        }
        else if (YG2.saves.IsMetaImprovementPopupOpen && YG2.saves.CurrentImprovementOptions != null)
        {
            RestoreMetaImprovementPopup(); // Восстанавливаем мета-попап
        }
        else
        {
            ShowOnBoardLetters();
        }
        
        _improvementPanel.ShowImprovements(YG2.saves.ActiveImprovements);
        OnScoreChanged?.Invoke(YG2.saves.TotalScore);
        _roundManager.SetRoundPanelData();
        ShowLetterBagCount();
        
        Debug.Log($"IsTutorialCompleted {_tutorialManager.IsTutorialCompleted()}");
        
        if (_tutorialManager != null && !_tutorialManager.IsTutorialCompleted())
        {
            _tutorialManager.StartTutorial();
            StartCoroutine(ShowTutorialStepWithDelayCoroutine(_tutorialManager.GetCurrentStepIndex(), 2f));
        }
    }
    
    private void RestoreImprovementPopup()
    {
        Debug.Log("Restoring improvement popup state");
        
        // Восстанавливаем попап с сохраненными данными
        _improvementChosePopup.ShowPopup(
            YG2.saves.CurrentImprovementOptions
        );
        
        // Очищаем флаг, чтобы при следующей загрузке не восстанавливать повторно
        YG2.saves.IsImprovementPopupOpen = false;
        YG2.SaveProgress();
    }

    private void RestoreMetaImprovementPopup()
    {
        Debug.Log("Restoring meta improvement popup state");
        
        // Восстанавливаем мета-попап с сохраненными данными
        _metaImprovementPopup.ShowPopup(
            YG2.saves.CurrentImprovementOptions, _improvementSystem.CanAddMoreImprovements()
        );
        
        // Очищаем флаг, чтобы при следующей загрузке не восстанавливать повторно
        YG2.saves.IsMetaImprovementPopupOpen = false;
        YG2.SaveProgress();
    }

    private void StartNewGame()
    {
        Debug.Log("Start New Game");
        
        _letterBag.InitializeFromJson(lettersJson);
        SpawnNewLetters();
        if (_metaGameData.currentPlayerRecord > 0)
            _isTutorialShow = false;

        // _letterBag.DebugPrintLetterInventory();
        _roundManager.SetRoundPanelData();
        ShowLetterBagCount();
        _improvementPanel.ShowImprovements(YG2.saves.ActiveImprovements);
        OnScoreChanged?.Invoke(YG2.saves.TotalScore);
        YG2.SaveProgress();
        
        // if (_tutorialManager != null && !_tutorialManager.IsTutorialCompleted())
        // {
        //     _tutorialManager.StartTutorial();
        //     StartCoroutine(ShowTutorialStepWithDelayCoroutine(0, 2f));
        // }
    }

    // ========== GAME STATE MANAGEMENT ==========
    private void GameOver(bool isOutOfLetters)
    {
        Debug.Log("Game Over ");
        _leaderboardManager.TrySetScore(YG2.saves.TotalScore);
        _gameOverPopup.Show(isOutOfLetters, YG2.saves.TotalScore, YG2.saves.GameStatistics);
    }

    private void WinGame()
    {
        _leaderboardManager.TrySetScore(YG2.saves.TotalScore);
        _gameWinPopup.Show(YG2.saves.TotalScore, YG2.saves.GameStatistics);
    }

    private void LoadNewGame()
    {
        YG2.SetDefaultSaves();
        YG2.saves.GameStatistics = new GameStatistics();
        YG2.saves.IsImprovementPopupOpen = false;
        YG2.saves.IsMetaImprovementPopupOpen = false; // Очищаем флаг мета-попапа
        YG2.saves.CurrentImprovementOptions.Clear();
        YG2.saves.LastValidatedWord = null;
        YG2.saves.LastScoreResult = null;
    }
    
    private void OnLeaderboardUpdate(LBData lbData)
    {
        if (lbData.technoName == leaderboardYG.nameLB && lbData.currentPlayer != null)
        {
            _metaGameData.currentPlayerRecord = lbData.currentPlayer.score;
            Debug.Log($"Текущий рекорд: {_metaGameData.currentPlayerRecord}");
        }
    }

    // ========== SCORE MANAGEMENT ==========
    public event Action<int> OnScoreChanged;
    public event Action OnInitialized;

    private void AddScore(int amount)
    {
        YG2.saves.TotalScore += amount;
        
        OnScoreChanged?.Invoke(YG2.saves.TotalScore);
    }

    // ========== UI MANAGEMENT ==========
    private void ShowLetterBagPopup()
    {
        if (_letterBag.GetCountInLocation(LetterLocation.InBag) > 0)
        {
            _letterBagPopup.ShowPopup();
        }
        else
        {
            _giveUpPopup.Show();
        }
    }

    private void ShowLetterBagCount()
    {
        int count = _letterBag.GetCountInLocation(LetterLocation.InBag);
        letterBagButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"Letter bag: {count}";
        Debug.Log($"Show letter bag count {count}");
    }

    // ========== IMPROVEMENT SYSTEM ==========
    private void ImprovementSelectedProceed(bool isRoundEnded)
    {
        // Очищаем сохраненное состояние попапа
        YG2.saves.IsImprovementPopupOpen = false;
        YG2.saves.IsMetaImprovementPopupOpen = false; // Очищаем флаг мета-попапа
        YG2.saves.CurrentImprovementOptions.Clear();
        YG2.saves.LastValidatedWord = null;
        YG2.saves.LastScoreResult = null;
        
        if (isRoundEnded)
        {
            SpawnNewLetters();
            _improvementPanel.ShowImprovements(YG2.saves.ActiveImprovements);
            if (_tutorialManager.GetCurrentStepIndex() == 5)
            {
                _tutorialManager?.ShowSpecificStep("new_round");
            }
        }
        else
        {
            FillEmptyUnlockedSlots();
            if (_tutorialManager.GetCurrentStepIndex() == 3)
            {
                _tutorialManager?.ShowSpecificStep("round_goal");
            }
        }
        
        YG2.SaveProgress();
        
        // _tutorialManager.ShowNextStep();
    }
    
    private IEnumerator ShowTutorialStepWithDelayCoroutine(int stepIndex, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        _tutorialManager?.ShowSpecificStep(stepIndex);
    }
   
    // ========== OBJECT POOL MANAGEMENT ==========
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject letter = Instantiate(letterPrefab, lettersGrid);
            var draggableScript = letter.GetComponent<LetterTile>(); 
            draggableScript.Initialize(_wordPanelManager);
            letter.SetActive(false);
            letter.name = "PooledLetterObject" + i;
            _letterObjectsPool.Enqueue(letter);
        }
    }

    private GameObject GetPooledLetter()
    {
        if (_letterObjectsPool.Count > 0)
        {
            GameObject letter = _letterObjectsPool.Dequeue();
            if (letter.activeSelf)
            {
                Debug.LogError($"GetPooledLetter: Объект {letter.name} активен в пуле! Это ошибка.");
            }
            letter.SetActive(true);
            return letter;
        }
        
        Debug.LogWarning("Letter Pool is empty!");
        return null;
    }
    
    private void ReturnLetterObjectToPool(GameObject letter)
    {
        letter.transform.SetParent(lettersGrid);
        letter.SetActive(false);
        _letterObjectsPool.Enqueue(letter);
    }

    // ========== LETTER MANAGEMENT ==========

    private void EraseWord()
    {
        _wordPanelManager.ClearWordSlots(false);
    }
    private void RemoveInGameLetterObject(GameObject letter)
    {
        _allInGameLetters.Remove(letter);
        ReturnLetterObjectToPool(letter);
    }
    
    private void CreateLetterInSlot(Slot slot)
    {
        if (slot == null)
        {
            Debug.LogWarning("CreateLetterInSlot: Slot is null. Aborting.");
            return;
        }

        if (slot.IsLocked)
        {
            Debug.Log($"CreateLetterInSlot: Slot {slot.name} is locked. Aborting.");
            return;
        }
        
        var letter = _letterBag.DrawLetter();

        if (letter == null)
        {
            Debug.Log("CreateLetterInSlot: Letter bag is empty. Aborting.");
            return; // Если мешочек пуст
        }

        GameObject letterObj = GetPooledLetter();

        if (letterObj != null)
        {
            LetterTile draggable = letterObj.GetComponent<LetterTile>();
            
            draggable.SetText(letter);
            draggable.HomeSlot = slot;
            letterObj.transform.SetParent(slot.transform);
            draggable.SetScale();
            letterObj.transform.localPosition = Vector3.zero;
            _allInGameLetters.Add(letterObj);
        }
    }

    private void ClearBoard()
    {
        _wordPanelManager.ClearWordSlots(false);
        
        // Очищаем все буквы на игровом поле
        foreach (var letterObj in _allInGameLetters.ToList())
        {
            if (letterObj == null) continue;
        
            // Возвращаем букву в мешок, если она еще не использована
            var letterTile = letterObj.GetComponent<LetterTile>();
            if (letterTile != null && letterTile.Letter != null)
            {
                _letterBag.MoveLetter(letterTile.Letter, LetterLocation.OnBoard, LetterLocation.InBag);
            }
        
            // Возвращаем объект в пул
            ReturnLetterObjectToPool(letterObj);
        }
        _allInGameLetters.Clear();

        // Очищаем все слоты на доске
        foreach (var slot in letterSlots)
        {
            if (slot == null) continue;
        
            // Удаляем все дочерние объекты (на случай, если что-то осталось)
            foreach (Transform child in slot.transform)
            {
                if (child != null && child.gameObject != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        ShowLetterBagCount();
    }
    
    private void SpawnNewLetters()
    {
        Debug.Log("Spawn New Letters");
        ClearBoard();
        
        foreach (var t in letterSlots)
        {
            if (t.IsLocked) continue;
            CreateLetterInSlot(t);
        }
        ShowLetterBagCount();
    }
    
    private void ShowOnBoardLetters()
    {
        ClearBoard();

        // Получаем все буквы на доске из мешочка
        var onBoardLetters = _letterBag.GetAllLetters()
            .Where(kv => _letterBag.GetLetterCount(kv.Key, LetterLocation.OnBoard) > 0)
            .SelectMany(kv => 
                Enumerable.Repeat(kv.Key, _letterBag.GetLetterCount(kv.Key, LetterLocation.OnBoard)))
            .ToList();
        
        Debug.Log($"Showing {onBoardLetters.Count} letters on board");

        if (onBoardLetters.Count <= 0) return;
        
        // Распределяем буквы по доступным слотам
        for (int i = 0; i < letterSlots.Length && i < onBoardLetters.Count; i++)
        {
            var slot = letterSlots[i];
            // if (slot.IsLocked) continue;

            var letter = onBoardLetters[i];
            GameObject letterObj = GetPooledLetter();

            if (letterObj != null)
            {
                LetterTile draggable = letterObj.GetComponent<LetterTile>();
                if (draggable != null)
                {
                    draggable.SetText(letter);
                    draggable.HomeSlot = slot;
                    letterObj.transform.SetParent(slot.transform);
                    draggable.SetScale();
                    letterObj.transform.localPosition = Vector3.zero;
                    _allInGameLetters.Add(letterObj);
                }
                else
                {
                    Debug.LogError($"ShowOnBoardLetters: LetterTile component not found on {letterObj.name}!");
                }
            }
        }
        ShowLetterBagCount();
    }

    private void HandleLetterReplaced(LetterData oldLetter, LetterData newLetter, int countChanged)
    {
        foreach (var letter in _allInGameLetters)
        {
            var letterTile = letter.GetComponent<LetterTile>();
            if (letterTile.Letter == oldLetter)
            {
                letterTile.SetLetter(newLetter);
                letterTile.UpdatePoints();
            }
        }
    }

    private void FillEmptyUnlockedSlots()
    {
        foreach (var slot in letterSlots)
        {
            if (!slot.IsLocked && slot.transform.childCount == 0)
                CreateLetterInSlot(slot);
        }
        ShowLetterBagCount();
    }

    // ========== WORD VALIDATION ==========
    private void CheckWord()
    {
        var result = _wordPanelManager.GetWordAndLetters();
        string word = result.word;
        List<LetterData> letterList = result.letterList;

        word = word.ToLower();
        Debug.Log($"Составлено  слово: {word}");

        if (word.Length < minWordLength)
        {
            Debug.Log("Слишком короткое слово");
            _wordPanelManager.ClearWordSlots(false);
            return;
        }

        var isValid = word.Contains('*') ? 
            _dictionaryManager.CheckWordWithWildcards(word, letterList) : 
            _dictionaryManager.CheckRegularWord(word);
        
        if (isValid)
        {
            Debug.Log($"Правильное слово: {word}");
            
            if (_tutorialManager != null && _tutorialManager.IsTutorialActive() 
                                         && _tutorialManager.GetCurrentStepIndex() == 1)
            {
                // Сохраняем данные слова для обработки после туториала
                _pendingWordData = (letterList, word);
                _tutorialManager?.ShowSpecificStep("tile_mechanics");
                // _tutorialManager.ShowNextStep();
            }
            else
            {
                // Если туториала нет, сразу обрабатываем слово
                ProcessValidWord(letterList, word);
            }
        }
        else
        {
            Debug.Log("Неизвестное слово");
            _wordPanelManager.ClearWordSlots(false);
        }
    }
    
    private (List<LetterData> letterList, string word)? _pendingWordData = null;

    private void ProcessPendingWord()
    {
        if (_pendingWordData.HasValue)
        {
            var (letterList, word) = _pendingWordData.Value;
            ProcessValidWord(letterList, word);
            _pendingWordData = null;
        }

        if (_tutorialManager.GetCurrentStepIndex() == 4)
        {
            // Debug.Log($"tutorial step: {_tutorialManager.GetCurrentStepIndex()}");
            StartCoroutine(ShowTutorialStepWithDelayCoroutine(4, 10f));
        }
    }

    // ========== WORD PROCESSING ==========
    private void ProcessValidWord(List<LetterData> letterList, string word)
    {
        var scoreResult = _scoreManager.CalculateWordScore(letterList);
        
        YG2.saves.GameStatistics.UpdateStatistics(word, scoreResult.WordScore, letterList);
        
        Debug.Log("Score calculated and saved");
    
        // Создаем локальный обработчик, который отпишется после выполнения
        Action animationCompleteHandler = null;
        animationCompleteHandler = () => 
        {
            Debug.Log("animation complete");
            _scoreAnimationController.OnAnimationComplete -= animationCompleteHandler;
        
            _letterBag.IncreaseWordPoints(letterList);
            AddScore(scoreResult.WordScore);
            WordProcessContinue(scoreResult, word);
        };

        // Подписываемся на событие
        _scoreAnimationController.OnAnimationComplete += animationCompleteHandler;
        _scoreAnimationController.StartAnimation(scoreResult);
    }

    private void WordProcessContinue(ScoreManager.ScoreResult scoreResult, string word)
    {
        Debug.Log("WordProcessContinue");
        bool isRoundEnds;
        _wordPanelManager.ClearWordSlots(true);
        
        var roundState = _roundManager.HandleWordConfirmed(scoreResult.WordScore);
        switch (roundState)
        {
            case RoundManager.RoundState.Success:
                ClearBoard();
                _letterBag.ReturnUsedLettersToBag();
                isRoundEnds = true;
                break;
            // case RoundManager.RoundState.Failed:
            //     GameOver(false);
            //     return;
            case RoundManager.RoundState.InProgress:
                isRoundEnds = false;
                break;
            // case RoundManager.RoundState.Win:
            //     WinGame();
            //     return;
            default:
                throw new ArgumentOutOfRangeException();
        }

        List<ImprovementRarity> improvementRarityList = RarityGenerator.GenerateRarityList();
        
        // if (isRoundEnds)
        // {
                // var excessRatio = _roundManager.CalculateRoundExcessRatio();
                // improvementRarityList = RarityGenerator.GetRoundCompletionRarities(excessRatio);
        // }
        // else
        // {
        //     var wordContributionPercentage = _roundManager.CalculateWordContributionPercentage(scoreResult.WordScore);
        //     improvementRarityList = RarityGenerator.GetWordRarities(wordContributionPercentage);
        // }

        _improvementSystem.GenerateImprovementOptions(isRoundEnds, improvementRarityList);
        
        SaveImprovementPopupState(word, scoreResult, isRoundEnds);
      
        if (isRoundEnds)
        {
            _metaImprovementPopup.ShowPopup(YG2.saves.CurrentImprovementOptions, 
                _improvementSystem.CanAddMoreImprovements());
        }
        else
        {
            _improvementChosePopup.ShowPopup(YG2.saves.CurrentImprovementOptions);
        }
        
        ShowLetterBagCount();
        _tutorialManager?.ShowSpecificStep("bag_improvements");
    }
    
    private void SaveImprovementPopupState(string word, ScoreManager.ScoreResult scoreResult, bool isMetaImprovement)
    {
        Debug.Log("SaveImprovementPopupState");
        
        if (isMetaImprovement)
        {
            YG2.saves.IsMetaImprovementPopupOpen = true;
            YG2.saves.IsImprovementPopupOpen = false;
        }
        else
        {
            YG2.saves.IsImprovementPopupOpen = true;
            YG2.saves.IsMetaImprovementPopupOpen = false;
        }
        
        YG2.saves.LastValidatedWord = word;
        YG2.saves.LastScoreResult = scoreResult;
        YG2.SaveProgress();
    }

    // ========== BOARD OPERATIONS ==========
    private void ShuffleLetters()
    {
        Debug.Log("Shuffling letters");
        // 1. Собираем все активные буквы с доски (кроме тех, что в wordSlots)
        List<(Slot slot, LetterTile letter)> filledSlots = new List<(Slot, LetterTile)>();
        
        foreach (var slot in letterSlots)
        {
            if (slot.transform.childCount > 0 && !slot.IsLocked)
            {
                var letter = slot.transform.GetChild(0).GetComponent<LetterTile>();
                if (letter == null) continue;
                letter.MoveToSlot(letterSlotsGread, false);
                filledSlots.Add((slot, letter));
            }
        }
        
        // 2. Перемешиваем только буквы из заполненных слотов
        var rng = new System.Random();
        var shuffledLetters = filledSlots.Select(x => x.letter).OrderBy(x => rng.Next()).ToList();

        // 3. Меняем буквы местами, сохраняя исходные слоты
        for (var i = 0; i < filledSlots.Count; i++)
        {
            var originalSlot = filledSlots[i].slot;
            var newLetter = shuffledLetters[i];
        
            // Меняем только букву в слоту
            newLetter.HomeSlot = originalSlot;
            newLetter.MoveToSlot(originalSlot.transform, true);
        }
    }
    
    private void Refresh()
    {
        if (_letterBag.GetCountInLocation(LetterLocation.InBag) > 0)
        {
            List<GameObject> lettersToReplace = new List<GameObject>();
            foreach (var letterObj in _allInGameLetters)
            {
                if (letterObj != null &&
                    !_wordPanelManager.ContainsLetter(letterObj)) // Check if the letter is NOT in word slots
                {
                    lettersToReplace.Add(letterObj);
                }
            }

            foreach (var letterObj in lettersToReplace)
            {
                if (letterObj != null)
                {

                    var letter = letterObj.GetComponentInChildren<LetterTile>().Letter;
                    var slot = letterObj.GetComponentInChildren<LetterTile>().HomeSlot;
                    _letterBag.MoveLetter(letter, LetterLocation.OnBoard, LetterLocation.Used);
                    ReturnLetterObjectToPool(letterObj); // Return object to pool.
                    _allInGameLetters.Remove(letterObj); // Remove from the in-game list
                    CreateLetterInSlot(slot);
                    ShowLetterBagCount();
                }
            }
            YG2.SaveProgress();
        }
        else
        {
            _giveUpPopup.Show();
        }
        
    }
}