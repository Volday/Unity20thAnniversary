using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Connection = Web.Connection;

public class SpiderMine : MonoBehaviour
{
    public Web web;

    public InputActionAsset inputActions;
    private InputAction moveAction;
    private InputAction lookAction;

    public List<Leg> legs;
    private Dictionary<Leg, Vector3> defaultLocalLegPosition;
    private float legLength;

    public float moveSpeed = 1;
    public float distanceToHoldWeb = 1;

    private bool onWeb;
    private Connection holdingConnection;
    private float connactionPosition;
    private Vector3 moveVector;

    private float velocity;
    private float maxVelosity = 5f;
    private float deathHeight = -20f;

    private Vector3 respawnPosition;

    void Start()
    {
        respawnPosition = transform.position;
        inputActions.FindActionMap("Player").Enable();
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");

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

        UpdateLegPosition();
    }

    private void UpdateLegPosition()
    {
        foreach (var leg in legs)
        {
            web.GetClosestConnection(leg.target.position, out var projection);
            if ((leg.root.position - (Vector3)projection).magnitude < legLength)
            {
                leg.target.position = projection;
            }
            else
            {
                leg.target.position = leg.root.position + defaultLocalLegPosition[leg];
            }
        }
    }

    [Serializable]
    public class Leg
    {
        public Transform target;
        public Transform tip;
        public Transform root;
    }
}
