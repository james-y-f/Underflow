using UnityEngine;
using TMPro;
using System.Text;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Assertions;

using Deck = System.Collections.Generic.List<CardTemplate>;
using System;
using UnityEngine.InputSystem;

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;
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

    public enum Entity
    {
        Undefined,
        Player,
        Enemy
    }

    public BattleState currentState;
    private bool gameIsOver = false;


    // --- Player Variables ---
    public Deck playerMasterDeck; // Assign Card ScriptableObjects here in the Inspector
    public Deck playerStack;
    public Deck playerDiscard;

    private int playerMaxViewSize = 7;
    private int enemyMaxViewSize = 7;
    private int playerBaseEnergy = 3; // Base energy gained each turn
    private int playerCarryOverEnergy = 0;
    private int playerCurrentEnergy = 0;

    // --- Enemy Variables ---
    public Deck enemyMasterDeck; // Assign Card ScriptableObjects here in the Inspector
    public Deck enemyStack;
    public Deck enemyDiscard;

    public int enemyBaseEnergy = 2; // Base energy gained each turn
    public int enemyCarryOverEnergy = 0;
    public int enemyCurrentEnergy = 0;

    [SerializeField] StackDisplay playerDisplay;
    [SerializeField] StackDisplay enemyDisplay;
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
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        InputActions = new PlayerInputActions();

        playerStack = new Deck();
        playerDiscard = new Deck();
        enemyStack = new Deck();
        enemyDiscard = new Deck();

        playerDisplay.StartingViewSize = playerMaxViewSize;
        enemyDisplay.StartingViewSize = enemyMaxViewSize;
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
        currentState = BattleState.Setup;
        SetupGame();
    }

    // --- Game Flow ---

    void SetupGame()
    {
        Log("Setting up game...");

        // Populate and shuffle player deck
        playerStack.AddRange(playerMasterDeck);
        Shuffle(ref playerStack);

        // Populate and shuffle enemy deck
        enemyStack.AddRange(enemyMasterDeck);
        Shuffle(ref enemyStack);

        ReloadDisplays();

        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        if (gameIsOver) return;
        currentState = BattleState.PlayerTurn;
        playerCurrentEnergy = playerBaseEnergy + playerCarryOverEnergy; // Gain
        enemyCurrentEnergy = enemyBaseEnergy + enemyCarryOverEnergy; // Enemy gains energy here so player can predict enemy's next turn
        Log("\n--- Your Turn ---");
        Log($"Gained {playerBaseEnergy} base energy and {playerCarryOverEnergy} carryover energy.");
        playerCarryOverEnergy = 0;
        enemyCarryOverEnergy = 0;
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
        if (currentState != BattleState.PlayerTurn)
        {
            return;
        }
        if (gameIsOver) return;

        currentState = BattleState.PlayerExecution;
        Log("\n--- Executing Jobs ---");
        UpdateDisplay();
        ExecuteTurn(Entity.Player);
        if (gameIsOver) return;

        // Transition to Enemy Turn
        StartEnemyTurn();
    }

    void StartEnemyTurn()
    {
        if (gameIsOver) return;

        currentState = BattleState.EnemyTurn;
        Log("\n--- Enemy Turn ---");
        UpdateDisplay(); // Show state before enemy action
        ExecuteTurn(Entity.Enemy);
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
        if (enemyStack.Count == 0 && playerStack.Count == 0)
        {
            Debug.LogError("Tie, This probably shouldn't happen");
            GameOver("TIE?");
            return true;
        }
        else if (enemyStack.Count == 0)
        {
            GameOver("YOU WIN! ENEMY STACK DEPLETED!");
            return true;
        }
        else if (playerStack.Count == 0)
        {
            GameOver("YOU LOST! YOUR STACK IS DEPLETED!");
            return true;
        }
        return false;
    }

    void GameOver(string message)
    {
        gameIsOver = true;
        currentState = BattleState.GameOver;
        Log("\n====================");
        Log($"GAME OVER: {message}");
        Log("====================");
        UpdateDisplay();
    }

    // --- Console Input Handling ---

    private void HandleInput(string input)
    {
        if (currentState != BattleState.PlayerTurn || gameIsOver || string.IsNullOrWhiteSpace(input))
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
                    commandSuccess = Swap(Entity.Player, idx1, idx2);
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


        if (commandSuccess && currentState == BattleState.PlayerTurn) // Update display if a valid action was taken and we are still in player turn
        {
            UpdateDisplay();
        }

        // Clear input field after processing
        consoleInput.text = "";
        // Re-focus input field if still player's turn
        if (currentState == BattleState.PlayerTurn && !gameIsOver)
        {
            consoleInput.ActivateInputField();
            consoleInput.Select();
        }
    }

    // --- Core Mechanics Implementation ---

    public bool Swap(Entity target, int a, int b)
    {
        Assert.AreNotEqual(target, Entity.Undefined);
        bool isPlayer = target == Entity.Player;
        Deck targetDeck = isPlayer ? playerStack : enemyStack;
        StackDisplay display = isPlayer ? playerDisplay : enemyDisplay;

        int maxIdx = isPlayer ? playerMaxViewSize : enemyMaxViewSize; // cannot operate on cards out of view
        maxIdx = Math.Min(maxIdx, targetDeck.Count); // cannot operate on cards exceeding current size of deck
        if (a < 0 || b < 0 || a > maxIdx - 1 || b > maxIdx - 1)
        {
            Log("invalid index for swap");
            return false;
        }
        // adjust for 1-indexing
        // a = a - 1;
        // b = b - 1;
        CardTemplate temp = targetDeck[a];
        targetDeck[a] = targetDeck[b];
        targetDeck[b] = temp;
        Log($"swapped {a + 1}: {targetDeck[b].GetDisplayText()} with {b + 1}: {targetDeck[a].GetDisplayText()}");
        display.Swap(a, b);
        return true;
    }

    void ExecuteTurn(Entity source)
    {
        Assert.AreNotEqual(source, Entity.Undefined);
        switch (source)
        {
            case Entity.Player:
                ExecuteTurnHelper(ref playerStack, ref playerDiscard, ref playerCurrentEnergy, source);
                break;
            case Entity.Enemy:
                ExecuteTurnHelper(ref enemyStack, ref enemyDiscard, ref enemyCurrentEnergy, source);
                break;
            default:
                Debug.LogError("something went very wrong, invalid entity on execute turn");
                break;
        }
    }

    // Executes cards until there is not sufficient energy to execute the next card
    void ExecuteTurnHelper(ref Deck stack, ref Deck discard, ref int energy, Entity source)
    {
        while (true)
        {
            if (CheckGameOver()) { return; }
            // we know at this point that both stack still has cards remaining
            CardTemplate nextCard = stack[0];
            if (nextCard.energyCost > energy) { return; }
            energy -= nextCard.energyCost;
            stack.RemoveAt(0);
            discard.Add(nextCard);
            Log($"Executing: {nextCard.title}");
            ExecuteCardEffects(nextCard, source);
            UpdateDisplay();
        }
    }

    void ExecuteCardEffects(CardTemplate card, Entity source)
    {
        foreach (CardEffect effect in card.effects)
        {
            switch (effect.type)
            {
                case EffectType.NoEffect:
                    break;

                case EffectType.Delete:
                    Entity deleteTarget = ResolveTarget(source, effect.target);
                    Delete(effect.values[0], deleteTarget, effect.mode);
                    break;

                case EffectType.Add:
                    Entity addTarget = ResolveTarget(source, effect.target);
                    Add(effect.referenceCard, effect.values[0], addTarget, effect.mode);
                    break;

                case EffectType.GainEnergy:
                    Entity gainTarget = ResolveTarget(source, effect.target);
                    Assert.AreNotEqual(gainTarget, Entity.Undefined);
                    int gainAmount = effect.values[0];
                    Assert.IsTrue(gainAmount > 0);
                    if (gainTarget == Entity.Player)
                    {
                        playerCurrentEnergy += gainAmount;
                    }
                    else
                    {
                        enemyCurrentEnergy += gainAmount;
                    }
                    break;

                case EffectType.GainEnergyNextTurn:
                    Entity gainNextTarget = ResolveTarget(source, effect.target);
                    Assert.AreNotEqual(gainNextTarget, Entity.Undefined);
                    int gainNextAmount = effect.values[0];
                    Assert.IsTrue(gainNextAmount > 0);
                    if (gainNextTarget == Entity.Player)
                    {
                        playerCarryOverEnergy += gainNextAmount;
                    }
                    else
                    {
                        enemyCarryOverEnergy += gainNextAmount;
                    }
                    break;

                // Add more cases here for other effects
                default:
                    Log($"Effect type '{effect.type}' not implemented.");
                    Debug.LogError($"Effect type '{effect.type}' not implemented.");
                    break;
            }
        }
    }

    void Delete(int amount, Entity target, EffectMode mode)
    {
        Assert.IsTrue(amount > 0);
        Assert.AreNotEqual(target, Entity.Undefined);
        Log($"Deleting {amount} cards from {Entity2Str(target)}");
        switch (target)
        {
            case Entity.Player:
                DeleteHelper(amount, ref playerStack, ref playerDiscard, mode);
                return;
            case Entity.Enemy:
                DeleteHelper(amount, ref enemyStack, ref enemyDiscard, mode);
                return;
            default:
                Debug.LogError("No target for deletion");
                return;
        }
    }

    void DeleteHelper(int amount, ref Deck target, ref Deck targetDiscard, EffectMode mode)
    {
        for (int i = 0; i < amount; i++)
        {
            if (CheckGameOver()) { return; }
            // we now know that there are for sure cards left in target deck 
            int deleteIdx = ResolveIndex(mode, target.Count);
            CardTemplate deleted = target[deleteIdx];
            target.RemoveAt(deleteIdx);
            targetDiscard.Add(deleted);
            Log($"deleted {deleteIdx}: {deleted.title}");
        }
    }

    void Add(CardTemplate card, int amount, Entity target, EffectMode mode)
    {
        Assert.AreNotEqual(target, Entity.Undefined);
        Assert.IsTrue(amount > 0);
        Deck targetDeck = target == Entity.Player ? playerStack : enemyStack;
        for (int i = 0; i < amount; i++)
        {
            int addIdx = ResolveIndex(mode, targetDeck.Count);
            targetDeck.Insert(addIdx, card);
            Log($"added {card.title} to {addIdx}");
        }
    }

    void Shuffle(ref Deck stack)
    {
        int n = stack.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n);
            CardTemplate temp = stack[k];
            stack[k] = stack[n];
            stack[n] = temp;
        }
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
        sb.Append($"player: ({playerStack.Count}) [{playerCurrentEnergy}] <{playerMaxViewSize}>\n");
        sb.Append(PrintDeckContent(playerStack));
        playerConsole.text = sb.ToString();

        // Enemy
        sb.Clear();
        sb.Append($"enemy: ({enemyStack.Count}) [{enemyCurrentEnergy}] <{enemyMaxViewSize}>\n");
        sb.Append(PrintDeckContent(enemyStack));
        enemyConsole.text = sb.ToString();
    }

    void ReloadDisplays()
    {
        playerDisplay.Clear();
        enemyDisplay.Clear();
        int playerViewSize = Math.Min(playerMaxViewSize, playerStack.Count);
        int enemyViewSize = Math.Min(enemyMaxViewSize, enemyStack.Count);
        for (int i = 0; i < playerViewSize; i++)
        {
            playerDisplay.InsertCard(playerStack[i]);
        }
        for (int i = 0; i < enemyViewSize; i++)
        {
            enemyDisplay.InsertCard(enemyStack[i]);
        }
    }

    Entity ResolveTarget(Entity source, EffectTarget target)
    {
        Assert.AreNotEqual(source, Entity.Undefined);
        switch (target)
        {
            case EffectTarget.Self:
                return source;
            case EffectTarget.Opponent:
                if (source == Entity.Player)
                {
                    return Entity.Enemy;
                }
                return Entity.Player;
            default:
                return Entity.Undefined;
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
        Log(target[idx].GetDescription());
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
        for (int i = 1; i <= limit; i++)
        {
            str.Append($"{i}. {target[i - 1].GetDisplayText()}\n");
        }
        return str.ToString();
    }

    string Entity2Str(Entity input)
    {
        switch (input)
        {
            case Entity.Player:
                return "Player";
            case Entity.Enemy:
                return "Enemy";
            case Entity.Undefined:
            default:
                return "Undefined";
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
                return playerStack;
            case "enemystack":
            case "es":
                return enemyStack;
            case "playerdiscard":
            case "discard":
            case "pd":
            case "d":
                return playerDiscard;
            case "enemydiscard":
            case "ed":
                return enemyDiscard;
            default:
                Debug.LogError("invalid input for str2Deck");
                return null;
        }
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
