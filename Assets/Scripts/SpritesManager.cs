using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SpritesManager : MonoBehaviour
{
    public Action<SpriteRenderer[]> updateSprites;

    private SpriteRenderer[] sprites;

    public Texture3D SpritesAtlas {
        get {
            List<Texture2D> spritesTextures = new List<Texture2D>();
            for (int i = 0; i < sprites.Length; i++) {
                if (spritesTextures.Contains(sprites[i].spriteTexture)) {
                    sprites[i].id = spritesTextures.IndexOf(sprites[i].spriteTexture);
                    continue;
                }
                sprites[i].id = i;
                spritesTextures.Add(sprites[i].spriteTexture);
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