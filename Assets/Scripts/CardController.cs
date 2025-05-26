using UnityEngine;
using UnityEngine.Events;

public class CardController : MonoBehaviour
{
    public UnityEvent<CardController> CardHover;
    public UnityEvent<CardController> CardHeld;
    public UnityEvent CardDrop;
    public UnityEvent CardUnHover;
    public StackDisplay ParentStack;
    public CardInfo Info;
    public bool CardSwappable = true;
    public bool Swappable
    {
        get { return ParentStack.DeckSwappable && CardSwappable; }
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
        Display.UpdateTextDisplay(Info);
    }

    private Vector3 CalcScreenPos()
    {
        return Camera.main.WorldToScreenPoint(transform.position);
    }

    void OnMouseEnter()
    {
        // TODO: display tooltip 
        Display.ShowSelectionHighlight();
        if (!Swappable || Held || !ParentStack.CardHoverable()) return;
        // prevents hovering over another card while holding a card
        Hovering = true;
        Motor.SetHover(true);
        CardHover.Invoke(this);
    }

    void OnMouseDown()
    {
        if (!Swappable || !ParentStack.CardHoldable()) return;
        Held = true;
        MouseOffset = Input.mousePosition - CalcScreenPos();
        CardHeld.Invoke(this);
    }

    void OnMouseDrag()
    {
        if (!Swappable || !Held) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition - MouseOffset);
        Motor.DragTo(mousePos.x);
    }

    void OnMouseUp()
    {
        if (!Swappable || !Held) return;
        Held = false;
        Motor.SetHover(false);
        CardDrop.Invoke();
    }

    void OnMouseExit()
    {
        // cancel tooltip 
        Display.ResetColor();
        if (!Swappable || Held || !Hovering) return;
        // prevents exiting the card while holding
        Hovering = false;
        CardUnHover.Invoke();
        Motor.SetHover(false);
    }

}
