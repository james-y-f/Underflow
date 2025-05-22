using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Assertions;
using System.Text;
using UnityEngine.Events;

public class DebugConsole : MonoBehaviour
{
    public static DebugConsole Instance;
    public UnityEvent<bool, int, int> SwapCommand;
    [SerializeField] TMP_InputField Input;
    [SerializeField] TextMeshProUGUI DisplayText;
    [SerializeField] ScrollRect Scroll;
    BattleManager Battle;
    StringBuilder DisplayLog;
    const string VALID_COMMANDS = "swap [target] [number] [number], debug";
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

        Assert.IsNotNull(Input);
        Assert.IsNotNull(DisplayText);
        Assert.IsNotNull(Scroll);
        DisplayLog = new StringBuilder();
    }

    void Start()
    {
        Battle = BattleManager.Instance;
        Input.onEndEdit.AddListener(HandleInput);
        // Keep input field focused
        Input.ActivateInputField();
        Input.Select();
    }

    void LogToConsole(string message)
    {
        DisplayLog.AppendLine(message);
        DisplayText.text = DisplayLog.ToString();
        if (!gameObject.activeSelf) return;
        StartCoroutine(ScrollToBottom());
    }

    public void Log(string message)
    {
        LogToConsole(message);
        Debug.Log(message); // Also log to Unity console
    }

    public void LogWarning(string message)
    {
        LogToConsole($"WARNING: {message}");
        Debug.LogWarning(message); // Also log to Unity console
    }

    public void LogError(string message)
    {
        LogToConsole($"ERROR: {message}");
        Debug.LogError(message); // Also log to Unity console
    }

    public void LogValidCommands()
    {
        LogToConsole(VALID_COMMANDS);
    }

    // TODO: maybe have a help command

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        Scroll.verticalNormalizedPosition = 0f;
        yield return new WaitForEndOfFrame();
        Scroll.verticalNormalizedPosition = 0f;
    }

    void ClearAndRefocusInput()
    {
        Input.text = "";
        Input.ActivateInputField();
        Input.Select();
    }

    private void HandleInput(string input)
    {

        if (Battle.CurrentState != BattleManager.BattleState.PlayerTurn)
        {
            LogWarning("You can only issue commands during the player's turn");
            ClearAndRefocusInput();
            return;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            ClearAndRefocusInput();
            return;
        }

        Log($"> {input}"); // Log player command
        string[] parts = input.ToLower().Split(' ');
        string command = parts[0];

        switch (command)
        {
            case "swap":
            case "swp":
            case "sw":
            case "s":
                if (parts.Length > 3 &&
                    int.TryParse(parts[2], out int currentIndex) &&
                    int.TryParse(parts[3], out int targetIndex))
                {
                    SwapCommand.Invoke(Str2Target(parts[1]), currentIndex, targetIndex);
                }
                else
                {
                    Log("Invalid command. Use 'swap [target] [number] [number]' (e.g., 'swap player 3 0').");
                }
                break;

            case "debug":
            case "d":
                Log(Battle.PrintDebugStatus());
                break;

            default:
                Log($"Unknown command: '{command}'. Use {VALID_COMMANDS}");
                break;
        }
        ClearAndRefocusInput();
    }

    bool Str2Target(string input) // outputs true if input is player, false otherwise
    {
        switch (input)
        {
            case "player":
            case "pl":
            case "p":
                return true;
            case "enemy":
            case "e":
                return false;
            default:
                LogWarning($"can't parse target: {input}, defaulting to player");
                return true;
        }
    }

    DeckEntity Str2Deck(string input)
    {
        switch (input)
        {
            case "playerstack":
            case "player":
            case "stack":
            case "ps":
            case "p":
            case "s":
                return DeckEntity.PlayerStack;
            case "enemystack":
            case "enemy":
            case "es":
            case "e":
                return DeckEntity.EnemyStack;
            case "playerdiscard":
            case "discard":
            case "pd":
            case "d":
                return DeckEntity.PlayerDiscard;
            case "enemydiscard":
            case "ed":
                return DeckEntity.EnemyDiscard;
            default:
                LogError($"invalid input: {input} for str2Deck");
                return DeckEntity.Undefined;
        }
    }
}