using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YG;

namespace YG
{
    public partial class SavesYG
    {
        public char lastLetterOfPreviousWord = ' ';
        public int wordChainCount = 0;
        
    }
}

public class ScoreManager 
{
    public class ScoreResult
    {
        public List<LetterScore> LetterScores { get; set; } = new List<LetterScore>();
        public List<BonusScore> BonusScores { get; set; } = new List<BonusScore>();
        public int WordScore { get; set; }
    
        public string GetFullDescription(string word)
        {
            var desc = $"Слово: {word}\n";
            desc += "Буквы:\n";
            foreach (var letter in LetterScores)
            {
                desc += $"- {letter.Letter.LetterChar}: {letter.Points} очков";
                if (letter.IsRepeater) desc += " (повторитель)";
                if (letter.IsCapital) desc += " (заглавная)";
                if (letter.IsFinal) desc += " (финальная)";
                if (letter.Letter != null)
                {
                    if (letter.Letter.Type == LetterType.NeighborMultiplierLeft) 
                        desc += " (множитель Left)";
                    if (letter.Letter.Type == LetterType.NeighborMultiplierRight) 
                        desc += " (множитель Right)";
                }
                desc += "\n";
            }

            if (BonusScores.Count > 0)
            {
                desc += "Бонусы:\n";
                foreach (var bonus in BonusScores)
                {
                    string bonusDesc = bonus.IsFromImprovement ? 
                        $"+{bonus.Points} ({bonus.SourceImprovement.ShortDescription})" :
                        $"+{bonus.Points} ({bonus.Description})";
        
                    desc += bonusDesc;
        
                    if (bonus.IsFromImprovement && !string.IsNullOrEmpty(bonus.SourceImprovement.EffectType))
                        desc += $" [Улучшение: {bonus.SourceImprovement.EffectType}]";
                    desc += "\n";
                }
            }

            desc += $"Итого: {WordScore} очков";
            return desc;
        }
    }

    public class LetterScore
    {
        public LetterData Letter { get; set; }
        public int Points { get; set; }
        public bool IsCapital { get; set; }
        public bool IsFinal { get; set; }
        public bool IsRepeater { get; set; }
        public bool IsNeighborMultiplierLeft => Letter?.Type == LetterType.NeighborMultiplierLeft;
        public bool IsNeighborMultiplierRight => Letter?.Type == LetterType.NeighborMultiplierRight;
    }

    public class BonusScore
    {
        public ImprovementOption SourceImprovement { get; set; } // null для базовых бонусов
        public string Description { get; set; }
        public int Points { get; set; }
        public bool IsFromImprovement => SourceImprovement != null;
    
        // Для базовых бонусов (не от улучшений)
        public static BonusScore CreateBaseBonus(string description, int points)
        {
            return new BonusScore
            {
                Description = description,
                Points = points
            };
        }
    
        // Для бонусов от улучшений
        public static BonusScore FromImprovement(ImprovementOption improvement, string description, int points)
        {
            return new BonusScore
            {
                SourceImprovement = improvement,
                Description = description,
                Points = points
            };
        }
    }
    
    
    private static readonly char[] Vowels = { 'а', 'е', 'ё', 'и', 'о', 'у', 'ы', 'э', 'ю', 'я' };

    public ScoreResult CalculateWordScore(List<LetterData> letterDataList)
    {
        Debug.Log("CalculateWordScore");
        
        var result = new ScoreResult();
        var word = string.Concat(letterDataList.Select(l => l.LetterChar));
    
        // Сначала собираем базовые очки букв
        foreach (var letter in letterDataList)
        {
            result.LetterScores.Add(new LetterScore
            {
                Letter = letter,
                Points = letter.Points,
                IsCapital = letter.Type == LetterType.Capital,
                IsFinal = letter.Type == LetterType.Final,
                IsRepeater = letter.Type == LetterType.Repeater
            });
        }
    
        // Применяем множители соседей
        ApplyNeighborMultipliers(result, letterDataList);
    
        // Обрабатываем буквы-повторители
        ProcessRepeaterLetters(result, letterDataList);
    
        result.WordScore = result.LetterScores.Sum(l => l.Points);
    
        // Бонусы за специальные буквы
        if (letterDataList[0].Type == LetterType.Capital)
        {
            result.BonusScores.Add(BonusScore.CreateBaseBonus(
                "CapitalLetter",
                5));
            result.WordScore += 5;
        }

        if (letterDataList[^1].Type == LetterType.Final)
        {
            result.BonusScores.Add(BonusScore.CreateBaseBonus(
                "FinalLetter",
                5));
            result.WordScore += 5;
        }
    
        ApplyBonuses(result, word, letterDataList);
    
        UpdateWordChain(word);
    
        Debug.Log(result.GetFullDescription(word));
        return result;
    }
    
    private void ProcessRepeaterLetters(ScoreResult result, List<LetterData> letterDataList)
    {
        // Находим все буквы-повторители
        var repeaterLetters = letterDataList
            .Where(l => l.Type == LetterType.Repeater)
            .ToList();

        foreach (var repeater in repeaterLetters)
        {
            // Находим индекс этой буквы в результате
            var repeaterScore = result.LetterScores
                .FirstOrDefault(ls => ls.Letter == repeater);
                
            if (repeaterScore != null)
            {
                // Считаем количество таких же букв в слове (включая саму себя)
                int sameLetterCount = letterDataList
                    .Count(l => l.LetterChar == repeater.LetterChar);
                    
                // Умножаем очки повторителя на количество одинаковых букв
                repeaterScore.Points = repeater.Points * sameLetterCount;
            }
        }
    }

    private void ApplyNeighborMultipliers(ScoreResult result, List<LetterData> letterDataList)
    {
        var baseScores = result.LetterScores.ToList();

        for (int i = 0; i < letterDataList.Count; i++)
        {
            var letter = letterDataList[i];
            var letterScore = result.LetterScores[i];

            if (letter.Type == LetterType.NeighborMultiplierLeft && i > 0)
            {
                // Умножаем левую соседнюю букву
                var leftNeighborScore = result.LetterScores[i - 1];
                int originalPoints = leftNeighborScore.Points;
                leftNeighborScore.Points *= 2;

                // Добавляем информацию о бонусе
                result.BonusScores.Add(BonusScore.CreateBaseBonus(
                    $"Множитель Left: буква '{leftNeighborScore.Letter.LetterChar}' удвоена",
                    originalPoints)); // Бонус равен исходной стоимости буквы

                Debug.Log(
                    $"Множитель Left: буква '{leftNeighborScore.Letter.LetterChar}' удвоена с {originalPoints} до {leftNeighborScore.Points}");
            }
            else if (letter.Type == LetterType.NeighborMultiplierRight && i < letterDataList.Count - 1)
            {
                // Умножаем правую соседнюю букву
                var rightNeighborScore = result.LetterScores[i + 1];
                int originalPoints = rightNeighborScore.Points;
                rightNeighborScore.Points *= 2;

                // Добавляем информацию о бонусе
                result.BonusScores.Add(BonusScore.CreateBaseBonus(
                    $"Множитель Right: буква '{rightNeighborScore.Letter.LetterChar}' удвоена",
                    originalPoints)); // Бонус равен исходной стоимости буквы

                Debug.Log(
                    $"Множитель Right: буква '{rightNeighborScore.Letter.LetterChar}' удвоена с {originalPoints} до {rightNeighborScore.Points}");
            }

            // Буквы-множители сами по себе приносят 0 очков
            if (letter.Type == LetterType.NeighborMultiplierLeft ||
                letter.Type == LetterType.NeighborMultiplierRight)
            {
                letterScore.Points = 0;
            }
        }
    }

    private static void ApplyBonuses(ScoreResult result, string word, List<LetterData> letters)
    {
        foreach (var improvement in YG2.saves.ActiveImprovements)
        {
            switch (improvement.EffectType)
            {
                case "VowelFirstBonus":
                    ApplyVowelFirstBonus(improvement, result, word);
                    break;
                    
                case "ConsonantComboBonus":
                    ApplyConsonantComboBonus(improvement,result, word, minCombo: 3, bonusPerCombo: 8);
                    break;
                    
                case "ConsonantDuoBonus":
                    ApplyConsonantComboBonus(improvement,result, word, minCombo: 2, bonusPerCombo: 5);
                    break;
                    
                case "VowelDuoBonus":
                    ApplyVowelComboBonus(improvement,result, word);
                    break;
                    
                case "DifferentPoints":
                    ApplyDifferentPointsBonus(improvement,result, letters);
                    break;
                    
                case "AllDifferent":
                    ApplyAllDifferentBonus(improvement,result, word);
                    break;
                    
                case "MirrorWordMultiplier":
                    ApplyMirrorWordBonus(improvement,result, word);
                    break;
                    
                case "WordChainMultiplier":
                    ApplyWordChainBonus(improvement, result, word);
                    break;
            }
        }
    }

    private static void ApplyVowelFirstBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        if (Vowels.Contains(char.ToLower(word[0])))
        {
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.ShortDescription, 5));
            result.WordScore += 5;
        }
    }

    private static void ApplyConsonantComboBonus(ImprovementOption improvement, ScoreResult result, string word, int minCombo, int bonusPerCombo)
    {
        int combos = CountLetterCombos(word, isVowel: false, minCombo);
        if (combos > 0)
        {
            int bonus = combos * bonusPerCombo;
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.ShortDescription, bonus));
            result.WordScore += bonus;
        }
    }

    private static void ApplyVowelComboBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        int combos = CountLetterCombos(word, isVowel: true, minCombo: 2);
        if (combos > 0)
        {
            int bonus = combos * 5;
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.ShortDescription, bonus));
            result.WordScore += bonus;
        }
    }

    private static int CountLetterCombos(string word, bool isVowel, int minCombo)
    {
        int combos = 0;
        int currentCombo = 0;

        foreach (char c in word.ToLower())
        {
            bool isMatch = isVowel ? Vowels.Contains(c) : !Vowels.Contains(c);
            
            if (isMatch)
            {
                currentCombo++;
                if (currentCombo >= minCombo) combos++;
            }
            else
            {
                currentCombo = 0;
            }
        }

        return combos;
    }

    private static void ApplyDifferentPointsBonus(ImprovementOption improvement, ScoreResult result, List<LetterData> letters)
    {
        int uniquePoints = letters.GroupBy(l => l.Points).Count();
        if (uniquePoints > 1)
        {
            int bonus = uniquePoints * 2;
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.ShortDescription, bonus));
            result.WordScore += bonus;
        }
    }

    private static void ApplyAllDifferentBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        if (word.Distinct().Count() == word.Length)
        {
            int bonus = (int)(result.WordScore * 0.5f);
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.ShortDescription, bonus));
            result.WordScore += bonus;
        }
    }

    private static void ApplyMirrorWordBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        if (word.Length > 1 && char.ToLower(word[0]) == char.ToLower(word[^1]))
        {
            int bonus = (int)(result.WordScore * 0.5f);
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.ShortDescription, bonus));
            result.WordScore += bonus;
        }
    }

    private static void ApplyWordChainBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        if (char.ToLower(word[0]) == char.ToLower(YG2.saves.lastLetterOfPreviousWord))
        {
            YG2.saves.wordChainCount++;
            int bonus = (int)(result.WordScore * 1f);
            
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.ShortDescription, bonus));
            result.WordScore += bonus;
        }
    }

    private void UpdateWordChain(string word)
    {
        if (char.ToLower(word[0]) != char.ToLower(YG2.saves.lastLetterOfPreviousWord))
        {
            YG2.saves.wordChainCount = 0;
        }
        YG2.saves.lastLetterOfPreviousWord = word[^1];
    }

}
