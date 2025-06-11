using System.Collections.Generic;

[System.Serializable]
public class Card
{
    public static int UID_COUNTER { get; private set; }
    public CardTemplate Template { get; private set; }
    public string Title;
    public string Description
    {
        get { return $"{(PropertyDescription == "" ? "" : $"{PropertyDescription}")}{EffectDescription}"; }
        private set { }
    }
    string PropertyDescription;
    string EffectDescription;
    public int EnergyCost;
    public RarityLevel Rarity;
    public int UID;
    public bool Swappable
    {
        get { return Properties == null || Properties.Count == 0 || !Properties.Contains(Property.Unswappable); }
        private set { }
    }
    public List<Property> Properties;
    public List<CardEffect> Effects;
    public List<CardEffect> OnDeleteEffects;

    public Card(CardTemplate template)
    {
        SetTemplate(template);
        UID = GetNewUID();
    }
    public void SetTemplate(CardTemplate template)
    {
        Template = template;
        Title = template.Title;
        EnergyCost = template.EnergyCost;
        Rarity = template.Rarity;

        Properties = new List<Property>();
        Effects = new List<CardEffect>();
        OnDeleteEffects = new List<CardEffect>();
        foreach (Property property in template.Properties)
        {
            Properties.Add(property);
        }
        foreach (CardEffect effect in template.Effects)
        {
            Effects.Add(effect);
        }
        foreach (CardEffect effect in template.OnDeleteEffects)
        {
            OnDeleteEffects.Add(effect);
        }
        PropertyDescription = Util.GeneratePropertyDesc(Properties);
        EffectDescription = template.OverrideGeneratedDescription ?
                            template.OverrideEffectDescription : Util.GenerateEffectDesc(Effects, OnDeleteEffects);
    }

    public void AddProperty(Property property)
    {
        if (Properties.Contains(property)) return;
        Properties.Add(property);
        PropertyDescription = Util.GeneratePropertyDesc(Properties);
    }

    public static void ResetUIDCounter()
    {
        UID_COUNTER = 0;
    }

    public int GetNewUID()
    {
        return UID_COUNTER++;
    }
}