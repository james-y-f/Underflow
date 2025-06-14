using System.Collections;
using UnityEngine;

public class CameraMotor : MonoBehaviour
{
    Coroutine ActiveCoroutine = null;
    public void Move(Vector3 targetPosition)
    {
        if (ActiveCoroutine != null)
        {
            StopCoroutine(ActiveCoroutine);
        }
        ActiveCoroutine = StartCoroutine(MoveCoroutine(targetPosition));
    }
    IEnumerator MoveCoroutine(Vector3 targetPosition, float moveSpeedFactor = Constants.DefaultCameraMoveSpeedFactor)
    {
        while (Vector3.Distance(transform.position, targetPosition) > Constants.PosTolerance)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeedFactor * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        transform.position = targetPosition;
    }
}