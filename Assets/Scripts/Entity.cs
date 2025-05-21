using UnityEngine;

[System.Serializable]
public class Entity
{
    public EntityBaseStats BaseStats;
    public string Name;
    public bool IsPlayer;
    public Deck Stack;
    public Deck Discard;
    public Deck Exile;
    public int BaseViewSize;
    public int ViewSizeModifier;
    public int ViewSize
    {
        get { return BaseViewSize + ViewSizeModifier; }
        private set { }
    }
    public int BaseEnergy;
    public int EnergyModifier;
    public int CarryOverEnergy;
    public int CurrentEnergy;
    public bool ShuffleAtStart;
    public StackDisplay StackDisplay;

    public Entity(EntityBaseStats baseStats, bool isPlayer, StackDisplay stackDisplay)
    {
        BaseStats = baseStats;
        IsPlayer = isPlayer;
        Name = BaseStats.Name;
        BaseViewSize = BaseStats.BaseViewSize;
        BaseEnergy = BaseStats.BaseEnergy;
        ShuffleAtStart = BaseStats.ShuffleAtStart;
        ResetTemporaryStats();
        Stack = new Deck(baseStats.Deck);
        Discard = new Deck();
        Exile = new Deck();
        if (ShuffleAtStart)
        {
            Stack.Shuffle();
        }
        // For now, the player's stack is by default swappale and the enemy is not
        // every other deck (discard, exile) is by default swappable
        Stack.Swappable = isPlayer;
        StackDisplay = stackDisplay;
    }

    public void ResetTemporaryStats()
    {
        Discard = new Deck();
        Exile = new Deck();
        ViewSizeModifier = 0;
        EnergyModifier = 0;
        CarryOverEnergy = 0;
        CurrentEnergy = 0;
    }

    public void ResetEnergy()
    {
        CurrentEnergy = BaseEnergy + EnergyModifier + CarryOverEnergy;
        CarryOverEnergy = 0;
        // TODO: log this
    }
}