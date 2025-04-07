using System;
using System.Collections.Generic;
using UnityEngine;

public enum Rarity
{
    Normal,
    Rare,
    Epic,
    Legendary
}

[Serializable]
public class Card
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Action<Nation, SquareGrid> Effect { get; private set; }

    public bool IsUnique { get; private set; }
    public int RequiredTechLevel { get; private set; }
    public string Category { get; private set; }
    public Rarity Rarity { get; private set; }
    public bool IsBlackTurnCard { get; private set; }

    public Card(
        string name,
        string description,
        Rarity rarity,
        bool isUnique,
        int requiredTechLevel,
        Action<Nation, SquareGrid> effect,
        bool isBlackTurnCard = false
    )
    {
        Name = name;
        Description = description;
        Rarity = rarity;
        IsUnique = isUnique;
        RequiredTechLevel = requiredTechLevel;
        Effect = effect;
        IsBlackTurnCard = isBlackTurnCard;
    }


    // Ylikirjoitetut Equals ja GetHashCode metodit
    public override bool Equals(object obj)
    {
        if (obj is Card otherCard)
        {
            return this.Name == otherCard.Name;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
