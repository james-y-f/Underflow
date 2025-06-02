using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;
using System.Collections;
using System.Linq;
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

        StackDisplay playerStackDisplay = GameObject.FindGameObjectWithTag(Constants.PlayerStackTag).GetComponent<StackDisplay>();
        StackDisplay enemyStackDisplay = GameObject.FindGameObjectWithTag(Constants.EnemyStackTag).GetComponent<StackDisplay>();
        Assert.IsNotNull(playerStackDisplay);
        Assert.IsNotNull(enemyStackDisplay);
        Player = new Entity(PlayerBaseStats, true, playerStackDisplay);
        Enemy = new Entity(EnemyBaseStats, false, enemyStackDisplay);
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
        CurrentState = BattleState.Setup;
        ConsoleInstance.Log("Setting up game...");

        LoadDisplays();

        StartCoroutine(StartPlayerTurn());
    }

    IEnumerator StartPlayerTurn()
    {
        if (GameIsOver) yield break;
        ConsoleInstance.Log("\n---Player Turn---");
        ConsoleInstance.LogValidCommands();

        Player.ResetEnergy();

        Player.StackDisplay.EnergyDisplay.RemoveAllTransparentEnergy();
        yield return StartCoroutine(Player.StackDisplay.EnergyDisplay.SetEnergy(Player.CurrentEnergy));

        Enemy.StackDisplay.EnergyDisplay.RemoveAllEnergy();
        yield return StartCoroutine(Enemy.StackDisplay.EnergyDisplay.SetTransparentEnergy(Enemy.BaseEnergy + Enemy.CarryOverEnergy));

        CurrentState = BattleState.PlayerTurn;
    }

    public void PlayerEndTurn()
    {
        if (CurrentState != BattleState.PlayerTurn) return;
        ConsoleInstance.Log("\n--- Executing Jobs ---");
        CurrentState = BattleState.PlayerExecution;
        StartCoroutine(PlayerExecute());
    }

    IEnumerator PlayerExecute()
    {
        yield return StartCoroutine(ExecuteTurnCoroutine(Player));
        if (GameIsOver) yield break;

        StartCoroutine(EnemyTurn());
    }

    IEnumerator EnemyTurn()
    {
        ConsoleInstance.Log("\n--- Enemy Turn ---");
        CurrentState = BattleState.EnemyTurn;

        Enemy.ResetEnergy();
        Enemy.StackDisplay.EnergyDisplay.RemoveAllTransparentEnergy();
        yield return StartCoroutine(Enemy.StackDisplay.EnergyDisplay.SetEnergy(Enemy.CurrentEnergy));
        yield return StartCoroutine(ExecuteTurnCoroutine(Enemy));
        if (GameIsOver) yield break;

        // Transition back to Player Turn
        StartCoroutine(StartPlayerTurn());
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
        // TODO: make a display for this
        CurrentState = BattleState.GameOver;
        ConsoleInstance.Log("\n====================");
        ConsoleInstance.Log($"GAME OVER: {message}");
        ConsoleInstance.Log("====================");
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

    IEnumerator ExecuteTurnCoroutine(Entity source)
    {
        Assert.IsNotNull(source);
        while (true)
        {
            if (CheckGameOver()) break;
            // we know at this point that both stack still has cards remaining
            Card nextCard = source.Stack[0];
            if (nextCard.Info.EnergyCost > source.CurrentEnergy) break;
            source.CurrentEnergy -= nextCard.Info.EnergyCost;
            yield return StartCoroutine(source.StackDisplay.EnergyDisplay.SetEnergy(source.CurrentEnergy));

            source.Stack.RemoveAt(0);
            ConsoleInstance.Log($"Executing: {nextCard.Info.Title}");
            yield return StartCoroutine(source.StackDisplay.MoveTopCardToExecutionPos());

            yield return StartCoroutine(ExecuteCardEffects(nextCard, source));

            yield return StartCoroutine(source.StackDisplay.DiscardExecutingCard());

            source.Discard.Add(nextCard);
        }
    }

    IEnumerator ExecuteCardEffects(Card card, Entity source)
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
                    yield return StartCoroutine(Delete(ResolveValue(source, effect.Values[0]), target, effect.Mode));
                    break;

                case EffectType.Transform:
                    Assert.IsNotNull(effect.ReferenceCardTemplate);
                    yield return StartCoroutine(Transform(ResolveValue(source, effect.Values[0]), target,
                                                          effect.Mode, effect.ReferenceCardTemplate));
                    break;

                // FIXME:
                // case EffectType.Add:
                //     yield return StartCoroutine(Add(new Card(effect.ReferenceCardTemplate), ResolveValue(source, effect.Values[0]), target, effect.Mode));
                //     break;

                case EffectType.ModEnergy:
                    target.CurrentEnergy += ResolveValue(source, effect.Values[0]);
                    yield return StartCoroutine(target.StackDisplay.EnergyDisplay.SetEnergy(target.CurrentEnergy));
                    break;

                case EffectType.ModEnergyNextTurn:
                    int addAmount = ResolveValue(source, effect.Values[0]);
                    target.CarryOverEnergy += addAmount;
                    yield return StartCoroutine(target.StackDisplay.EnergyDisplay.SetTransparentEnergy(target.CarryOverEnergy));
                    break;

                // Add more cases here for other effects
                default:
                    Debug.LogError($"Effect type '{effect.Type}' not implemented.");
                    break;
            }
        }
    }

    IEnumerator Delete(int amount, Entity target, EffectMode mode)
    {
        if (amount < 1) yield break;
        Assert.IsNotNull(target);
        ConsoleInstance.Log($"Deleting {amount} cards from {target.Name}");
        List<int> deleteIndices = target.Stack.ResolveIndex(mode, amount, target.ViewSize);
        List<Card> deleteList = new List<Card>();
        foreach (int deleteIdx in deleteIndices)
        {
            deleteList.Add(target.Stack[deleteIdx]);
            target.StackDisplay.TargetCard(deleteIdx);
        }
        foreach (Card deleteTarget in deleteList)
        {
            int deleteIdx = target.Stack.FindIndex(x => x == deleteTarget);
            target.Stack.RemoveAt(deleteIdx);
            yield return StartCoroutine(target.StackDisplay.DeleteCard(deleteIdx));

            target.Discard.Add(deleteTarget);
            ConsoleInstance.Log($"deleted {deleteIdx}: {deleteTarget.Info.GetDisplayText()}");
        }
    }

    IEnumerator Transform(int amount, Entity target, EffectMode mode, CardTemplate template)
    {
        if (amount < 1) yield break;
        Assert.IsNotNull(target);
        ConsoleInstance.Log($"Transforming {amount} cards from {target.Name} to {template.Info.Title}");
        List<int> transformIndices = target.Stack.ResolveIndex(mode, amount, target.ViewSize);
        foreach (int transformIdx in transformIndices)
        {
            Card transformTarget = target.Stack[transformIdx];
            transformTarget.SetTemplate(template);
            ConsoleInstance.Log($"transformed {transformIdx} to {transformTarget.Info.GetDisplayText()}");
            target.StackDisplay.TargetCard(transformIdx);
            yield return StartCoroutine(target.StackDisplay.TransformCard(template.Info, transformIdx));
        }
    }

    // void Add(Card card, int amount, Entity target, EffectMode mode)
    // {
    //     if (amount < 1) return;
    //     Assert.IsNotNull(target);
    //     for (int i = 0; i < amount; i++)
    //     {
    //         int addIdx = ResolveIndex(mode, target.Stack.Count);
    //         target.Stack.Insert(addIdx, card);
    //         ConsoleInstance.Log($"added {card.Info.Title} to {addIdx}");
    //     }
    // }

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

    void LoadDisplays()
    {
        for (int i = 0; i < Player.Stack.Count; i++)
        {
            Player.StackDisplay.InsertCard(Player.Stack[i].Info);
        }
        for (int i = 0; i < Enemy.Stack.Count; i++)
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
