using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Camera : MonoBehaviour
{
    [SerializeField]
    private int verticalLines = 240;


    [SerializeField]
    private int horizontalLines = 176;

    [SerializeField]
    private float fieldOfView = 90f;

    [SerializeField]
    private float wallsHeight = 5f;

    [SerializeField]
    private bool debug = false;


    [SerializeField]
    private ComputeShader shader;
    [SerializeField]
    private RenderTexture outputTexture;

    public RenderTexture OutputTexture => outputTexture;

    private int shaderHandle;
    private bool initialized = false;

    private int[] hitScans;
    ComputeBuffer hitScansBuffer;
    private float[] hitDistances;
    ComputeBuffer hitDistancesbuffer;

    [ContextMenu("Init")]
    private void Init()
    {
        outputTexture = new RenderTexture(verticalLines, horizontalLines, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        shaderHandle = shader.FindKernel("CSMain");

        initialized = true;
    }

    private void Dispatch()
    {
        if (!initialized)
        {
            return;
        }

        shader.GetKernelThreadGroupSizes(shaderHandle, out uint groupSizeX, out uint groupSizeY, out uint groupSizeZ);

        int xCount = verticalLines / (int)groupSizeX;
        int yCount = horizontalLines / (int)groupSizeY;

        shader.Dispatch(shaderHandle, xCount, yCount, 1);
    }

    private void Update()
    {
        hitScans = new int[verticalLines];
        hitDistances = new float[verticalLines];

        float angleDelta = fieldOfView / verticalLines;

        float initialAngle = -fieldOfView / 2f;

        for (int i = 0; i < verticalLines; i++)
        {
            float angle = initialAngle + angleDelta * i;

            Ray ray = new Ray(transform.position, Quaternion.AngleAxis(angle, -Vector3.forward) * Vector2.up);

            bool hitScan = Physics.Raycast(ray, out RaycastHit hit);

            hitScans[i] = hitScan ? 1 : 0;
            hitDistances[i] = hit.distance;

            if (debug)
            {
                float hitDistance = hitScan ? hit.distance : 1000f;
                Color hitColor = hitScan ? Color.red : Color.white;
                Vector3 debugLine = ray.direction * hitDistance;
                Debug.DrawRay(transform.position, debugLine, hitColor);
            }
        }

        hitScansBuffer = new ComputeBuffer(verticalLines, sizeof(int));
        hitScansBuffer.SetData(hitScans);
        shader.SetBuffer(shaderHandle, "hitScansBuffer", hitScansBuffer);

        hitDistancesbuffer = new ComputeBuffer(verticalLines, sizeof(float));
        hitDistancesbuffer.SetData(hitDistances);
        shader.SetBuffer(shaderHandle, "hitDistancesBuffer", hitDistancesbuffer);

        shader.SetVector("textureSize", new Vector2(verticalLines, horizontalLines));
        shader.SetFloat("wallsHeight", wallsHeight);

        shader.SetTexture(shaderHandle, "Result", outputTexture);

        Dispatch();

        hitScansBuffer.Release();
        hitDistancesbuffer.Release();
    }
}