using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Card : MonoBehaviour
{
    public UnityEvent CardDrop;
    public CardTemplate Template;
    public int Index = -1;
    Vector3 MouseOffset;
    Vector3 OriginalPosOnClick;
    Rigidbody RB;
    float BaseHeight;
    float HoverHeight = 0.2f;

    bool Hovering;
    bool Held;

    void Awake()
    {
        BaseHeight = transform.position.y;
        RB = gameObject.GetComponent<Rigidbody>();
        Held = false;
    }

    private Vector3 CalcScreenPos()
    {
        return Camera.main.WorldToScreenPoint(transform.position);
    }

    void OnMouseEnter()
    {
        // display tooltip 
        // change color
        //StartCoroutine(setHover(true));
    }

    void OnMouseDown()
    {
        MouseOffset = Input.mousePosition - CalcScreenPos();
        OriginalPosOnClick = transform.position;
        Held = true;
    }

    void OnMouseDrag()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition - MouseOffset);
        transform.position = new Vector3(mousePos.x, OriginalPosOnClick.y, OriginalPosOnClick.z); // cards can only be moved horizontally
    }

    void OnMouseUp()
    {
        Held = false;
        CardDrop.Invoke();
    }

    void OnMouseExit()
    {
        // cancel tooltip 
        // change color back
        // StartCoroutine(setHover(false));
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject otherObject = other.gameObject;
        Debug.Log($"trigger on {Index} with {other.gameObject.name}");
        // check if the other gameobject is also a card
        if (Held && otherObject.CompareTag(gameObject.tag))
        {
            BattleManager.instance.Swap(BattleManager.Entity.Player, Index, otherObject.GetComponent<Card>().Index);
        }
    }

    // helper functions
    IEnumerator setHover(bool hover)
    {
        float newHeight = hover ? BaseHeight + HoverHeight : BaseHeight;
        Vector3 newPos = new Vector3(transform.position.x, newHeight, transform.position.z);
        RB.MovePosition(newPos);
        yield return null;
    }
}
