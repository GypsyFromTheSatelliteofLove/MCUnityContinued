using UnityEngine;

public class PartAnimator_OLD : MonoBehaviour
{
    public Texture2D palette; // not yet assigned dynamically - to do
    
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Sprite[] sprites; // all the sprites of this part
    private Transform tr;
    private Vector3 position; // these will be converted to meshFilter
    private Vector3 scale = Vector3.one; // retain
    // need a reference to the material

    private PartPositionsData positionData; // convert to PartVertAndUVData
    private AnimationData currentAnimationData; // convert to PartAnimationStateData
    //private PartAnimationData partAnimationData;
    //private PartAnimationStateData animationStateData;
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
        var shader = Shader.Find("MechCommanderUnity/PaletteSwapLookup");
        
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.material = new Material(shader);    
        spriteRenderer.material.SetTexture("_PaletteTex", palette);    
        tr = transform;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // instantiate new material?
        // set the texture as returned by the sprite loader and passed by the mechanimator
    }
    /*
    public void AssignPartAnimationData(PartAnimationData data)
    {
        partAnimationData = data;

    }*/

    public void UpdateFrames(int frameIndex)
    {
        currentIndex = (frameIndex + (useTestOffset ? testOffset : offset)) % numOfFrames;

        UpdateAnimatorInternal();
    }
    public void ChangeCurrentAnimation(AnimationData newAnimData)
	{
        currentAnimationData = newAnimData;
        numOfFrames = currentAnimationData.NumOfImages;
        numOfFaces = currentAnimationData.NumOfFaces;
        halfOfFrames = numOfFrames / 2;
        Debug.Log("playing " + numOfFrames + " frames starting at " + newAnimData.IndexOfStart);
	}


    // WE have to take into account how the animation started
    // in the game we just flip the sprites; we dont offset the legs so they start the same, we just make the transition into the flip
    // BUT we have to offset the arms to connect to the animation
    public void StartTransition()
    {
        currentStart = currentAnimationData.IndexOfStart + ((byte)facing * numOfFrames);
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
        currentStart = currentAnimationData.IndexOfStart + ((byte)facing * numOfFrames);
        // if we have 360 degree part, we have to offset on the mirrored side
        //Debug.Log("setting start at " + currentStart);
        if (numOfFaces == 32 && (int)facing > 16)
        {
            Debug.Log("32 frames offset set");
            offset = halfOfFrames;
        }
        else offset = 0;

        // if the frame count is 1, we update the sprite immediatetly
        if (numOfFrames > 1) return;
        /*
        if (mirrorValue < 1) offset = halfOfFrames;
        else offset = 0;
        */
        position.x = positionData.partPositions[currentStart + currentIndex].x * pixelsPerUnit * mirrorValue;
        position.y = -positionData.partPositions[currentStart + currentIndex].y * pixelsPerUnit;
        tr.localPosition = position;

        spriteRenderer.sprite = sprites[currentStart + currentIndex];

        scale.x = mirrorValue;
        tr.localScale = scale;
        //spriteRenderer.flipX = mirrorValue == -1;

        //spriteRenderer.transform.localScale = scale;

    }

    private void UpdateAnimatorInternal()
    {
        /*
        if (mirrorValue < 1) offset = halfOfFrames;
        else offset = 0;
        */
        // IF a change in texture is required this is where we shouild check (if the previous textureIDx is same as the current?)
        // these must be mesh filter assignments 
        position.x = positionData.partPositions[currentStart + currentIndex].x * pixelsPerUnit * mirrorValue;
        position.y = -positionData.partPositions[currentStart + currentIndex].y * pixelsPerUnit;
        tr.localPosition = position;
        //spriteRenderer.flipX = mirrorValue == -1;
        // retian these
        //spriteRenderer.transform.localScale = scale;
        scale.x = mirrorValue;
        tr.localScale = scale;
        Debug.Log("playing anim at " + (currentIndex + currentStart));
		// these must be uv updates into meshfilter
		spriteRenderer.sprite = sprites[currentStart + currentIndex];
        
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
        numOfFaces = currentAnimationData.NumOfFaces;
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
        tr.localPosition = position;
    }

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
    }
}