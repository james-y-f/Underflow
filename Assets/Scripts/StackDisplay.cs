using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class StackDisplay : MonoBehaviour
{
    public UnityEvent<bool, int, int> SwapAttempt;
    public int BaseViewSize = 7;
    public bool DeckSwappable = true;
    [SerializeField] GameObject CardPrefab;
    [SerializeField] bool IsPlayer;
    Transform leftPos;
    Transform rightPos;
    [SerializeField] List<GameObject> CardObjects;
    List<Vector3> CardPos;
    int LastViewSize;
    int CurrentViewSize;


    GameObject HoveredCard;
    GameObject HeldCardObject;
    Coroutine HoldingCardCoroutine;


    // for testing
    [SerializeField] CardTemplate testTemplate;
    void Awake()
    {
        Assert.IsNotNull(CardPrefab);
        leftPos = transform.Find("LeftmostCardPos");
        rightPos = transform.Find("RightmostCardPos");
        CardObjects = new List<GameObject>();
        // precalculate the position for the first {ViewSize} cards
        CalcCardPos(BaseViewSize);
        CurrentViewSize = BaseViewSize;
        LastViewSize = BaseViewSize + 1; // calcCardPos trigger when first called
    }

    public void InsertCard(CardTemplate template, int index = -1)
    {
        if (index == -1)
        {
            index = CardObjects.Count(); // default to inserting at the end
        }
        Assert.IsTrue(index >= 0 && index <= CardObjects.Count);
        Assert.IsNotNull(template);
        GameObject card = Instantiate(CardPrefab, transform.position, CardPrefab.transform.rotation);
        card.tag = IsPlayer ? "PlayerCard" : "EnemyCard";
        card.layer = IsPlayer ? LayerMask.NameToLayer("PlayerCards") : LayerMask.NameToLayer("EnemyCards");
        card.name = template.title;
        CardController controller = card.GetComponent<CardController>();
        controller.ParentStack = this;
        controller.CardDrop.AddListener(UpdateCardLocations);
        controller.CardHover.AddListener(HandleCardHover);
        controller.CardHeld.AddListener(HandleCardHeld);
        controller.CardDrop.AddListener(HandleCardDrop);
        controller.CardUnHover.AddListener(HandleCardUnHover);
        CardDisplay display = card.GetComponent<CardDisplay>();
        display.UpdateDisplay(template);
        CardObjects.Insert(index, card);
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
        Assert.IsTrue(a >= 0 && a <= CardObjects.Count);
        Assert.IsTrue(b >= 0 && b <= CardObjects.Count);
        Debug.Log($"Display swapping {a} and {b}");
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
        CalcCardPos(BaseViewSize);
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
            for (int i = 1; i < CardPos.Count; i++)
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
                Debug.Log($"Invoking {heldIndex}, {closestCardPositionIndex}");
                SwapAttempt.Invoke(IsPlayer, heldIndex, closestCardPositionIndex);
                Debug.Log($"Finished Invoking {heldIndex}, {closestCardPositionIndex}");
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
        if (LastViewSize <= BaseViewSize)
        {
            LastViewSize = cardCount;
            return;
        } // no need to recalc if we are already at 7 slots or less
        LastViewSize = cardCount;
        CardPos = new List<Vector3>();
        Vector3 left = leftPos.position;
        // only the x coordinate of rightPos really matters, since deck lays out horizontally
        float xRight = rightPos.position.x;
        CardPos.Add(left);
        float xInterval = (xRight - left.x) / (cardCount - 1);
        for (int i = 1; i < cardCount; i++)
        {
            CardPos.Add(new Vector3(left.x + xInterval * i, left.y, left.z));
        }
    }

    void UpdateCardLocations()
    {
        int newCount = CardObjects.Count;
        CalcCardPos(newCount);
        for (int i = 0; i < newCount; i++)
        {
            CardObjects[i].GetComponent<CardMotor>().Move(CardPos[i]);
        }
    }
}
