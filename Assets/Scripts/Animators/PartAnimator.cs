using UnityEngine;
using System;
/// <summary>
/// This is in charge of running animations by changing the sprite on the renderer every frame change
/// it should be compatible with vehicles BUT HAS ONLY BEEN TESTED ON MECHS
/// </summary>
public class PartAnimator : MonoBehaviour
{
    private Mesh mesh;
    private Material material;
    private Transform tr;
    public Texture2D palette; // not yet assigned dynamically - to do
    /*
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Sprite[] sprites; // all the sprites of this part
    
    private Vector3 position; // these will be converted to meshFilter
    */
    private Vector3 scale = Vector3.one; // retain
    private Vector3[] verts = new Vector3[4];
    private Vector2[] uvs = new Vector2[4];
    // need a reference to the material

    //private PartPositionsData positionData; // convert to PartVertAndUVData
    //private AnimationData currentAnimationData; // convert to PartAnimationStateData
    private PartAnimationData partAnimationData;
    private PartAnimationStateData animationStateData;
    private Facing facing;

    private const int maxFacingValue = 31;
    private const float pixelsPerUnit = 1f / 100f;
    private float zDepth;

    private int mirrorValue = 1;
    [SerializeField]
    private int currentIndex;
    [SerializeField]
    private int currentStart;
    private int numOfFrames = 0;
    private int numOfFaces = 0;
    private int halfOfFrames = 0;
    [SerializeField]
    private int offset;

    public bool useTestOffset;
    public int testOffset;

    public int CurrentFacingCount
	{
		get { return numOfFaces; }
	}

    public int CurrentFrameCount
	{
        get { return numOfFrames; }
	}

    private void Awake()
	{
        //spriteRenderer = GetComponent<SpriteRenderer>();
        tr = transform;
        mesh = new Mesh();

        CreateMesh();
	}
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        // instantiate new material?
        // set the texture as returned by the sprite loader and passed by the mechanimator
	}

    public void AssignPartAnimationData(PartAnimationData data)
	{
        partAnimationData = data;
        
	}

    public void CreateMesh()
    {
        var tileSizeX = 30 * pixelsPerUnit;
        var tileSizeY = 30 * pixelsPerUnit;

        mesh = new Mesh();
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();

        var shader = Shader.Find("MechCommanderUnity/PaletteSwapLookup");
        material = new Material(shader);
        //material.mainTexture = texture;
        material.SetTexture("_PaletteTex", palette);

        // verts - applied every frame for animation - must put in struct to be iterated upon
        Vector3[] vectors = new Vector3[4];// this has to be instantiated at the start of the meshAnim struct

        Vector3 value = Vector3.zero;
        vectors[0] = value; //top-left
        value.Set(tileSizeX, 0, 0);
        vectors[1] = value; //top-right
        value.Set(0, -tileSizeY, 0);
        vectors[2] = value; //bottom-left
        value.Set(tileSizeX, -tileSizeY, 0);
        vectors[3] = value; //bottom-right

        // tris - probably constant for what we want to do in animation
        var idxTriangles = 0;//LstVertex.Count;
        // Indices
        int[] triangles = new int[6]
        {
            idxTriangles, idxTriangles+1, idxTriangles+2,
            idxTriangles+3, idxTriangles+2, idxTriangles+1,
        };

        // Normals - only assigned at start i assum;
        value = Vector3.Normalize(Vector3.up + Vector3.forward);
        var normals = new Vector3[4];
        normals[0] = value;
        normals[1] = value;
        normals[2] = value;
        normals[3] = value;

        
        // uvs - also needs to be iterated upon.. struct would probably have both verts and uvs to animate
        Vector2[] uvs = new Vector2[4]; // this has to be instantiated at the start of the meshAnim struct
        value.Set(0, 1, 0);
        uvs[0] = value;
        value.Set(1, 1, 0);
        uvs[1] = value;
        value.Set(0, 0, 0);
        uvs[2] = value;
        value.Set(1, 0, 0);
        uvs[3] = value;

        // Assign mesh
        mesh.vertices = vectors;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        meshFilter.sharedMesh = mesh;
        meshFilter.sharedMesh.uv = uvs;
        meshRenderer.sharedMaterial = material;
    }

    public void UpdateFrames(int frameIndex)
    {
        currentIndex = (frameIndex + (useTestOffset ? testOffset : offset)) % numOfFrames;
        
        UpdateAnimatorInternal();
    }
	/*
    public void ChangeCurrentAnimation(AnimationData newAnimData)
	{
        currentAnimationData = newAnimData;
        numOfFrames = currentAnimationData.NumOfImages;
        numOfFaces = currentAnimationData.NumOfFaces;
        halfOfFrames = numOfFrames / 2;
        Debug.Log("playing " + numOfFrames + " frames");
	}*/
    
    public void ChangeCurrentAnimation(PartAnimationStateData newAnimData)
	{
        animationStateData = newAnimData;
        numOfFrames = newAnimData.Length;
        numOfFaces = newAnimData.FacingCount;
        halfOfFrames = numOfFrames / 2;

        var textureID = partAnimationData.vertsAndUVs[animationStateData.StartIndex].textureIndex;
        material.mainTexture = partAnimationData.textures[textureID];
        Debug.Log("playing " + numOfFrames + " frames");
	}

    public void SetStateToSingleFrameStateAtID(int id)
	{
        ChangeCurrentAnimation(partAnimationData.singleFrameAnimationStates[id]);
	}

    public void SetState(int id)
	{
        ChangeCurrentAnimation(partAnimationData.animationStates[id]);
	}

    // WE have to take into account how the animation started
    // in the game we just flip the sprites; we dont offset the legs so they start the same, we just make the transition into the flip
    // BUT we have to offset the arms to connect to the animation
    public void StartTransition()
	{
        currentStart = animationStateData.StartIndex + ((byte)facing * numOfFrames);
        /*if (numOfFaces == 32 && facing > Facing.N)
        {
            currentPosStart = currentAnimationData.IndexOfStart + ((numOfFaces * 2 - 2 - (int)facing) * numOfFrames);
            mirrorValue = -1;
        }
        else
		{
            currentPosStart = currentStart;
            mirrorValue = 1;
        }*/
        currentIndex = 0;
    }
    public void StartAnimator() // we can merge this and updateAniamtor just make sure start is zero
	{
        currentIndex = 0;
        UpdateAnimator();
        /*
        currentStart = currentAnimationData.IndexOfStart + ((byte)facing * numOfFrames);
        if (numOfFaces == 32 && (int)facing > 16)
        {
            Debug.Log("32 frames offset set");
            offset = halfOfFrames;
        }
        else offset = 0;
        
        Debug.Log("startin anim at " + currentStart + " offset " + offset + " current ind " + currentIndex);
        
        // if the frame count is 1, we update the sprite immediatetly
        if (numOfFrames > 1) return;
        position.x = positionData.partPositions[currentStart].x / pixelsPerUnit * mirrorValue;
        position.y = -positionData.partPositions[currentStart].y / pixelsPerUnit;
        tr.localPosition = position;

        spriteRenderer.sprite = sprites[currentStart];
        */
    }

    public void UpdateAnimator()
	{
        currentStart = animationStateData.StartIndex + ((byte)facing * numOfFrames);
        // if we have 360 degree part, we have to offset on the mirrored side
        if (numOfFaces == 32 && (int)facing > 16)
        {
            Debug.Log("32 frames offset set");
            offset = halfOfFrames;
        }
        else offset = 0;

        // if the frame count is 1, we update the sprite immediatetly
        if (numOfFrames > 1) return;

        // on single farmes we just assign the position and uvs to the mesh
        var currentVertsAndUvs = partAnimationData.vertsAndUVs[currentStart + currentIndex];
        verts[0].Set(currentVertsAndUvs.vert_0.x, currentVertsAndUvs.vert_0.y, zDepth);
        verts[1].Set(currentVertsAndUvs.vert_1.x, currentVertsAndUvs.vert_1.y, zDepth);
        verts[2].Set(currentVertsAndUvs.vert_2.x, currentVertsAndUvs.vert_2.y, zDepth);
        verts[3].Set(currentVertsAndUvs.vert_3.x, currentVertsAndUvs.vert_3.y, zDepth);
        //Debug.Log("vrts 0: " + verts[0] + "; 1: " + verts[1] + "; 2: " + verts[2] + "; 3: " + verts[3]);
        
        uvs[0] = currentVertsAndUvs.uv_0;
        uvs[1] = currentVertsAndUvs.uv_1;
        uvs[2] = currentVertsAndUvs.uv_2;
        uvs[3] = currentVertsAndUvs.uv_3;
        //Debug.Log("uvs 0: " + uvs[0] + "; 1: " + uvs[1] + "; 2: " + uvs[2] + "; 3: " + uvs[3]);

        scale.x = mirrorValue;
        tr.localScale = scale;

        mesh.vertices = verts;
        mesh.uv = uvs;
        if (mirrorValue < 1) offset = halfOfFrames;
        else offset = 0;
        /*
        position.x = positionData.partPositions[currentStart + currentIndex].x * pixelsPerUnit * mirrorValue;
        position.y = -positionData.partPositions[currentStart + currentIndex].y * pixelsPerUnit;
        tr.localPosition = position;

        spriteRenderer.sprite = sprites[currentStart + currentIndex];

        //spriteRenderer.flipX = mirrorValue == -1;
        
        spriteRenderer.transform.localScale = scale;
        */
    }

    private void UpdateAnimatorInternal()
    {
        var currentVertsAndUvs = partAnimationData.vertsAndUVs[currentStart + currentIndex];
        verts[0].Set(currentVertsAndUvs.vert_0.x, currentVertsAndUvs.vert_0.y, zDepth);
        verts[1].Set(currentVertsAndUvs.vert_1.x, currentVertsAndUvs.vert_1.y, zDepth);
        verts[2].Set(currentVertsAndUvs.vert_2.x, currentVertsAndUvs.vert_2.y, zDepth);
        verts[3].Set(currentVertsAndUvs.vert_3.x, currentVertsAndUvs.vert_3.y, zDepth);
        //Debug.Log("vrts 0: " + verts[0] + "; 1: " + verts[1] + "; 2: " + verts[2] + "; 3: " + verts[3]);
        
        uvs[0] = currentVertsAndUvs.uv_0;
        uvs[1] = currentVertsAndUvs.uv_1;
        uvs[2] = currentVertsAndUvs.uv_2;
        uvs[3] = currentVertsAndUvs.uv_3;
        //Debug.Log("uvs 0: " + uvs[0] + "; 1: " + uvs[1] + "; 2: " + uvs[2] + "; 3: " + uvs[3]);

        scale.x = mirrorValue;
        tr.localScale = scale;

        mesh.vertices = verts;
        mesh.uv = uvs;
        if (mirrorValue < 1) offset = halfOfFrames;
        else offset = 0;

        // IF a change in texture is required this is where we shouild check (if the previous textureIDx is same as the current?)
        /*
        // these must be mesh filter assignments 
        position.x = positionData.partPositions[currentStart + currentIndex].x * pixelsPerUnit * mirrorValue;
        position.y = -positionData.partPositions[currentStart + currentIndex].y * pixelsPerUnit;
        tr.localPosition = position;
        //spriteRenderer.flipX = mirrorValue == -1;
        // retian these
        scale.x = mirrorValue; 
        spriteRenderer.transform.localScale = scale;

        // these must be uv updates into meshfilter
        spriteRenderer.sprite = sprites[currentStart + currentIndex];
        */
    }

    public void UpdateFacingRounded(int newFacing)
	{
        // legs can only do 16 turns/facings so we have to round some facings
        if (newFacing > maxFacingValue) newFacing -= 32;
        else if (newFacing < 0) newFacing += 32;

        // the logic here is that the num of faces can be a set of images for half or a whole rotation
        // if it is 17, it is half
        // if it is 32 it is whole
        // if it is 5 or 9, its half but have to round
        // if its 8 or 16, its whole but have to round

        // this should take into account number of faces
        numOfFaces = animationStateData.FacingCount;
        Debug.Log(gameObject.name + " has " + numOfFaces + " faces");
        float divAmount = 1;
        if (numOfFaces <= 8) divAmount = 4;
        else if (numOfFaces <= 16) divAmount = 2;

        newFacing = Mathf.RoundToInt(newFacing / divAmount);
        
        if (newFacing >= numOfFaces)
        {
            mirrorValue = -1;
            facing = (Facing)(numOfFaces + numOfFaces - 2 - newFacing);
        }

        else
		{
            mirrorValue = 1;
            facing = (Facing)newFacing;
        }

    }

    public void AdjustZDepth(float p_zDepth)
	{
        zDepth = p_zDepth;
        // this IS NOT effecient - temporary
        var currentVertsAndUvs = partAnimationData.vertsAndUVs[currentStart + currentIndex];

        verts[0].Set(currentVertsAndUvs.vert_0.x, currentVertsAndUvs.vert_0.y, zDepth);
        verts[1].Set(currentVertsAndUvs.vert_1.x, currentVertsAndUvs.vert_1.y, zDepth);
        verts[2].Set(currentVertsAndUvs.vert_2.x, currentVertsAndUvs.vert_2.y, zDepth);
		verts[3].Set(currentVertsAndUvs.vert_3.x, currentVertsAndUvs.vert_3.y, zDepth);
        Debug.Log("vrts 0: " + verts[0] + "; 1: " + verts[1] + "; 2: " + verts[2] + "; 3: " + verts[3]);
        mesh.vertices = verts;
        //tr.localPosition = position;
    }

    public void TestMesh(PartAnimationData data)
	{
        CreateMesh();
        material.mainTexture = data.textures[0];
        var idx = 10;

        var currentVertsAndUvs = data.vertsAndUVs[idx];
        verts[0].Set(currentVertsAndUvs.vert_0.x, currentVertsAndUvs.vert_0.y, zDepth);
        verts[1].Set(currentVertsAndUvs.vert_1.x, currentVertsAndUvs.vert_1.y, zDepth);
        verts[2].Set(currentVertsAndUvs.vert_2.x, currentVertsAndUvs.vert_2.y, zDepth);
        verts[3].Set(currentVertsAndUvs.vert_3.x, currentVertsAndUvs.vert_3.y, zDepth);
        Debug.Log("vrts 0: " + verts[0] + "; 1: " + verts[1] + "; 2: " + verts[2] + "; 3: " + verts[3]);
        mesh.vertices = verts;
        
        uvs[0] = currentVertsAndUvs.uv_0;
        uvs[1] = currentVertsAndUvs.uv_1;
        uvs[2] = currentVertsAndUvs.uv_2;
        uvs[3] = currentVertsAndUvs.uv_3;
        Debug.Log("uvs 0: " + uvs[0] + "; 1: " + uvs[1] + "; 2: " + uvs[2] + "; 3: " + uvs[3]);
        mesh.uv = uvs;
    }
    /*
    public void LoadPositionData(PartPositionsData p_positionData)
	{
        positionData = p_positionData;
	}

    public void LoadSprites(Sprite[] p_sprites)
	{
        sprites = p_sprites;
	}

    public void ChangeSprite(Sprite newSprite)
	{
        spriteRenderer.sprite = newSprite;
	}*/

}
