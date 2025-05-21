using System.Collections.Generic;

[System.Serializable]
// this is so that we can get a mutable card object
public class Card
{
    public static int UID_COUNTER { get; private set; }
    public CardTemplate Template { get; private set; }
    public CardInfo Info;
    public List<CardEffect> Effects;
    public List<CardEffect> OnDeleteEffects;

    public Card(CardTemplate template)
    {
        Info = new CardInfo();
        SetTemplate(template);
        Info.UID = GetNewUID();
    }

    public void SetTemplate(CardTemplate template)
    {
        Template = template;

        // prevent uid from being overwritten by template change
        int uid = Info.UID;
        Info = template.Info;
        Info.UID = uid;

        Effects = new List<CardEffect>();
        OnDeleteEffects = new List<CardEffect>();
        foreach (CardEffect effect in template.Effects)
        {
            Effects.Add(effect);
        }
        foreach (CardEffect effect in template.OnDeleteEffects)
        {
            OnDeleteEffects.Add(effect);
        }
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