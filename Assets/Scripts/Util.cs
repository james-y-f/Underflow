using System.Collections.Generic;
using System.Text;

public class Util
{
    public static string GeneratePropertyDesc(List<Property> properties)
    {
        StringBuilder descriptionBuilder = new StringBuilder();
        for (int i = 0; i < properties.Count; i++)
        {
            descriptionBuilder.Append(properties[i].ToString());
            if (i < properties.Count - 1)
            {
                descriptionBuilder.Append(", ");
            }
            else
            {
                descriptionBuilder.Append("\n");
            }
        }
        return descriptionBuilder.ToString();
    }

    public static string GenerateEffectDesc(List<CardEffect> effects, List<CardEffect> onDeleteEffects)
    {
        StringBuilder descriptionBuilder = new StringBuilder();
        for (int i = 0; i < effects.Count; i++)
        {
            descriptionBuilder.Append(effects[i]);
            if (i < effects.Count - 1)
            {
                descriptionBuilder.Append(";\n");
            }
        }
        if (onDeleteEffects.Count > 0)
        {
            descriptionBuilder.Append("\nOn Delete:\n");
            for (int i = 0; i < onDeleteEffects.Count; i++)
            {
                descriptionBuilder.Append(onDeleteEffects[i]);
                if (i < effects.Count - 1)
                {
                    descriptionBuilder.Append(";\n");
                }
            }
        }
        return descriptionBuilder.ToString();
    }

    public static string GetDisplayText(CardTemplate template)
    {
        return $"{template.Title} [{template.EnergyCost}]";
    }

    public static string GetDisplayText(Card card)
    {
        return $"{card.Title} [{card.EnergyCost}]";
    }

    public static string GetFullText(CardTemplate template)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append($"{GetDisplayText(template)}:\n");
        if (template.Properties != null && template.Properties.Count > 0)
        {
            builder.Append(GeneratePropertyDesc(template.Properties));
        }
        builder.Append(template.OverrideGeneratedDescription ?
                       template.OverrideEffectDescription : GenerateEffectDesc(template.Effects, template.OnDeleteEffects));
        return builder.ToString();
    }

    public static string GetFullText(Card card)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(GetDisplayText(card));
        builder.Append($":\n{card.Description}");
        string referencedCardText = GetReferenceCardsFullText(card.Effects);
        if (referencedCardText != string.Empty)
        {
            builder.Append($"\n\n{referencedCardText}");
        }
        return builder.ToString();
    }

    // TODO: prevent recursive references
    static string GetReferenceCardsFullText(List<CardEffect> effects)
    {
        List<CardTemplate> uniqueTemplates = new List<CardTemplate>();
        foreach (CardEffect effect in effects)
        {
            CardTemplate referencedTemplate = effect.ReferenceCardTemplate;
            if (referencedTemplate == null) continue;
            if (uniqueTemplates.Contains(referencedTemplate)) continue;
            uniqueTemplates.Add(referencedTemplate);
        }
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < uniqueTemplates.Count; i++)
        {
            builder.Append(GetFullText(uniqueTemplates[i]));
            if (i < uniqueTemplates.Count - 1) builder.Append("\n\n");
        }
        return builder.ToString();
    }
}