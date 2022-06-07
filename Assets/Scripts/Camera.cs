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
    private Texture2D wallsTexture;

    [SerializeField]
    private int heightOffset = 0;

    [SerializeField]
    private Color light = Color.white;

    [SerializeField]
    private Color groundColor = Color.white;

    [SerializeField]
    private Color ceillingColor = Color.white;

    [SerializeField]
    private bool debug;

    [SerializeField]
    private ComputeShader shader;

    private RenderTexture environmentOutTexture;
    private RenderTexture depthOutTexture;
    private RenderTexture spritesOutTexture;

    public RenderTexture EnvironmentOutTexture => environmentOutTexture;
    public RenderTexture DepthOutTexture => depthOutTexture;
    public RenderTexture SpritesOutTexture => spritesOutTexture;

    private int environmentHandle;
    private int spritesHandle;
    private bool initialized = false;
    private float initialAngle;

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

    private int[] hitScans;
    private ComputeBuffer hitScansBuffer;
    private float[] hitDistances;
    private ComputeBuffer hitDistancesbuffer;
    private Vector2[] hitPositions;
    private ComputeBuffer hitPositionsBuffer;
    private Vector2[] hitNormals;
    private ComputeBuffer hitNormalsBuffer;

    private List<Sprite> spritesList;
    private ComputeBuffer spritesBuffer;

    private uint groupSizeX;
    private uint groupSizeY;
    private int countX;
    private int countY;

    private SpritesManager spritesManager;

    private void OnEnable()
    {
        Init();
    }

    [ContextMenu("Init")]
    private void Init()
    {
        spritesManager = FindObjectOfType<SpritesManager>();
        spritesManager.updateSprites += UpdateSprites;

        environmentOutTexture = new RenderTexture(verticalLines, horizontalLines, 0) {
            filterMode = FilterMode.Point,
            enableRandomWrite = true
        };
        environmentOutTexture.Create();

        depthOutTexture = new RenderTexture(verticalLines, 1, 0, RenderTextureFormat.RFloat)
        {
            filterMode = FilterMode.Point,
            enableRandomWrite = true
        };
        depthOutTexture.Create();

        spritesOutTexture = new RenderTexture(verticalLines, horizontalLines, 0)
        {
            filterMode = FilterMode.Point,
            enableRandomWrite = true
        };
        spritesOutTexture.Create();

        shader.GetKernelThreadGroupSizes(environmentHandle, out groupSizeX, out groupSizeY, out _);
        countX = verticalLines / (int)groupSizeX;
        countY = horizontalLines / (int)groupSizeY;

        environmentHandle = shader.FindKernel("DrawEnvironment");
        spritesHandle = shader.FindKernel("DrawSprites");

        hitScansBuffer = new ComputeBuffer(verticalLines, sizeof(int));
        hitDistancesbuffer = new ComputeBuffer(verticalLines, sizeof(float));
        hitPositionsBuffer = new ComputeBuffer(verticalLines, sizeof(float) * 2);
        hitNormalsBuffer = new ComputeBuffer(verticalLines, sizeof(float) * 2);
        spritesBuffer = new ComputeBuffer(verticalLines, sizeof(int));

        shader.SetBuffer(environmentHandle, "hitScansBuffer", hitScansBuffer);
        shader.SetBuffer(environmentHandle, "hitDistancesBuffer", hitDistancesbuffer);
        shader.SetBuffer(environmentHandle, "hitPositionsBuffer", hitPositionsBuffer);
        shader.SetBuffer(environmentHandle, "hitNormalsBuffer", hitNormalsBuffer);

        if (wallsTexture)
        {
            shader.SetTexture(environmentHandle, "WallsTexture", wallsTexture);
        }

        shader.SetTexture(environmentHandle, "EnvironmentOutTexture", environmentOutTexture);
        shader.SetTexture(environmentHandle, "DepthOutTexture", depthOutTexture);

        shader.SetTexture(spritesHandle, "SpritesOutTexture", spritesOutTexture);
        shader.SetTexture(spritesHandle, "DepthOutTexture", depthOutTexture);
        
        shader.SetTexture(spritesHandle, "spritesAtlas", spritesManager.SpritesAtlas);

        initialAngle = -fieldOfView / 2f;

        initialized = true;
    }

    private void Render()
    {
        if (!initialized)
        {
            Init();
        }

        shader.Dispatch(environmentHandle, countX, countY, 1);

        shader.SetBuffer(spritesHandle, "spritesBuffer", spritesBuffer);
        RenderTexture.active = spritesOutTexture;
        GL.Clear(true, true, Color.clear);

        shader.Dispatch(spritesHandle, countX, countY, 1);
    }

    private void Update()
    {
        hitScans = new int[verticalLines];
        hitDistances = new float[verticalLines];
        hitPositions = new Vector2[verticalLines];
        hitNormals = new Vector2[verticalLines];

        float angleDelta = fieldOfView / verticalLines;

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
            hitPositions[i] = raycastHit.point;
            hitNormals[i] = raycastHit.normal;

            if (debug)
            {
                float hitDistance = hitScan ? raycastHit.distance : 1000f;
                Color hitColor = hitScan ? Color.red : Color.white;
                Vector3 debugLine = rayDir * hitDistance;
                Debug.DrawRay(transform.position, debugLine, hitColor);
            }
        }

        hitScansBuffer.SetData(hitScans);

        hitDistancesbuffer.SetData(hitDistances);

        hitPositionsBuffer.SetData(hitPositions);

        hitNormalsBuffer.SetData(hitNormals);

        shader.SetInt("heightOffset", heightOffset);
        shader.SetFloat("wallsHeight", wallsHeight);
        shader.SetVector("groundColor", groundColor);
        shader.SetVector("ceillingColor", ceillingColor);
        shader.SetVector("light", light);

        Render();
    }

    private int CompareSpriteDist(Sprite a, Sprite b)
    {
        return (a.distance < b.distance ? 1 : -1);
    }

    private void UpdateSprites(SpriteRenderer[] spritesRenderers)
    {
        spritesList = new List<Sprite>();
        for (int i = 0; i < spritesRenderers.Length; i++)
        {
            Vector2 toSprite_ = spritesRenderers[i].Position - (Vector2)transform.position;
            float angleToSprite_ = Vector2.SignedAngle(toSprite_, transform.up);
            angleToSprite_ -= initialAngle;
            int spritePos_ = Mathf.RoundToInt((angleToSprite_ / fieldOfView) * verticalLines);

            Sprite sprite = new Sprite()
            {
                position = spritePos_,
                id = spritesRenderers[i].id,
                frame = spritesRenderers[i].frame,
                distance = toSprite_.magnitude,
                spriteSize = spritesRenderers[i].SpriteSize,
                spriteHeight = spritesRenderers[i].SpriteHeight,
            };
            spritesList.Add(sprite);
        }

        spritesList.Sort(CompareSpriteDist);

        spritesBuffer = new ComputeBuffer(spritesRenderers.Length, SpriteSize);
        spritesBuffer.SetData(spritesList);
        shader.SetInt("spritesCount", spritesList.Count);
    }

    private void OnGUI()
    {
        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
        GUI.DrawTexture(screenRect, environmentOutTexture);
        GUI.DrawTexture(screenRect, spritesOutTexture);
        //GUI.DrawTexture(screenRect, depthTexture);
    }

    private void OnDestroy()
    {
        hitScansBuffer.Dispose();
        hitDistancesbuffer.Dispose();
        hitPositionsBuffer.Dispose();
        hitNormalsBuffer.Dispose();
        spritesBuffer.Dispose();
    }
}