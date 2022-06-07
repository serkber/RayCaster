using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteRenderer : MonoBehaviour
{
    public float SpriteSize = 1f;
    public float SpriteHeight = 0f;
    public Vector2 Position => transform.position;
    public int id;
    public int frame;
}