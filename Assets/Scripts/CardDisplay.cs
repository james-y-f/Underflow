using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Burst.CompilerServices;

public class CardDisplay : MonoBehaviour
{
    TMP_Text Title;
    TMP_Text Energy;
    TMP_Text Description;
    public Renderer Render;
    Color DefaultColor;

    void Awake()
    {
        Transform tf = gameObject.transform;
        Title = tf.Find("Title").GetComponent<TMP_Text>();
        Energy = tf.Find("Energy").GetComponent<TMP_Text>();
        Description = tf.Find("Description").GetComponent<TMP_Text>();
        Render = GetComponent<Renderer>();
    }

    void Start()
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
        BlinkToColor(DefaultColor);
    }

    public void UpdateTextDisplay(CardInfo info)
    {
        Title.text = info.Title;
        Energy.text = info.EnergyCost.ToString();
        Description.text = info.Description;
    }

    public IEnumerator FlashColor(Color targetColor, float speed = Constants.DefaultColorFlashSpeed)
    {
        yield return StartCoroutine(FadeToColor(targetColor, speed));
        yield return StartCoroutine(FadeToColor(DefaultColor, speed));
    }

    public IEnumerator FadeToColor(Color targetColor, float speed = Constants.DefaultColorFadeSpeed)
    {
        while (Vector4.Distance(Render.material.color, targetColor) > Constants.ColorTolerance)
        {
            Render.material.color = Color.Lerp(Render.material.color, targetColor, speed);
            yield return null;
        }
        BlinkToColor(targetColor);
    }

    public void ShowSelectionHighlight()
    {
        BlinkToColor(DefaultColor + Constants.SelectionHighlight);
    }

    public void ResetColor()
    {
        BlinkToColor(DefaultColor);
    }

    public void BlinkToColor(Color color)
    {
        Render.material.color = color;
    }

    [ContextMenu("flash red")]
    void FlashRed()
    {
        StartCoroutine(FlashColor(Color.red));
    }
}
