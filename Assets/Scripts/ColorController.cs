using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public class ColorController : MonoBehaviour
{
    Renderer Render;
    protected Color DefaultColor
    {
        get { return Dimmed ? dimmedColor : defaultColor; }
        set
        {
            defaultColorSet = true;
            defaultColor = value;
            dimmedColor = Color.gray; // FIXME:
        }
    }
    private bool Dimmed = false;
    private Color defaultColor;
    private Color dimmedColor;
    private bool defaultColorSet = false;

    protected virtual void Awake()
    {
        Render = GetComponent<Renderer>();
        DefaultColor = Render.material.color;
    }

    protected virtual void Start()
    {
        ResetColor();
    }

    public IEnumerator FlashColor(Color targetColor, float speed = Constants.DefaultColorFlashSpeed)
    {
        Assert.IsTrue(defaultColorSet);
        yield return StartCoroutine(FadeToColor(targetColor, speed));
        yield return StartCoroutine(FadeToColor(DefaultColor, speed));
    }

    public IEnumerator FadeToColor(Color targetColor, float speed = Constants.DefaultColorFadeSpeed)
    {
        Assert.IsNotNull(Render);
        while (Vector4.Distance(Render.material.color, targetColor) > Constants.ColorTolerance)
        {
            Render.material.color = Color.Lerp(Render.material.color, targetColor, speed);
            yield return null;
        }
        BlinkToColor(targetColor);
    }

    public void ShowSelectionHighlight()
    {
        Assert.IsTrue(defaultColorSet);
        BlinkToColor(DefaultColor + Constants.SelectionHighlight);
    }

    public void ResetColor()
    {
        Assert.IsTrue(defaultColorSet);
        BlinkToColor(DefaultColor);
    }

    public void BlinkToColor(Color color)
    {
        Assert.IsNotNull(Render);
        Render.material.color = color;
    }

    public void SetDimmed(bool dimmed)
    {
        Dimmed = dimmed;
        ResetColor();
    }

    public Color GetColor()
    {
        Assert.IsNotNull(Render);
        return Render.material.color;
    }

    // for testing
    [ContextMenu("flash red")]
    void FlashRed()
    {
        StartCoroutine(FlashColor(Color.red));
    }
}