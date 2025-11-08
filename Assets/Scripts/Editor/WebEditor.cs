using UnityEditor;
using UnityEngine;
using Joint = Web.Joint;

[CustomEditor(typeof(Web)), CanEditMultipleObjects]
public class WebEditor : Editor
{
    public bool editMode;
    public Joint dragingJoint;
    public Vector3 dragShift;

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button($"Editor {(editMode ? "On" : "Off")}"))
        {
            editMode = !editMode;
        }
        base.OnInspectorGUI();
    }

    protected virtual void OnSceneGUI()
    {
        Web web = (Web)target;

        Event e = Event.current;

        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        var plane = new Plane(Vector3.back, Vector3.zero);
        plane.Raycast(ray, out var dist);
        var mousePos = ray.GetPoint(dist);
        for (int i = 0; i < web.joints.Count; i++)
        {
            var joint = web.joints[i];
            var mouseOn = (mousePos - (Vector3)joint.position).magnitude < 0.1f;

            if (joint.isStatic)
            {
                Handles.color = new Color(0.2f, 0.2f, 0.2f);
            }
            else
            {
                Handles.color = new Color(0.8f, 0.8f, 0.8f);
            }
            if (mouseOn)
            {
                Handles.color += Color.green * 0.2f;
            }
            Handles.DrawSolidDisc(joint.position, Vector3.back, 0.1f);
        }
        if (!editMode)
        {
            return;
        }

        if (!e.control)
        {
            for (int i = 0; i < web.joints.Count; i++)
            {
                var joint = web.joints[i];
                var mouseOn = (mousePos - (Vector3)joint.position).magnitude < 0.1f;
                if (!mouseOn)
                {
                    continue;
                }
                if (e.keyCode == KeyCode.Alpha1 &&
                    e.type == EventType.KeyDown)
                {
                    joint.isStatic = !joint.isStatic;
                    EditorUtility.SetDirty(web);
                }

                if (e.type == EventType.MouseDown)
                {
                    if (e.button == 0)
                    {
                        SetDragJoin(joint, mousePos);
                    }
                    else if (e.button == 1)
                    {
                        var newJoint = web.CreateJoint(joint.position, joint.isStatic);
                        dragingJoint = newJoint;
                        SetDragJoin(newJoint, mousePos);
                        web.TryCrateConnction(joint, newJoint, out _);
                        EditorUtility.SetDirty(web);
                        break;
                    }
                }

                if (e.keyCode == KeyCode.Escape &&
                    e.type == EventType.KeyDown)
                {
                    web.RemoveJoint(joint);
                    EditorUtility.SetDirty(web);
                    break;
                }

                if (e.type == EventType.MouseUp &&
                    e.shift &&
                    joint != dragingJoint)
                {
                    web.MergeJoints(joint, dragingJoint);
                }
            }

            if (e.type == EventType.MouseDrag &&
            (e.button == 0 || e.button == 1) && dragingJoint != null)
            {
                dragingJoint.position = mousePos + dragShift;
                EditorUtility.SetDirty(web);
            }
            if (e.type == EventType.MouseUp)
            {
                SetDragJoin(null, Vector3.zero);
            }
        }
        else
        {
            SetDragJoin(null, Vector3.zero);
            var closestConnection = web.GetClosestConnection(mousePos, out var projection);
            if (closestConnection != null)
            {
                Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                Handles.DrawSolidDisc(projection, Vector3.back, 0.1f);
                if (e.button == 1 && e.type == EventType.MouseDown)
                {
                    web.RemoveConnection(closestConnection);
                }
                if (e.button == 0 && e.type == EventType.MouseDown)
                {
                    var newJoint = web.CreateJoint(projection, false);
                    web.InsertJoint(closestConnection, newJoint);
                }
            }
        }

        if (e.type != EventType.Layout &&
            e.type != EventType.Repaint &&
            !(e.keyCode == KeyCode.S && e.control) &&
            !e.isScrollWheel &&
            e.button != 2)
        {
            e.Use();
        }
        Repaint();
    }


    private void SetDragJoin(Joint joint, Vector3 startDragPos)
    {
        dragingJoint = joint;
        if (joint != null)
        {
            this.dragShift = (Vector3)joint.position - startDragPos;
        }
    }
}
