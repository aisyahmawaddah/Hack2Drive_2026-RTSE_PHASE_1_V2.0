using UnityEngine;

public struct Line
{
    public float x, y, z, w;//3d center of line
    public float X, Y, W; //screen coord
    public float curve, spriteX, clip, scale;
    public Sprite sprite;
    public bool flipX;
    public Sprite originalSprite;
    public float originalSpriteX;
    // Game data — set at runtime by TokenManager / EventManager
    public int tokenIndex;      // -1 = no token, else index into TokenManager.tokens list
    public int npcBlockedLane;  // -99 = no NPC, else the lane (0..4) an event car occupies

    public void project(int camX, int camY, int camZ, int screenWidth2, int screenHeight2, float cameraDepth)
    {
        scale = cameraDepth / (z - camZ);
        X = scale * (x - camX) * screenWidth2;
        Y = scale * (y - camY) * screenHeight2;
        W = scale * w * screenWidth2;
    }

    public void projectRear(int camX, int camY, int camZ, int screenWidth2, int screenHeight2, float cameraDepth)
    {
        // Enforce positive scale mapping when tracing backwards
        scale = cameraDepth / Mathf.Max(0.1f, Mathf.Abs(z - camZ));
        
        // Reverse X perspective mapping so geometry traces correctly reflecting back
        X = scale * (camX - x) * screenWidth2; 
        
        Y = scale * (y - camY) * screenHeight2;
        W = scale * w * screenWidth2;
    }
}
