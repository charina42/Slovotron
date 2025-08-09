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
        public int TotalScore;
        public int BaseScore;
        public readonly List<BonusInfo> Bonuses = new List<BonusInfo>();
        
        public string GetFullDescription(string word)
        {
            var desc = $"Слово: {word}\nБазовые очки: {BaseScore}\n";
            foreach (var bonus in Bonuses)
            {
                desc += $"+{bonus.Amount} ({bonus.Description})\n";
            }
            desc += $"Итого: {TotalScore} очков";
            return desc;
        }
    }

    public class BonusInfo
    {
        public string Description;
        public int Amount;
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
        
        // Base calculation
        result.BaseScore = CalculateBaseScore(letterDataList);
        result.TotalScore = result.BaseScore;
        
        ApplyBonuses(result, word, letterDataList);
        
        UpdateWordChain(word);
        
        Debug.Log(result.GetFullDescription(word));
        return result;
    }

    private string BuildWord(List<LetterData> letters)
    {
        return string.Concat(letters.Select(l => l.LetterChar));
    }

    private int CalculateBaseScore(List<LetterData> letters)
    {
        int score = letters.Sum(l => l.Points);
        
        if (letters[0].Type == LetterType.Capital)
            score += 10;
        
        if (letters[^1].Type == LetterType.Final)
            score += 10;
            
        return score;
    }

    private void ApplyBonuses(ScoreResult result, string word, List<LetterData> letters)
    {
        foreach (var improvement in YG2.saves.ActiveImprovements)
        {
            switch (improvement.EffectType)
            {
                case "VowelFirstBonus":
                    ApplyVowelFirstBonus(result, word);
                    break;
                    
                case "ConsonantComboBonus":
                    ApplyConsonantComboBonus(result, word, minCombo: 3, bonusPerCombo: 8);
                    break;
                    
                case "ConsonantDuoBonus":
                    ApplyConsonantComboBonus(result, word, minCombo: 2, bonusPerCombo: 5);
                    break;
                    
                case "VowelDuoBonus":
                    ApplyVowelComboBonus(result, word);
                    break;
                    
                case "DifferentPoints":
                    ApplyDifferentPointsBonus(result, letters);
                    break;
                    
                case "AllDifferent":
                    ApplyAllDifferentBonus(result, word);
                    break;
                    
                case "MirrorWordMultiplier":
                    ApplyMirrorWordBonus(result, word);
                    break;
                    
                case "WordChainMultiplier":
                    ApplyWordChainBonus(result, word);
                    break;
            }
        }
    }

    private void ApplyVowelFirstBonus(ScoreResult result, string word)
    {
        if (Vowels.Contains(char.ToLower(word[0])))
        {
            result.Bonuses.Add(new BonusInfo {
                Description = "Гласная в начале слова",
                Amount = 5
            });
            result.TotalScore += 5;
        }
    }

    private void ApplyConsonantComboBonus(ScoreResult result, string word, int minCombo, int bonusPerCombo)
    {
        int combos = CountLetterCombos(word, isVowel: false, minCombo);
        if (combos > 0)
        {
            int bonus = combos * bonusPerCombo;
            result.Bonuses.Add(new BonusInfo {
                Description = $"Комбо согласных ({minCombo}+ буквы)",
                Amount = bonus
            });
            result.TotalScore += bonus;
        }
    }

    private void ApplyVowelComboBonus(ScoreResult result, string word)
    {
        int combos = CountLetterCombos(word, isVowel: true, minCombo: 2);
        if (combos > 0)
        {
            int bonus = combos * 5;
            result.Bonuses.Add(new BonusInfo {
                Description = "Комбо гласных (2+ буквы)",
                Amount = bonus
            });
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

    private void ApplyDifferentPointsBonus(ScoreResult result, List<LetterData> letters)
    {
        int uniquePoints = letters.GroupBy(l => l.Points).Count();
        if (uniquePoints > 1)
        {
            int bonus = uniquePoints * 2;
            result.Bonuses.Add(new BonusInfo {
                Description = "Уникальные значения букв",
                Amount = bonus
            });
            result.TotalScore += bonus;
        }
    }

    private void ApplyAllDifferentBonus(ScoreResult result, string word)
    {
        if (word.Distinct().Count() == word.Length)
        {
            int bonus = (int)(result.BaseScore * 0.5f);
            result.Bonuses.Add(new BonusInfo {
                Description = "Все буквы уникальны",
                Amount = bonus
            });
            result.TotalScore += bonus;
        }
    }

    private void ApplyMirrorWordBonus(ScoreResult result, string word)
    {
        if (word.Length > 1 && char.ToLower(word[0]) == char.ToLower(word[^1]))
        {
            int bonus = (int)(result.BaseScore * 0.5f);
            result.Bonuses.Add(new BonusInfo {
                Description = "Зеркальное слово",
                Amount = bonus
            });
            result.TotalScore += bonus;
        }
    }

    private void ApplyWordChainBonus(ScoreResult result, string word)
    {
        if (char.ToLower(word[0]) == char.ToLower(YG2.saves.lastLetterOfPreviousWord))
        {
            YG2.saves.wordChainCount++;
            int bonus = YG2.saves.wordChainCount switch
            {
                < 3 => 5,
                3 or 4 => 10,
                >= 5 => 15
            };
            
            result.Bonuses.Add(new BonusInfo {
                Description = $"Цепочка слов ({YG2.saves.wordChainCount})",
                Amount = bonus
            });
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

// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
//
// public class ScoreManager 
// {
//     // private WordGameManager _wordGameManager;
//     private LetterBag _letterBag;
//     private ImprovementSystem _improvementSystem;
//     private int _totalScore = 0;
//     
//     // Событие для обновления UI
//     public delegate void ScoreUpdatedHandler(int newScore);
//     public event ScoreUpdatedHandler OnScoreUpdated;
//     
//     private char _lastLetterOfPreviousWord = ' ';
//     private int _wordChainCount = 0;
//     
//     private static readonly char[] Vowels = new[] { 'а', 'е', 'ё', 'и', 'о', 'у', 'ы', 'э', 'ю', 'я' };
//     
//     public void Initialize( LetterBag letterBag, ImprovementSystem improvementSystem )
//     {
//         _letterBag = letterBag;
//         _improvementSystem = improvementSystem;
//     }
//
//     public int CalculateWordScore(List<LetterData> letterDataList)
//     {
//        int wordScore = 0;
//        string word = "";
//        string description = "";
//         
//         // Считаем очки за буквы
//         foreach (var letter in letterDataList)
//         {
//             // Debug.Log(letter);
//             word += letter.LetterChar;
//             wordScore += letter.Points;
//         }
//
//         description += $"Слово '{word}' принесло {wordScore} очков. \n";
//         
//         
//         if (letterDataList[0].Type == LetterType.Capital)
//         {
//             wordScore += 10;
//             description += " За заглавную букву +10 очков. \n";
//         }
//
//         if (letterDataList[^1].Type == LetterType.Final)
//         {
//             wordScore += 10;
//             description += " За финальную букву +10 очков. \n";
//         }
//         
//         CalculateImprovementScore(word, wordScore, letterDataList);
//
//         description += $" Всего {wordScore} очков.";
//         AddScore(wordScore);
//         
//         Debug.Log(description);
//         // Debug.Log($"Слово '{word}' принесло {wordScore} очков. Общий счет: {_totalScore}");
//         
//         _lastLetterOfPreviousWord = word[^1];
//         return wordScore;
//     }
//
//     private void CalculateImprovementScore(string word, int wordScore, List<LetterData> letterDataList)
//     {
//         Debug.Log("CalculateImprovementScore");
//         var activeImprovements = _improvementSystem.ActiveImprovements;
//         // var scoreBonus = 0;
//         var bonus = 0;
//         foreach (var improvement in activeImprovements)
//         {
//             Debug.Log($"activeImprovements EffectType {improvement.EffectType} ");
//             switch (improvement.EffectType)
//             {
//                 case "VowelFirstBonus":
//                     if (Vowels.Contains(char.ToLower(word[0])))
//                     {
//                         bonus = 5;
//                         wordScore += bonus;
//                         Debug.Log($"VowelFirstBonus added {bonus} ");
//                     }
//
//                     break;
//
//                 case "ConsonantComboBonus":
//                     int comboCount = 0;
//                     int currentCombo = 0;
//
//                     foreach (char c in word.ToLower())
//                     {
//                         if (!Vowels.Contains(c)) 
//                         {
//                             currentCombo++;
//                             if (currentCombo >= 3)
//                                 comboCount++;
//                         }
//                         else
//                             currentCombo = 0;
//                     }
//
//                     bonus = comboCount * 8;
//
//                     wordScore += bonus;
//                     Debug.Log($"ConsonantComboBonus added {bonus} ");
//                     break;
//                 
//                 case "ConsonantDuoBonus":
//                     comboCount = 0;
//                     currentCombo = 0;
//
//                     foreach (char c in word.ToLower())
//                     {
//                         if (!Vowels.Contains(c))
//                         {
//                             currentCombo++;
//                             if (currentCombo >= 2)
//                                 comboCount++;
//                         }
//                         else
//                             currentCombo = 0;
//                     }
//                     bonus = comboCount *5;
//
//                     wordScore += bonus;
//                     Debug.Log($"ConsonantDuoBonus added {bonus} ");
//                     break;
//                 
//                 case "VowelDuoBonus":
//                     comboCount = 0;
//                     currentCombo = 0;
//
//                     foreach (char c in word.ToLower())
//                     {
//                         if (Vowels.Contains(c))
//                         {
//                             currentCombo++;
//                             if (currentCombo >= 2)
//                                 comboCount++;
//                         }
//                         else
//                             currentCombo = 0;
//                     }
//                     bonus = comboCount *5;
//
//                     wordScore += bonus;
//                     Debug.Log($"ConsonantDuoBonus added {bonus} ");
//                     break;
//                     
//                 case "DifferentPoints":
//                     var uniquePointCount = letterDataList
//                         .GroupBy(letter => letter.Points)
//                         .Count();
//                     bonus = (int)(uniquePointCount * 2);
//                     wordScore += bonus;
//                     Debug.Log($"DifferentPoints added {bonus} ");
//                     break;
//                 
//                 case "AllDifferent":
//                     if (word.Distinct().Count() == word.Length)
//                     {
//                         bonus = (int)(wordScore * 2);
//                         wordScore += bonus;
//                         Debug.Log($"AllDifferent added {bonus} ");
//                     }
//                     break;
//
//                 case "MirrorWordMultiplier":
//                     if (char.ToLower(word[0]) == char.ToLower(word[^1]))
//                     {
//                         bonus = (int)(wordScore * 1.5);
//                         wordScore += bonus;
//                         Debug.Log($"MirrorWordMultiplier added {bonus} ");
//                     }
//
//                     break;
//                
//                 case "WordChainMultiplier":
//                     if (char.ToLower(word[0]) == char.ToLower(_lastLetterOfPreviousWord))
//                     {
//                         _wordChainCount++;
//                         var multiplier = _wordChainCount switch
//                         {
//                             < 3 => 5,
//                             3 or 4 => 10,
//                             >= 5 => 15
//                         };
//                         bonus = multiplier;
//                         wordScore += bonus;
//                         Debug.Log($"WordChainMultiplier added {bonus} ");
//                     }
//                     else
//                     {
//                         _wordChainCount = 0;
//                     }
//
//                     break;
//                 
//             }
//         }
//     }
//
//     private void AddScore(int amount)
//     {
//         _totalScore += amount;
//         OnScoreUpdated?.Invoke(_totalScore);
//     }
//
//     public void ResetScore()
//     {
//         _totalScore = 0;
//         OnScoreUpdated?.Invoke(_totalScore);
//     }
//
//     public int GetCurrentScore()
//     {
//         return _totalScore;
//     }
//
//    
// }