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
        switch (Type)
        {
            case EffectType.Undefined:
                Debug.LogWarning("Undefined Effects, this shouldn't happen");
                return "Undefined";
            case EffectType.NoEffect:
                return "Does Nothing";
            case EffectType.Delete:
                effectString.Append($"Delete {Values[0]} cards ");
                switch (Mode)
                {
                    case EffectMode.Top:
                        effectString.Append("from the top of ");
                        break;
                    case EffectMode.RandomFromDeck:
                        effectString.Append("randomly from ");
                        break;
                    case EffectMode.Bottom:
                        effectString.Append("from the bottom of ");
                        break;
                    default:
                        Debug.LogError("undefined mode for delete");
                        Assert.IsTrue(false);
                        break;
                }
                switch (Target)
                {
                    case EffectTarget.Opponent:
                        effectString.Append("the opponent's stack");
                        break;
                    case EffectTarget.Self:
                        effectString.Append("the owner's stack");
                        break;
                    default:
                        Debug.LogError("undefined target for delete");
                        Assert.IsTrue(false);
                        break;
                }
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


            default:
                Debug.LogError("Card Effect ToString case not yet implemented");
                return "undefined description";
        }
    }
}