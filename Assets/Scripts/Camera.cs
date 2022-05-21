using System;
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

    private void Start()
    {
        Init();
    }

    [ContextMenu("Init")]
    private void Init()
    {
        outputTexture = new RenderTexture(verticalLines, horizontalLines, 0) {
            filterMode = FilterMode.Point,
            enableRandomWrite = true
        };
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

            Vector3 rayDir = Quaternion.AngleAxis(angle, -transform.forward) * transform.up;
            int environmentLayer = LayerMask.NameToLayer("Environment");
            int layerMask = 1 << environmentLayer;
            RaycastHit2D raycastHit = Physics2D.Raycast(transform.position, rayDir, 100f, layerMask);
            bool hitScan = raycastHit.collider != null;

            hitScans[i] = hitScan ? 1 : 0;
            hitDistances[i] = raycastHit.distance;

            if (debug)
            {
                float hitDistance = hitScan ? raycastHit.distance : 1000f;
                Color hitColor = hitScan ? Color.red : Color.white;
                Vector3 debugLine = rayDir * hitDistance;
                Debug.DrawRay(transform.position, debugLine, hitColor);
            }
        }

        hitScansBuffer = new ComputeBuffer(verticalLines, sizeof(int));
        hitScansBuffer.SetData(hitScans);
        shader.SetBuffer(shaderHandle, "hitScansBuffer", hitScansBuffer);

        hitDistancesbuffer = new ComputeBuffer(verticalLines, sizeof(float));
        hitDistancesbuffer.SetData(hitDistances);
        shader.SetBuffer(shaderHandle, "hitDistancesBuffer", hitDistancesbuffer);

        shader.SetInt("textureHeight", horizontalLines);
        shader.SetFloat("wallsHeight", wallsHeight);

        shader.SetTexture(shaderHandle, "Result", outputTexture);

        Dispatch();

        hitScansBuffer.Release();
        hitDistancesbuffer.Release();
    }

    private void OnGUI()
    {
        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
        GUI.DrawTexture(screenRect, outputTexture);
    }
}