using System.Collections;
using UnityEngine;

public class CardMotor : MonoBehaviour
{
    Rigidbody Rb;
    Coroutine ActiveCoroutine;

    Vector3 CurrentTargetPosition;

    void Awake()
    {
        Rb = gameObject.GetComponent<Rigidbody>();
        ActiveCoroutine = null;
        CurrentTargetPosition = new Vector3(0, 0, 0);
    }

    public void Move(Vector3 targetPosition, float moveSpeedFactor = Constants.DefaultCardMoveSpeedFactor)
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
        float newHeight = hover ? Constants.BaseHeight + Constants.HoverHeight : Constants.BaseHeight;
        Vector3 newPos = new Vector3(CurrentTargetPosition.x, newHeight, CurrentTargetPosition.z);
        Snap(newPos);
    }

    public void DragTo(float x, float y = float.MaxValue, float z = float.MaxValue)
    {
        // default to dragging only on the x axis
        if (y == float.MaxValue) y = Constants.BaseHeight + Constants.HoverHeight;
        if (z == float.MaxValue) z = transform.position.z;
        Snap(new Vector3(x, y, z));
    }

    public IEnumerator MoveCoroutine(Vector3 targetPosition, float moveSpeedFactor = Constants.DefaultCardMoveSpeedFactor)
    {
        while (Vector3.Distance(Rb.position, targetPosition) > Constants.PosTolerance)
        {
            Rb.MovePosition(Vector3.Lerp(Rb.position, targetPosition, moveSpeedFactor * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
        Snap(targetPosition);
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
