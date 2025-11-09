using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    private Vector3 lookAtTargetPosition;

    private void Start()
    {
        lookAtTargetPosition = target.position;
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            var targetPosition = target.position + new Vector3(0, 1, -9);
            transform.position = Vector3.Slerp(transform.position, targetPosition, Time.fixedDeltaTime);
            lookAtTargetPosition = Vector3.Slerp(lookAtTargetPosition, target.position, Time.fixedDeltaTime * 3f);
            transform.LookAt(lookAtTargetPosition);
        }
    }
}
