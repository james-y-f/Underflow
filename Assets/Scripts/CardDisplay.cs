using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

public class CardDisplay : ColorController
{
    TMP_Text Title;
    TMP_Text Energy;
    TMP_Text Description;
    Renderer CardBackRenderer;
    protected override void Awake()
    {
        base.Awake();
        Transform tf = gameObject.transform;
        Title = tf.Find("Title").GetComponent<TMP_Text>();
        Energy = tf.Find("Energy").GetComponent<TMP_Text>();
        Description = tf.Find("Description").GetComponent<TMP_Text>();
        CardBackRenderer = tf.Find("Back").GetComponent<Renderer>();
        Assert.IsNotNull(Title);
        Assert.IsNotNull(Energy);
        Assert.IsNotNull(Description);
        Assert.IsNotNull(CardBackRenderer);
    }

    public void UpdateDisplay(Card reference)
    {
        Title.text = reference.Title;
        Energy.text = reference.EnergyCost.ToString();
        Description.text = reference.Description;
        gameObject.name = $"{gameObject.tag} {reference.Title} {reference.UID}";
        if (gameObject.tag == Constants.PlayerCardTag)
        {
            DefaultColor = Constants.PlayerCardColor;
            CardBackRenderer.material = AssetLoader.Instance.PlayerCardBack;
        }
        else if (gameObject.tag == Constants.EnemyCardTag)
        {
            DefaultColor = Constants.EnemyCardColor;
            CardBackRenderer.material = AssetLoader.Instance.EnemyCardBack;
        }
        else
        {
            Debug.LogWarning($"Undefined behavior in setting color with tag {gameObject.tag}");
            DefaultColor = Color.grey;
        }

        SetDimmed(!reference.Swappable);
        ResetColor();
    }
}
