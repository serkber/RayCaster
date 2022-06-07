using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Camera))]
public class CameraEditor : Editor
{
    private const int previewSize = 256;

    private Camera camera;

    private void OnEnable()
    {
        camera = target as Camera;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        if (!camera.EnvironmentTexture)
        {
            return;
        }
        float width = camera.EnvironmentTexture.width * previewSize / camera.EnvironmentTexture.height;

        Handles.BeginGUI();

            GUI.DrawTexture(new Rect(10, 10, width, previewSize), camera.EnvironmentTexture);
            GUI.DrawTexture(new Rect(10, 10, width, previewSize), camera.SpritesTexture);

        Handles.EndGUI();
    }
}