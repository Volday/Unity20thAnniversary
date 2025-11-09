using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlyManager))]
public class FlyManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var fluManager = (FlyManager)target;
        if (GUILayout.Button("Capture points")) {
            fluManager.CapturePoints();
        }
        base.OnInspectorGUI();
    }
}
