using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Events;

public class BattleConsole : MonoBehaviour
{
    public static BattleConsole Instance;
    public bool IsActive;
    [SerializeField] TMP_InputField Input;
    [SerializeField] TextMeshProUGUI Display;
    [SerializeField] ScrollRect Scroll;
    public UnityEvent<string, int, int> SwapCommand;
    public UnityEvent<string> PrintCommand;
    public UnityEvent WinCommand;
    string VALID_COMMANDS = "swap(s) [target] [number] [number], print(p) [list], win(w)";
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        Assert.IsNotNull(Input);
        Assert.IsNotNull(Display);
        Assert.IsNotNull(Scroll);
        Input.onEndEdit.AddListener(HandleInput);
        // Keep input field focused
        Input.ActivateInputField();
    }

    public void Log(string message)
    {
        Display.text += message + "\n";
        StartCoroutine(ScrollToBottom());
        Debug.Log(message);
    }

    public void LogError(string message)
    {
        // TODO: change the text color of this somehow
        Display.text += message + "\n";
        StartCoroutine(ScrollToBottom());
        Debug.LogError(message);
    }

    // TODO: Set Active (disable / enable all children)

    public void SetActivation(bool expectedState)
    {
        if (expectedState != IsActive)
        {
            ToggleActivation();
        }
    }

    public void ToggleActivation()
    {
        IsActive = !IsActive;
        // this somehow access all children
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(IsActive);
        }
    }

    void HandleInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            Input.text = "";
            return;
        }
        Log($"> {input}"); // Log player command
        string[] parts = input.ToLower().Split(' ');
        string command = parts[0];
        switch (command)
        {
            case "swap":
            case "s":
                if (parts.Length > 2 && int.TryParse(parts[2], out int index1) && int.TryParse(parts[3], out int index2))
                {
                    SwapCommand.Invoke(parts[1], index1, index2);
                }
                else
                {
                    Log("Invalid command. Use 'swap [target] [number] [number]' (e.g., 'swap player 3 1').");
                }
                break;

            case "print":
            case "p":
                if (parts.Length > 1)
                {
                    PrintCommand.Invoke(parts[1]);
                }
                else
                {
                    Log("Invalid command. Use 'print [list]' (e.g., 'print playerdiscard').");
                }
                break;

            case "win":
            case "w":
                WinCommand.Invoke();
                break;

            default:
                Log($"Unknown command: '{command}'. Use {VALID_COMMANDS}");
                break;
        }

        // Clear input field after processing
        Input.text = "";
        // Re-focus input field if still player's turn
        Input.ActivateInputField();
        Input.Select();
    }


    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        Scroll.verticalNormalizedPosition = 0f;
        yield return new WaitForEndOfFrame();
        Scroll.verticalNormalizedPosition = 0f;
    }

}