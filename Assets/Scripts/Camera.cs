using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Camera : MonoBehaviour
{
    public int verticalLines = 240;
    public int horizontalLines = 176;
    public float fieldOfView = 90f;
    public Texture2D wallsTexture;
    public float wallsHeight = 5f;
    public int cameraHeight;
    public Color light = Color.white;
    public Color groundColor = Color.white;
    public Color ceillingColor = Color.white;
    public bool debug;
    public ComputeShader shader;

    public SpritesManager spritesManager;
    public SpritesRenderer spritesRenderer;
    public EnvironmentRenderer environmentRenderer;
    
    public float initialAngle;

    private void OnEnable()
    {
        Init();
    }

    [ContextMenu("Init")]
    private void Init()
    {
        environmentRenderer = new EnvironmentRenderer(this);
        spritesManager = FindObjectOfType<SpritesManager>();
        spritesRenderer = new SpritesRenderer(this);
        
        initialAngle = -fieldOfView / 2f;
    }

    private void Render()
    {
        if (environmentRenderer == null || spritesRenderer == null) {
            Init();
        }

        environmentRenderer.Render();
        spritesRenderer.Render();
    }

    private void Update()
    {
        Render();
    }

    private void OnGUI()
    {
        if (environmentRenderer == null || spritesRenderer == null) {
            return;
        }
        
        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
        GUI.DrawTexture(screenRect, environmentRenderer.environmentOutTexture);
        GUI.DrawTexture(screenRect, spritesRenderer.SpritesOutTexture);
    }

    private void OnDestroy()
    {
        environmentRenderer.Dispose();
        spritesRenderer.Dispose();
    }
}