using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Timeline;

// TODO: implement an object pool for storing cards that are not in the window yet
// TODO: make bypassing swappability work

public class StackDisplay : MonoBehaviour
{
    public UnityEvent<bool, int, int> SwapAttempt;
    int viewSize;
    public int ViewSize
    {
        get { return viewSize; }
        set
        {
            viewSize = value;
            if (LeftPos != null)
            {
                UpdateCardLocations();
            }
        }
    }
    public bool DeckSwappable = true;
    public EnergyDisplay EnergyDisplay;
    [SerializeField] GameObject CardPrefab;
    [SerializeField] bool IsPlayer;
    Transform SpawnPoint;
    Transform LeftPos;
    Transform RightPos;
    Transform ExecutionPos;
    [SerializeField] List<GameObject> CardObjects;
    List<Vector3> CardPos;

    GameObject ExecutingCard;
    GameObject HoveredCard;
    GameObject HeldCardObject;
    Coroutine HoldingCardCoroutine;

    void Awake()
    {
        Assert.IsNotNull(CardPrefab);
        SpawnPoint = transform;
        LeftPos = transform.Find("LeftmostCardPos");
        RightPos = transform.Find("RightmostCardPos");
        ExecutionPos = transform.Find("ExecutionPos");
        EnergyDisplay = transform.Find("EnergyDisplay").GetComponent<EnergyDisplay>();
        CardObjects = new List<GameObject>();
        IsPlayer = gameObject.tag == Constants.PlayerStackTag;
        DeckSwappable = IsPlayer; // for now, TODO: find a way to couple this with entity in battle manager
    }

    void Start()
    {
        // precalculate the position for the first {ViewSize} cards
        CalcCardPos(ViewSize);
    }

    public IEnumerator MoveTopCardToExecutionPos()
    {
        ExecutingCard = CardObjects[0];
        Assert.IsNotNull(ExecutingCard);
        Debug.Log($"Moving {ExecutingCard.name} to Execution");
        CardObjects.RemoveAt(0);
        UpdateCardLocations();
        yield return StartCoroutine(GetMotor(ExecutingCard).MoveCoroutine(ExecutionPos.position));
    }

    public IEnumerator DiscardExecutingCard()
    {
        Debug.Log($"Execution Done, Discarding {ExecutingCard.name}");
        Assert.IsNotNull(ExecutingCard);

        yield return new WaitForSeconds(1);
        yield return StartCoroutine(GetDisplay(ExecutingCard).FlashColor(Constants.ExecutionDoneColor));

        Destroy(ExecutingCard);
        ExecutingCard = null;
        yield break; // placeholder line
    }

    public IEnumerator DiscardCard(int index = 0)
    {
        Assert.IsTrue(index >= 0 && index <= CardObjects.Count);
        GameObject removedCard = CardObjects[index];

        // TODO: add more animation in the future
        yield return new WaitForSeconds(1);
        yield return GetDisplay(removedCard).FlashColor(Constants.DeletionEffectColor);

        CardObjects.RemoveAt(index);
        Destroy(removedCard);
        UpdateCardLocations();
    }

    public void InsertCard(CardInfo info, int index = int.MaxValue)
    {
        if (index == int.MaxValue)
        {
            index = CardObjects.Count(); // default to inserting at the end
        }
        Assert.IsTrue(index >= 0 && index <= CardObjects.Count);
        GameObject card = Instantiate(CardPrefab, SpawnPoint.position, CardPrefab.transform.rotation);
        card.tag = IsPlayer ? Constants.PlayerCardTag : Constants.EnemyCardTag;
        card.layer = IsPlayer ? LayerMask.NameToLayer(Constants.PlayerCardsLayerName)
                              : LayerMask.NameToLayer(Constants.EnemyCardsLayerName);
        card.name = $"{card.tag} {info.Title} {info.UID}";
        CardController controller = card.GetComponent<CardController>();
        controller.ParentStack = this;
        controller.CardDrop.AddListener(UpdateCardLocations);
        controller.CardHover.AddListener(HandleCardHover);
        controller.CardHeld.AddListener(HandleCardHeld);
        controller.CardDrop.AddListener(HandleCardDrop);
        controller.CardUnHover.AddListener(HandleCardUnHover);
        controller.Info = info;
        CardObjects.Insert(index, card);
        UpdateCardLocations();
    }

    public void DiscardAndDistroyCard(int index = 0)
    {
        Assert.IsTrue(index >= 0 && index <= CardObjects.Count);
        Destroy(CardObjects[index]);
        CardObjects.RemoveAt(index);
        UpdateCardLocations();
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
            if (i < viewSize)
            {
                card.SetActive(true);
                GetMotor(card).Move(CardPos[i]);
            }
            else
            {
                card.SetActive(false);
                card.transform.position = SpawnPoint.position;
            }
        }
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
}
