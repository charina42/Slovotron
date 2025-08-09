using UnityEngine;

public class UIManager
{

    private UIRoot _uiRoot;
    // private WordGameManager _wordGameManager;

    public void Initialize(UIRoot uiRoot, WordGameManager wordGameManager)
    {
        // _wordGameManager = wordGameManager;
        _uiRoot = uiRoot;
        wordGameManager.OnScoreChanged += UpdateScoreDisplay;
        // UpdateScoreDisplay(wordGameManager.TotalScore);
    }


    private void UpdateScoreDisplay(int newScore)
    {
        Debug.Log("Score: " + newScore);
        _uiRoot.ScoreText.text = $"Очки {newScore}";
    }

}
