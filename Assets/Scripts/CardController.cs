using UnityEngine;
using UnityEngine.Events;

public class CardController : MonoBehaviour
{
    public UnityEvent<int> CardHover;
    public UnityEvent<int> CardHeld;
    public UnityEvent CardDrop;
    public UnityEvent CardUnHover;
    public int Index = -1;
    public bool Swappable;
    public StackDisplay ParentStack;
    Vector3 MouseOffset;
    CardMotor Motor;



    public bool Hovering { get; private set; }
    public bool Held { get; private set; }

    void Awake()
    {
        Held = false;
        Motor = GetComponent<CardMotor>();
    }

    private Vector3 CalcScreenPos()
    {
        return Camera.main.WorldToScreenPoint(transform.position);
    }

    void OnMouseEnter()
    {
        // display tooltip 
        // change color
        if (!Swappable || Held || Hovering || !ParentStack.CardHoverable()) return;
        // prevents hovering over another card while holding a card
        Hovering = true;
        CardHover.Invoke(Index);
    }

    void OnMouseDown()
    {
        if (!Swappable || !ParentStack.CardHoldable()) return;
        Held = true;
        MouseOffset = Input.mousePosition - CalcScreenPos();
        CardHeld.Invoke(Index);
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
        CardDrop.Invoke();
    }

    void OnMouseExit()
    {
        // cancel tooltip 
        // change color back
        if (!Swappable || Held || !Hovering) return;
        // prevents exiting the card while holding
        Hovering = false;
        CardUnHover.Invoke();
        Motor.SetHover(false);
    }

    // helper functions

}
