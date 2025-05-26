using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;
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

    public BattleState CurrentState
    {
        get;
        private set;
    }
    private bool GameIsOver
    {
        get { return CurrentState == BattleState.GameOver; }
    }

    [SerializeField] Entity Player;
    [SerializeField] Entity Enemy;
    [SerializeField] EntityBaseStats PlayerBaseStats;
    [SerializeField] EntityBaseStats EnemyBaseStats;
    [SerializeField] StackDisplay PlayerStackDisplay;
    [SerializeField] StackDisplay EnemyStackDisplay;

    // --- UI References ---
    [SerializeField] bool ConsoleActiveAtStart = false;
    DebugConsole ConsoleInstance;


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

        Assert.IsNotNull(PlayerStackDisplay);
        Assert.IsNotNull(EnemyStackDisplay);
        Player = new Entity(PlayerBaseStats, true, PlayerStackDisplay);
        Enemy = new Entity(EnemyBaseStats, false, EnemyStackDisplay);
        InputActions = new PlayerInputActions();
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
        ConsoleInstance = DebugConsole.Instance;
        ConsoleInstance.SwapCommand.AddListener(HandleSwapCommand);
        ConsoleInstance.gameObject.SetActive(ConsoleActiveAtStart);

        Player.StackDisplay.SwapAttempt.AddListener(HandleSwapAttempt);
        Enemy.StackDisplay.SwapAttempt.AddListener(HandleSwapAttempt);

        CurrentState = BattleState.Setup;
        SetupGame();
    }

    // --- Game Flow ---

    void SetupGame()
    {
        ConsoleInstance.Log("Setting up game...");

        ReloadDisplays();

        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        if (GameIsOver) return;
        CurrentState = BattleState.PlayerTurn;
        Player.ResetEnergy();
        Enemy.ResetEnergy();
        UpdateDisplay();
        ConsoleInstance.Log("\n---Player Turn---");
        ConsoleInstance.LogValidCommands();
    }

    public void PlayerExecute()
    {
        if (CurrentState != BattleState.PlayerTurn)
        {
            return;
        }
        if (GameIsOver) return;

        CurrentState = BattleState.PlayerExecution;
        ConsoleInstance.Log("\n--- Executing Jobs ---");
        UpdateDisplay();
        ExecuteTurn(Player);
        if (GameIsOver) return;

        // Transition to Enemy Turn
        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        if (GameIsOver) return;

        CurrentState = BattleState.EnemyTurn;
        ConsoleInstance.Log("\n--- Enemy Turn ---");
        UpdateDisplay(); // Show state before enemy action
        ExecuteTurn(Enemy);
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
            Debug.LogError("Tie, This probably shouldn't happen");
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
        CurrentState = BattleState.GameOver;
        ConsoleInstance.Log("\n====================");
        ConsoleInstance.Log($"GAME OVER: {message}");
        ConsoleInstance.Log("====================");
        UpdateDisplay();
    }


    // --- Core Mechanics Implementation ---

    bool SwapStack(Entity target, int currentIndex, int targetIndex, bool hard = false, bool bypassSwappability = false)
    {
        Assert.IsNotNull(target);
        int maxIdx = Math.Min(target.ViewSize, target.Stack.Count); // cannot operate on cards exceeding current size of deck
        if (currentIndex < 0 || targetIndex < 0 || currentIndex >= maxIdx || targetIndex >= maxIdx)
        {
            ConsoleInstance.Log("invalid index for swap");
            return false;
        }
        List<int> newOrder = target.Stack.Swap(target.ViewSize, currentIndex, targetIndex, hard, bypassSwappability);
        if (newOrder == null)
        {
            ConsoleInstance.Log($"swap({currentIndex} -> {targetIndex}) failed");
            return false;
        }
        target.StackDisplay.UpdateToOrder(newOrder);
        ConsoleInstance.Log($"swap({currentIndex} -> {targetIndex}) successful");
        return true;
    }

    void ExecuteTurn(Entity source)
    {
        Assert.IsNotNull(source);
        while (true)
        {
            if (CheckGameOver()) return;
            // we know at this point that both stack still has cards remaining
            Card nextCard = source.Stack[0];
            if (nextCard.Info.EnergyCost > source.CurrentEnergy) return;
            source.CurrentEnergy -= nextCard.Info.EnergyCost;
            source.Stack.RemoveAt(0);
            source.Discard.Add(nextCard);
            ConsoleInstance.Log($"Executing: {nextCard.Info.Title}");
            ExecuteCardEffects(nextCard, source);
            UpdateDisplay();
        }
    }

    void ExecuteCardEffects(Card card, Entity source)
    {
        foreach (CardEffect effect in card.Effects)
        {
            Entity target = ResolveTarget(source, effect.Target);
            Assert.IsNotNull(target);
            switch (effect.Type)
            {
                case EffectType.NoEffect:
                    break;

                case EffectType.Delete:
                    Delete(ResolveValue(source, effect.Values[0]), target, effect.Mode);
                    break;

                case EffectType.Add:
                    Add(new Card(effect.ReferenceCardTemplate), ResolveValue(source, effect.Values[0]), target, effect.Mode);
                    break;

                case EffectType.ModEnergy:
                    target.CurrentEnergy += ResolveValue(source, effect.Values[0]);
                    break;

                case EffectType.ModEnergyNextTurn:
                    target.CarryOverEnergy += ResolveValue(source, effect.Values[0]);
                    break;

                // Add more cases here for other effects
                default:
                    Debug.LogError($"Effect type '{effect.Type}' not implemented.");
                    break;
            }
        }
    }

    void Delete(int amount, Entity target, EffectMode mode)
    {
        if (amount < 1) return;
        Assert.IsNotNull(target);
        ConsoleInstance.Log($"Deleted {amount} cards from {target.Name}");
        for (int i = 0; i < amount; i++)
        {
            if (CheckGameOver()) { return; }
            // we now know that there are for sure cards left in target deck 
            int deleteIdx = ResolveIndex(mode, target.Stack.Count);
            Card deleted = target.Stack[deleteIdx];
            target.Stack.RemoveAt(deleteIdx);
            target.Discard.Add(deleted);
            ConsoleInstance.Log($"deleted {deleteIdx}: {deleted.Info.GetDisplayText()}");
        }
    }

    void Add(Card card, int amount, Entity target, EffectMode mode)
    {
        if (amount < 1) return;
        Assert.IsNotNull(target);
        for (int i = 0; i < amount; i++)
        {
            int addIdx = ResolveIndex(mode, target.Stack.Count);
            target.Stack.Insert(addIdx, card);
            ConsoleInstance.Log($"added {card.Info.Title} to {addIdx}");
        }
    }

    // --- Event Handlers ---

    void HandleSwapAttempt(bool IsPlayer, int currentIndex, int targetIndex)
    {
        Entity targetEntity = IsPlayer ? Player : Enemy;
        Debug.Log($"handling swap attempt, {currentIndex}, {targetIndex}");
        SwapStack(targetEntity, currentIndex, targetIndex);
    }

    void HandleSwapCommand(bool IsPlayer, int currentIndex, int targetIndex)
    {
        Entity targetEntity = IsPlayer ? Player : Enemy;
        Debug.Log($"handling swap command, {currentIndex}, {targetIndex}");
        SwapStack(targetEntity, currentIndex, targetIndex, false, true);
    }
    void ToggleConsole(InputAction.CallbackContext context)
    {
        Debug.Log($"Toggling Console to {!ConsoleInstance.gameObject.activeSelf}");
        ConsoleInstance.gameObject.SetActive(!ConsoleInstance.gameObject.activeSelf);
    }

    // --- Helper Functions ---

    void UpdateDisplay()
    {
        // Displays
        // This deletes everything and replaces them, inefficient but works for now
        // TODO: replace this logic
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
    }

    Entity ResolveTarget(Entity source, EffectTarget target)
    {
        Assert.IsNotNull(source);
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
                Debug.LogError("unable to resolve index");
                return 0;
        }
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

    public string PrintDebugStatus()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Player.PrintEntityDebugStatus());
        builder.Append(Enemy.PrintEntityDebugStatus());
        return builder.ToString();
    }
}
