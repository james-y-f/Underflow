using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.ShaderGraph.Internal;
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

    public List<int> Swap(int viewSize, int currentIndex, int targetIndex, bool hard = false, bool bypassSwappability = false)
    {
        if (!Swappable && !bypassSwappability)
        {
            Debug.LogWarning("Deck not swappable");
            return null;
        }

        if (currentIndex >= viewSize || targetIndex >= viewSize)
        {
            Debug.LogWarning("Cannot swap cards outside of view size");
            return null;
        }

        if (currentIndex == targetIndex)
        {
            Debug.LogWarning("Cannot swap card with itself");
            return null;
        }

        List<int> result = new List<int>();
        for (int i = 0; i < viewSize; i++)
        {
            result.Add(i);
        }
        CheckInRange(currentIndex);
        CheckInRange(targetIndex);
        Card currentCard = this[currentIndex];

        // check if the operation is possible
        if (!currentCard.Info.Swappable && !bypassSwappability)
        {
            Debug.LogError("current card is not swappable");
            return null;
        }

        // hard swap means only swapping the cards on the two specified indices
        if (hard)
        {
            Card targetCard = this[targetIndex];
            if (!targetCard.Info.Swappable && !bypassSwappability)
            {
                Debug.LogError("target card is not swappable");
                return null;
            }
            this[currentIndex] = targetCard;
            this[targetIndex] = currentCard;

            // swap these two things in the result vector too
            result[currentIndex] = targetIndex;
            result[targetIndex] = currentIndex;
            Debug.Log($"hard swapped {currentIndex}: {currentCard.Info.Title} and {targetIndex}: {targetCard.Info.Title}");
            return result;
        }

        // otherwise, we are doing an insertion-style swap
        bool forwardSwap = currentIndex > targetIndex; // forward meaning towards front of deck
        int minIndex = forwardSwap ? targetIndex : currentIndex;
        int maxIndex = forwardSwap ? currentIndex : targetIndex;

        // record all the swappable cards and their indicies
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

        // record the sorted order of cards
        int removalIndex = forwardSwap ? affectedIndices.Count - 1 : 0;
        int insertionIndex = forwardSwap ? 0 : affectedIndices.Count - 1;
        Card temp = affectedCards[removalIndex];
        affectedCards.RemoveAt(removalIndex);
        affectedCards.Insert(insertionIndex, temp);

        // update the deck according to the sorted order
        Assert.AreEqual(affectedIndices.Count, affectedCards.Count);
        for (int i = 0; i < affectedIndices.Count; i++)
        {
            this[affectedIndices[i]] = affectedCards[i];
        }

        // output the swapped order for display
        List<int> affectedIndicesSwapped = new List<int>();
        foreach (int i in affectedIndices) // start by making a copy of the swapped indices
        {
            affectedIndicesSwapped.Add(i);
        }
        affectedIndicesSwapped.RemoveAt(removalIndex);
        affectedIndicesSwapped.Insert(insertionIndex, currentIndex);
        Assert.AreEqual(affectedIndices.Count, affectedIndicesSwapped.Count);
        for (int i = 0; i < affectedIndices.Count; i++)
        {
            result[affectedIndices[i]] = affectedIndicesSwapped[i];
        }
        Debug.Log($"swapped {currentIndex}: {temp.Info.GetDisplayText()} to {targetIndex}");
        Debug.Log($"resulting order: {result}");
        return result;
    }

    void CheckInRange(int index)
    {
        Assert.GreaterOrEqual(index, 0);
        Assert.LessOrEqual(index, Count);
    }
}