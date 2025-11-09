using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Web : MonoBehaviour
{
    [HideInInspector]
    public List<Joint> joints;
    [HideInInspector]
    public List<Connection> connections;

    public float stiffness = 100;
    public float damping = 2;
    public float airResistance = 0.5f;
    public float gravityMultiplier = 1;
    [Range(0, 1)]
    public float extensionResistance = 0.99f;

    public int iterationCount;

    public List<IWebWeightProvider> webWeightProviders;

    private void Awake()
    {
        webWeightProviders = new List<IWebWeightProvider>();
    }

    private void Start()
    {
        foreach (var con in connections)
        {
            var firstJoint = joints[con.first];
            var secondJoint = joints[con.second];
            var length = (firstJoint.position - secondJoint.position).magnitude;
            con.SetLength(length);
        }
    }

    private void FixedUpdate()
    {
        for (var i = 0; i < iterationCount; i++)
        {
            UpdateWeb(Time.fixedDeltaTime / iterationCount);
        }
    }

    private void UpdateWeb(float deltaTime)
    {
        var forces = new Vector2[joints.Count];
        var masses = Enumerable.Repeat(1f, joints.Count).ToArray();

        foreach (var con in connections)
        {
            var firstJoint = joints[con.first];
            var secondJoint = joints[con.second];
            var length = (secondJoint.position - firstJoint.position).magnitude;
            var direction = (secondJoint.position - firstJoint.position).normalized;

            if (length == 0f) continue;
            var extantion = length - con.length;
            float velAlongSpring = Vector2.Dot((secondJoint.velocity - firstJoint.velocity), direction);
            var force = stiffness * extantion * direction + damping * velAlongSpring * direction;
            if (extantion < 0)
            {
                force *= 1 - extensionResistance;
            }
            if (!firstJoint.isStatic)
            {
                forces[con.first] += force * (secondJoint.isStatic ? 2 : 1);
            }
            if (!secondJoint.isStatic)
            {
                forces[con.second] -= force * (firstJoint.isStatic ? 2 : 1);
            }

            foreach (var weightProvider in webWeightProviders)
            {
                if (weightProvider.TryGetPositionAndWightForConnection(con, out var position, out float weight))
                {
                    var coefficient = (firstJoint.position - position).magnitude / length;
                    masses[con.first] += weight * (1 - coefficient);
                    masses[con.second] += weight * coefficient;
                }
            }
        }

        for (int i = 0; i < joints.Count; i++)
        {
            var joint = joints[i];
            if (joint.isStatic) continue;
            var mass = masses[i];
            var gravity = new Vector2(0, -9.81f * gravityMultiplier);
            var gravitiForce = gravity * mass;
            float speed = joint.velocity.magnitude;
            if (speed > 0f)
            {
                Vector2 drag = -airResistance * speed * joint.velocity;
                gravitiForce += drag;
            }
            forces[i] += gravitiForce;

            var acceleration = forces[i] / mass;
            joint.velocity += acceleration * deltaTime;
            joint.position += joint.velocity * deltaTime;
            joint.velocity *= 0.99f;
        }
    }

    public Joint CreateJoint(Vector2 position, bool isStatic)
    {
        var joint = new Joint() { position = position, isStatic = isStatic };
        joints.Add(joint);
        return joint;
    }

    public void RemoveJoint(Joint joint)
    {
        var index = joints.IndexOf(joint);
        if (index == -1)
        {
            return;
        }
        joints.RemoveAt(index);
        for (int i = 0; i < connections.Count; i++)
        {
            var con = connections[i];
            if (con.first == index || con.second == index)
            {
                connections.RemoveAt(i);
                i--;
            }
            else
            {
                if (con.first > index)
                {
                    con.first--;
                }
                if (con.second > index)
                {
                    con.second--;
                }
            }
        }
    }

    public void InsertJoint(Connection connection, Joint joint)
    {
        var firstJoint = joints[connection.first];
        var secondJoint = joints[connection.second];
        RemoveConnection(connection);
        TryCrateConnction(firstJoint, joint, out var firstConnection);
        TryCrateConnction(secondJoint, joint, out var secondConnection);
    }

    public void RemoveConnection(Connection connection)
    {
        connections.Remove(connection);
    }

    public bool TryCrateConnction(Joint first, Joint second, out Connection connection)
    {
        connection = null;
        var firstIndex = joints.IndexOf(first);
        var secondIndex = joints.IndexOf(second);
        if (firstIndex == -1 || secondIndex == -1)
        {
            return false;
        }
        var newConnection = new Connection(firstIndex, secondIndex);
        if (connections.Any(c => c.Equals(newConnection)))
        {
            return false;
        }
        connection = newConnection;
        var length = (first.position - second.position).magnitude;
        connection.SetLength(length);
        connections.Add(connection);
        return true;
    }

    public void MergeJoints(Joint first, Joint second)
    {
        var firstIndex = joints.IndexOf(first);
        var secondIndex = joints.IndexOf(second);
        if (firstIndex == -1 || secondIndex == -1)
        {
            return;
        }
        for (int i = 0; i < connections.Count; i++)
        {
            var con = connections[i];
            if (con.first == secondIndex)
            {
                con.first = firstIndex;
            }
            if (con.second == secondIndex)
            {
                con.second = firstIndex;
            }
        }
        RemoveJoint(second);
        RemoveInvalideConnactions();
    }

    private void RemoveInvalideConnactions()
    {
        connections = connections
            .Where(c => c.first != c.second)
            .Where(c => c.first < joints.Count)
            .Where(c => c.second < joints.Count)
            .Distinct()
            .ToList();
    }

    public bool IsConnectionExist(Connection connection)
    {
        return connections.Contains(connection);
    }

    public bool IsConnectionStatic(Connection connection)
    {
        var firstJoint = joints[connection.first];
        var secondJoint = joints[connection.second];
        return firstJoint.isStatic && secondJoint.isStatic;
    }

    public Connection GetClosestConnection(Vector2 point, out Vector2 projection)
    {
        projection = Vector2.zero;
        Connection closest = null;
        foreach (var connection in connections)
        {
            var projectionOnLineSegment = GetClosestPointOnConnection(connection, point);
            if (closest == null ||
                (projectionOnLineSegment - point).sqrMagnitude < (projection - point).sqrMagnitude)
            {
                closest = connection;
                projection = projectionOnLineSegment;
            }
        }
        return closest;
    }

    public Vector2 GetClosestPointOnConnection(Connection connection, Vector2 point)
    {
        var firstJoint = joints[connection.first];
        var secondJoint = joints[connection.second];
        return WebUtils.GetClosestPointOnLineSegment(firstJoint.position, secondJoint.position, point);
    }

    public Vector2 PushFromStaticConnections(Vector2 point, float minDistance)
    {
        foreach (var connection in connections)
        {
            if (!IsConnectionStatic(connection))
            {
                continue;
            }
            var projection = GetClosestPointOnConnection(connection, point);
            var vector = point - projection;
            if (vector.magnitude < minDistance)
            {
                vector = vector.normalized * minDistance;
            }
            point = projection + vector;
        }
        return point;
    }

    public bool TryConnect(Connection startConnection, Vector2 startPosition, Vector2 direction, float minDistance)
    {
        if (IsConnectionStatic(startConnection))
        {
            startPosition = GetClosestPointOnConnection(startConnection, startPosition);
        }

        Connection closestConnection = null;
        var closestPosition = Vector2.zero;
        foreach (var connection in connections)
        {
            if (connection == startConnection)
            {
                continue;
            }
            var firstJoint = joints[connection.first];
            var secondJoint = joints[connection.second];
            if (WebUtils.SegmentRayIntersection(
                firstJoint.position,
                secondJoint.position,
                startPosition,
                direction,
                out var position))
            {
                var distance = (position - startPosition).magnitude;
                if (distance < minDistance)
                {
                    continue;
                }
                var bestDistance = (closestPosition - startPosition).magnitude;
                if (closestConnection == null ||
                    distance < bestDistance)
                {
                    closestConnection = connection;
                    closestPosition = position;
                }
            }
        }
        if (closestConnection == null)
        {
            return false;
        }
        var startJoint = CreateJoint(startPosition, IsConnectionStatic(startConnection));
        InsertJoint(startConnection, startJoint);
        var endJoint = CreateJoint(closestPosition, IsConnectionStatic(closestConnection));
        InsertJoint(closestConnection, endJoint);
        TryCrateConnction(startJoint, endJoint, out _);
        return true;
    }

    private void OnDrawGizmos()
    {
        if (connections != null)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                var connection = connections[i];
                if (IsConnectionStatic(connection))
                {
                    Gizmos.color = new Color(0.8f, 0.8f, 0.8f);
                }
                else
                {
                    Gizmos.color = Color.white;
                }
                Gizmos.DrawLine(joints[connection.first].position, joints[connection.second].position);
            }
        }
    }

    [Serializable]
    public class Joint
    {
        public Vector2 position;
        public Vector2 velocity;
        public bool isStatic;
    }

    [Serializable]
    public class Connection : IEquatable<Connection>
    {
        public int first;
        public int second;
        public float length { get; private set; }

        public Connection(int first, int second)
        {
            this.first = first;
            this.second = second;
        }

        public void SetLength(float length)
        {
            this.length = length;
        }

        public bool Equals(Connection other)
        {
            if (other.first == first &&
                other.second == second)
            {
                return true;
            }
            if (other.first == second &&
                other.second == first)
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int firstHash = first.GetHashCode();
            int secondHash = second.GetHashCode();

            return firstHash ^ secondHash;
        }
    }

    public interface IWebWeightProvider
    {
        public bool TryGetPositionAndWightForConnection(Connection connection, out Vector2 position, out float weight);
    }
}
