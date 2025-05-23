// TODO: use events instead of accessing the battlemanager instance directly
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using UnityEngine.Events;

// TODO: implement a display state to further decouple this display and gamestate

public class StackDisplay : MonoBehaviour
{
    public int WindowSize
    {
        get { return WindowSize; }
        set
        {
            WindowSize = value;
            UpdateCardLocations();
        }
    }
    public bool DeckSwappable;
    public UnityEvent<bool, int, int> SwapAttempt;
    [SerializeField] GameObject CardPrefab;
    bool IsPlayer;
    Transform LeftPos;
    Transform RightPos;
    List<GameObject> CardObjects;
    List<Vector3> CardPos;
    int lastViewSize; // to prevent repeated calculations of cardPos

    GameObject HoveredCard;
    GameObject HeldCardObject;
    Coroutine HoldingCardCoroutine;
    TMP_Text StackCountDisplay;

    void Awake()
    {
        Assert.IsNotNull(CardPrefab);
        LeftPos = transform.Find("LeftmostCardPos");
        RightPos = transform.Find("RightmostCardPos");
        CardObjects = new List<GameObject>();
        // precalculate the position for the first {ViewSize} cards
        CalcCardPos(WindowSize);
        lastViewSize = WindowSize + 1; // calcCardPos trigger when first called
        HeldCardObject = null;
        HoveredCard = null;
        IsPlayer = tag == "PlayerStack";
        StackCountDisplay = transform.Find("StackCount").GetComponent<TMP_Text>();
    }

    void Start()
    {
        UpdateCountDisplay();
    }

    public void InsertCard(CardInfo info, int index = -1)
    {
        // FIXME: account for index change in other cards when inserting not at the end
        Debug.Log($"Inserting card {info.Title}");
        if (index == -1)
        {
            index = CardObjects.Count(); // default to inserting at the end
        }
        Assert.IsTrue(index >= 0 && index <= CardObjects.Count);
        GameObject newCardObject = Instantiate(CardPrefab, transform.position, CardPrefab.transform.rotation);
        newCardObject.tag = IsPlayer ? "PlayerCard" : "EnemyCard";
        newCardObject.layer = IsPlayer ? LayerMask.NameToLayer("PlayerCards") : LayerMask.NameToLayer("EnemyCards");
        newCardObject.GetComponent<CardDisplay>().UpdateDisplay(info);
        CardController newCard = newCardObject.GetComponent<CardController>();
        newCard.Index = index;
        newCard.ParentStack = this;
        newCard.CardHover.AddListener(HandleCardHover);
        newCard.CardHeld.AddListener(HandleCardHeld);
        newCard.CardDrop.AddListener(HandleCardDrop);
        newCard.CardUnHover.AddListener(HandleCardUnHover);
        CardObjects.Insert(index, newCardObject);
        UpdateCardLocations();
    }

    public void RemoveCard(int index = 0)
    {
        Assert.IsTrue(index >= 0 && index <= CardObjects.Count);
        Destroy(CardObjects[index]);
        CardObjects.RemoveAt(index);
        UpdateCardLocations();
    }

    public void Swap(int a, int b)
    {
        Debug.Log($"{tag} Swapping {a} and {b}");
        Assert.IsTrue(a >= 0 && a <= CardObjects.Count);
        Assert.IsTrue(b >= 0 && b <= CardObjects.Count);
        CardObjects[a].GetComponent<CardController>().Index = b;
        CardObjects[b].GetComponent<CardController>().Index = a;
        GameObject temp = CardObjects[a];
        CardObjects[a] = CardObjects[b];
        CardObjects[b] = temp;
        UpdateCardLocations();
    }

    public void Clear()
    {
        foreach (GameObject card in CardObjects)
        {
            Destroy(card);
        }
        CardObjects = new List<GameObject>();
        CalcCardPos(WindowSize);
    }

    IEnumerator CardHeldCoroutine()
    {
        CardController heldCard = HeldCardObject.GetComponent<CardController>();
        while (HeldCardObject != null)
        {
            Vector3 heldCardPos = HeldCardObject.transform.position;
            int closestCardPositionIndex = 0;
            float closestCardPositionDistance = Vector3.Distance(CardPos[0], heldCardPos);
            for (int i = 1; i < CardPos.Count; i++)
            {
                float newDistance = Vector3.Distance(CardPos[i], heldCardPos);
                if (newDistance < closestCardPositionDistance)
                {
                    closestCardPositionDistance = newDistance;
                    closestCardPositionIndex = i;
                }
            }
            if (heldCard.Index != closestCardPositionIndex)
            {
                SwapAttempt.Invoke(IsPlayer, heldCard.Index, closestCardPositionIndex);
            }
            yield return null;
        }
    }

    // helper functions
    void CalcCardPos(int cardCount)
    {
        if (lastViewSize <= WindowSize)
        {
            lastViewSize = cardCount;
            return;
        } // no need to recalc if we are already at view size or less
        lastViewSize = cardCount;
        CardPos = new List<Vector3>();
        Vector3 left = LeftPos.position;
        // only the x coordinate of rightPos really matters, since deck lays out horizontally
        float xRight = RightPos.position.x;
        CardPos.Add(left);
        float xInterval = (xRight - left.x) / (cardCount - 1);
        for (int i = 1; i < cardCount; i++)
        {
            CardPos.Add(new Vector3(left.x + xInterval * i, left.y, left.z));
        }
        Debug.Log("calculated card positions");
    }

    void HandleCardHover(int index)
    {
        HoveredCard = CardObjects[index];
    }

    void HandleCardHeld(int index)
    {
        HeldCardObject = CardObjects[index];
        HoldingCardCoroutine = StartCoroutine(CardHeldCoroutine());
    }

    void HandleCardDrop()
    {
        HeldCardObject = null;
        StopCoroutine(HoldingCardCoroutine);
        UpdateCardLocations();
    }

    void HandleCardUnHover()
    {
        HoveredCard = null;
    }
    // helper methods 
    void UpdateCardLocations()
    {
        int newCount = CardObjects.Count;
        CalcCardPos(newCount);
        for (int i = 0; i < newCount; i++)
        {
            CardObjects[i].GetComponent<CardMotor>().Move(CardPos[i]);
        }
    }

    public bool CardHoverable()
    {
        return BattleManager.Instance.CurrentState == BattleManager.BattleState.PlayerTurn && HeldCardObject == null && HoveredCard == null;
    }

    public bool CardHoldable()
    {
        return BattleManager.Instance.CurrentState == BattleManager.BattleState.PlayerTurn && HeldCardObject == null;
    }

    public void UpdateCountDisplay()
    {
        // TODO: make this better
        int stackCount;
        int energy;
        if (IsPlayer)
        {
            stackCount = BattleManager.Instance.Player.Stack.Count;
            energy = BattleManager.Instance.Player.CurrentEnergy;
        }
        else
        {
            stackCount = BattleManager.Instance.Enemy.Stack.Count;
            energy = BattleManager.Instance.Enemy.CurrentEnergy;
        }
        StackCountDisplay.text = $"{stackCount}\n({energy})";
    }
}
