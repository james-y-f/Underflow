using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class StackDisplay : MonoBehaviour
{
    public int StartingViewSize = 7;
    [SerializeField] GameObject CardPrefab;
    [SerializeField] bool IsPlayer;
    Transform leftPos;
    Transform rightPos;
    [SerializeField] List<GameObject> cards;
    List<Vector3> cardPos;
    int lastViewSize;

    // for testing
    [SerializeField] CardTemplate testTemplate;
    void Awake()
    {
        Assert.IsNotNull(CardPrefab);
        leftPos = transform.Find("LeftmostCardPos");
        rightPos = transform.Find("RightmostCardPos");
        cards = new List<GameObject>();
        // precalculate the position for the first {ViewSize} cards
        CalcCardPos(StartingViewSize);
        lastViewSize = StartingViewSize + 1; // calcCardPos trigger when first called
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void InsertCard(CardTemplate template, int index = -1)
    {
        if (index == -1)
        {
            index = cards.Count(); // default to inserting at the end
        }
        Assert.IsTrue(index >= 0 && index <= cards.Count);
        Assert.IsNotNull(template);
        GameObject newCardObject = Instantiate(CardPrefab, transform.position, CardPrefab.transform.rotation);
        newCardObject.tag = IsPlayer ? "PlayerCard" : "EnemyCard";
        newCardObject.layer = IsPlayer ? LayerMask.NameToLayer("PlayerCards") : LayerMask.NameToLayer("EnemyCards");
        Card newCard = newCardObject.GetComponent<Card>();
        newCard.Template = template;
        newCard.Index = index;
        newCard.CardDrop.AddListener(UpdateCardLocations);
        cards.Insert(index, newCardObject);
        UpdateCardLocations();
    }

    public void RemoveCard(int index = 0)
    {
        Assert.IsTrue(index >= 0 && index <= cards.Count);
        Destroy(cards[index]);
        cards.RemoveAt(index);
        UpdateCardLocations();
    }

    public void Swap(int a, int b)
    {
        Debug.Log($"Swapping {a} and {b}");
        Assert.IsTrue(a >= 0 && a <= cards.Count);
        Assert.IsTrue(b >= 0 && b <= cards.Count);
        cards[a].GetComponent<Card>().Index = b;
        cards[b].GetComponent<Card>().Index = a;
        GameObject temp = cards[a];
        cards[a] = cards[b];
        cards[b] = temp;
        UpdateCardLocations();
    }

    public void Clear()
    {
        foreach (GameObject card in cards)
        {
            Destroy(card);
        }
        cards = new List<GameObject>();
        CalcCardPos(StartingViewSize);
    }

    // helper functions
    void CalcCardPos(int cardCount)
    {
        if (lastViewSize <= StartingViewSize)
        {
            lastViewSize = cardCount;
            return;
        } // no need to recalc if we are already at 7 slots or less
        lastViewSize = cardCount;
        cardPos = new List<Vector3>();
        Vector3 left = leftPos.position;
        // only the x coordinate of rightPos really matters, since deck lays out horizontally
        float xRight = rightPos.position.x;
        cardPos.Add(left);
        float xInterval = (xRight - left.x) / (cardCount - 1);
        for (int i = 1; i < cardCount; i++)
        {
            cardPos.Add(new Vector3(left.x + xInterval * i, left.y, left.z));
        }
        Debug.Log("calculated card positions");
    }

    void UpdateCardLocations()
    {
        int newCount = cards.Count;
        CalcCardPos(newCount);
        for (int i = 0; i < newCount; i++)
        {
            cards[i].transform.position = cardPos[i];
        }
    }

    // for testing purposes
    [ContextMenu("Add Placeholder")]
    void AddPlaceholder()
    {
        InsertCard(testTemplate);
    }

    [ContextMenu("Remove First")]
    void RemoveFirst()
    {
        RemoveCard();
    }
}
