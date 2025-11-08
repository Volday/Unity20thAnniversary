using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Web : MonoBehaviour
{
    [SerializeField]
    private List<Joint> joints;
    [SerializeField]
    private List<Connection> connections;

    public void RemoveJoint(Joint joint) {
        var index = joints.IndexOf(joint);
        if (index == -1) {
            return;
        }
        joints.RemoveAt(index);
        for (int i = 0; i < connections.Count; i++)
        {
            var con = connections[i];
            if (con.first == index || con.second == index) {
                connections.RemoveAt(i);
                i--;
            }
            else {
                if (con.first > index) {
                    con.first--;
                }
                if (con.second > index){
                    con.second--;
                }
            }
        }
    }

    public bool TryCrateConnction(Joint first, Joint second, out Connection connection) {
        connection = null;
        var firstIndex = joints.IndexOf(first);
        var secondIndex = joints.IndexOf(second);
        if (firstIndex == -1 || secondIndex == -1) {
            return false;
        }
        connection = new Connection(firstIndex, secondIndex);
        connections.Add(connection);
        return true;
    }

    private void OnDrawGizmos()
    {
        if (connections != null)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                var connection = connections[i];
                Gizmos.DrawLine(joints[connection.first].position, joints[connection.second].position);
            }
        }
    }

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

                if (joint.isStatic) {
                    Handles.color = new Color(0.2f, 0.2f, 0.2f);
                } else {
                    Handles.color = new Color(0.8f, 0.8f, 0.8f);
                }
                if (mouseOn) {
                    Handles.color += Color.green * 0.2f;
                }
                Handles.DrawSolidDisc(joint.position, Vector3.back, 0.1f);
            }
            if (!editMode) {
                return;
            }

            for (int i = 0; i < web.joints.Count; i++) {
                var joint = web.joints[i];
                var mouseOn = (mousePos - (Vector3)joint.position).magnitude < 0.1f;
                if (!mouseOn) {
                    continue;
                }
                if (e.type == EventType.MouseDown &&
                    e.button == 2)
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
                        var newJoint = new Joint() { position = joint.position, isStatic = joint.isStatic };
                        dragingJoint = newJoint;
                        SetDragJoin(newJoint, mousePos);
                        web.joints.Add(newJoint);
                        web.TryCrateConnction(joint, newJoint, out _);
                        EditorUtility.SetDirty(web);
                        break;
                    }
                }

                if (e.keyCode == KeyCode.Escape) {
                    web.RemoveJoint(joint);
                    EditorUtility.SetDirty(web);
                    break;
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
            
            if (e.type != EventType.Layout &&
                e.type != EventType.Repaint) {
                e.Use();
            }
            Repaint();
        }

        private void SetDragJoin(Joint joint, Vector3 startDragPos) {
            dragingJoint = joint;
            if (joint != null)
            {
                this.dragShift = (Vector3)joint.position - startDragPos;
            }
        }
    }

    [Serializable]
    public class Joint
    {
        public Vector2 position;
        public bool isStatic;
    }

    [Serializable]
    public class Connection 
    {
        public int first;
        public int second;

        public Connection(int first, int second)
        {
            this.first = first;
            this.second = second;
        }
    }
}
