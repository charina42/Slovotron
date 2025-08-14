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
        public int TotalScore { get; set; }
    
        public string GetFullDescription(string word)
        {
            var desc = $"Слово: {word}\n";
            desc += "Буквы:\n";
            foreach (var letter in LetterScores)
            {
                desc += $"- {letter.Letter.LetterChar}: {letter.Points} очков";
                if (letter.IsCapital) desc += " (заглавная)";
                if (letter.IsFinal) desc += " (финальная)";
                desc += "\n";
            }
    
            if (BonusScores.Count > 0)
            {
                desc += "Бонусы:\n";
                foreach (var bonus in BonusScores)
                {
                    string bonusDesc = bonus.IsFromImprovement ? 
                        $"+{bonus.Points} ({bonus.SourceImprovement.shortDescription})" :
                        $"+{bonus.Points} ({bonus.Description})";
            
                    desc += bonusDesc;
            
                    if (bonus.IsFromImprovement && !string.IsNullOrEmpty(bonus.SourceImprovement.EffectType))
                        desc += $" [Улучшение: {bonus.SourceImprovement.EffectType}]";
                    desc += "\n";
                }
            }
    
            desc += $"Итого: {TotalScore} очков";
            return desc;
        }
    }

    public class LetterScore
    {
        public LetterData Letter { get; set; }
        public int Points { get; set; }
        public bool IsCapital { get; set; }
        public bool IsFinal { get; set; }
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

    public void Initialize(LetterBag letterBag, ImprovementSystem improvementSystem)
    {
        // _letterBag = letterBag;
        // _improvementSystem = improvementSystem;
    }

    public ScoreResult CalculateWordScore(List<LetterData> letterDataList)
    {
        var result = new ScoreResult();
        string word = BuildWord(letterDataList);
        
        foreach (var letter in letterDataList)
        {
            result.LetterScores.Add(new LetterScore
            {
                Letter = letter,
                Points = letter.Points,
                IsCapital = letter.Type == LetterType.Capital,
                IsFinal = letter.Type == LetterType.Final
            });
        }
        result.TotalScore = result.LetterScores.Sum(l => l.Points);
        
        // Бонусы за специальные буквы
        if (letterDataList[0].Type == LetterType.Capital)
        {
            result.BonusScores.Add(BonusScore.CreateBaseBonus(
                "CapitalLetter",
                10));
            result.TotalScore += 10;
        }

        if (letterDataList[^1].Type == LetterType.Final)
        {
            result.BonusScores.Add(BonusScore.CreateBaseBonus(
                "FinalLetter",
                10));
            result.TotalScore += 10;
        }
        
        ApplyBonuses(result, word, letterDataList);
        
        UpdateWordChain(word);
        
        Debug.Log(result.GetFullDescription(word));
        return result;
    }

    private string BuildWord(List<LetterData> letters)
    {
        return string.Concat(letters.Select(l => l.LetterChar));
    }

    // private int CalculateBaseScore(List<LetterData> letters)
    // {
    //     
    //     int score = letters.Sum(l => l.Points);
    //     
    //     if (letters[0].Type == LetterType.Capital)
    //         score += 10;
    //     
    //     if (letters[^1].Type == LetterType.Final)
    //         score += 10;
    //         
    //     return score;
    // }

    private void ApplyBonuses(ScoreResult result, string word, List<LetterData> letters)
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

    private void ApplyVowelFirstBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        if (Vowels.Contains(char.ToLower(word[0])))
        {
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.shortDescription, 5));
            result.TotalScore += 5;
        }
    }

    private void ApplyConsonantComboBonus(ImprovementOption improvement, ScoreResult result, string word, int minCombo, int bonusPerCombo)
    {
        int combos = CountLetterCombos(word, isVowel: false, minCombo);
        if (combos > 0)
        {
            int bonus = combos * bonusPerCombo;
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.shortDescription, bonus));
            result.TotalScore += bonus;
        }
    }

    private void ApplyVowelComboBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        int combos = CountLetterCombos(word, isVowel: true, minCombo: 2);
        if (combos > 0)
        {
            int bonus = combos * 5;
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.shortDescription, bonus));
            result.TotalScore += bonus;
        }
    }

    private int CountLetterCombos(string word, bool isVowel, int minCombo)
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

    private void ApplyDifferentPointsBonus(ImprovementOption improvement, ScoreResult result, List<LetterData> letters)
    {
        int uniquePoints = letters.GroupBy(l => l.Points).Count();
        if (uniquePoints > 1)
        {
            int bonus = uniquePoints * 2;
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.shortDescription, bonus));
            result.TotalScore += bonus;
        }
    }

    private void ApplyAllDifferentBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        if (word.Distinct().Count() == word.Length)
        {
            int bonus = (int)(result.TotalScore * 0.5f);
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.shortDescription, bonus));
            result.TotalScore += bonus;
        }
    }

    private void ApplyMirrorWordBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        if (word.Length > 1 && char.ToLower(word[0]) == char.ToLower(word[^1]))
        {
            int bonus = (int)(result.TotalScore * 0.5f);
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.shortDescription, bonus));
            result.TotalScore += bonus;
        }
    }

    private void ApplyWordChainBonus(ImprovementOption improvement, ScoreResult result, string word)
    {
        if (char.ToLower(word[0]) == char.ToLower(YG2.saves.lastLetterOfPreviousWord))
        {
            YG2.saves.wordChainCount++;
            int bonus = (int)(result.TotalScore * 1f);
            
            result.BonusScores.Add(BonusScore.FromImprovement(
                improvement, improvement.shortDescription, bonus));
            result.TotalScore += bonus;
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

    public void ResetScore()
    {
        // _totalScore = 0;
        YG2.saves.wordChainCount = 0;
        YG2.saves.lastLetterOfPreviousWord = ' ';
    }

    // public int GetCurrentScore() => _totalScore;
}
