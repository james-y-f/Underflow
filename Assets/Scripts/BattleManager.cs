// TODO: change reload displays logic
// TODO: use an object pool for displaying cards
// TODO: add animation for existing effects
// TODO: add game over screen
using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;
using System;
using UnityEngine.InputSystem;
using System.Text;
using System.Collections.Generic;

using Deck = System.Collections.Generic.List<Card>;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;
    PlayerInputActions InputActions;

    // --- Game State ---
    public enum BattleState
    {
        Setup,
        PlayerTurn,
        PlayerExecution,
        EnemyTurn,
        GameOver
    }

    public BattleState CurrentState { get; private set; }
    private bool GameIsOver = false;

    public Entity Player;
    public Entity Enemy;

    [SerializeField] EntityBaseStats PlayerBaseStats;
    [SerializeField] EntityBaseStats EnemyBaseStats;

    // --- UI References ---
    [SerializeField] bool ConsoleActiveAtStart = false;
    BattleConsole Console;


    // --- Unity Methods ---
    void Awake()
    {
        // singleton object
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        InputActions = new PlayerInputActions();
        Card.ResetUIDCounter();
        Player = new Entity(PlayerBaseStats, GameObject.FindWithTag("PlayerStack").GetComponent<StackDisplay>(), true);
        Enemy = new Entity(EnemyBaseStats, GameObject.FindWithTag("EnemyStack").GetComponent<StackDisplay>(), false);
        Player.StackDisplay.SwapAttempt.AddListener(HandleSwapAttempt);
        Console.SetActivation(ConsoleActiveAtStart);
    }

    void OnEnable()
    {
        InputActions.UI.Enable();
        InputActions.UI.ToggleConsole.performed += ToggleConsole;
    }

    void OnDisable()
    {
        InputActions.UI.Disable();
        InputActions.UI.ToggleConsole.performed -= ToggleConsole;
    }
    void Start()
    {
        CurrentState = BattleState.Setup;
        Console = BattleConsole.Instance;
        Console.SwapCommand.AddListener(HandleSwapCommand);
        Console.PrintCommand.AddListener(HandlePrintCommand);
        Console.WinCommand.AddListener(HandleWinCommand);
        SetupGame();
    }

    // --- Game Flow ---

    void SetupGame()
    {
        Console.Log("Setting up game...");

        ReloadDisplays();

        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        if (GameIsOver) return;
        CurrentState = BattleState.PlayerTurn;
        Console.Log("\n--- Your Turn ---");
        Player.ResetEnergy();
        Enemy.ResetEnergy();
        UpdateDisplay();
        // Log($"Enter command: {VALID_COMMANDS}");
    }

    public void PlayerExecute()
    {
        if (CurrentState != BattleState.PlayerTurn)
        {
            return;
        }
        if (GameIsOver) return;

        CurrentState = BattleState.PlayerExecution;
        Console.Log("\n--- Executing Jobs ---");
        UpdateDisplay();
        ExecuteAll(Player);
        if (GameIsOver) return;

        // Transition to Enemy Turn
        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        if (GameIsOver) return;

        CurrentState = BattleState.EnemyTurn;
        Console.Log("\n--- Enemy Turn ---");
        UpdateDisplay(); // Show state before enemy action
        ExecuteAll(Enemy);
        if (GameIsOver) return;

        // Transition back to Player Turn
        StartPlayerTurn();
    }

    bool CheckGameOver()
    {
        if (GameIsOver)
        {
            return true;
        }
        if (Enemy.Stack.Count == 0 && Player.Stack.Count == 0)
        {
            Console.LogError("Tie, This probably shouldn't happen");
            GameOver("TIE?");
            return true;
        }
        else if (Enemy.Stack.Count == 0)
        {
            GameOver("YOU WIN! ENEMY STACK DEPLETED!");
            return true;
        }
        else if (Player.Stack.Count == 0)
        {
            GameOver("YOU LOST! YOUR STACK IS DEPLETED!");
            return true;
        }
        return false;
    }

    void GameOver(string message)
    {
        // TODO: change this so something actually happens on screen
        GameIsOver = true;
        CurrentState = BattleState.GameOver;
        Console.Log("\n====================");
        Console.Log($"GAME OVER: {message}");
        Console.Log("====================");
        UpdateDisplay();
    }

    // --- Core Mechanics Implementation ---

    bool Swap(Entity target, int currentIndex, int targetIndex, bool hard = false, bool bypassViewSize = false, bool bypassSwappability = false)
    {
        if (!target.Swappable && !bypassSwappability)
        {
            Console.LogError("deck unswappable");
            return false;
        }
        // cannot operate on cards exceeding current size of deck
        int maxIdx = bypassViewSize ? target.Stack.Count : Math.Min(target.ViewSize, target.Stack.Count);
        if (currentIndex < 0 || targetIndex < 0 || currentIndex >= maxIdx || targetIndex >= maxIdx || currentIndex == targetIndex)
        {
            Console.LogError("invalid index for swap");
            return false;
        }
        // execute the swap
        // meaning that we just swap the two card without consideration for cards in between
        // a swap between two adjacent cards are seen as a hard swap
        if (hard || Math.Abs(currentIndex - targetIndex) == 1)
        {
            Card card1 = target.Stack[currentIndex];
            Card card2 = target.Stack[targetIndex];
            if (!card1.Info.Swappable || !card2.Info.Swappable) return false;
            target.Stack[currentIndex] = card2;
            target.Stack[targetIndex] = card1;
            Console.Log($"hard swapped {currentIndex}: {card1.Info.GetDisplayText()} with {targetIndex}: {card2.Info.GetDisplayText()}");
        }
        else
        {
            bool forwardSwap = currentIndex > targetIndex; // forward meaning towards front of deck
            int minIndex = forwardSwap ? targetIndex : currentIndex;
            int maxIndex = forwardSwap ? currentIndex : targetIndex;
            List<int> affectedIndices = new List<int>();
            Deck affectedCards = new Deck();
            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (target.Stack[i].Info.Swappable || bypassSwappability)
                {
                    affectedIndices.Add(i);
                    affectedCards.Add(target.Stack[i]);
                }
            }
            int removalIndex = forwardSwap ? affectedIndices.Count - 1 : 0;
            int insertionIndex = forwardSwap ? 0 : affectedIndices.Count - 1;
            Card temp = affectedCards[removalIndex];
            affectedCards.RemoveAt(removalIndex);
            affectedCards.Insert(insertionIndex, temp);
            foreach (int i in affectedIndices)
            {
                target.Stack[i] = affectedCards[0];
                affectedCards.RemoveAt(0);
            }
            Console.Log($"swapped {currentIndex}: {temp.Info.GetDisplayText()} to {targetIndex}");
        }
        // TODO: somehow implement the same logic to display
        target.StackDisplay.Swap(currentIndex, targetIndex);
        return true;
    }

    // Executes cards until there is not sufficient energy to execute the next card
    void ExecuteAll(Entity source)
    {
        while (ExecuteCard(source)) ;
    }

    bool ExecuteCard(Entity source)
    {
        if (CheckGameOver()) return false;
        Card nextCard = source.Stack[0];
        if (nextCard.Info.EnergyCost > source.CurrentEnergy) return false;
        source.CurrentEnergy -= nextCard.Info.EnergyCost;
        source.Stack.RemoveAt(0);
        source.Discard.Add(nextCard);
        Console.Log($"Executing: {nextCard.Info.Title}");
        ExecuteCardEffects(nextCard, source);
        UpdateDisplay();
        return true;
    }

    void ExecuteCardEffects(Card card, Entity source)
    {
        foreach (CardEffect effect in card.Effects)
        {
            Entity target = ResolveTargetEntity(source, effect.Target);
            switch (effect.Type)
            {
                case EffectType.NoEffect:
                    break;

                case EffectType.Delete:
                    Delete(ResolveValue(source, effect.Values[0]), target, effect.Mode);
                    break;

                case EffectType.Add:
                    Assert.IsNotNull(effect.ReferenceCardTemplate);
                    Add(new Card(effect.ReferenceCardTemplate),
                        ResolveValue(source, effect.Values[0]), target, effect.Mode);
                    break;

                case EffectType.ModEnergy:
                    target.CurrentEnergy += ResolveValue(source, effect.Values[0]);
                    break;

                case EffectType.ModEnergyNextTurn:
                    target.CarryOverEnergy += ResolveValue(source, effect.Values[0]);
                    break;

                // Add more cases here for other effects
                default:
                    Console.LogError($"Effect type '{effect.Type}' not implemented.");
                    break;
            }
        }
    }

    void Delete(int amount, Entity target, EffectMode mode)
    {
        if (amount < 1) return;
        Console.Log($"Deleting {amount} cards from {target.Name}");
        for (int i = 0; i < amount; i++)
        {
            if (CheckGameOver()) { return; }
            // we now know that there is at least one card left in target deck 
            int deleteIdx = ResolveIndex(mode, target.Stack.Count);
            Card deleted = target.Stack[deleteIdx];
            target.Stack.RemoveAt(deleteIdx);
            target.Discard.Add(deleted);
            // execute on delete effects here
            Console.Log($"deleted {deleteIdx}: {deleted.Info.GetDisplayText()}");
        }
    }

    void Add(Card card, int amount, Entity target, EffectMode mode)
    {
        Assert.IsTrue(amount > 0);
        for (int i = 0; i < amount; i++)
        {
            int addIdx = ResolveIndex(mode, target.Stack.Count);
            target.Stack.Insert(addIdx, card);
            Console.Log($"added {card.Info.GetDisplayText()} to {addIdx}");
        }
    }

    // --- Event Handlers

    void HandleSwapAttempt(bool isPlayer, int index1, int index2)
    {
        Entity target = isPlayer ? Player : Enemy;
        Swap(target, index1, index2);
    }

    // --- Console Commands
    void HandleSwapCommand(string target, int index1, int index2)
    {
        if (!Swap(Str2Entity(target), index1, index2, false, true, true))
        {
            Console.LogError("swap command failed");
        }
    }


    void HandlePrintCommand(string deck)
    {
        Console.Log(PrintDeckContent(Str2Deck(deck)));
    }

    void HandleWinCommand()
    {
        GameOver("You Won by winning!");
    }

    // --- Helper Functions ---

    void ToggleConsole(InputAction.CallbackContext ctx)
    {
        Console.ToggleActivation();
    }

    void UpdateDisplay()
    {
        // Displays
        // This deletes everything and replaces them, inefficient but works for now
        ReloadDisplays();
    }

    void ReloadDisplays()
    {
        Player.StackDisplay.Clear();
        Enemy.StackDisplay.Clear();
        int playerViewSize = Math.Min(Player.ViewSize, Player.Stack.Count);
        int enemyViewSize = Math.Min(Enemy.ViewSize, Enemy.Stack.Count);
        for (int i = 0; i < playerViewSize; i++)
        {
            Player.StackDisplay.InsertCard(Player.Stack[i].Info);
        }
        for (int i = 0; i < enemyViewSize; i++)
        {
            Enemy.StackDisplay.InsertCard(Enemy.Stack[i].Info);
        }
        Player.StackDisplay.UpdateCountDisplay();
        Enemy.StackDisplay.UpdateCountDisplay();
    }

    Entity ResolveTargetEntity(Entity source, EffectTarget target)
    {
        switch (target)
        {
            case EffectTarget.Self:
                return source;
            case EffectTarget.Opponent:
                if (source == Player)
                {
                    return Enemy;
                }
                return Player;
            default:
                return null;
        }
    }

    int ResolveIndex(EffectMode mode, int stackSize)
    {
        switch (mode)
        {
            case EffectMode.Top:
                return 0;
            case EffectMode.Random:
                return UnityEngine.Random.Range(0, stackSize - 1);
            case EffectMode.Bottom:
                return stackSize - 1;
            default:
                Console.LogError("unable to resolve index");
                return 0;
        }
    }
    Deck Str2Deck(string input)
    {
        switch (input)
        {
            case "playerstack":
            case "stack":
            case "ps":
            case "s":
                return Player.Stack;
            case "enemystack":
            case "es":
                return Enemy.Stack;
            case "playerdiscard":
            case "discard":
            case "pd":
            case "d":
                return Player.Discard;
            case "enemydiscard":
            case "ed":
                return Enemy.Discard;
            default:
                Console.LogError("invalid input for str2Deck");
                return null;
        }
    }

    Entity Str2Entity(string input)
    {
        switch (input)
        {
            case "player":
            case "p":
                return Player;
            case "enemy":
            case "e":
                return Enemy;
            default:
                Console.LogError("invalid input for str2Entity");
                return null;
        }
    }

    string PrintDeckContent(Deck target, int lines = -1)
    {
        if (lines == -1) // default to printing entire deck
        {
            lines = target.Count;
        }
        StringBuilder str = new StringBuilder();
        int limit = Math.Min(lines, target.Count);
        for (int i = 0; i < limit; i++)
        {
            str.Append($"{i}. {target[i].Info.GetDisplayText()}\n");
        }
        return str.ToString();
    }

    int ResolveValue(Entity source, EffectValue value)
    {
        if (value.Type == ValueType.Constant)
        {
            return value.Constant;
        }
        // FIXME: finish this
        return 0;
    }
}