using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Environment))]
public class EnvironmentEditor : Editor {

    public override void OnInspectorGUI()
    {
        Environment env = (Environment)target;

        if (GUILayout.Button("Clear"))
        {
            env.Clear();
        }

        DrawDefaultInspector();
    }
}
