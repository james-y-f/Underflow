using UnityEngine;
using UnityEngine.InputSystem;

public class Draggable : MonoBehaviour
{
    Vector3 offset;

    private Vector3 CalcScreenPos()
    {
        return Camera.main.WorldToScreenPoint(transform.position);
    }

    void OnMouseDown()
    {
        offset = Input.mousePosition - CalcScreenPos();
    }

    void OnMouseDrag()
    {
        transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition - offset);
    }
}
