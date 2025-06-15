using UnityEngine;
using UnityEngine.Events;

public class CardController : MonoBehaviour
{
    public UnityEvent<CardController> CardHover;
    public UnityEvent<CardController> CardHeld;
    public UnityEvent CardDrop;
    public UnityEvent CardUnHover;
    public StackDisplay ParentStack;
    public Card CardReference;
    public bool InView = false;
    public bool Swappable
    {
        get { return ParentStack.DeckSwappable && CardReference.Swappable; }
        private set { }
    }
    Vector3 MouseOffset;
    CardDisplay Display;
    CardMotor Motor;

    bool Hovering;
    bool Held;

    void Awake()
    {
        Held = false;
        Hovering = false;
        Motor = GetComponent<CardMotor>();
        Display = GetComponent<CardDisplay>();
    }

    void Start()
    {
        Display.UpdateDisplay(CardReference);
    }

    private Vector3 CalcScreenPos()
    {
        return Camera.main.WorldToScreenPoint(transform.position);
    }

    void OnMouseOver()
    {
        if (!InView) return;
        TooltipManager.Instance.ShowTooltip(Util.GetFullText(CardReference));
        Display.ShowSelectionHighlight();
        if (!Swappable || Held || !ParentStack.CardHoverable()) return;
        // prevents hovering over another card while holding a card
        Hovering = true;
        Motor.SetHover(true);
        CardHover.Invoke(this);
    }

    void OnMouseDown()
    {
        if (!InView) return;
        if (!Swappable || !ParentStack.CardHoldable()) return;
        Held = true;
        MouseOffset = Input.mousePosition - CalcScreenPos();
        CardHeld.Invoke(this);
    }

    void OnMouseDrag()
    {
        if (!InView) return;
        if (!Swappable || !Held) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition - MouseOffset);
        Motor.DragTo(mousePos.x);
    }

    void OnMouseUp()
    {
        if (!InView) return;
        if (!Swappable || !Held) return;
        Held = false;
        Motor.SetHover(false);
        CardDrop.Invoke();
    }

    void OnMouseExit()
    {
        if (!InView) return;
        TooltipManager.Instance.HideTooltip();
        Display.ResetColor();
        if (!Swappable || Held || !Hovering) return;
        // prevents exiting the card while holding
        Hovering = false;
        CardUnHover.Invoke();
        Motor.SetHover(false);
    }
}
