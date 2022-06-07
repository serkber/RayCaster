using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SpritesManager : MonoBehaviour
{
    public Action<SpriteRenderer[]> updateSprites;

    private SpriteRenderer[] sprites;

    private void OnEnable()
    {
        sprites = FindObjectsOfType<SpriteRenderer>();
    }

    private void Update()
    {
        if (sprites != null)
        {

#if UNITY_EDITOR
            sprites = FindObjectsOfType<SpriteRenderer>();
#endif

            updateSprites?.Invoke(sprites);
        }
    }
}