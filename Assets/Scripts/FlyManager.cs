using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyManager : MonoBehaviour
{
    public GameObject flyPrefab;
    public Web web;

    public float flghtsPerMinute = 20f;
    private float initialFlghtsPerMinute;

    [SerializeField]
    private List<Vector3> points;

    private void Start()
    {
        initialFlghtsPerMinute = flghtsPerMinute;
        flghtsPerMinute *= 3f;
        LaunchFly(new Vector3(-6.84f, -1.745f, 0f));
        StartCoroutine(FlyLouncherCoroutine());
    }

    private void Update()
    {
        flghtsPerMinute -= (initialFlghtsPerMinute / 90f) * Time.deltaTime;
        flghtsPerMinute = Mathf.Max(flghtsPerMinute, initialFlghtsPerMinute);
    }

    private void LaunchFly(Vector3 target)
    {
        var go = Instantiate(flyPrefab);
        var fly = go.GetComponent<Fly>();
        fly.web = web;
        fly.StartFlight(target);
    }

    private IEnumerator FlyLouncherCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f / flghtsPerMinute);
            LaunchFly(GetRandomTraget());
        }
    }

    private Vector3 GetRandomTraget()
    {
        var target = points[Random.Range(0, points.Count)];
        target += new Vector3(Random.Range(1f, 1f), Random.Range(1f, 1f), 0).normalized;
        return target;
    }

    public void CapturePoints()
    {
        points = new List<Vector3>();
        var fields = transform.GetChild(0);
        for (int i = 0; i < fields.childCount; i++)
        {
            var row = fields.GetChild(i);
            for (int j = 0; j < row.childCount; j++)
            {
                points.Add(row.GetChild(j).transform.position);
            }
        }
    }
}
