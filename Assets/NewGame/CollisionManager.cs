using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CollisionManager : MonoBehaviour
{
    [SerializeField]
    private Texture2D environmentTexture;

    [SerializeField]
    private ComputeShader shader;

    [SerializeField]
    private int sampleSize = 16;

    private int shaderHandle;

    private Matrix4x4[] transformationMatrices;
    private ComputeBuffer transformationMatricesBuffer;

    private int[] collisionData;
    private ComputeBuffer collisionDataBuffer;

    private int countX;
    private int countY;
    private int countZ;

    private int oldSampleSize;

    public int SampleSize => sampleSize;

    [SerializeField]
    private RenderTexture rt;

    private BoxCheck[] boxCheckPool = new BoxCheck[512];
    private int lastBoxCheckAlloc;

    private void OnEnable()
    {
        Initialize();
    }

    private void Update()
    {
        if(sampleSize != oldSampleSize)
        {
            Initialize();
        }

        lastBoxCheckAlloc = 0;
        SetMatrices();

        shader.Dispatch(shaderHandle, countX, countY, countZ);

        collisionDataBuffer.GetData(collisionData);

        int[] newCollisionData = new int[boxCheckPool.Length];
        Array.Fill(newCollisionData, -1);
        collisionDataBuffer.SetData(newCollisionData);

        oldSampleSize = sampleSize;
    }

    private void OnDestroy()
    {
        collisionDataBuffer.Release();
    }

    private void InitBoxCheckPool()
    {
        for (int i = 0; i < boxCheckPool.Length; i++)
        {
            boxCheckPool[i] = new BoxCheck();
        }
    }

    [ContextMenu("Initialize")]
    void Initialize()
    {
        InitBoxCheckPool();

        if (collisionDataBuffer != null)
        {
            collisionDataBuffer.Dispose();
        }
        if (transformationMatricesBuffer != null)
        {
            transformationMatricesBuffer.Dispose();
        }

        if (shader == null)
        {
            return;
        }
        shaderHandle = shader.FindKernel("CSMain");

        transformationMatrices = new Matrix4x4[boxCheckPool.Length];

        transformationMatricesBuffer = new ComputeBuffer(boxCheckPool.Length, sizeof(float) * 16);
        SetMatrices();
        shader.SetBuffer(shaderHandle, "TransformationMatrices", transformationMatricesBuffer);

        collisionData = new int[boxCheckPool.Length];
        collisionDataBuffer = new ComputeBuffer(boxCheckPool.Length, sizeof(int));
        shader.SetBuffer(shaderHandle, "CollisionData", collisionDataBuffer);

        shader.SetTexture(shaderHandle, "EnvironmentTexture", environmentTexture);

        shader.GetKernelThreadGroupSizes(shaderHandle, out uint groupX, out uint groupY, out _);

        countX = sampleSize / (int)groupX;
        countY = sampleSize / (int)groupY;
        countZ = boxCheckPool.Length;

        rt = new RenderTexture(sampleSize, sampleSize, 1);
        rt.enableRandomWrite = true;
        shader.SetTexture(shaderHandle, "Result", rt);
    }

    public int GetCollisionData(int boxCheckIndex)
    {
        return collisionData[boxCheckIndex];
    }

    public Vector2 GetBoxCheckPosition(int boxCheckIndex)
    {
        return boxCheckPool[boxCheckIndex].Position;
    }

    private void SetMatrices()
    {
        for (int i = 0; i < boxCheckPool.Length; i++)
        {
            if (boxCheckPool[i] == null)
            {
                continue;
            }


            float radAngle = boxCheckPool[i].Rotation.eulerAngles.z  * Mathf.Deg2Rad;
            float cosAngle = Mathf.Cos(radAngle);
            float sinAngle = Mathf.Sin(radAngle);

            Matrix4x4 offsetMatrix = new Matrix4x4(
                new Vector4(1f, 0f, 0f, 0f),
                new Vector4(0f, 1f, 0f, 0f),
                new Vector4(0f, -sampleSize / 2f, 1f, 0f),
                new Vector4(0f, 0f, 0f, 1f));

            Matrix4x4 posMatrix = new Matrix4x4(
                new Vector4(1f, 0f, 0f, 0f),
                new Vector4(0f, 1f, 0f, 0f),
                new Vector4(boxCheckPool[i].Position.x, boxCheckPool[i].Position.y, 1f, 0f),
                new Vector4(0f, 0f, 0f, 1f));

            Matrix4x4 rotMatrix = new Matrix4x4(
                new Vector4(cosAngle, sinAngle, 0f, 0f),
                new Vector4(-sinAngle, cosAngle, 0f, 0f),
                new Vector4(0f, 0f, 1f, 0f),
                new Vector4(0f, 0f, 0f, 1f));

            transformationMatrices[i] = posMatrix * rotMatrix * offsetMatrix;
        }
        transformationMatricesBuffer.SetData(transformationMatrices);
    }

    public void SetBoxCast(Vector2 origin, Vector2 direction, BoxCaster boxCaster)
    {
        if (boxCheckPool == null || boxCheckPool.Length == 0)
        {
            return;
        }

        direction = direction.normalized;

        int boxCheckAlloc = (environmentTexture.width + environmentTexture.height) / sampleSize;

        int count = 0;
        for (int i = lastBoxCheckAlloc; i < lastBoxCheckAlloc + boxCheckAlloc; i++)
        {
            Vector2 pos = origin + direction * sampleSize * (i - lastBoxCheckAlloc);

            if(pos.x < 0f || pos.x > environmentTexture.width || pos.y < 0f || pos.y > environmentTexture.height)
            {
                break;
            }

            boxCheckPool[i].Position = pos;
            boxCheckPool[i].Rotation = Quaternion.FromToRotation(Vector2.right, direction);

            boxCaster.AddBoxCheck(i);

            count++;
        }

        lastBoxCheckAlloc += count;
    }

    private void OnDrawGizmos()
    {
        if (boxCheckPool == null)
        {
            return;
        }

        for (int i = 0; i < boxCheckPool.Length; i++)
        {
            if (boxCheckPool[i] == null)
            {
                continue;
            }

            if (collisionData[i] != -1)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.white;
            }

            Vector3[] vecs = new Vector3[] {
                Vector3.right * sampleSize + Vector3.up * sampleSize,
                Vector3.right * sampleSize,
                Vector3.zero,
                Vector3.up * sampleSize};

            for (int v = 0; v < vecs.Length; v++)
            {
                vecs[v] -= Vector3.up * sampleSize / 2f;
                vecs[v] = boxCheckPool[i].Rotation * vecs[v];
                vecs[v] += (Vector3)boxCheckPool[i].Position;
            }

            Gizmos.DrawLine(vecs[0], vecs[1]);
            Gizmos.DrawLine(vecs[1], vecs[2]);
            Gizmos.DrawLine(vecs[2], vecs[3]);
            Gizmos.DrawLine(vecs[3], vecs[0]);
        }
    }

    private void OnGUI()
    {
        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
        GUI.DrawTexture(screenRect, rt);
    }
}

public class BoxCheck
{
    public Vector2 Position;
    public Quaternion Rotation;

    public BoxCheck()
    {
        Position = -Vector2.one * 512f;
        Rotation = Quaternion.identity;
    }
}