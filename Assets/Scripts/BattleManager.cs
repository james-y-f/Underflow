using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
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
    ButtonController ExecuteNextButton;
    ButtonController EndTurnButton;
    ButtonController ExecuteAllButton;
    ButtonController PauseButton;
    ButtonController LevelSelectButton;
    ButtonController RestartButton;
    StackDisplay PlayerStackDisplay;
    StackDisplay EnemyStackDisplay;
    Level CurrentLevel;

    // --- UI References ---
    [SerializeField] bool ConsoleActiveAtStart = false;
    DebugConsole ConsoleInstance;
    TMP_Text DisplayText;

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

        PlayerStackDisplay = GameObject.FindGameObjectWithTag(Constants.PlayerStackTag).GetComponent<StackDisplay>();
        EnemyStackDisplay = GameObject.FindGameObjectWithTag(Constants.EnemyStackTag).GetComponent<StackDisplay>();
        Assert.IsNotNull(PlayerStackDisplay);
        Assert.IsNotNull(EnemyStackDisplay);

        ExecuteNextButton = GameObject.FindGameObjectWithTag(Constants.ExecuteNextButtonTag).GetComponent<ButtonController>();
        EndTurnButton = GameObject.FindGameObjectWithTag(Constants.EndTurnButtonTag).GetComponent<ButtonController>();
        ExecuteAllButton = GameObject.FindGameObjectWithTag(Constants.ExecuteAllButtonTag).GetComponent<ButtonController>();
        PauseButton = GameObject.FindGameObjectWithTag(Constants.PauseButtonTag).GetComponent<ButtonController>();
        LevelSelectButton = GameObject.FindGameObjectWithTag(Constants.LevelSelectButtonTag).GetComponent<ButtonController>();
        RestartButton = GameObject.FindGameObjectWithTag(Constants.RestartButtonTag).GetComponent<ButtonController>();
        Assert.IsNotNull(ExecuteNextButton);
        Assert.IsNotNull(EndTurnButton);
        Assert.IsNotNull(ExecuteAllButton);
        Assert.IsNotNull(PauseButton);
        Assert.IsNotNull(RestartButton);

        InputActions = new PlayerInputActions();
        DisplayText = transform.Find("ResultDisplay").transform.Find("DisplayText").GetComponent<TMP_Text>();
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
        ConsoleInstance.WinCommand.AddListener(HandleWinCommand);
        ConsoleInstance.gameObject.SetActive(ConsoleActiveAtStart);

        PlayerStackDisplay.SwapAttempt.AddListener(HandleSwapAttempt);
        EnemyStackDisplay.SwapAttempt.AddListener(HandleSwapAttempt);

        ExecuteNextButton.OnClick.AddListener(PlayerExecuteNext);
        EndTurnButton.OnClick.AddListener(PlayerEndTurn);
        ExecuteAllButton.OnClick.AddListener(PlayerExecuteAll);
        PauseButton.OnClick.AddListener(HandlePause);
        LevelSelectButton.OnClick.AddListener(HandleLevelSelect);
        RestartButton.OnClick.AddListener(HandleRestart);
    }

    // --- Game Flow ---

    public void LoadLevel(Level level)
    {
        DisplayText.text = "";

        CurrentLevel = level;
        CurrentState = BattleState.Setup;
        ConsoleInstance.Log($"Setting up level {level.LevelNumber}:");

        Player = new Entity(level.PlayerBaseStats, true, PlayerStackDisplay);
        Enemy = new Entity(level.EnemyBaseStats, false, EnemyStackDisplay);
        // load both decks
        for (int i = 0; i < Player.Stack.Count; i++)
        {
            Player.StackDisplay.InsertCardNoAnim(Player.Stack[i]);
        }
        for (int i = 0; i < Enemy.Stack.Count; i++)
        {
            Enemy.StackDisplay.InsertCardNoAnim(Enemy.Stack[i]);
        }

        Player.StackDisplay.EnergyDisplayReference.SpawnBaseEnergy(Player.BaseEnergy);
        Enemy.StackDisplay.EnergyDisplayReference.SpawnBaseEnergy(Enemy.BaseEnergy);
        LevelSelectButton.gameObject.SetActive(false);
        RestartButton.gameObject.SetActive(false);

        if (level.LevelNumber == 0)
        {
            TutorialManager.Instance.StartTutorial();
        }

        StartCoroutine(StartPlayerTurn());
    }

    IEnumerator StartPlayerTurn()
    {
        if (CheckGameOver(Player)) yield break;
        ConsoleInstance.Log("\n---Player Turn---");
        ConsoleInstance.LogValidCommands();
        ExecuteNextButton.SetInteractible(true);
        ExecuteAllButton.SetInteractible(true);
        PauseButton.SetInteractible(true);
        EndTurnButton.SetInteractible(false); // player must execute at least one card per turn

        Player.ResetEnergy();
        yield return StartCoroutine(Player.StackDisplay.EnergyDisplayReference.StartTurnCoroutine(Player.CurrentEnergy));

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
        StartCoroutine(PlayerEndTurnCoroutine());
    }

    IEnumerator PlayerEndTurnCoroutine()
    {
        yield return StartCoroutine(Player.StackDisplay.EnergyDisplayReference.RemoveUnusedEnergy());
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
        yield return StartCoroutine(Enemy.StackDisplay.EnergyDisplayReference.StartTurnCoroutine(Enemy.CurrentEnergy));

        yield return StartCoroutine(ExecuteAllCoroutine(Enemy));
        yield return StartCoroutine(Enemy.StackDisplay.EnergyDisplayReference.RemoveUnusedEnergy());
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
                GameOver(false, "YOU LOST! YOUR STACK IS DEPLETED!", "Game Over");
            }
            else if (source == Enemy)
            {
                GameOver(true, "YOU WIN! ENEMY STACK DEPLETED!", "Victory!");
            }
            return true;
        }
        return false;
    }

    void GameOver(bool win, string message, string displayMessage)
    {
        CurrentState = BattleState.GameOver;
        SetAllButtonsInteractible(false);
        ConsoleInstance.Log("\n====================");
        ConsoleInstance.Log($"GAME OVER: {message}");
        ConsoleInstance.Log("====================");
        DisplayText.text = displayMessage;
        CleanUp();
        if (win)
        {
            if (CurrentLevel.LevelNumber >= GameManager.Instance.MaxLevel - 1)
            {
                GameManager.Instance.MoveToVictory();
            }
            GameManager.Instance.UnlockLevel(CurrentLevel.LevelNumber + 1);
        }
        LevelSelectButton.gameObject.SetActive(true);
        RestartButton.gameObject.SetActive(true);
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
        // ConsoleInstance.Log($"swap({currentIndex} -> {targetIndex}) successful");
        return true;
    }

    IEnumerator ExecuteNextCoroutine(Entity source, bool executingAll = false)
    {
        if (!executingAll && !CheckNextCardExecutable(source)) yield break;
        Card nextCard = source.Stack[0];
        source.CurrentEnergy -= nextCard.EnergyCost;
        yield return StartCoroutine(source.StackDisplay.EnergyDisplayReference.SetActiveEnergy(source.CurrentEnergy));

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
                PauseButton.SetInteractible(true);
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
                yield return StartCoroutine(target.StackDisplay.EnergyDisplayReference.SetActiveEnergy(target.CurrentEnergy));
                break;

            case EffectType.ModEnergyNextTurn:
                int addAmount = ResolveValue(source, effect.Values[0]);
                target.CarryOverEnergy += addAmount;
                yield return StartCoroutine(target.StackDisplay.EnergyDisplayReference.SetDimmedTempEnergy(target.CarryOverEnergy));
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
        for (int i = 0; i < amount; i++) // have to add card one at a time because adding changes state significantly
        {
            List<int> addIndices = target.Stack.ResolveIndex(mode, 1, target.ViewSize);
            int addIdx;
            if (addIndices.Count == 0)
            {
                addIdx = 0; // because you can still add to an empty list (and you always add to the top)
            }
            else
            {
                addIdx = addIndices[0];
            }
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

    void HandleLevelSelect()
    {
        GameManager.Instance.MoveToLevels();
    }

    void HandlePause()
    {
        GameManager.Instance.MoveToPause();
    }

    public void HandleRestart()
    {
        if (!GameIsOver)
        {
            GameOver(false, "", "");
        }
        LoadLevel(CurrentLevel);
        GameManager.Instance.MoveToBattle();
    }

    public void HandleMainMenu()
    {
        if (!GameIsOver)
        {
            GameOver(false, "", "");
        }
        GameManager.Instance.MoveToStart();
    }

    void HandleWinCommand()
    {
        GameOver(true, "YOU WON BY CHEATING!", "CHEAT SUCCESSFUL");
    }

    void ToggleConsole(InputAction.CallbackContext context)
    {
        Debug.Log($"Toggling Console to {!ConsoleInstance.gameObject.activeSelf}");
        ConsoleInstance.gameObject.SetActive(!ConsoleInstance.gameObject.activeSelf);
    }

    // --- Helper Functions ---
    void CleanUp()
    {
        Player.StackDisplay.Clear();
        Enemy.StackDisplay.Clear();
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
        PauseButton.SetInteractible(interactible);
    }

    public string PrintDebugStatus()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Player.PrintEntityDebugStatus());
        builder.Append(Enemy.PrintEntityDebugStatus());
        return builder.ToString();
    }
}
