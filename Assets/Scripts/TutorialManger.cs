using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    TextMeshProUGUI TutorialText;
    Button PrevButton;
    Button NextButton;
    Button SkipButton;
    int CurrentChapter = 0;
    string[] Chapters = new string[]
    {
        "Welcome to Underflow! (Please do not skip this tutorial if it is your first time playing)",
        "The objective of the game is to not run out of cards.",
        "If your stack has 0 cards at the beginning of your turn, you lose the game.",
        "The same goes for your opponent, so they can only lose at the beginning of their turn.",
        "It is very important that you understand this win condition, so feel free to go back and review it if you are still confused.",
        "At all times, some cards from the top of both your and your opponent's decks are revealed.",
        "Hover over them to learn more details about what they do.",
        "The number in the upper right corner of the card displays how much energy it costs to play the card.",
        "You can see how much energy you have remaining by the green cubes on the right side of your screen.",
        "You regenerate a base amount of energy at the beginning of each turn.",
        "Any unused energy at the end of the turn is NOT carried over to the next turn.",
        "You can rearrange your cards using drag and drop.",
        "Once you're satisfied, you can begin to execute your cards. Cards execute from left to right ->",
        "BUT BEWARE, you will NOT be able to rearrange your cards for the rest of the turn after you execute your first card that turn",
        "So, make sure you're ready, then use the \"Execute Next\" button to execute your leftmost card",
        "When you think it's appropriate, you can click \"End Turn\" to end your turn. Alternatively, click \"Execute All\" to execute cards until you do not have enough energy to execute your next card.",
        "After you end your turn, your opponent will execute cards sequencially until they run out of energy.",
        "Since you can see the top cards of your opponent's deck, be sure to use this to your advantage when planning your turn.",
        "If you're really stuck, press Alt+B and prepare for a surprise.",
        "Good luck and have fun! Thanks for playing!"
    };
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
        TutorialText = transform.Find("TutorialText").GetComponent<TextMeshProUGUI>();
        PrevButton = transform.Find("Buttons").transform.Find("PrevButton").GetComponent<Button>();
        NextButton = transform.Find("Buttons").transform.Find("NextButton").GetComponent<Button>();
        SkipButton = transform.Find("Buttons").transform.Find("SkipButton").GetComponent<Button>();
        Assert.IsNotNull(TutorialText);
        Assert.IsNotNull(PrevButton);
        Assert.IsNotNull(NextButton);
        Assert.IsNotNull(SkipButton);
    }

    void Start()
    {
        PrevButton.onClick.AddListener(HandlePrev);
        NextButton.onClick.AddListener(HandleNext);
        SkipButton.onClick.AddListener(HandleSkip);
        gameObject.SetActive(false);
    }

    public void StartTutorial()
    {
        PrevButton.interactable = false;
        CurrentChapter = 0;
        TutorialText.text = Chapters[0];
        gameObject.SetActive(true);
    }

    void HandlePrev()
    {
        CurrentChapter--;
        if (CurrentChapter == 0)
        {
            PrevButton.interactable = false;
        }
        TutorialText.text = Chapters[CurrentChapter];
    }

    void HandleNext()
    {
        PrevButton.interactable = true;
        CurrentChapter++;
        if (CurrentChapter >= Chapters.Length)
        {
            HandleSkip();
        }
        else
        {
            TutorialText.text = Chapters[CurrentChapter];
        }
    }

    void HandleSkip()
    {
        TutorialText.text = string.Empty;
        gameObject.SetActive(false);
    }
}
