using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Card", menuName = "Card")] // Makes it easy to create card assets in Unity Editor
public class CardTemplate : ScriptableObject
{
    public CardInfo Info;

    // --- Effect Information ---
    public List<CardEffect> Effects;
    public List<CardEffect> OnDeleteEffects;
}
