using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Connection = Web.Connection;

public class WebRenderer : MonoBehaviour
{
    public GameObject webLinePrefab;

    private Web web;
    private Dictionary<Connection, LineRenderer> renderers;

    private List<LineRenderer> rendererPool;

    void Awake()
    {
        web = GetComponent<Web>();
        renderers = new Dictionary<Connection, LineRenderer>();
        rendererPool = new List<LineRenderer>();
    }

    void Update()
    {
        RemoveOldConnection();
        foreach (var connection in web.connections)
        {
            if (web.IsConnectionStatic(connection))
            {
                continue;
            }
            if (!renderers.ContainsKey(connection))
            {
                renderers.Add(connection, GetRenderer());
            }
            var renderer = renderers[connection];
            renderer.positionCount = 2;
            var start = web.joints[connection.first].position;
            var end = web.joints[connection.second].position;
            renderer.SetPositions(new Vector3[] { start, end });
        }
    }

    private LineRenderer GetRenderer()
    {
        if (rendererPool.Count > 0)
        {
            var renderer = rendererPool.First();
            rendererPool.Remove(renderer);
            renderer.gameObject.SetActive(true);
            return renderer;
        }

        var go = Instantiate(webLinePrefab);
        go.transform.parent = transform;
        go.transform.position = Vector3.zero;
        return go.GetComponent<LineRenderer>();
    }

    private void RemoveOldConnection()
    {
        var oldConnections = new List<Connection>();
        foreach (var renderer in renderers)
        {
            if (!web.IsConnectionExist(renderer.Key))
            {
                oldConnections.Add(renderer.Key);
            }
        }
        foreach (var connection in oldConnections)
        {
            var renderer = renderers[connection];
            rendererPool.Add(renderer);
            renderer.gameObject.SetActive(false);
            renderers.Remove(connection);
        }
    }
}
