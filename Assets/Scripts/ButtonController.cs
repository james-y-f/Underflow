using UnityEngine;
using UnityEngine.Events;

public class ButtonController : ColorController
{
    public UnityEvent OnClick;
    [SerializeField] bool Interactible = true;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        SetInteractible(Interactible);
    }

    void OnMouseEnter()
    {
        if (!Interactible) return;
        ShowSelectionHighlight();
    }

    void OnMouseDown()
    {
        if (!Interactible) return;
        BlinkToColor(Constants.FlashHighlight);
        OnClick.Invoke();
        Debug.Log("Clicked Button");
    }

    void OnMouseUp()
    {
        if (!Interactible) return;
        ResetColor();
    }

    void OnMouseExit()
    {
        if (!Interactible) return;
        ResetColor();
    }

    public void SetInteractible(bool interactible)
    {
        Interactible = interactible;
        SetDimmed(!interactible);
    }
}
