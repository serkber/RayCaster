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
        if (camera.environmentRenderer == null || camera.spritesRenderer == null)
        {
            return;
        }
        float width = camera.environmentRenderer.environmentOutTexture.width * previewSize / (float)camera.environmentRenderer.environmentOutTexture.height;

        Handles.BeginGUI();

            GUI.DrawTexture(new Rect(10, 10, width, previewSize), camera.environmentRenderer.environmentOutTexture);
            GUI.DrawTexture(new Rect(10, 10, width, previewSize), camera.spritesRenderer.SpritesOutTexture);

        Handles.EndGUI();
    }
}