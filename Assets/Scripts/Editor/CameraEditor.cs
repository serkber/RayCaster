using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(Camera))]
public class CameraEditor : Editor
{
    private int previewSize = 128;

    private Camera camera;

    private void OnEnable()
    {
        camera = target as Camera;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        previewSize = EditorGUILayout.IntField("Preview Size", previewSize);

        if (EditorGUI.EndChangeCheck())
        {
            SceneView.lastActiveSceneView.Repaint();
        }


        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        if (!camera.OutputTexture)
        {
            return;
        }
        float width = camera.OutputTexture.width * previewSize / camera.OutputTexture.height;

        Handles.BeginGUI();

            GUI.DrawTexture(new Rect(10, 10, width, previewSize), camera.OutputTexture);

        Handles.EndGUI();
    }
}