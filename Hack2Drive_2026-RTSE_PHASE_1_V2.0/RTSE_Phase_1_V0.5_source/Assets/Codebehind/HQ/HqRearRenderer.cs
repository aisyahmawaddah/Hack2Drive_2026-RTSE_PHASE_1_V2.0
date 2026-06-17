using System;
using System.Collections.Generic;
using HQ;
using UnityEngine;

[ExecuteInEditMode]
public class HqRearRenderer : MonoBehaviour
{
    public RenderWindow Renderer;

    public Camera targetCamera;
    public SpriteRenderer BG;
    public SpriteRenderer Plane;
    public SpriteRenderer FG;
    public Sprite BGSprite;
    public int PPU;

    public TrackObject track;
    // Reference to the player body — provides playerX (lane + centrifugal drift)
    // so the camera perspective shifts correctly on corners.
    public ProjectedBody body;
    public TokenManager tokenManager;

    public Material grass1;
    public Material grass2;
    public Material rumble1;
    public Material rumble2;
    public Material road1;
    public Material road2;
    public Material dashline;

    public int screenWidthRef = 320;
    public int screenHeightRef = 240;
    public float cameraDepth = 0.84f; //camera depth [0..1]
    public int DravingDistance = 300; //segments
    public int quadCapacity = 4000;
    public int cameraHeight = 1500; //pixels?
    public float cameraOffset = 0;
    public float centrifugal = 0.1f;
    public float paralaxSpeed = 0.1f;
    public bool drawRoad;
    public bool drawSprites;
    public int rumbleWidth;
    public float SpriteScale;

    [NonSerialized]
    int screenWidth2;
    [NonSerialized]
    int screenHeight2;
    [NonSerialized]
    Mesh[] combined;
    [NonSerialized]
    Dictionary<Material, Quad> dictionary = new Dictionary<Material, Quad>();
    [NonSerialized]
    private Material[] materials;

    //[NonSerialized]
    public int trip = 0; //pixels
    [NonSerialized]
    float playerX = 0;
    [NonSerialized]
    float playerY = 0;
    [NonSerialized]
    float playerZ = 0;

    [NonSerialized]
    private int startPos;
    [NonSerialized]
    private int playerPos;

    [NonSerialized]
    Quad[] quad;
    [NonSerialized]
    private RenderTexture _renderTexture;
    [NonSerialized]
    private float speed;
    [NonSerialized]
    private float prevTrip;
    [NonSerialized]
    private Vector2 bgOffset;

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Awake();
        }
#endif
        Camera.onPostRender += PostRender;
    }
    private void OnDisable()
    {
        Camera.onPostRender -= PostRender;
    }

    void Awake()
    {
        // Clone the master track only during runtime so backward mathematical projection doesn't corrupt the forward camera's dataset
        if (Application.isPlaying && track != null && !track.name.Contains("(Clone)")) 
        {
            track = Instantiate(track);
        }

        Renderer = new RenderWindow();
        tokenManager = FindObjectOfType<TokenManager>();

        Texture2D tex1 = new Texture2D(screenWidthRef, screenHeightRef, TextureFormat.RGBA32, false);
        tex1.filterMode = FilterMode.Point;
        FG.sprite = Sprite.Create(tex1, new Rect(0, 0, screenWidthRef, screenHeightRef), new Vector2(0.5f,0.5f), PPU);
        FG.sprite.name = "rearFG";

        Texture2D tex2 = new Texture2D(screenWidthRef, screenHeightRef, TextureFormat.RGBA32, false);
        tex2.filterMode = FilterMode.Point;
        Plane.sprite = Sprite.Create(tex2, new Rect(0, 0, screenWidthRef, screenHeightRef), new Vector2(0.5f, 0.5f), PPU);
        Plane.sprite.name = "rearPlane";
        
        Texture2D tex3 = new Texture2D(BGSprite.texture.width,  BGSprite.texture.height, TextureFormat.RGBA32, false);
        tex3.filterMode = FilterMode.Point;
        BG.sprite = Sprite.Create(tex3, BGSprite.rect, new Vector2(0.5f, 0.5f), PPU);
        BG.sprite.name = "rearBG";

        quad = new Quad[] {
            new Quad(quadCapacity), 
            new Quad(quadCapacity), 
            new Quad(quadCapacity),
            new Quad(quadCapacity),
            new Quad(quadCapacity), 
            new Quad(quadCapacity),
            new Quad(quadCapacity)
        };
        combined = new Mesh[] { new Mesh(), new Mesh(),new Mesh(),new Mesh(),new Mesh(),new Mesh(),new Mesh(),new Mesh(),new Mesh()};
        dictionary = new Dictionary<Material, Quad>()
        {
            { grass1, quad[0]},
            { grass2, quad[1]},
            { rumble1, quad[2]},
            { rumble2, quad[3]},
            { road1, quad[4]},
            { road2, quad[5]},
            { dashline, quad[6]},
        };
        materials = new Material[] { grass1, grass2, rumble1, rumble2, road1, road2, dashline };
    }
    public void drawSprite(ref Line line)
    {
        if (line.Y < -screenHeight2) { return; }
        Sprite s = line.sprite;
        if (s == null) { return; }
        var w = s.rect.width;
        var h = s.rect.height;

        float destX = line.X + line.W * line.spriteX + screenWidth2;
        float destY = -line.Y + screenHeight2;
        float destW = w * line.scale * screenWidth2 * SpriteScale;
        float destH = h * line.scale * screenWidth2 * SpriteScale;

        destX += destW * Mathf.Sign(line.spriteX) / 2; //offsetX
        destY += destH * (-1);    //offsetY

        float clipH = -line.Y + line.clip;
        if (clipH < 0) clipH = 0;

        if (clipH >= destH) return;

        Rect target = new Rect(destX, destY, destW, destH);
        Rect source = new Rect(Vector2Int.zero, new Vector2(1, 1 - clipH / destH));
        Renderer.draw(source, s, target, line.flipX);
    }

    public void drawToken(ref Line line, Sprite s, float laneSpriteX)
    {
        if (line.Y < -screenHeight2) { return; }
        if (s == null) { return; }

        float destX = line.X + line.W * laneSpriteX + screenWidth2;
        float destY = -line.Y + screenHeight2;

        // Force token to exactly fit the visual width of 1 lane 
        // 5 lanes cover a total road width of 2*W. 1 lane = 0.4 * W
        float destW = line.W * 0.4f; 
        
        // Scale height proportionally to maintain sprite aspect ratio
        float aspectRatio = s.rect.width / s.rect.height;
        float destH = destW / aspectRatio;

        destX -= destW / 2; // Center horizontally on the lane line
        destY -= destH;     // Sit on top of the road

        float clipH = -line.Y + line.clip;
        if (clipH < 0) clipH = 0;
        if (clipH >= destH) return;

        Rect target = new Rect(destX, destY, destW, destH);
        Rect source = new Rect(Vector2Int.zero, new Vector2(1, 1 - clipH / destH));
        Renderer.draw(source, s, target, false);
    }
    private void addQuad(Material c, float x1, float y1, float w1, float x2, float y2, float w2, float z)
    {
        dictionary[c].SetQuad(x1 / PPU, y1 / PPU, w1 / PPU, x2 / PPU, y2 / PPU, w2 / PPU, z);
    }

    private void DrawObjects()
    {
        ////////draw objects////////
        if (drawSprites)
        {
            _renderTexture = RenderTexture.GetTemporary(screenWidthRef, screenHeightRef);
            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            //Work in the pixel matrix of the texture resolution.
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, screenWidthRef, screenHeightRef, 0);
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            
            // Draw from furthest point backward up to the player, enforcing painter's depth
            for (int n = startPos - DravingDistance; n < startPos; n++)
            {
                int segment = (n % track.Length + track.Length) % track.Length;
                ref Line line = ref track.lines[segment];
                
                drawSprite(ref line);

                if (tokenManager != null)
                {
                    tokenManager.DrawTokensForSegment(segment, ref line, null, this); // Null normal renderer
                }
            }
            Graphics.CopyTexture(_renderTexture, FG.sprite.texture);
            //Revert the matrix and active render texture.
            GL.PopMatrix();
            RenderTexture.active = currentActiveRT;
            RenderTexture.ReleaseTemporary(_renderTexture);
        }
    }
    private void DrawRoad()
    {
        if (drawRoad)
        {
            _renderTexture = RenderTexture.GetTemporary(screenWidthRef, screenHeightRef);
            RenderTexture currentActiveRT = RenderTexture.active;
            Graphics.SetRenderTarget(_renderTexture);
            GL.Clear(false, true, new Color(0.0f, 0.0f, 0, 0));
            GL.PushMatrix();
            float refH = targetCamera.orthographicSize * PPU * 2;
            float refHScale = refH / screenHeightRef;
            float HScale = ((float)screenHeightRef) / targetCamera.pixelHeight;
            float unscaledAspectRation = (HScale * targetCamera.pixelWidth) / screenWidthRef;

            var m = Matrix4x4.Scale(new Vector3(unscaledAspectRation * refHScale, refHScale, 1));

            int i = 0;
            foreach (var material in materials)
            {
                Renderer.draw(dictionary[material].ToMesh(combined[i++]), material, m);
            }
            Graphics.CopyTexture(_renderTexture, Plane.sprite.texture);
            GL.PopMatrix();
            Graphics.SetRenderTarget(currentActiveRT);
            RenderTexture.ReleaseTemporary(_renderTexture);
        }
    }

    private void DrawBackground()
    {
        //Good enough
        _renderTexture = RenderTexture.GetTemporary(BG.sprite.texture.width, BG.sprite.texture.height, 0, BG.sprite.texture.graphicsFormat);
        RenderTexture currentActiveRT = RenderTexture.active;
        RenderTexture.active = _renderTexture;
        //Work in the pixel matrix of the texture resolution.
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, screenWidthRef, screenHeightRef, 0);

        bgOffset += new Vector2(paralaxSpeed/PPU * speed * Time.deltaTime * track.lines[playerPos].curve, 0);

        Graphics.Blit(BGSprite.texture, _renderTexture, Vector2.one, bgOffset, 0, 0);

        Graphics.CopyTexture(_renderTexture, BG.sprite.texture);

        GL.PopMatrix();
        Graphics.SetRenderTarget(currentActiveRT);
        RenderTexture.ReleaseTemporary(_renderTexture);
    }

    private void PostRender(Camera cam)
    {
        if (cam == targetCamera)
        {
            DrawRoad();
        }
    }

    private void FixedUpdate()
    {
        CalculateProjection();
    }
    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CalculateProjection();
        }
#endif
        DrawBackground();
        DrawObjects();
    }

    void CalculateProjection()
    {
        speed = trip - prevTrip;
        prevTrip = trip;
        startPos = trip / track.segmentLength;
        playerZ = trip + cameraHeight * cameraDepth; // car is in front of cammera
        playerPos = (int)(playerZ / track.segmentLength) % track.lines.Length;
        playerY = track.lines[playerPos].y;
        int camH = (int)(playerY + cameraHeight);
        // Use the body's accumulated playerX (lane target + centrifugal drift) so the
        // road perspective shifts with the car. Falls back to cameraOffset if not wired.
        playerX = (body != null) ? body.playerX : cameraOffset;
        screenWidth2 = screenWidthRef / 2;
        screenHeight2 = screenHeightRef / 2;

        float maxy = -screenHeight2;
        int counter = 0;
        float x = 0, dx = 0;
        float res = 1f / PPU;

        foreach (var q in quad) { q.Clear(); }
        foreach (var m in combined) { m.Clear(); }
        ///////draw road backwords////////
        for (int n = startPos - 1; n > startPos - DravingDistance; n--)
        {
            int index = (n % track.Length + track.Length) % track.Length;
            ref Line l = ref track.lines[index];
            
            l.projectRear(
                (int)(playerX * track.roadWidth - x),
                camH,
                startPos * track.segmentLength + (n < 0 ? track.Length * track.segmentLength : 0),
                screenWidth2,
                screenHeight2,
                cameraDepth);
                
            // Reverse curve math!
            x -= dx;
            dx -= l.curve;

            l.clip = maxy;
            if (l.Y <= maxy)
            {
                continue;
            }
            maxy = l.Y;

            Material grass = (Mathf.Abs(n) / 3 / 3) % 2 == 0 ? grass1 : grass2;
            Material rumble = (Mathf.Abs(n) / 3) % 2 == 0 ? rumble1 : rumble2;
            Material road = (Mathf.Abs(n) / 3 / 2) % 2 == 0 ? road1 : road2;

            int prevIndex = ((n + 1) % track.Length + track.Length) % track.Length;
            ref Line p = ref track.lines[prevIndex]; //previous line looking backward is actually +1 absolute

            if (Mathf.Abs(l.Y - p.Y) < res)
            {
                continue;
            }

            var z = (float)(startPos - n) / DravingDistance;

            addQuad(grass, 0, p.Y, screenWidth2, 0, l.Y, screenWidth2, z);
            addQuad(rumble, p.X, p.Y, p.W + p.scale * rumbleWidth * screenWidth2, l.X, l.Y, l.W + l.scale * rumbleWidth * screenWidth2, z);
            addQuad(road, p.X, p.Y, p.W, l.X, l.Y, l.W, z);

            if ((Mathf.Abs(n) / 3) % 2 == 0)
            {
                for (int li = 0; li < ProjectedBody.LaneCount - 1; li++)
                {
                    float nd = (ProjectedBody.LanePositions[li] + ProjectedBody.LanePositions[li + 1]) * 0.5f;
                    addQuad(dashline,
                        p.X + nd * p.W, p.Y, p.W * 0.015f,
                        l.X + nd * l.W, l.Y, l.W * 0.015f, z);
                }
            }

            counter++;
        }
    }
}
