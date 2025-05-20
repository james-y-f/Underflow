using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[System.Serializable]
public class Deck : List<Card>
{
    public bool Swappable = true;
    public Deck() { }

    public Deck(DeckTemplate template)
    {
        foreach (CardTemplate card in template.Cards)
        {
            Debug.Log($"adding {card.Info.Title}");
            Add(new Card(card));
        }
    }

    public void Shuffle()
    {
        int n = Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n);
            Card temp = this[k];
            this[k] = this[n];
            this[n] = temp;
        }
        // TODO: log this
    }

    public void Swap(int currentIndex, int targetIndex, bool hard = false, bool bypassSwappability = false)
    {
        if (!Swappable && !bypassSwappability)
        {
            Debug.LogError("Deck not swappable");
            return;
        }
        CheckInRange(currentIndex);
        CheckInRange(targetIndex);
        if (hard)
        {
            Card currentCard = this[currentIndex];
            Card targetCard = this[targetIndex];
            if (!bypassSwappability && (!currentCard.Info.Swappable || !targetCard.Info.Swappable))
            {
                Debug.LogError("at least one card is not swappable during hard swap");
            }
            this[currentIndex] = targetCard;
            this[targetIndex] = currentCard;
            Debug.Log($"hard swapped {currentIndex}: {currentCard.Info.Title} and {targetIndex}: {targetCard.Info.Title}");
            return;
        }
        // otherwise, we are doing an insertion-style swap
        bool forwardSwap = currentIndex > targetIndex; // forward meaning towards front of deck
        int minIndex = forwardSwap ? targetIndex : currentIndex;
        int maxIndex = forwardSwap ? currentIndex : targetIndex;
        List<int> affectedIndices = new List<int>();
        List<Card> affectedCards = new List<Card>();
        for (int i = minIndex; i <= maxIndex; i++)
        {
            if (this[i].Info.Swappable || bypassSwappability)
            {
                affectedIndices.Add(i);
                affectedCards.Add(this[i]);
            }
        }
        int removalIndex = forwardSwap ? affectedIndices.Count - 1 : 0;
        int insertionIndex = forwardSwap ? 0 : affectedIndices.Count - 1;
        Card temp = affectedCards[removalIndex];
        affectedCards.RemoveAt(removalIndex);
        affectedCards.Insert(insertionIndex, temp);
        foreach (int i in affectedIndices)
        {
            this[i] = affectedCards[0];
            affectedCards.RemoveAt(0);
        }
        Debug.Log($"swapped {currentIndex}: {temp.Info.GetDisplayText()} to {targetIndex}");
    }

    void CheckInRange(int index)
    {
        Assert.GreaterOrEqual(index, 0);
        Assert.LessOrEqual(index, Count);
    }
}