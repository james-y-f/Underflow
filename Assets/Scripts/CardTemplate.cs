using UnityEngine;

// Enum to define the different types of card effects we can have in the POC
public enum EffectType
{
    Undefined,
    NoEffect,          // does nothing
    Delete,
    Add,
    GainEnergy,
    GainEnergyNextTurn,
    // Add more basic types as needed for POC
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

public enum Rarity
{
    Undefined,
    Common,
    Uncommon,
    Rare,
    Mythic,
}

[System.Serializable]
public struct CardEffect
{
    public EffectType type;
    public EffectTarget target;
    public EffectMode mode;
    public int[] values;
    public CardTemplate referenceCard;
}

[CreateAssetMenu(fileName = "New Card", menuName = "Card")] // Makes it easy to create card assets in Unity Editor
public class CardTemplate : ScriptableObject
{
    public string title;
    [TextArea] // Makes description easier to edit in Inspector
    public string description;
    public int energyCost; // Renamed from 'cost' to be specific to Job Slots

    // --- Effect Information ---
    public CardEffect[] effects;

    // --- Optional: For future Durability mechanic ---
    // public bool hasDurability = false;
    // public int maxUses = 3;
    // [HideInInspector] public int currentUses = 0; // Runtime value, maybe track elsewhere

    // Helper to get a display string for the card
    public string GetDisplayText()
    {
        return $"{title} [{energyCost}]";
    }

    public string GetDescription()
    {
        return $"{title} [{energyCost}] - {description}";
    }
}
