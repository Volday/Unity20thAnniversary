using Unity.VisualScripting;
using UnityEngine;
using static Web;

public class SpiderMine : MonoBehaviour
{
    public Web web;

    public float distanceToHoldWeb = 1;

    private bool onWeb;
    private float velocity;
    private float maxVelosity = 10f;
    private float deathHeight = -20f;

    private Vector3 respawnPosition;

    void Start()
    {
        respawnPosition = transform.position;
    }

    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        onWeb = false;
        var connection = web.GetClosestConnection(transform.position, out Vector2 projection);
        if (connection != null) {
            if ((transform.position - (Vector3)projection).magnitude < distanceToHoldWeb) { 
                onWeb = true;
                velocity = 0;
            }
        }

        if (!onWeb) {
            var gravity = 9.81f;
            velocity += gravity * Time.fixedDeltaTime;
            velocity = Mathf.Clamp(velocity, 0f, maxVelosity);
            transform.position += Vector3.down * velocity * Time.fixedDeltaTime;
        }

        if (transform.position.y < deathHeight) {
            velocity = 0;
            transform.position = respawnPosition;
        }
    }
}
