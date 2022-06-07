using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentRenderer
{
    private ComputeShader shader;

    private int environmentHandle;
    
    public RenderTexture environmentOutTexture;
    public RenderTexture depthOutTexture;
    
    private bool initialized = false;
    
    private struct Hit
    {
        public float distance;
        public Vector3 position;
        public Vector3 normal;
    }

    private Hit[] hitsArray;
    private ComputeBuffer hitsBuffer;

    private uint groupSizeX;
    private uint groupSizeY;
    private int countX;
    private int countY;

    private Camera camera;

    public EnvironmentRenderer(Camera camera)
    {
        this.camera = camera;
        shader = camera.shader;
        
        environmentOutTexture = new RenderTexture(camera.verticalLines, camera.horizontalLines, 0) {
            filterMode = FilterMode.Point,
            enableRandomWrite = true
        };
        environmentOutTexture.Create();

        depthOutTexture = new RenderTexture(camera.verticalLines, 1, 0, RenderTextureFormat.RFloat)
        {
            filterMode = FilterMode.Point,
            enableRandomWrite = true
        };
        depthOutTexture.Create();
        
        environmentHandle = shader.FindKernel("DrawEnvironment");
        shader.GetKernelThreadGroupSizes(environmentHandle, out groupSizeX, out groupSizeY, out _);
        countX = camera.verticalLines / (int)groupSizeX;
        countY = camera.horizontalLines / (int)groupSizeY;

        hitsArray = new Hit[camera.verticalLines];
        hitsBuffer = new ComputeBuffer(camera.verticalLines, sizeof(float) * 7);
        shader.SetBuffer(environmentHandle, "hitsBuffer", hitsBuffer);

        if (camera.wallsTexture)
        {
            shader.SetTexture(environmentHandle, "WallsTexture", camera.wallsTexture);
        }

        shader.SetTexture(environmentHandle, "EnvironmentOutTexture", environmentOutTexture);
        shader.SetTexture(environmentHandle, "DepthOutTexture", depthOutTexture);
    }

    private void UpdateEnvironment()
    {
        float angleDelta = camera.fieldOfView / camera.verticalLines;

        for (int i = 0; i < camera.verticalLines; i++)
        {
            float angle = camera.initialAngle + angleDelta * i;

            Vector3 rayDir = Quaternion.AngleAxis(angle, -camera.transform.forward) * camera.transform.up;
            int environmentLayer = LayerMask.NameToLayer("Environment");
            int layerMask = 1 << environmentLayer;
            RaycastHit2D raycastHit = Physics2D.Raycast(camera.transform.position, rayDir, 100f, layerMask);
            bool hit = raycastHit.collider != null;

            hitsArray[i] = new Hit {
                distance = hit ? raycastHit.distance : Mathf.Infinity,
                normal = raycastHit.normal,
                position = raycastHit.point
            };
        }

        hitsBuffer.SetData(hitsArray);

        shader.SetInt("heightOffset", camera.cameraHeight);
        shader.SetFloat("wallsHeight", camera.wallsHeight);
        shader.SetVector("groundColor", camera.groundColor);
        shader.SetVector("ceillingColor", camera.ceillingColor);
        shader.SetVector("light", camera.light);
    }

    public void Render()
    {
        UpdateEnvironment();
        shader.Dispatch(environmentHandle, countX, countY, 1);
    }

    public void Dispose()
    {
        hitsBuffer.Dispose();
    }
}
