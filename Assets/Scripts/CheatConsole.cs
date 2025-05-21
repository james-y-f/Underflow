using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Assertions;
using System.Text;
using System;

public class CheatConsole : MonoBehaviour
{
    public static CheatConsole Instance;

    [SerializeField] TMP_InputField consoleInput;
    [SerializeField] TextMeshProUGUI consoleDisplay;
    [SerializeField] ScrollRect consoleScroll;
    [SerializeField] TextMeshProUGUI playerConsole;
    [SerializeField] TextMeshProUGUI enemyConsole;
    const string VALID_COMMANDS = "swap [number] [number], end, show [list], desc [list] [number]";
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

        Assert.IsNotNull(consoleInput);
        Assert.IsNotNull(consoleDisplay);
        Assert.IsNotNull(consoleScroll);
        Assert.IsNotNull(enemyConsole);
        Assert.IsNotNull(playerConsole);
    }
    void Start()
    {
        consoleInput.onEndEdit.AddListener(HandleInput);
        // Keep input field focused
        consoleInput.ActivateInputField();
        consoleInput.Select();
    }
    void Log(string message)
    {
        consoleDisplay.text += message + "\n";
        StartCoroutine(ScrollToBottom());
        Debug.Log(message); // Also log to Unity console
    }

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        consoleScroll.verticalNormalizedPosition = 0f;
        yield return new WaitForEndOfFrame();
        consoleScroll.verticalNormalizedPosition = 0f;
    }

    void RefocusInput()
    {
        consoleInput.ActivateInputField();
        consoleInput.Select();
    }
    string PrintDeckContent(Deck target, int lines = -1)
    {
        if (lines == -1) // default to printing entire deck
        {
            lines = target.Count;
        }
        StringBuilder builder = new StringBuilder();
        int limit = Math.Min(lines, target.Count);
        for (int i = 0; i < limit; i++)
        {
            builder.Append($"{i}. {target[i].Info.GetDisplayText()}\n");
        }
        return builder.ToString();
    }



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
                    commandSuccess = SwapStack(Player, idx1, idx2, true, true);
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
}