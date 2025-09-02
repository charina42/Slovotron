using System;
using System.Collections.Generic;
using UnityEngine;

public enum LetterType
{
    Standard,
    Capital,
    Final,
    NeighborMultiplierLeft,
    NeighborMultiplierRight,
    Repeater,
    Return,
    Wild,
    Disposable
}

public class LetterData : IEquatable<LetterData>
{
    public char LetterChar;
    public int Points;
    public LetterType Type;

    public LetterData(char letterChar, int points, LetterType type)
    {
        LetterChar = letterChar;
        Points = points;
        Type = type;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as LetterData);
    }

    public bool Equals(LetterData other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return LetterChar == other.LetterChar && Points == other.Points && Type == other.Type;
    }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + LetterChar.GetHashCode();
            hash = hash * 23 + Points.GetHashCode();
            hash = hash * 23 + Type.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(LetterData left, LetterData right)
    {
        return EqualityComparer<LetterData>.Default.Equals(left, right);
    }

    public static bool operator !=(LetterData left, LetterData right)
    {
        return !(left == right);
    }
}

public enum LetterLocation
{
    InBag,    // В мешке
    OnBoard,  // На поле (вытянуты, но не сыграны)
    Used      // Использованные (возвращаются в мешок при вызове ReturnUsedLettersToBag)
}