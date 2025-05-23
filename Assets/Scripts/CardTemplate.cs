using System.Collections.Generic;
using UnityEngine;

// Enum to define the different types of card effects we can have in the POC
public enum EffectType
{
    Undefined,
    NoEffect,          // does nothing
    Delete,
    Add,
    ModEnergy,
    ModEnergyNextTurn,
    Transform,
    Exile,
}

public enum EffectTarget
{
    Undefined,
    NoTarget,
    Opponent,
    Self
}

public enum EffectMode
{
    Undefined,
    Top,
    Bottom,
    Random,

}

public enum RarityLevel
{
    Undefined,
    Common,
    Uncommon,
    Rare,
    Mythic,
    Enemy,
    Token
}

public enum ValueType
{
    Undefined,
    Constant,
    Variable
}

[System.Serializable]
public struct CardInfo
{
    public string Title;
    [TextArea] // Makes description easier to edit in Inspector
    public string Description;
    public int EnergyCost;
    public RarityLevel Rarity;
    public bool Swappable;

    // parameters that only matter to downstream objects, irrelevant for card template
    public int UID;

    public CardInfo(string title, string description, int energyCost, RarityLevel rarity,
            bool unswappable, int index = -1, int uid = -1)
    {
        Title = title;
        Description = description;
        EnergyCost = energyCost;
        Rarity = rarity;
        Swappable = unswappable;
        UID = uid;
    }

    public string GetDisplayText()
    {
        return $"{Title} [{EnergyCost}]";
    }
    public string GetDescription()
    {
        return $"{Title} [{EnergyCost}] - {Description}";
    }
    // TODO: generate description from list of effects
}

[System.Serializable]
public struct VariableValue
{
    // FIXME: implement this    
}

[System.Serializable]
public struct EffectValue
{
    public ValueType Type;
    public int Constant;
    public VariableValue Variable;
}

[System.Serializable]
public struct CardEffect
{
    public EffectType Type;
    public EffectTarget Target;
    public EffectMode Mode;
    public List<EffectValue> Values;
    public CardTemplate ReferenceCardTemplate;
}

[CreateAssetMenu(fileName = "New Card", menuName = "Card")] // Makes it easy to create card assets in Unity Editor
public class CardTemplate : ScriptableObject
{
    public CardInfo Info;

    // --- Effect Information ---
    public List<CardEffect> Effects;
    public List<CardEffect> OnDeleteEffects;
}
