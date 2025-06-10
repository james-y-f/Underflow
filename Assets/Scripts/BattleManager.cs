using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;
using System.Collections;
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
    [SerializeField] ButtonController ExecuteNextButton;

    [SerializeField] ButtonController EndTurnButton;
    [SerializeField] ButtonController ExecuteAllButton;

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

        ExecuteNextButton = GameObject.FindGameObjectWithTag(Constants.ExecuteNextButtonTag).GetComponent<ButtonController>();
        EndTurnButton = GameObject.FindGameObjectWithTag(Constants.EndTurnButtonTag).GetComponent<ButtonController>();
        ExecuteAllButton = GameObject.FindGameObjectWithTag(Constants.ExecuteAllButtonTag).GetComponent<ButtonController>();
        Assert.IsNotNull(ExecuteNextButton);
        Assert.IsNotNull(EndTurnButton);
        Assert.IsNotNull(ExecuteAllButton);

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

        ExecuteNextButton.OnClick.AddListener(PlayerExecuteNext);
        EndTurnButton.OnClick.AddListener(PlayerEndTurn);
        ExecuteAllButton.OnClick.AddListener(PlayerExecuteAll);

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
        if (CheckGameOver(Player)) yield break;
        ConsoleInstance.Log("\n---Player Turn---");
        ConsoleInstance.LogValidCommands();
        ExecuteNextButton.SetInteractible(true);
        ExecuteAllButton.SetInteractible(true);
        EndTurnButton.SetInteractible(false); // player must execute at least one card per turn

        Player.ResetEnergy();

        Player.StackDisplay.EnergyDisplay.RemoveAllTransparentEnergy();
        yield return StartCoroutine(Player.StackDisplay.EnergyDisplay.SetEnergy(Player.CurrentEnergy));

        Enemy.StackDisplay.EnergyDisplay.RemoveAllEnergy();
        yield return StartCoroutine(Enemy.StackDisplay.EnergyDisplay.SetTransparentEnergy(Enemy.BaseEnergy + Enemy.CarryOverEnergy));

        CurrentState = BattleState.PlayerTurn;
    }

    public void PlayerExecuteNext()
    {
        if (CurrentState != BattleState.PlayerTurn && CurrentState != BattleState.PlayerExecution) return;
        if (CurrentState == BattleState.PlayerTurn)
        {
            CurrentState = BattleState.PlayerExecution;
        }
        if (CheckNextCardExecutable(Player))
        {
            SetAllButtonsInteractible(false);
            StartCoroutine(ExecuteNextCoroutine(Player));
        }
    }

    public void PlayerEndTurn()
    {
        if (CurrentState != BattleState.PlayerTurn && CurrentState != BattleState.PlayerExecution) return;
        SetAllButtonsInteractible(false);
        StartCoroutine(EnemyTurn());
    }

    public void PlayerExecuteAll()
    {
        if (CurrentState != BattleState.PlayerTurn && CurrentState != BattleState.PlayerExecution) return;
        ConsoleInstance.Log("\n--- Executing All Cards ---");
        CurrentState = BattleState.PlayerExecution;
        SetAllButtonsInteractible(false);
        StartCoroutine(PlayerExecuteAllCoroutine());
    }

    IEnumerator PlayerExecuteAllCoroutine()
    {
        yield return StartCoroutine(ExecuteAllCoroutine(Player));
        PlayerEndTurn();
    }

    IEnumerator EnemyTurn()
    {
        if (CheckGameOver(Enemy)) yield break;
        ConsoleInstance.Log("\n--- Enemy Turn ---");
        CurrentState = BattleState.EnemyTurn;

        Enemy.ResetEnergy();
        Enemy.StackDisplay.EnergyDisplay.RemoveAllTransparentEnergy();
        yield return StartCoroutine(Enemy.StackDisplay.EnergyDisplay.SetEnergy(Enemy.CurrentEnergy));
        yield return StartCoroutine(ExecuteAllCoroutine(Enemy));
        if (GameIsOver) yield break;

        // Transition back to Player Turn
        StartCoroutine(StartPlayerTurn());
    }


    // At the beginning of your turn, if you have no cards, then you lose
    bool CheckGameOver(Entity source)
    {
        if (GameIsOver)
        {
            return true;
        }
        if (source.Stack.Count == 0)
        {
            if (source == Player)
            {
                GameOver("YOU LOST! YOUR STACK IS DEPLETED!");
            }
            else if (source == Enemy)
            {
                GameOver("YOU WIN! ENEMY STACK DEPLETED!");
            }
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

    IEnumerator ExecuteNextCoroutine(Entity source, bool executingAll = false)
    {
        if (!executingAll && !CheckNextCardExecutable(source)) yield break;
        Card nextCard = source.Stack[0];
        source.CurrentEnergy -= nextCard.EnergyCost;
        yield return StartCoroutine(source.StackDisplay.EnergyDisplay.SetEnergy(source.CurrentEnergy));

        source.Stack.RemoveAt(0);
        ConsoleInstance.Log($"Executing: {nextCard.Title}");
        yield return StartCoroutine(source.StackDisplay.MoveTopCardToExecutionPos());

        yield return StartCoroutine(ExecuteCardEffects(nextCard, source));

        yield return StartCoroutine(source.StackDisplay.DiscardExecutingCard());
        source.Discard.Add(nextCard);

        if (source == Player && !executingAll)
        {
            if (CheckNextCardExecutable(Player))
            {
                SetAllButtonsInteractible(true);
            }
            else
            {
                EndTurnButton.SetInteractible(true);
            }
        }
    }

    IEnumerator ExecuteAllCoroutine(Entity source)
    {
        Assert.IsNotNull(source);
        while (CheckNextCardExecutable(source))
        {
            yield return StartCoroutine(ExecuteNextCoroutine(source, true));
        }
    }

    IEnumerator ExecuteCardEffects(Card card, Entity source)
    {
        foreach (CardEffect effect in card.Effects)
        {
            yield return StartCoroutine(ExecuteEffect(effect, source));
        }
    }

    IEnumerator ExecuteCardOnDeleteEffects(Card card, Entity source)
    {
        foreach (CardEffect effect in card.OnDeleteEffects)
        {
            yield return StartCoroutine(ExecuteEffect(effect, source));
        }
    }

    IEnumerator ExecuteEffect(CardEffect effect, Entity source)
    {
        if (effect.Type == EffectType.NoEffect) yield break;
        Entity target = ResolveTarget(source, effect.Target);
        Assert.IsNotNull(target);
        switch (effect.Type)
        {
            case EffectType.Delete:
                yield return StartCoroutine(Delete(ResolveValue(source, effect.Values[0]), target, effect.Mode));
                break;

            case EffectType.Transform:
                Assert.IsNotNull(effect.ReferenceCardTemplate);
                yield return StartCoroutine(Transform(ResolveValue(source, effect.Values[0]), target,
                                                      effect.Mode, effect.ReferenceCardTemplate));
                break;

            case EffectType.Add:
                yield return StartCoroutine(Add(ResolveValue(source, effect.Values[0]), target,
                                                effect.Mode, effect.ReferenceCardTemplate));
                break;

            case EffectType.ModEnergy:
                target.CurrentEnergy += ResolveValue(source, effect.Values[0]);
                yield return StartCoroutine(target.StackDisplay.EnergyDisplay.SetEnergy(target.CurrentEnergy));
                break;

            case EffectType.ModEnergyNextTurn:
                int addAmount = ResolveValue(source, effect.Values[0]);
                target.CarryOverEnergy += addAmount;
                yield return StartCoroutine(target.StackDisplay.EnergyDisplay.SetTransparentEnergy(target.CarryOverEnergy));
                break;

            case EffectType.MakeUnswappable:
                yield return StartCoroutine(MakeUnswappable(ResolveValue(source, effect.Values[0]), target, effect.Mode));
                break;

            // Add more cases here for other effects
            default:
                Debug.LogError($"Effect type '{effect.Type}' not implemented.");
                break;
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
            if (deleteIdx == -1) continue; // means we didn't find the card
            target.Stack.RemoveAt(deleteIdx);

            if (deleteTarget.OnDeleteEffects.Count > 0)
            {
                yield return StartCoroutine(target.StackDisplay.MoveTopCardToExecutionPos());
                yield return ExecuteCardOnDeleteEffects(deleteTarget, target);
                yield return StartCoroutine(target.StackDisplay.DiscardExecutingCard());
            }
            else
            {
                yield return StartCoroutine(target.StackDisplay.DeleteCard(deleteIdx));
            }
            target.Discard.Add(deleteTarget);
            ConsoleInstance.Log($"deleted {deleteIdx}: {deleteTarget.Title}");
        }
    }

    IEnumerator Transform(int amount, Entity target, EffectMode mode, CardTemplate template)
    {
        if (amount < 1) yield break;
        Assert.IsNotNull(target);
        ConsoleInstance.Log($"Transforming {amount} cards from {target.Name} to {template.Title}");
        List<int> transformIndices = target.Stack.ResolveIndex(mode, amount, target.ViewSize);
        foreach (int transformIdx in transformIndices)
        {
            Card transformTarget = target.Stack[transformIdx];
            transformTarget.SetTemplate(template);
            ConsoleInstance.Log($"transformed {transformIdx} to {transformTarget.Title}");
            target.StackDisplay.TargetCard(transformIdx);
            yield return StartCoroutine(target.StackDisplay.TransformCard(transformTarget, transformIdx));
        }
    }

    IEnumerator MakeUnswappable(int amount, Entity target, EffectMode mode)
    {
        if (amount < 1) yield break;
        Assert.IsNotNull(target);
        ConsoleInstance.Log($"Making {amount} cards from {target.Name} Unswappable");
        List<int> transformIndices = target.Stack.ResolveIndex(mode, amount, target.ViewSize);
        foreach (int transformIdx in transformIndices)
        {
            Card transformTarget = target.Stack[transformIdx];
            transformTarget.AddProperty(Property.Unswappable);
            ConsoleInstance.Log($"made {transformIdx} {transformTarget.Title} unswappable");
            target.StackDisplay.TargetCard(transformIdx);
            yield return StartCoroutine(target.StackDisplay.TransformCard(transformTarget, transformIdx));
        }
    }

    IEnumerator Add(int amount, Entity target, EffectMode mode, CardTemplate template)
    {
        if (amount < 1) yield break;
        Assert.IsNotNull(target);
        ConsoleInstance.Log($"Adding {amount} {template.Title} to {target.Name}");
        for (int i = 0; i < amount; i++)
        {
            int addIdx = target.Stack.ResolveIndex(mode, 1, target.ViewSize)[0];
            Card newCard = new Card(template);
            target.Stack.Insert(addIdx, newCard);
            ConsoleInstance.Log($"added {newCard.Title} to {addIdx}");
            yield return StartCoroutine(target.StackDisplay.AddCard(newCard, addIdx));
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

    void LoadDisplays()
    {
        for (int i = 0; i < Player.Stack.Count; i++)
        {
            Player.StackDisplay.InsertCardNoAnim(Player.Stack[i]);
        }
        for (int i = 0; i < Enemy.Stack.Count; i++)
        {
            Enemy.StackDisplay.InsertCardNoAnim(Enemy.Stack[i]);
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

    bool CheckNextCardExecutable(Entity source)
    {
        return source.Stack.Count > 0 && source.CurrentEnergy >= source.Stack[0].EnergyCost;
    }

    void SetAllButtonsInteractible(bool interactible)
    {
        ExecuteNextButton.SetInteractible(interactible);
        EndTurnButton.SetInteractible(interactible);
        ExecuteAllButton.SetInteractible(interactible);
    }

    public string PrintDebugStatus()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Player.PrintEntityDebugStatus());
        builder.Append(Enemy.PrintEntityDebugStatus());
        return builder.ToString();
    }
}
