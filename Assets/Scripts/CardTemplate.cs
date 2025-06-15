using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Card", menuName = "Card")] // Makes it easy to create card assets in Unity Editor
public class CardTemplate : ScriptableObject
{
    public string Title;
    public bool OverrideGeneratedDescription;
    [TextArea]
    public string OverrideEffectDescription;
    public int EnergyCost;
    public RarityLevel Rarity;
    public bool Swappable
    {
        get { return Properties == null || Properties.Count == 0 || Properties.Contains(Property.Unswappable); }
        private set { }
    }
    public List<Property> Properties;


    // --- Effect Information ---
    public List<CardEffect> Effects;
    public List<CardEffect> OnDeleteEffects;
}
