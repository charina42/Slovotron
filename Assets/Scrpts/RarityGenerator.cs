using System;
using System.Collections.Generic;

public static class RarityGenerator
{
    private static readonly Random Random = new Random();
    
    private static readonly Dictionary<ImprovementRarity, int> RarityWeights = new Dictionary<ImprovementRarity, int>
    {
        { ImprovementRarity.Common, 60 },  // 60%
        { ImprovementRarity.Rare, 30 },    // 30%
        { ImprovementRarity.Epic, 10 }     // 10%
    };
    
    public static List<ImprovementRarity> GenerateRarityList()
    {
        var result = new List<ImprovementRarity>();
        
        for (int i = 0; i < 3; i++)
        {
            result.Add(GetWeightedRandomRarity());
        }
        
        return result;
    }
    
    private static ImprovementRarity GetWeightedRandomRarity()
    {
        int totalWeight = 0;
        foreach (var weight in RarityWeights.Values)
        {
            totalWeight += weight;
        }
        
        int randomValue = Random.Next(totalWeight);
        int currentWeight = 0;
        
        foreach (var rarity in RarityWeights)
        {
            currentWeight += rarity.Value;
            if (randomValue < currentWeight)
                return rarity.Key;
        }
        
        return ImprovementRarity.Common; // fallback
    }
    
    public static List<ImprovementRarity> GetWordRarities(float contributionPercentage)
    {
        List<ImprovementRarity> rarities = new List<ImprovementRarity>();

        if (contributionPercentage <= 25f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Common, ImprovementRarity.Common, ImprovementRarity.Common });
        }
        else if (contributionPercentage >= 26f && contributionPercentage <= 35f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Common, ImprovementRarity.Common, ImprovementRarity.Rare });
        }
        else if (contributionPercentage >= 36f && contributionPercentage <= 50f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Rare, ImprovementRarity.Rare, ImprovementRarity.Rare });
        }
        else if (contributionPercentage >= 51f && contributionPercentage <= 75f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Rare, ImprovementRarity.Rare, ImprovementRarity.Epic });
        }
        else if (contributionPercentage >= 76f && contributionPercentage <= 100f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Rare, ImprovementRarity.Epic, ImprovementRarity.Epic });
        }
        else if (contributionPercentage > 100f)
        {
            rarities.AddRange(new[] { ImprovementRarity.Epic, ImprovementRarity.Epic, ImprovementRarity.Epic });
        }

        return rarities;
    }
    
    public static List<ImprovementRarity> GetRoundCompletionRarities(float excessRatio)
    {
        List<ImprovementRarity> rarities = new List<ImprovementRarity>();

        if (excessRatio <= 1.0f)
        {
            // Просто прошли раунд без превышения
            rarities.AddRange(new[]
            {
                ImprovementRarity.Common,
                ImprovementRarity.Common,
                ImprovementRarity.Common
            });
        }
        else if (excessRatio > 1.0f && excessRatio <= 1.5f)
        {
            // Небольшое превышение (1-1.5x)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Common,
                ImprovementRarity.Common,
                ImprovementRarity.Rare
            });
        }
        else if (excessRatio > 1.5f && excessRatio <= 2.0f)
        {
            // Хорошее превышение (1.5-2x)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Common,
                ImprovementRarity.Rare,
                ImprovementRarity.Rare
            });
        }
        else if (excessRatio > 2.0f && excessRatio <= 3.0f)
        {
            // Отличное превышение (2-3x)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Rare,
                ImprovementRarity.Rare,
                ImprovementRarity.Rare
            });
        }
        else if (excessRatio > 3.0f && excessRatio <= 4.0f)
        {
            // Выдающееся превышение (3-4x)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Rare,
                ImprovementRarity.Rare,
                ImprovementRarity.Epic
            });
        }
        else if (excessRatio > 4.0f)
        {
            // Феноменальное превышение (4x+)
            rarities.AddRange(new[]
            {
                ImprovementRarity.Rare,
                ImprovementRarity.Epic,
                ImprovementRarity.Epic
            });
        }

        return rarities;
    }
}