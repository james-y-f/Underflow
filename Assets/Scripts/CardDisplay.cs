using UnityEngine;
using TMPro;

public class CardDisplay : ColorController
{
    TMP_Text Title;
    TMP_Text Energy;
    TMP_Text Description;
    protected override void Awake()
    {
        base.Awake();
        Transform tf = gameObject.transform;
        Title = tf.Find("Title").GetComponent<TMP_Text>();
        Energy = tf.Find("Energy").GetComponent<TMP_Text>();
        Description = tf.Find("Description").GetComponent<TMP_Text>();
    }

    public void UpdateDisplay(Card reference)
    {
        Title.text = reference.Title;
        Energy.text = reference.EnergyCost.ToString();
        Description.text = reference.Description;
        gameObject.name = $"{gameObject.tag} {reference.Title} {reference.UID}";
        if (!reference.Swappable)
        {
            DefaultColor = Constants.UnswappableColor;
            ResetColor();
        }
        else if (gameObject.tag == Constants.PlayerCardTag)
        {
            DefaultColor = Constants.PlayerCardColor;
        }
        else if (gameObject.tag == Constants.EnemyCardTag)
        {
            DefaultColor = Constants.EnemyCardColor;
        }
        else
        {
            Debug.LogWarning($"Undefined behavior in setting color with tag {gameObject.tag}");
            DefaultColor = Color.grey;
        }
        ResetColor();
    }
}
