using UnityEngine;

public class WebUtils
{
    public static Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P)
    {
        Vector2 AP = P - A;
        Vector2 AB = B - A;

        float magnitudeAB = AB.sqrMagnitude;
        if (magnitudeAB == 0)
        {
            return A;
        }
        float ABAPproduct = Vector2.Dot(AP, AB);
        float distance = ABAPproduct / magnitudeAB;

        if (distance < 0)
        {
            return A;
        }
        else if (distance > 1)
        {
            return B;
        }
        else
        {
            return A + AB * distance;
        }
    }

    public static bool SegmentRayIntersection(Vector2 A, Vector2 B, Vector2 R0, Vector2 Rd, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        Vector2 s = B - A;
        float det = Rd.x * (-s.y) - Rd.y * (-s.x);
        if (Mathf.Abs(det) < 1e-6f)
            return false;

        Vector2 diff = A - R0;

        float t = (Rd.x * diff.y - Rd.y * diff.x) / det;
        float u = (s.x * diff.y - s.y * diff.x) / det;

        if (t >= 0f && t <= 1f && u >= 0f)
        {
            intersection = A + s * t;
            return true;
        }

        return false;
    }
}
