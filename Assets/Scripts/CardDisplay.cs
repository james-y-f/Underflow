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

    protected override void Start()
    {
        if (gameObject.tag == Constants.PlayerCardTag)
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

    public void UpdateTextDisplay(CardInfo info)
    {
        Title.text = info.Title;
        Energy.text = info.EnergyCost.ToString();
        Description.text = info.Description;
    }
}
