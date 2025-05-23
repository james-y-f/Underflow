using UnityEngine;
using Deck = System.Collections.Generic.List<Card>;
public class Entity : MonoBehaviour
{
    public EntityBaseStats BaseStats;
    public string Name;
    public bool IsPlayer;
    public Deck Stack;
    public Deck Discard;
    public Deck Exile;
    public int BaseViewSize;
    public StackDisplay StackDisplay;
    public int ViewSizeModifier
    {
        get { return ViewSizeModifier; }
        set
        {
            ViewSizeModifier = value;
            StackDisplay.WindowSize = ViewSize;
        }
    }
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
    public bool Swappable;

    public Entity(EntityBaseStats baseStats, StackDisplay stackDisplay, bool isPlayer)
    {
        BaseStats = baseStats;
        StackDisplay = stackDisplay;
        IsPlayer = isPlayer;
        Name = BaseStats.Name;
        foreach (CardTemplate template in BaseStats.Deck.Cards)
        {
            Stack.Add(new Card(template));
        }
        BaseViewSize = BaseStats.BaseViewSize;
        BaseEnergy = BaseStats.BaseEnergy;
        ShuffleAtStart = BaseStats.ShuffleAtStart;
        ResetTemporaryStats();
        StackDisplay.WindowSize = ViewSize;
        if (ShuffleAtStart)
        {
            ShuffleStack();
        }
        // For now, the player is by default swappale and the enemy is not
        Swappable = isPlayer;
        StackDisplay.DeckSwappable = Swappable;
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
    }

    //TODO: move shuffle stack here
    public void ShuffleStack()
    {
        int n = Stack.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n);
            Card temp = Stack[k];
            Stack[k] = Stack[n];
            Stack[n] = temp;
        }
    }
}