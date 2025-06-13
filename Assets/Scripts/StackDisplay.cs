using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using TMPro;

// TODO: implement an object pool for storing cards that are not in the window yet
// TODO: make bypassing viewsize work

public class StackDisplay : MonoBehaviour
{
    public UnityEvent<bool, int, int> SwapAttempt;
    public string Name;
    int viewSize;
    public int ViewSize
    {
        get { return viewSize; }
        set
        {
            viewSize = value;
            if (LeftPos != null) // so that this only runs after Start()
            {
                UpdateCardLocations();
            }
        }
    }
    public bool DeckSwappable = true;
    public EnergyDisplay EnergyDisplayReference;
    [SerializeField] GameObject CardPrefab;
    [SerializeField] bool IsPlayer;
    Transform SpawnPoint;
    Transform LeftPos;
    Transform RightPos;
    Transform ExecutionPos;
    [SerializeField] List<GameObject> CardObjects;
    List<Vector3> CardPos;
    Stack<GameObject> ExecutingCards;
    GameObject HoveredCard;
    GameObject HeldCardObject;
    Coroutine HoldingCardCoroutine;
    TMP_Text DisplayText;

    void Awake()
    {
        Assert.IsNotNull(CardPrefab);
        SpawnPoint = transform.Find("SpawnPoint");
        LeftPos = transform.Find("LeftmostCardPos");
        RightPos = transform.Find("RightmostCardPos");
        ExecutionPos = transform.Find("ExecutionPos");
        EnergyDisplayReference = transform.Find("EnergyDisplay").GetComponent<EnergyDisplay>();
        DisplayText = transform.Find("Display").transform.Find("DisplayText").GetComponent<TMP_Text>();
        CardObjects = new List<GameObject>();
        ExecutingCards = new Stack<GameObject>();
        IsPlayer = gameObject.tag == Constants.PlayerStackTag;
        DeckSwappable = IsPlayer; // for now, TODO: find a way to couple this with entity in battle manager
        CalcCardPos(ViewSize);
    }

    public IEnumerator MoveTopCardToExecutionPos()
    {
        Assert.IsNotNull(CardObjects[0]);
        ExecutingCards.Push(CardObjects[0]);
        Debug.Log($"Moving {ExecutingCards.Peek().name} to Execution");
        CardObjects.RemoveAt(0);
        UpdateCardLocations();
        Vector3 executionPos = new Vector3(ExecutionPos.position.x, ExecutionPos.position.y + (ExecutingCards.Count - 1) * Constants.CardHeight, ExecutionPos.position.z);
        yield return StartCoroutine(GetMotor(ExecutingCards.Peek()).MoveCoroutine(executionPos));
    }

    public IEnumerator DiscardExecutingCard()
    {
        Assert.IsTrue(ExecutingCards.Count > 0);
        Debug.Log($"Execution Done, Discarding {ExecutingCards.Peek().name}");

        yield return new WaitForSecondsRealtime(Constants.StandardActionDelay);
        yield return StartCoroutine(GetDisplay(ExecutingCards.Peek()).FlashColor(Constants.ExecutionDoneColor));

        Destroy(ExecutingCards.Pop());
        yield break; // placeholder line
    }

    public void TargetCard(int index = 0)
    {
        AssertIndexInRange(index);
        GameObject targetedCard = CardObjects[index];
        GetMotor(targetedCard).SetHover(true);
        GetDisplay(targetedCard).ShowSelectionHighlight();
    }

    public void UntargetCard(int index = 0)
    {
        AssertIndexInRange(index);
        GameObject targetedCard = CardObjects[index];
        GetMotor(targetedCard).SetHover(false);
        GetDisplay(targetedCard).ResetColor();
    }

    public IEnumerator DeleteCard(int index = 0)
    {
        AssertIndexInRange(index);
        GameObject removedCard = CardObjects[index];

        // TODO: add more animation in the future
        yield return new WaitForSecondsRealtime(Constants.StandardActionDelay);
        yield return GetDisplay(removedCard).FlashColor(Constants.DeletionEffectColor);

        CardObjects.RemoveAt(index);
        Destroy(removedCard);
        UpdateCardLocations();
    }

    public IEnumerator TransformCard(Card newCard, int index = 0)
    {
        AssertIndexInRange(index);
        GameObject transformedCard = CardObjects[index];

        yield return new WaitForSecondsRealtime(Constants.StandardActionDelay);
        yield return GetDisplay(transformedCard).FlashColor(Constants.FlashHighlight);
        GetDisplay(transformedCard).UpdateDisplay(newCard);
        GetMotor(transformedCard).SetHover(false);
        GetDisplay(transformedCard).ResetColor();
    }


    public IEnumerator AddCard(Card card, int index)
    {
        CardDisplay cardDisplay = GetDisplay(InsertCardNoAnim(card, index));
        yield return StartCoroutine(cardDisplay.FlashColor(Constants.AddEffectColor));
    }

    public GameObject InsertCardNoAnim(Card reference, int index = int.MaxValue)
    {
        if (index == int.MaxValue)
        {
            index = CardObjects.Count(); // default to inserting at the end
        }
        Assert.IsTrue(index >= 0 && index <= CardObjects.Count);
        Vector3 spawnPosition = index < CardPos.Count ? CardPos[index] : SpawnPoint.position;
        GameObject card = Instantiate(CardPrefab, spawnPosition, CardPrefab.transform.rotation);
        card.tag = IsPlayer ? Constants.PlayerCardTag : Constants.EnemyCardTag;
        card.layer = IsPlayer ? LayerMask.NameToLayer(Constants.PlayerCardsLayerName)
                              : LayerMask.NameToLayer(Constants.EnemyCardsLayerName);
        card.name = $"{card.tag} {reference.Title} {reference.UID}";
        CardController controller = card.GetComponent<CardController>();
        controller.ParentStack = this;
        controller.CardDrop.AddListener(UpdateCardLocations);
        controller.CardHover.AddListener(HandleCardHover);
        controller.CardHeld.AddListener(HandleCardHeld);
        controller.CardDrop.AddListener(HandleCardDrop);
        controller.CardUnHover.AddListener(HandleCardUnHover);
        controller.CardReference = reference;
        CardObjects.Insert(index, card);
        UpdateCardLocations();
        return card;
    }

    public void UpdateToOrder(List<int> newOrder)
    {
        Assert.IsNotNull(newOrder);
        // check that we are only assigning order to existing cards
        Assert.IsTrue(newOrder.Count <= CardObjects.Count);
        //check that the order should contain every number from 0 to count-1
        for (int i = 0; i < newOrder.Count; i++)
        {
            Assert.IsTrue(newOrder.Contains(i));
        }
        // make a copy of the current card objects
        GameObject[] CardCopies = new GameObject[newOrder.Count];
        for (int i = 0; i < newOrder.Count; i++)
        {
            CardCopies[i] = CardObjects[i];
        }

        // update the current cardObjects according to the new order
        for (int i = 0; i < newOrder.Count; i++)
        {
            CardObjects[i] = CardCopies[newOrder[i]];
        }
        UpdateCardLocations();
    }

    public void Clear()
    {
        foreach (GameObject card in CardObjects)
        {
            Destroy(card);
        }
        CardObjects = new List<GameObject>();
        CalcCardPos(ViewSize);
    }

    void HandleCardHover(CardController card)
    {
        HoveredCard = card.gameObject;
    }

    void HandleCardHeld(CardController card)
    {
        HeldCardObject = card.gameObject;
        Debug.Log($"holding card {HeldCardObject.name}");
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

    IEnumerator CardHeldCoroutine()
    {
        CardController heldCard = HeldCardObject.GetComponent<CardController>();
        while (HeldCardObject != null)
        {
            Vector3 heldCardPos = HeldCardObject.transform.position;
            int closestCardPositionIndex = 0;
            float closestCardPositionDistance = Vector3.Distance(CardPos[0], heldCardPos);
            for (int i = 1; i < Math.Min(CardPos.Count, CardObjects.Count); i++)
            {
                float newDistance = Vector3.Distance(CardPos[i], heldCardPos);
                if (newDistance < closestCardPositionDistance)
                {
                    closestCardPositionDistance = newDistance;
                    closestCardPositionIndex = i;
                }
            }
            int heldIndex = CardObjects.FindIndex(x => x == HeldCardObject);
            if (heldIndex != closestCardPositionIndex)
            {
                SwapAttempt.Invoke(IsPlayer, heldIndex, closestCardPositionIndex);
            }
            yield return new WaitForFixedUpdate();
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


    // helper functions
    void CalcCardPos(int cardCount)
    {
        CardPos = new List<Vector3>();
        Vector3 left = LeftPos.position;
        // only the x coordinate of rightPos really matters, since deck lays out horizontally
        float xRight = RightPos.position.x;
        CardPos.Add(left);
        float xInterval = (xRight - left.x) / (cardCount - 1);
        for (int i = 1; i < cardCount; i++)
        {
            CardPos.Add(new Vector3(left.x + xInterval * i, Constants.BaseHeight, left.z));
        }
    }

    void UpdateCardLocations()
    {
        CalcCardPos(viewSize);
        for (int i = 0; i < CardObjects.Count; i++)
        {
            GameObject card = CardObjects[i];
            CardMotor motor = GetMotor(card);
            CardController controller = GetController(card);
            if (i < viewSize)
            {
                controller.InView = true;
                motor.InView = true;
                motor.Move(CardPos[i]);
                motor.TurnFace(true);
            }
            else
            {
                controller.InView = false;
                motor.InView = false;
                motor.Move(new Vector3(SpawnPoint.position.x,
                                        SpawnPoint.position.y + (CardObjects.Count - i - 1) * Constants.CardHeight,
                                        SpawnPoint.position.z));
                motor.TurnFace(false);
            }
        }
        DisplayText.text = $"Cards Left: {CardObjects.Count}";
    }

    CardController GetController(GameObject card)
    {
        Assert.IsNotNull(card);
        CardController controller = card.GetComponent<CardController>();
        Assert.IsNotNull(controller);
        return controller;
    }

    CardMotor GetMotor(GameObject card)
    {
        Assert.IsNotNull(card);
        CardMotor motor = card.GetComponent<CardMotor>();
        Assert.IsNotNull(motor);
        return motor;
    }

    CardDisplay GetDisplay(GameObject card)
    {
        Assert.IsNotNull(card);
        CardDisplay display = card.GetComponent<CardDisplay>();
        Assert.IsNotNull(display);
        return display;
    }

    void AssertIndexInRange(int index)
    {
        Assert.IsTrue(index >= 0 && index <= CardObjects.Count);
    }
}
