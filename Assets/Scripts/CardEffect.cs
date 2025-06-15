using UnityEngine;
using UnityEngine.Assertions;
using System.Text;
using System.Collections.Generic;
public enum EffectType
{
    Undefined,
    NoEffect,          // does nothing
    Delete,
    Add,
    ModEnergy,
    ModEnergyNextTurn,
    Transform,
    MakeUnswappable,
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
    RandomFromView,
    RandomFromDeck,
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

public enum DeckEntity
{
    Undefined,
    PlayerStack,
    PlayerDiscard,
    EnemyStack,
    EnemyDiscard
}

public enum Property
{
    Unswappable
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

    public override string ToString()
    {
        if (Type == ValueType.Constant)
        {
            return $"{Constant}";
        }
        else
        {
            Debug.LogWarning("Variable value logging hasn't been implemented yet!");
            return "a variable amount";
        }
    }
}

[System.Serializable]
public struct CardEffect
{
    public EffectType Type;
    public EffectTarget Target;
    public EffectMode Mode;
    public List<EffectValue> Values;
    public CardTemplate ReferenceCardTemplate;

    public override string ToString()
    {
        StringBuilder effectString = new StringBuilder();
        // TODO: somehow test if Values[0] exists
        switch (Type)
        {
            case EffectType.Undefined:
                Debug.LogWarning("Undefined Effects, this shouldn't happen");
                return "Undefined";
            case EffectType.NoEffect:
                return "Does Nothing";
            case EffectType.Delete:
                effectString.Append($"Delete {PluralHelper(Values[0].Constant, "card")} ");
                effectString.Append(ModeTargetHelper("from"));
                return effectString.ToString();

            case EffectType.ModEnergy:
                Assert.AreEqual(Target, EffectTarget.Self); // opponent gaining energy this turn doesn't make sense
                if (Values[0].Type == ValueType.Variable)
                {
                    Debug.LogError("Can't generate variable string yet");
                    return "";
                }
                Assert.AreNotEqual(Values[0].Constant, 0);
                if (Values[0].Constant < 0)
                {
                    return $"Lose {Values[0]} energy";
                }
                else
                {
                    return $"Gain {Values[0]} energy";
                }

            case EffectType.ModEnergyNextTurn:
                if (Values[0].Type == ValueType.Variable)
                {
                    Debug.LogError("Can't generate variable string yet");
                    return "";
                }
                Assert.AreNotEqual(Values[0].Constant, 0);
                Assert.AreNotEqual(Target, EffectTarget.Undefined);
                Assert.AreNotEqual(Target, EffectTarget.NoTarget);
                bool targetSelf = Target == EffectTarget.Self;
                string action;
                if (Values[0].Constant < 0)
                {
                    action = targetSelf ? "Lose" : "The opponent loses";
                }
                else
                {
                    action = targetSelf ? "Gain" : "The opponent gains";
                }
                return $"{action} {Values[0]} energy next turn";

            case EffectType.Transform:
                Assert.IsNotNull(ReferenceCardTemplate);
                effectString.Append($"Transform {PluralHelper(Values[0].Constant, "card")} ");
                effectString.Append(ModeTargetHelper("from"));
                effectString.Append($" to {ReferenceCardTemplate.Title}");
                return effectString.ToString();

            case EffectType.Add:
                Assert.IsNotNull(ReferenceCardTemplate);
                effectString.Append($"Add {PluralHelper(Values[0].Constant, "copy", "copies")} of ");
                effectString.Append($"{ReferenceCardTemplate.Title} ");
                effectString.Append(ModeTargetHelper("to"));
                return effectString.ToString();

            case EffectType.MakeUnswappable:
                effectString.Append($"Lock {PluralHelper(Values[0].Constant, "card")} ");
                effectString.Append(ModeTargetHelper("from"));
                effectString.Append(" (They become Unswappable)");
                return effectString.ToString();

            default:
                Debug.LogError("Card Effect ToString case not yet implemented");
                return "undefined description";
        }
    }

    string ModeTargetHelper(string prep)
    {
        StringBuilder effectString = new StringBuilder();
        switch (Mode)
        {
            case EffectMode.Top:
                effectString.Append($"{prep} top of ");
                break;
            case EffectMode.RandomFromView:
                effectString.Append($"randomly {prep} visible portion of ");
                break;
            case EffectMode.RandomFromDeck:
                effectString.Append($"randomly {prep} ");
                break;
            case EffectMode.Bottom:
                effectString.Append($"{prep} bottom of ");
                break;
            default:
                Debug.LogError("undefined mode for helper");
                Assert.IsTrue(false);
                break;
        }
        switch (Target)
        {
            case EffectTarget.Opponent:
                effectString.Append("opponent's stack");
                break;
            case EffectTarget.Self:
                effectString.Append("owner's stack");
                break;
            default:
                Debug.LogError("undefined target for helper");
                Assert.IsTrue(false);
                break;
        }
        return effectString.ToString();
    }

    string PluralHelper(int amount, string single, string plural = "")
    {
        if (plural == "")
        {
            plural = $"{single}s";
        }
        Assert.IsTrue(amount > 0);
        return amount > 1 ? $"{amount} {plural}" : $"{amount} {single}";
    }
}