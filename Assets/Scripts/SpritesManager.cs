using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SpritesManager : MonoBehaviour
{
    [HideInInspector]
    public SpriteRenderer[] SpriteRenderers;

    public Texture3D SpritesAtlas {
        get {
            List<Texture2D> spritesTextures = new List<Texture2D>();
            for (int i = 0; i < SpriteRenderers.Length; i++) {
                if (spritesTextures.Contains(SpriteRenderers[i].spriteTexture)) {
                    SpriteRenderers[i].id = spritesTextures.IndexOf(SpriteRenderers[i].spriteTexture);
                    continue;
                }
                SpriteRenderers[i].id = i;
                spritesTextures.Add(SpriteRenderers[i].spriteTexture);
            }
            Texture3D spritesAtlas = new Texture3D(64, 64, spritesTextures.Count, TextureFormat.RGBA32, false);

            for (int i = 0; i < spritesTextures.Count; i++) {
                Graphics.CopyTexture(spritesTextures[i], 0, 0, spritesAtlas, i, 0);
            }
            return spritesAtlas;
        }
    }

    private void OnEnable()
    {
        SpriteRenderers = FindObjectsOfType<SpriteRenderer>();
    }

    private void Update()
    {
        if (SpriteRenderers != null)
        {

#if UNITY_EDITOR
            SpriteRenderers = FindObjectsOfType<SpriteRenderer>();
#endif
        }
    }
}