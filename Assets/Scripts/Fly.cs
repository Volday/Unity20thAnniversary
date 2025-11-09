using UnityEngine;

public class Fly : MonoBehaviour
{
    public float flyTime = 5f;
    public float flyRadius = 30f;

    private Vector3 target;
    private float startAngle;
    private float endAngle;
    private int direction; 
    private float progress;
    private Vector3 lastPosition;

    private bool captured;
    [HideInInspector]
    public Web web;

    public void StartFlight(Vector3 target)
    {
        this.target = target;
        direction = (int)((Random.Range(0, 2) - 0.5f) * 2);
        startAngle = Random.Range(-200f, 20f);
        endAngle = Random.Range(-200f, 20f);
    }

    private void FixedUpdate()
    {
        if (captured)
        {
            UpdateLiberation();
        }
        else
        {
            UpdateProgress();
        }
    }

    private void UpdateLiberation()
    {

    }

    private void UpdateProgress()
    {
        progress += (1f / flyTime) * Time.fixedDeltaTime;
        if (progress >= 1)
        {
            Destroy(gameObject);
        }

        var currentAngle = Mathf.LerpAngle(startAngle, endAngle, progress) + 180f;
        var currentAngleRad = currentAngle * Mathf.Deg2Rad;
        var circleCenter = target + new Vector3(Mathf.Cos(currentAngleRad), Mathf.Sin(currentAngleRad), 0) * flyRadius;
        var circleNormal = new Vector3(Mathf.Cos(90 * direction + currentAngleRad), Mathf.Sin(90 * direction + currentAngleRad), 0).normalized;

        Vector3 xAxis = (target - circleCenter).normalized;
        Vector3 yAxis = Vector3.Cross(circleNormal, xAxis).normalized;

        float circleStartAngle = -90f * Mathf.Deg2Rad;
        float circleEndAngle = 90f * Mathf.Deg2Rad;
        float circleAngle = Mathf.Lerp(circleStartAngle, circleEndAngle, progress);

        Vector3 pos = circleCenter + (xAxis * Mathf.Cos(circleAngle) + yAxis * Mathf.Sin(circleAngle)) * flyRadius;
        lastPosition = transform.position;
        transform.position = pos;

        transform.LookAt((pos - lastPosition) + pos);
    }
}
