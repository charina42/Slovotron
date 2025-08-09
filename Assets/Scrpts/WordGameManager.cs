using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Scrpts;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using YG;
using YG.Utils.LB;

namespace YG
{
    public partial class SavesYG
    {
        public int TotalScore { get; set; }
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
    [SerializeField] public LeaderboardYG leaderboardYG;

    private bool _isTutorialShow = true;

    private readonly Queue<GameObject> _letterObjectsPool = new Queue<GameObject>();
    private readonly Dictionary<int, List<string>> _wordsByLengthDictionary = new Dictionary<int, List<string>>();
    
    private readonly List<GameObject> _allInGameLetters = new List<GameObject>();
    // private readonly List<GameObject> _lettersInWordSlots = new List<GameObject>() ; 
    // public int TotalScore { get; private set; }

    public event Action<int> OnScoreChanged;
    public event Action OnInitialized;
    // public static event Action<bool> OnClearWordSlots;
    
    private LetterBag _letterBag;
    private WordPanelManager _wordPanelManager;
    private LetterBagPopup _letterBagPopup;
    private RoundManager _roundManager;
    private ScoreManager _scoreManager;
    private ImprovementSystem _improvementSystem;
    
    private ImprovementPanel _improvementPanel;
    private ImprovementChosePopup _improvementChosePopup;
    private GameOverPopup _gameOverPopup;
    private GiveUpPopup _giveUpPopup;
    private GameWinPopup _gameWinPopup;
    private LeaderboardManager _leaderboardManager;

    private MetaGameData _metaGameData;


    public void Initialize(MetaGameData metaGameData, LetterBag letterBag, WordPanelManager wordPanelManager, LetterBagPopup letterBagPopup,
        RoundManager roundManager, ScoreManager scoreManager, ImprovementSystem improvementSystem, 
        ImprovementPanel improvementPanel, ImprovementChosePopup improvementChosePopup, GameOverPopup gameOverPopup,
        GiveUpPopup giveUpPopup, GameWinPopup gameWinPopup, LeaderboardManager leaderboardManager)
    {
        _metaGameData = metaGameData; 
        _letterBag = letterBag;
         _letterBagPopup = letterBagPopup;
         _roundManager = roundManager;
         _scoreManager = scoreManager;
         _improvementSystem = improvementSystem;
         _improvementPanel = improvementPanel;
         _improvementChosePopup = improvementChosePopup;
         _wordPanelManager = wordPanelManager;
         _gameOverPopup = gameOverPopup;
         _giveUpPopup = giveUpPopup;
         _gameWinPopup = gameWinPopup;
         _leaderboardManager = leaderboardManager;
        
        submitButton.onClick.AddListener(CheckWord);
        refreshButton.onClick.AddListener(Refresh);
        letterBagButton.onClick.AddListener(ShowLetterBagPopup);
        shuffleButton.onClick.AddListener(ShuffleLetters);
        
        ImprovementSystem.OnImprovementSelected += ImprovementSelectedProceed;
        _letterBag.OnLetterReplaced += HandleLetterReplaced;
        GiveUpPopup.OnGiveUpSelected += GameOver;
        GameOverPopup.OnNewGameSelected += LoadNewGame;
        GameWinPopup.OnNewGameSelected += LoadNewGame;
        WordPanelManager.OnRemoveInGameLetter += RemoveInGameLetterObject;
        YG2.onDefaultSaves += StartNewGame;
        YG2.onGetLeaderboard += OnLeaderboardUpdate;
        this.OnInitialized += StartGame;
        
        YG2.GetLeaderboard(leaderboardYG.nameLB);
        
        Debug.Log("WordGameManager initialized");
        OnInitialized?.Invoke();
    }

    private void StartGame()
    {
        Debug.Log("StartGame");
        
        InitializePool();
        LoadDictionary();

        _letterBag.InitializeBasePoints(lettersJson);
        // _letterBag.DebugPrintLetterInventory();
        ShowOnBoardLetters();
        // ShuffleLetters();
        _improvementPanel.ShowImprovements(YG2.saves.ActiveImprovements);
        
        OnScoreChanged?.Invoke(YG2.saves.TotalScore);
        _roundManager.SetRoundPanelData();
        ShowLetterBagCount();
    }

    private void StartNewGame()
    {
        Debug.Log("Start New Game");
        
        // _letterBag.DebugPrintLetterInventory();
        
        _letterBag.InitializeFromJson(lettersJson);
        // _allInGameLetters.Clear();
        // _lettersInWordSlots.Clear();
        SpawnNewLetters();
        if (_metaGameData.currentPlayerRecord > 0)
            _isTutorialShow = false;

        _letterBag.DebugPrintLetterInventory();
        _roundManager.SetRoundPanelData();
        ShowLetterBagCount();
        _improvementPanel.ShowImprovements(YG2.saves.ActiveImprovements);
        OnScoreChanged?.Invoke(YG2.saves.TotalScore);
        YG2.SaveProgress();
    }
    
    private void GameOver(bool isOutOfLetters)
    {
        Debug.Log("Game Over ");
        _leaderboardManager.TrySetScore(YG2.saves.TotalScore);
        _gameOverPopup.Show(isOutOfLetters, YG2.saves.TotalScore);
    }

    private void WinGame()
    {
        _leaderboardManager.TrySetScore(YG2.saves.TotalScore);
        _gameWinPopup.Show(YG2.saves.TotalScore);
    }

    private void LoadNewGame()
    {
        YG2.SetDefaultSaves();
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    private void OnLeaderboardUpdate(LBData lbData)
    {
        if (lbData.technoName == leaderboardYG.nameLB && lbData.currentPlayer != null)
        {
            // Debug.Log($"");
            _metaGameData.currentPlayerRecord = lbData.currentPlayer.score;
            // previousRecordText.text = currentPlayerRecord.ToString();
            Debug.Log($"Текущий рекорд: {_metaGameData.currentPlayerRecord}");
        }
    }

    private void AddScore(int amount)
    {
        YG2.saves.TotalScore += amount;
        
        OnScoreChanged?.Invoke(YG2.saves.TotalScore);
        // Здесь можно добавить логику сохранения
    }
    
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


    private void ImprovementSelectedProceed(bool isRoundEnded)
    {
        if (isRoundEnded)
        {
            SpawnNewLetters();
            _improvementPanel.ShowImprovements(YG2.saves.ActiveImprovements);
        }
        else
        {
            FillEmptyUnlockedSlots();
        }
        
        YG2.SaveProgress();
    }

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
                // letter.SetActive(false); // Принудительно деактивируем
            }
            letter.SetActive(true);
            return letter;
        }
        
        Debug.LogWarning("Letter Pool is empty!");
        // GameObject newLetter = Instantiate(letterPrefab, lettersGrid);
        return null;
    }
    
    private void ReturnLetterObjectToPool(GameObject letter)
    {
        letter.transform.SetParent(lettersGrid);
        letter.SetActive(false);
        _letterObjectsPool.Enqueue(letter);
    }

    private void LoadDictionary()
    {
        TextAsset dictFile = Resources.Load<TextAsset>(wordsJson);
        
        var data = JsonUtility.FromJson<SerializableDictionary>(dictFile.text);
        
        foreach (var wordList in data.wordLists)
        {
            _wordsByLengthDictionary[wordList.length] = wordList.words;
        }
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
        // Очищаем слоты для слов
        // OnClearWordSlots?.Invoke(false);
        
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
            // OnClearWordSlots?.Invoke(false);
            return;
        }

        bool isValid = word.Contains('*') ? CheckWordWithWildcards(word, letterList) : CheckRegularWord(word);
        
        if (isValid)
        {
            Debug.Log($"Правильное слово: {word}");

            ProcessValidWord(letterList, word);
        }
        else
        {
            Debug.Log("Неизвестное слово");
            _wordPanelManager.ClearWordSlots(false);
            // OnClearWordSlots?.Invoke(false);
        }
    }

    private bool CheckRegularWord(string word)
    {
        bool exists = _wordsByLengthDictionary.ContainsKey(word.Length) && 
                      _wordsByLengthDictionary[word.Length].Contains(word);
        
        return exists;
    }

    private bool CheckWordWithWildcards(string pattern,  List<LetterData> letterList)
    {
        int length = pattern.Length;
        if (!_wordsByLengthDictionary.ContainsKey(length))
            return false;
        
        int maxScore = -1;
        string bestMatch = null;
        
        foreach (string word in _wordsByLengthDictionary[length])
        {
            if (MatchesWildcardPattern(word, pattern))
            {
                    bestMatch = word;
                    break;
            }
        }
        
        if (bestMatch != null)
        {
            Debug.Log($"Лучшее совпадение: {bestMatch} (Очки: {maxScore})");
            
            return true;
        }

        return false;
    }

    private bool MatchesWildcardPattern(string word, string pattern)
    {
        for (int i = 0; i < word.Length; i++)
        {
            if (pattern[i] != '*' && pattern[i] != word[i])
                return false;
        }
        return true;
    }

    private void ProcessValidWord(List<LetterData> letterList, string word)
    {
        var scoreResult = _scoreManager.CalculateWordScore(letterList);
        AddScore(scoreResult.TotalScore);

        _letterBag.IncreaseWordPoints(letterList);

        _wordPanelManager.PlayWordJumpAnimation(
            onComplete: () => WordProcessContinue(scoreResult, word),
            jumpPower: 10f,
            duration: 0.2f,
            delayBetweenLetters: 0.15f);
    }

    private void WordProcessContinue(ScoreManager.ScoreResult scoreResult, string word)
    {
        bool isRoundEnds;
        _wordPanelManager.ClearWordSlots(true);
        // OnClearWordSlots?.Invoke(true);
        
        var roundState = _roundManager.HandleWordConfirmed(scoreResult.TotalScore);
        switch (roundState)
        {
            case RoundManager.RoundState.Success:
                // End round
                ClearBoard();
                _letterBag.ReturnUsedLettersToBag();
                isRoundEnds = true;
                break;
            case RoundManager.RoundState.Failed:
                GameOver(false);
                return;
            case RoundManager.RoundState.InProgress:
                isRoundEnds = false;
                break;
            case RoundManager.RoundState.Win:
                WinGame();
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        var improvementOptions = _improvementSystem.ShowImprovementOptions(isRoundEnds);
        YG2.SaveProgress();
        
        _improvementChosePopup.ShowPopup(improvementOptions, word,  scoreResult);
        ShowLetterBagCount();
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
        System.Random rng = new System.Random();
        var shuffledLetters = filledSlots.Select(x => x.letter).OrderBy(x => rng.Next()).ToList();

        // 3. Меняем буквы местами, сохраняя исходные слоты
        for (int i = 0; i < filledSlots.Count; i++)
        {
            var originalSlot = filledSlots[i].slot;
            var newLetter = shuffledLetters[i];
        
            // Меняем только букву в слоту
            newLetter.HomeSlot = originalSlot;
            newLetter.MoveToSlot(originalSlot.transform, true);
            // newLetter.transform.SetParent(originalSlot.transform);
            // newLetter.transform.localPosition = Vector3.zero;
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

[System.Serializable]
public class SerializableDictionary
{
    public List<WordList> wordLists = new List<WordList>();

    public SerializableDictionary(Dictionary<int, List<string>> dict)
    {
        foreach (var kvp in dict)
        {
            wordLists.Add(new WordList(kvp.Key, kvp.Value));
        }
    }
}

[System.Serializable]
public class WordList
{
    public int length;
    public List<string> words;

    public WordList(int length, List<string> words)
    {
        this.length = length;
        this.words = words;
    }
}