using System.Collections.Generic;
using UnityEngine;

public class SpritesRenderer
{
    private Camera camera;

    private ComputeShader shader;
    
    private RenderTexture spritesOutTexture;
    public RenderTexture SpritesOutTexture => spritesOutTexture;

    private int spritesHandle;
    private List<Sprite> spritesList;
    private ComputeBuffer spritesBuffer;

    private uint groupSizeX;
    private uint groupSizeY;
    private int countX;
    private int countY;
    
    private SpritesManager spritesManager;
    
    private struct Sprite
    {
        public int position;
        public int id;
        public int frame;
        public float distance;
        public float spriteSize;
        public float spriteHeight;
    }
    private int SpriteSize => sizeof(int) * 3 + sizeof(float) * 3;

    public SpritesRenderer(Camera camera)
    {
        this.camera = camera;
        shader = camera.shader;
        
        spritesHandle = shader.FindKernel("DrawSprites");
        shader.GetKernelThreadGroupSizes(spritesHandle, out groupSizeX, out groupSizeY, out _);
        countX = camera.verticalLines / (int)groupSizeX;
        countY = camera.horizontalLines / (int)groupSizeY;
        
        spritesManager = camera.spritesManager;

        spritesOutTexture = new RenderTexture(camera.verticalLines, camera.horizontalLines, 0)
        {
            filterMode = FilterMode.Point,
            enableRandomWrite = true
        };
        spritesOutTexture.Create();
        
        shader.SetTexture(spritesHandle, "SpritesOutTexture", spritesOutTexture);
        shader.SetTexture(spritesHandle, "DepthOutTexture", camera.environmentRenderer.depthOutTexture);
        
        shader.SetTexture(spritesHandle, "spritesAtlas", spritesManager.SpritesAtlas);
    }

    public void Render()
    {
        UpdateSprites();
        
        RenderTexture.active = spritesOutTexture;
        GL.Clear(true, true, Color.clear);

        camera.shader.Dispatch(spritesHandle, countX, countY, 1);
    }

    private int CompareSpriteDist(Sprite a, Sprite b)
    {
        return a.distance < b.distance ? 1 : -1;
    }

    private void UpdateSprites()
    {
        spritesList = new List<Sprite>();
        for (int i = 0; i < spritesManager.SpriteRenderers.Length; i++) {
            SpriteRenderer spriteRenderer = spritesManager.SpriteRenderers[i];
            Vector2 toSprite = spriteRenderer.Position - (Vector2)camera.transform.position;
            float angleToSprite = Vector2.SignedAngle(toSprite, camera.transform.up);
            angleToSprite -= camera.initialAngle;
            int spritePos = Mathf.RoundToInt((angleToSprite / camera.fieldOfView) * camera.verticalLines);

            Sprite sprite = new Sprite()
            {
                position = spritePos,
                id = spriteRenderer.id,
                frame = spriteRenderer.frame,
                distance = toSprite.magnitude,
                spriteSize = spriteRenderer.SpriteSize,
                spriteHeight = spriteRenderer.SpriteHeight,
            };
            spritesList.Add(sprite);
        }

        spritesList.Sort(CompareSpriteDist);

        spritesBuffer = new ComputeBuffer(spritesManager.SpriteRenderers.Length, SpriteSize);
        spritesBuffer.SetData(spritesList);
        camera.shader.SetBuffer(spritesHandle, "spritesBuffer", spritesBuffer);
        camera.shader.SetInt("spritesCount", spritesList.Count);
    }

    public void Dispose()
    {
        spritesBuffer.Dispose();
    }
}