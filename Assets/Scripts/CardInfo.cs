using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct CardInfo
{
    public string Title;
    public bool OverrideGeneratedDescription;
    [TextArea]
    public string Description;
    public int EnergyCost;
    public RarityLevel Rarity;
    public bool Swappable;

    // parameters that only matter to downstream objects, irrelevant for card template
    public int UID;

    public string GetDisplayText()
    {
        return $"{Title} [{EnergyCost}]";
    }
    public string GetDescription()
    {
        return $"{Title} [{EnergyCost}] - {Description}";
    }
    public void GenerateDescription(List<CardEffect> effects)
    {
        // TODO: implement this
    }
}