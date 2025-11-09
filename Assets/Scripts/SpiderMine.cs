using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Web;
using Connection = Web.Connection;

public class SpiderMine : MonoBehaviour, IWebWeightProvider
{
    public Web web;

    public InputActionAsset inputActions;
    private InputAction moveAction;
    private InputAction attackAction;

    public Transform directionMarker;
    public Transform spiderBody;
    public List<Leg> legs;
    private Dictionary<Leg, Vector3> defaultLocalLegPosition;
    private float legLength;

    public float moveSpeed = 1;
    public float distanceToHoldWeb = 1;

    private bool onWeb;
    private Connection holdingConnection;
    private Vector3 moveVector;

    private float velocity;
    private float maxVelosity = 5f;
    private float deathHeight = -20f;

    private Vector3 respawnPosition;

    void Start()
    {
        web.webWeightProviders.Add(this);

        respawnPosition = transform.position;
        inputActions.FindActionMap("Player").Enable();
        moveAction = InputSystem.actions.FindAction("Move");
        attackAction = InputSystem.actions.FindAction("Attack");

        defaultLocalLegPosition = new();
        foreach (var leg in legs)
        {
            defaultLocalLegPosition.Add(leg, leg.tip.position - leg.root.position);
        }
        var leg1 = legs[0].tip;
        var leg1Parent = leg1.parent;
        legLength = (leg1.position - leg1Parent.position).magnitude + (leg1Parent.position - leg1Parent.parent.position).magnitude;
    }

    void Update()
    {
        moveVector = moveAction.ReadValue<Vector2>();

        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        var plane = new Plane(Vector3.back, Vector3.zero);
        plane.Raycast(ray, out var dist);
        var aimPosition = ray.origin + ray.direction * dist;
        directionMarker.LookAt(aimPosition);
        if (attackAction.WasPressedThisFrame() &&
            web.IsConnectionExist(holdingConnection))
        {
            var aimVector = (aimPosition - transform.position).normalized;
            web.TryConnect(holdingConnection, transform.position, aimVector, 0.3f);
        }
    }

    private void FixedUpdate()
    {
        var connection = web.GetClosestConnection(transform.position, out Vector2 projection);
        if (connection != null)
        {
            if ((transform.position - (Vector3)projection).magnitude < distanceToHoldWeb)
            {
                onWeb = true;
                velocity = 0;
            }
            else
            {
                onWeb = false;
            }
        }

        if (!onWeb)
        {
            if (web.IsConnectionExist(holdingConnection))
            {
                connection = holdingConnection;
                onWeb = true;
            }
            else
            {
                holdingConnection = null;
            }
        }

        if (onWeb)
        {
            var newPosition = transform.position + moveVector * moveSpeed * Time.fixedDeltaTime;
            var positionOnWeb = (Vector3)web.GetClosestPointOnConnection(connection, newPosition);
            var shiftFromWeb = newPosition - positionOnWeb;
            newPosition = positionOnWeb + Vector3.ClampMagnitude(shiftFromWeb, distanceToHoldWeb * 0.9f);
            newPosition = web.PushFromStaticConnections(newPosition, distanceToHoldWeb * 0.7f);
            transform.position = newPosition;
            holdingConnection = connection;
        }
        else
        {
            var gravity = 9.81f;
            velocity += gravity * Time.fixedDeltaTime;
            velocity = Mathf.Clamp(velocity, 0f, maxVelosity);
            transform.position += Vector3.down * velocity * Time.fixedDeltaTime;
        }

        if (transform.position.y < deathHeight)
        {
            velocity = 0;
            transform.position = respawnPosition;
        }

        UpdateRig();
    }

    private void UpdateRig()
    {
        UpdateLegPosition();

        var connection = web.GetClosestConnection(transform.position, out Vector2 projection);
        Vector3 center = projection;
        center.z = 0f;
        var radius = distanceToHoldWeb * 0.9f;
        var delta = transform.position - center;
        var underRoot = radius * radius - delta.sqrMagnitude;
        if (underRoot < 0)
        {
            underRoot = 0;
        }
        var z = -Mathf.Sqrt(underRoot);
        var posOnSphere = new Vector3(transform.position.x, transform.position.y, z);
        Vector3 fromCenter = (posOnSphere - center).normalized;
        Vector3 right = Vector3.Cross(Vector3.forward, fromCenter);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(Vector3.right, fromCenter);

        Vector3 forward = Vector3.Cross(fromCenter, right);
        Quaternion targetRotation = Quaternion.LookRotation(forward, fromCenter);
        spiderBody.rotation = Quaternion.Slerp(
            spiderBody.rotation,
            targetRotation,
            5 * Time.fixedDeltaTime
        );
    }

    private void UpdateLegPosition()
    {
        foreach (var leg in legs)
        {
            var hint = leg.target.parent.GetChild(1);
            var hintPosition = hint.position;
            var hintLocalUp = spiderBody.rotation * (new Vector3(0, hint.localPosition.y, 0) * spiderBody.lossyScale.y);
            hintPosition -= hintLocalUp;
            hintPosition.z = 0;
            hintPosition += moveVector * (legLength / 4f);
            var connection = web.GetClosestConnection(hintPosition, out var projection);
            if ((leg.root.position - (Vector3)projection).magnitude < legLength)
            {
                if ((leg.target.position - (Vector3)projection).magnitude > legLength)
                {
                    leg.target.position = projection;
                    leg.currentPosition = projection;
                }
                else
                {
                    leg.currentPosition = web.GetClosestPointOnConnection(connection, leg.currentPosition);
                    leg.target.position = leg.currentPosition;
                }
            }
            else
            {
                leg.target.position = leg.root.position + defaultLocalLegPosition[leg];
            }
        }
    }

    public bool TryGetPositionAndWightForConnection(Connection connection, out Vector2 position, out float weight)
    {
        position = Vector2.zero;
        weight = 10;
        if (connection != holdingConnection)
        {
            return false;
        }
        position = web.GetClosestPointOnConnection(connection, transform.position);
        return true;
    }

    [Serializable]
    public class Leg
    {
        public Transform target;
        public Transform tip;
        public Transform root;
        [HideInInspector]
        public Vector3 currentPosition;
    }
}
