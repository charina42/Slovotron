using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class GameStatistics
{
    public int totalWords;
    public int totalScore;
    public string longestWord;
    public int longestWordLength;
    public string highestScoringWord;
    public int highestWordScore;
    public Dictionary<char, int> LettersUsed;
    public int wildcardsUsed;
    
    public GameStatistics()
    {
        totalWords = 0;
        totalScore = 0;
        longestWord = "";
        longestWordLength = 0;
        highestScoringWord = "";
        highestWordScore = 0;
        LettersUsed = new Dictionary<char, int>();
        wildcardsUsed = 0;
    }
    
    public void UpdateStatistics(string word, int wordScore, List<LetterData> letters)
    {
        totalWords++;
        totalScore += wordScore;
        
        // Проверяем самое длинное слово
        if (word.Length > longestWordLength)
        {
            longestWord = word;
            longestWordLength = word.Length;
        }
        
        // Проверяем слово с наибольшим количеством очков
        if (wordScore > highestWordScore)
        {
            highestScoringWord = word;
            highestWordScore = wordScore;
        }
        
        // Считаем использование букв
        foreach (var letter in letters)
        {
            if (letter.LetterChar == '*')
            {
                wildcardsUsed++;
            }
            else
            {
                if (LettersUsed.ContainsKey(letter.LetterChar))
                {
                    LettersUsed[letter.LetterChar]++;
                }
                else
                {
                    LettersUsed[letter.LetterChar] = 1;
                }
            }
        }
    }
    
    public string GetStatisticsReport()
    {
        string report = "=== СТАТИСТИКА ИГРЫ ===\n";
        report += $"Всего слов: {totalWords}\n";
        report += $"Общий счет: {totalScore}\n";
        report += $"Самое длинное слово: {longestWord} ({longestWordLength} букв)\n";
        report += $"Слово с наибольшим счетом: {highestScoringWord} ({highestWordScore} очков)\n";
        report += $"Использовано wildcard-символов: {wildcardsUsed}\n";
        
        // Самые используемые буквы
        if (LettersUsed.Count > 0)
        {
            var topLetters = LettersUsed.OrderByDescending(x => x.Value).Take(5);
            report += "Самые используемые буквы:\n";
            foreach (var letter in topLetters)
            {
                report += $"- '{letter.Key}': {letter.Value} раз\n";
            }
        }
        
        return report;
    }
}