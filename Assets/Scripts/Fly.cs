using UnityEngine;
using UnityEngine.UI;
using static Web;
using Connection = Web.Connection;

public class Fly : MonoBehaviour, IWebWeightProvider
{
    public float flyTime = 5f;
    public float flyRadius = 30f;
    public float cuptureRadius = 0.2f;
    public float weight;
    public float escapeTime = 10f;
    private float initEscapeTime;

    public Slider liberationTimerUI;

    private Vector3 target;
    private float startAngle;
    private float endAngle;
    private int direction;
    private float progress;
    private Vector3 lastPosition;

    private Connection connection;
    private bool captureChecked;
    private bool captured;
    [HideInInspector]
    public Web web;

    private void Start()
    {
        initEscapeTime = escapeTime;
        liberationTimerUI.value = 1f;
        var canvas = liberationTimerUI.transform.parent.GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
    }

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
        connection = EnsureConnection();
        transform.position = web.GetClosestPointOnConnection(connection, transform.position);
        transform.LookAt(transform.position + Vector3.up);
        liberationTimerUI.value = escapeTime / initEscapeTime;
        escapeTime -= Time.deltaTime;
        if (escapeTime <= 0)
        {
            captured = false;
            liberationTimerUI.gameObject.SetActive(false);
            web.RemoveConnection(connection);
        }
    }

    private void UpdateProgress()
    {
        progress += (1f / flyTime) * Time.fixedDeltaTime;
        if (progress >= 1)
        {
            Die();
        }

        if (!captureChecked && progress >= 0.5f)
        {
            captureChecked = true;
            var positionOnWeb = transform.position;
            positionOnWeb.z = 0f;
            connection = web.GetClosestNonStaticConnection(positionOnWeb, out var projection);
            if (connection != null && ((Vector3)projection - positionOnWeb).magnitude <= cuptureRadius)
            {
                transform.position = projection;
                captured = true;
                liberationTimerUI.gameObject.SetActive(true);
                web.webWeightProviders.Add(this);
                return;
            }
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

    public void Die()
    {
        if (web.webWeightProviders.Contains(this))
        {
            web.webWeightProviders.Remove(this);
        }
        Destroy(gameObject);
    }

    public bool TryGetPositionAndWightForConnection(Connection connection, out Vector2 position, out float weight)
    {
        weight = this.weight;
        position = Vector2.zero;
        if (connection != this.connection)
        {
            return false;
        }
        position = web.GetClosestPointOnConnection(connection, transform.position);
        return true;
    }

    private Connection EnsureConnection()
    {
        if (connection == null ||
            !web.IsConnectionExist(connection))
        {
            connection = web.GetClosestNonStaticConnection(transform.position, out var projection);
        }
        return connection;
    }
}
