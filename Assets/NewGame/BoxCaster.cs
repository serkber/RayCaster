using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BoxCaster : MonoBehaviour
{
    private CollisionManager collisionManager;

    private List<int> boxChecksIndices = new List<int>();

    private void Start()
    {
        collisionManager = FindObjectOfType<CollisionManager>();
    }

    private void Update()
    {
        if (collisionManager == null)
        {
            return;
        }

        int distance = 0;
        int count = 0;
        foreach (int index in boxChecksIndices)
        {
            var collisionData = collisionManager.GetCollisionData(index);

            if (collisionData != -1)
            {
                distance = collisionManager.SampleSize * count + collisionData;
                break;
            }

            count++;
        }

        Debug.Log(distance);
        Debug.DrawLine(transform.position, transform.position + transform.up * distance);

        boxChecksIndices.Clear();
        collisionManager.SetBoxCast(transform.position, transform.up, this);
    }

    public void AddBoxCheck(int boxCheckIndex)
    {
        boxChecksIndices.Add(boxCheckIndex);
    }
}
