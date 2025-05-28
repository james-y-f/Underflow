using System.Text;

[System.Serializable]
public class Entity
{
    public EntityBaseStats BaseStats;
    public string Name;
    public bool IsPlayer;
    public Deck Stack;
    public Deck Discard;
    public Deck Exile;
    int baseViewSize;
    public int BaseViewSize
    {
        get { return baseViewSize; }
        set
        {
            baseViewSize = value;
            StackDisplay.ViewSize = ViewSize;
        }
    }
    int viewSizeMod;
    public int ViewSizeModifier
    {
        get { return viewSizeMod; }
        set
        {
            viewSizeMod = value;
            StackDisplay.ViewSize = ViewSize;
        }
    }
    public int ViewSize
    {
        get { return baseViewSize + viewSizeMod; }
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
        baseViewSize = BaseStats.BaseViewSize;
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
        StackDisplay.ViewSize = ViewSize;
        StackDisplay.Name = Name;
    }

    public void ResetTemporaryStats()
    {
        Discard = new Deck();
        Exile = new Deck();
        viewSizeMod = 0;
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

    public string PrintEntityDebugStatus()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"{Name} : ------------------------");
        builder.AppendLine($"- IsPlayer       : {IsPlayer}");
        builder.AppendLine($"- BaseViewSize   : {BaseViewSize}");
        builder.AppendLine($"- ViewSizeMod    : {ViewSizeModifier}");
        builder.AppendLine($"- ViewSize       : {ViewSize}");
        builder.AppendLine($"- BaseEnergy     : {BaseEnergy}");
        builder.AppendLine($"- EnergyModifier : {EnergyModifier}");
        builder.AppendLine($"- CarryOverEnergy: {CarryOverEnergy}");
        builder.AppendLine($"- CurrentEnergy  : {CurrentEnergy}");
        builder.AppendLine($"- ShuffleAtStart : {ShuffleAtStart}");
        builder.AppendLine($" ------ Stack ------");
        builder.Append(Stack.PrintDeckContent());
        builder.AppendLine($" ----- Discard -----");
        builder.Append(Discard.PrintDeckContent());
        builder.AppendLine($" ------ Exile ------");
        builder.Append(Exile.PrintDeckContent());
        return builder.ToString();
    }
}