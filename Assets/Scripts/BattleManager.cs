using UnityEngine;
using TMPro;
using System.Text;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
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

    public BattleState CurrentState;
    private bool gameIsOver = false;


    [SerializeField] Entity Player;
    [SerializeField] Entity Enemy;
    [SerializeField] EntityBaseStats PlayerBaseStats;
    [SerializeField] EntityBaseStats EnemyBaseStats;

    // --- UI References ---
    [SerializeField] bool consoleActive = false;
    [SerializeField] Canvas console;
    [SerializeField] TMP_InputField consoleInput;
    [SerializeField] TextMeshProUGUI consoleDisplay;
    [SerializeField] ScrollRect consoleScroll;
    [SerializeField] TextMeshProUGUI playerConsole;
    [SerializeField] TextMeshProUGUI enemyConsole;
    string VALID_COMMANDS = "swap [number] [number], end, show [list], desc [list] [number]";

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

        Player = new Entity(PlayerBaseStats, true);
        Enemy = new Entity(EnemyBaseStats, false);
        console.gameObject.SetActive(consoleActive);
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
        consoleInput.onEndEdit.AddListener(HandleInput);
        // Keep input field focused
        consoleInput.ActivateInputField();
        consoleInput.Select();
        CurrentState = BattleState.Setup;
        SetupGame();
    }

    // --- Game Flow ---

    void SetupGame()
    {
        Log("Setting up game...");

        // Populate and shuffle player deck

        // Populate and shuffle enemy deck

        ReloadDisplays();

        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        if (gameIsOver) return;
        CurrentState = BattleState.PlayerTurn;
        Player.ResetEnergy();
        // TODO: move this somewhere else when ready
        Enemy.ResetEnergy();
        Log("\n--- Your Turn ---");
        UpdateDisplay();
        Log($"Enter command: {VALID_COMMANDS}");

        // Re-focus input field
        if (consoleInput != null)
        {
            consoleInput.ActivateInputField();
            consoleInput.Select();
        }
    }

    public void PlayerExecute()
    {
        if (CurrentState != BattleState.PlayerTurn)
        {
            return;
        }
        if (gameIsOver) return;

        CurrentState = BattleState.PlayerExecution;
        Log("\n--- Executing Jobs ---");
        UpdateDisplay();
        ExecuteTurn(Player);
        if (gameIsOver) return;

        // Transition to Enemy Turn
        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        if (gameIsOver) return;

        CurrentState = BattleState.EnemyTurn;
        Log("\n--- Enemy Turn ---");
        UpdateDisplay(); // Show state before enemy action
        ExecuteTurn(Enemy);
        if (gameIsOver) return;

        // Transition back to Player Turn
        StartPlayerTurn();
    }

    bool CheckGameOver()
    {
        if (gameIsOver)
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
        gameIsOver = true;
        CurrentState = BattleState.GameOver;
        Log("\n====================");
        Log($"GAME OVER: {message}");
        Log("====================");
        UpdateDisplay();
    }

    // --- Console Input Handling ---

    private void HandleInput(string input)
    {
        if (CurrentState != BattleState.PlayerTurn || gameIsOver || string.IsNullOrWhiteSpace(input))
        {
            // Clear input field if not player turn or input is empty
            if (consoleInput != null) consoleInput.text = "";
            // Re-focus input field
            if (consoleInput != null && !gameIsOver)
            {
                consoleInput.ActivateInputField();
                consoleInput.Select();
            }
            return;
        }

        Log($"> {input}"); // Log player command
        string[] parts = input.ToLower().Split(' ');
        string command = parts[0];

        bool commandSuccess = false;

        switch (command)
        {
            case "swap":
            case "s":
                if (parts.Length > 2 && int.TryParse(parts[1], out int idx1) && int.TryParse(parts[2], out int idx2))
                {
                    commandSuccess = SwapStack(Player, idx1, idx2, true, true, true);
                }
                else
                {
                    Log("Invalid command. Use 'swap [number] [number]' (e.g., 'swap 3 1').");
                }
                break;

            case "end":
            case "e":
                commandSuccess = true; // Always succeeds
                PlayerExecute();
                break;

            case "show":
            case "sh":
                if (parts.Length > 1)
                {
                    Deck deckToShow = Str2Deck(parts[1]);
                    if (deckToShow == null) { Log("invalid list name"); break; }
                    Log(PrintDeckContent(deckToShow, deckToShow.Count));
                    commandSuccess = true;
                }
                else
                {
                    Log("Invalid command. Use 'show [list]' (e.g., 'show playerdiscard').");
                }
                break;

            case "desc":
            case "d":
                if (parts.Length > 2 && int.TryParse(parts[2], out int idx))
                {
                    // Adjust index to be 0-based
                    Deck deckToDesc = Str2Deck(parts[1]);
                    if (deckToDesc == null) { Log("invalid list name"); break; }
                    commandSuccess = PrintDesc(Str2Deck(parts[1]), idx - 1);
                }
                else
                {
                    Log("Invalid command. Use 'desc [target] [number]' (e.g., 'desc enemydiscard 1').");
                }
                break;

            default:
                Log($"Unknown command: '{command}'. Use {VALID_COMMANDS}");
                break;
        }


        if (commandSuccess && CurrentState == BattleState.PlayerTurn) // Update display if a valid action was taken and we are still in player turn
        {
            UpdateDisplay();
        }

        // Clear input field after processing
        consoleInput.text = "";
        // Re-focus input field if still player's turn
        if (CurrentState == BattleState.PlayerTurn && !gameIsOver)
        {
            consoleInput.ActivateInputField();
            consoleInput.Select();
        }
    }

    // --- Core Mechanics Implementation ---

    public bool SwapStack(Entity target, int a, int b, bool hard = false, bool bypassSwappability = false, bool bypassViewSize = false)
    {
        Assert.IsNotNull(target);
        int maxIdx = Math.Min(target.ViewSize, target.Stack.Count); // cannot operate on cards exceeding current size of deck
        if (bypassViewSize)
        {
            maxIdx = target.Stack.Count;
        }
        if (a < 0 || b < 0 || a >= maxIdx || b >= maxIdx)
        {
            Log("invalid index for swap");
            return false;
        }
        target.Stack.Swap(a, b, hard, bypassSwappability);
        // target.StackDisplay.Swap(a, b);
        Log("swap successful");
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
            Log($"Executing: {nextCard.Info.Title}");
            ExecuteCardEffects(nextCard, source);
            UpdateDisplay();
        }
    }


    // Executes cards until there is not sufficient energy to execute the next card
    void ExecuteTurnHelper(ref Deck stack, ref Deck discard, ref int energy, Entity source)
    {

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
        Log($"Deleted {amount} cards from {target.Name}");
        for (int i = 0; i < amount; i++)
        {
            if (CheckGameOver()) { return; }
            // we now know that there are for sure cards left in target deck 
            int deleteIdx = ResolveIndex(mode, target.Stack.Count);
            Card deleted = target.Stack[deleteIdx];
            target.Stack.RemoveAt(deleteIdx);
            target.Discard.Add(deleted);
            Log($"deleted {deleteIdx}: {deleted.Info.GetDisplayText()}");
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
            Log($"added {card.Info.Title} to {addIdx}");
        }
    }

    // --- Event Handlers ---

    void HandleSwapAttempt(bool IsPlayer, int currentIndex, int targetIndex)
    {
        Entity targetEntity = IsPlayer ? Player : Enemy;
        Debug.Log($"processing swap from handler, {currentIndex}, {targetIndex}");
        SwapStack(targetEntity, currentIndex, targetIndex);
    }

    // --- Helper Functions ---

    void ToggleConsole(InputAction.CallbackContext ctx)
    {
        consoleActive = !consoleActive;
        console.gameObject.SetActive(consoleActive);
    }

    void Log(string message)
    {
        consoleDisplay.text += message + "\n";
        StartCoroutine(ScrollToBottom());
        Debug.Log(message); // Also log to Unity console
    }

    void UpdateDisplay()
    {
        // Displays
        // This deletes everything and replaces them, inefficient but works for now
        // TODO: replace this logic
        ReloadDisplays();

        // Consoles
        // Player
        StringBuilder sb = new StringBuilder();
        sb.Append($"player: ({Player.Stack.Count}) [{Player.CurrentEnergy}] <{Player.ViewSize}>\n");
        sb.Append(PrintDeckContent(Player.Stack));
        playerConsole.text = sb.ToString();

        // Enemy
        sb.Clear();
        sb.Append($"enemy: ({Enemy.Stack.Count}) [{Enemy.CurrentEnergy}] <{Enemy.ViewSize}>\n");
        sb.Append(PrintDeckContent(Enemy.Stack));
        enemyConsole.text = sb.ToString();
    }

    void ReloadDisplays()
    {
        // Player.StackDisplay.Clear();
        // Enemy.StackDisplay.Clear();
        int playerViewSize = Math.Min(Player.ViewSize, Player.Stack.Count);
        int enemyViewSize = Math.Min(Enemy.ViewSize, Enemy.Stack.Count);
        for (int i = 0; i < playerViewSize; i++)
        {
            // Player.StackDisplay.InsertCard(Player.Stack[i].Info);
        }
        for (int i = 0; i < enemyViewSize; i++)
        {
            // Enemy.StackDisplay.InsertCard(Enemy.Stack[i].Info);
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

    bool PrintDesc(Deck target, int idx)
    {
        if (idx < 0 || idx >= target.Count)
        {
            Log($"Invalid card index: {idx + 1}. Choose a number between 1 and {target.Count}.");
            return false;
        }
        Log(target[idx].Info.GetDescription());
        return true;
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
                Debug.LogError("invalid input for str2Deck");
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

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        consoleScroll.verticalNormalizedPosition = 0f;
        yield return new WaitForEndOfFrame();
        consoleScroll.verticalNormalizedPosition = 0f;
    }
}
