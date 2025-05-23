using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Deck", menuName = "Deck")]
public class DeckTemplate : ScriptableObject
{
    public List<CardTemplate> Cards;
}