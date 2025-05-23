using System.Collections;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class CardMotor : MonoBehaviour
{
    public float MoveSpeedFactor = 5;
    Rigidbody Rb;
    Coroutine ActiveCoroutine;
    float PosTolerance = 0.01f;
    Vector3 TargetPosition;
    float BaseHeight = 0.1f;
    float HoverHeight = 0.2f;

    void Awake()
    {
        Rb = gameObject.GetComponent<Rigidbody>();
        ActiveCoroutine = null;
    }

    public void Move(Vector3 targetPosition)
    {
        TargetPosition = targetPosition;
        if (ActiveCoroutine == null)
        {
            ActiveCoroutine = StartCoroutine(MoveCoroutine());
        }
    }
    public void SetHover(bool hover)
    {
        float newHeight = hover ? BaseHeight + HoverHeight : BaseHeight;
        Vector3 newPos = new Vector3(TargetPosition.x, newHeight, TargetPosition.z);
        Snap(newPos);
    }

    public void DragTo(float x, float y = float.MaxValue, float z = float.MaxValue)
    {
        // default to dragging only on the x axis
        if (y == float.MaxValue) y = BaseHeight + HoverHeight;
        if (z == float.MaxValue) z = transform.position.z;
        Snap(new Vector3(x, y, z));
    }

    IEnumerator MoveCoroutine()
    {
        while (Vector3.Distance(Rb.position, TargetPosition) > PosTolerance)
        {
            Rb.MovePosition(Vector3.Lerp(Rb.position, TargetPosition, MoveSpeedFactor * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
    }

    public void Snap(Vector3 targetPosition)
    {
        TargetPosition = targetPosition;
        Rb.MovePosition(targetPosition);
    }
}
