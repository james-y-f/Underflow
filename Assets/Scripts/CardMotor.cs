using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

// TODO: figure out hovering behavior bug

public class CardMotor : MonoBehaviour
{
    Rigidbody Rb;
    Coroutine ActiveCoroutine;
    const float PosTolerance = 0.05f;
    const float DefaultSpeedFactor = 5;
    public static float BaseHeight = 0.1f;
    static float HoverHeight = 0.2f;
    Vector3 CurrentTargetPosition;

    void Awake()
    {
        Rb = gameObject.GetComponent<Rigidbody>();
        ActiveCoroutine = null;
        CurrentTargetPosition = new Vector3(0, 0, 0);
    }

    public void Move(Vector3 targetPosition, float moveSpeedFactor = DefaultSpeedFactor)
    {
        if (CurrentTargetPosition == targetPosition) return;
        CurrentTargetPosition = targetPosition;
        if (ActiveCoroutine != null)
        {
            StopCoroutine(ActiveCoroutine);
        }
        ActiveCoroutine = StartCoroutine(MoveCoroutine(targetPosition, moveSpeedFactor));
    }
    public void SetHover(bool hover)
    {
        float newHeight = hover ? BaseHeight + HoverHeight : BaseHeight;
        Vector3 newPos = new Vector3(CurrentTargetPosition.x, newHeight, CurrentTargetPosition.z);
        Snap(newPos);
    }

    public void DragTo(float x, float y = float.MaxValue, float z = float.MaxValue)
    {
        // default to dragging only on the x axis
        if (y == float.MaxValue) y = BaseHeight + HoverHeight;
        if (z == float.MaxValue) z = transform.position.z;
        Snap(new Vector3(x, y, z));
    }

    IEnumerator MoveCoroutine(Vector3 targetPosition, float moveSpeedFactor)
    {
        while (Vector3.Distance(Rb.position, targetPosition) > PosTolerance)
        {
            Rb.MovePosition(Vector3.Lerp(Rb.position, targetPosition, moveSpeedFactor * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
    }

    void Snap(Vector3 targetPosition)
    {
        if (ActiveCoroutine != null)
        {
            StopCoroutine(ActiveCoroutine);
        }
        Rb.MovePosition(targetPosition);
    }
}
