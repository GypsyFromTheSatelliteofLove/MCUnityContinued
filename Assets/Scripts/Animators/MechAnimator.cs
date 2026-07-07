using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// THIS IS JUST A PROOF OF CONCEPT still using OOP / Unity based concepts instead of something more DOD
/// Using the animator system for this would be far too taxing load work wise due to the sheer number of sprites (having to create an animator for each SUCKS)
/// BUT this solution is a stop gap; we need someone with more Data Oriented experience to optimize this as this relies on premade sprite sheets
/// It is entirely possible to load the entire sprite sheet into an atlas or 2 per mech and pack it tighter (the included arevalo packer can do it)
/// but this will require a system for indexing which i havent figured out yet :( 
/// </summary>
public class MechAnimator : MonoBehaviour
{
    // WE really shouldnt rely on these monobehaviours for updating the mechs?
    // we might think about arrays in a main animation class that iterates thru all the animation states per part
    // this then fetches the appropriate vertanduv data for that part and feeds that to a meshFilter and material
    // so the class would need to have an array of all the animation states of parts, struct of arrays that contian the arrays of all the vertanduv,  
    // and struct of arrays of all the Texture2Ds of all the parts?
    // example:
    // MeshFilter allLegsMeshFilter[] - updated for verts and uvs
    // Material allLegsMaterials[] - updated for textures
    // Transform allTransforms[] - updated for position
    // NavMeshAgent allAgents[] - queried for path but updated in its own mono (to keep async)

    // PartAnimationStateData allLegsAnimationState[]
    // PartVertAndUVData allLegsVertAndUV[]
    // Texture2D allLegsTextures[]
    // minimum we still have to access all the meshFilters, Materials, and navmeshagents
    // but probs thats at the end
    // in the main thing we just need a 
    // since it wont be much of a problem to add more to the RAM for VertAndUV data, it might be better to just flatten the animation?
    // meaning every animation frame is now a struct with a set of 4 VertAndUVData (for each part)
    // this is stored contigous in the array for each animation state so we can ensure that all the data is close together when we access it in the array
    //  - this only applies to the arms and torso as they will never go out of sync
    // when we calculate paths, it might be better to use the navmeshagent to get a path 
    // if we decide to use the navmeshagent to dictate movement is up for debate... we could just use the path.corners and move via pixel perfect vectors
    // if we do this we need to use NavMeshAgent.Warp(newPosition) to set the next destination 

    // IN THE DOD WAY, WE JUST KEEP all of these in 1 class to operate on them
    // BUT we can still use the traditional unity way of getting navmesh agents to path so we can take advantage of its async behaviour
    // we set the path thru the monobehaviour attached to each unit but we query the path from the main class every frame and base movement from that
    // this way any async behaviour is maintained, but we still keep our 1 large update running with all of its array of structs
    public MechSpriteLoader loader;

    // change publics later
    public MechSHPIndexTable mechID;
    public MechAnimationState animationState;
    public Facing facing;

    public int currentAnimationPosition; // used to transition between states of similar nature


    public PartAnimator legsAnimator;
    public PartAnimator torsoAnimator;
    public PartAnimator l_armAnimator;
    public PartAnimator r_armAnimator;

    private MechAnimationData animationData;
    /*
    // convert to new data structs
    private PartAnimationData_OLD legsAnimData;
    private PartAnimationData_OLD torsoAnimData;
    private PartAnimationData_OLD l_armAnimData;
    private PartAnimationData_OLD r_armAnimData;

    private PartPositionsData positionData; // need some part manager to use these?

    private const string dirOfMechsInResourceFolder = "Sprites/Mechs"; // add the mech name and part name to get sprites

    public string legsAnimDataFileLoc = Path.Combine("Assets/Resources/", dirOfMechsInResourceFolder);
    public string torsosAnimDataFileLoc = Path.Combine("Assets/Resources/", dirOfMechsInResourceFolder);
    public string l_armsAnimDataFileLoc = Path.Combine("Assets/Resources/", dirOfMechsInResourceFolder);
    public string r_armsAnimDataFileLoc = Path.Combine("Assets/Resources/", dirOfMechsInResourceFolder);
    */
    public int frameRate = 16;
    private float secondsPerFrame = 1f;
    [SerializeField]
    private int currentFrameIndex = 0;
    //private int lastFrameIndex = 0;
    private float lastFrameTime = 0;
    private int transitionFactor;
    [SerializeField]
    private int numOfFrames;
    [SerializeField]
    private int runTransitionFactor = 1;

    private const int maxFacingValue = 31; // the max number of facing direction
    private int speed;
    private int commandability;

    private Action pendingCommand; // we can store the last command given and try to call it when unit is ready
    private Action pendingTransition;

    public UnitState unitState;

    public bool clearTransition = false;

    // as far as we know we can store all the animation files in one array per part
    // we can then use a set of indices to indicate the current animation (but this has to be stored somewhere?)
    // if we use the sprite packer to get smaller mem footprint, we will have to index the locations
    // the animation indices are hardcoded, so we can just put these in some database
    
    // needs some sort of database for its part locations? - there will be 26k of these so maybe stored in bytes?
    // 50k bytes... but thats not that big right?
    // we need: short indexStart, byte animLength, byte num of faces, then for every sprite we need byte posX, byte posY
    // we would need 14+ animation structs and ~26k position structs for all the mechs
    // we could write a file that just stores this in memory? so that we can easily access / read it when needed?

    // we need to make it so that this object asks the loader for a set of Texture2Ds per part
    // an array is probably best as every part may need more than 1 texture
    // we store the matching data in an array of structs containing the info on verts and uvs and Texture index
    //

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // REPLACE ALL WITH A CALL TO THE MECHSPRITE LOADER THAT WILL RETURN 3 SETS OF DATA FOR EACH PART:
        // Texture2D[] texture - all the sprites for a part
        // PartAnimationState[] animationStates - all animation states for a part (WILL HAVE ERRORS as deciphering the animations are incomplete
        // PartVertAndUVData[] vertAndUVData - all sprite uv coords and verts for positions and size to display animations

        animationData = loader.GetMechAnimationData(mechID);

        // CONVERT THE NEED FOR POSITION DATA TO VERTANDUVDATALoc = Path.Combine(legsAnimDataFileLoc, (int)mechName + "_" + mechName + "/" + mechName + "_" + "LEGS" + "_AnimData.Json");
        /*string jsonString = File.ReadAllText(legsAnimDataFileLoc);
        var animDataDB = JsonUtility.FromJson<PartAnimationDBSet>(jsonString);
        legsAnimData = new PartAnimationData_OLD(animDataDB);
        legsAnimator.LoadPositionData(new PartPositionsData(animDataDB)); 

        torsosAnimDataFileLoc = Path.Combine(torsosAnimDataFileLoc, (int)mechID + "_" + mechID + "/" + mechID + "_" + "TORSOS" + "_AnimData.Json");
        jsonString = File.ReadAllText(torsosAnimDataFileLoc);
        animDataDB = JsonUtility.FromJson<PartAnimationDBSet>(jsonString);
        torsoAnimData = new PartAnimationData_OLD(animDataDB);
        torsoAnimator.LoadPositionData(new PartPositionsData(animDataDB));

        l_armsAnimDataFileLoc = Path.Combine(l_armsAnimDataFileLoc, (int)mechID + "_" + mechID + "/" + mechID + "_" + "L_ARMS" + "_AnimData.Json");
        jsonString = File.ReadAllText(l_armsAnimDataFileLoc);
        animDataDB = JsonUtility.FromJson<PartAnimationDBSet>(jsonString);
        l_armAnimData = new PartAnimationData_OLD(animDataDB);
        l_armAnimator.LoadPositionData(new PartPositionsData(animDataDB));

        r_armsAnimDataFileLoc = Path.Combine(r_armsAnimDataFileLoc, (int)mechID + "_" + mechID + "/" + mechID + "_" + "R_ARMS" + "_AnimData.Json");
        jsonString = File.ReadAllText(r_armsAnimDataFileLoc);
        animDataDB = JsonUtility.FromJson<PartAnimationDBSet>(jsonString);
        r_armAnimData = new PartAnimationData_OLD(animDataDB);
        r_armAnimator.LoadPositionData(new PartPositionsData(animDataDB));
        */

        legsAnimator.AssignPartAnimationData(animationData.legsAnimationData);
        torsoAnimator.AssignPartAnimationData(animationData.torsoAnimationData);
        l_armAnimator.AssignPartAnimationData(animationData.l_armAnimationData);
        r_armAnimator.AssignPartAnimationData(animationData.r_armAnimationData);

        Stop();
        torsoAnimator.AdjustZDepth(-.01f);
        
        animationState = MechAnimationState.IDLE;
        secondsPerFrame = 1f / frameRate;

        // maybe its a bit faster to just load the sprites into a texture 2D in memory rather than in disk
        // we have to get our sprite loader and ask for the relevant mech sprites
        // then we generate our files from there? instead of having to construct sprite sheets?
    }

    // Update is called once per frame
    void Update()
    {
        // temp move to some main update later
        // later these inputs should come from a controller
        if (Input.GetKeyDown(KeyCode.RightArrow))
		{
            facing = (Facing)(((int)facing + 1) % (maxFacingValue + 1));
            ChangeFacing(); // all of these should be done at the end... input is just what needs to be changed?
		}

        else if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
            
            facing = facing == 0 ? (Facing)(maxFacingValue) : facing - 1;
            ChangeFacing();

        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
		{
            if (animationState == MechAnimationState.IDLE)
			{
                transitionFactor = 1;
                BeginWalk();
			}

            else if (animationState == MechAnimationState.WALK)
			{
                transitionFactor = runTransitionFactor;
                pendingTransition = BeginRun;
			}

        }

        else if (Input.GetKeyDown(KeyCode.DownArrow))
		{
            //animationState = animationState == 0 ? MechAnimationState.IDLE : animationState - 1;
            //UpdateAnimation();
            if (animationState == MechAnimationState.RUN)
            {
                transitionFactor = runTransitionFactor;
                pendingTransition = RunToWalk;
            }

            else if (animationState == MechAnimationState.WALK)
            {
                transitionFactor = 1;
                pendingTransition = StopWalk;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
		{
            animationState = (MechAnimationState)(((int)animationState + 1) % ((int)MechAnimationState.IDLE + 1));
            UpdateAnimation();
        }

        // after input we should iterate over the necessary data arrays:
        // input just determines facing and the animation to play
        // update all animation info ? for all the animated objects, update the current animation if needed, iterate the animation index
        // update all position info (3 floats for x y z (depth))
        // last we just update the mesh uvs... if the mesh is constant size ; else we need to update the size based on the uv image size
        // then we iterate over all the mesh filters, update them to the size of the rect in a dictionary? or at least an array of rect type
        // then we update the meshfilter size and the uv to reflect the position of the animation

        if (numOfFrames <= 1) return;

        if (pendingTransition != null && currentFrameIndex + 1 == (numOfFrames / transitionFactor))
        {
            Debug.Log("transition attempt");
            pendingTransition.Invoke();
            if (clearTransition) pendingTransition = null;
        }

        if (lastFrameTime >= secondsPerFrame)
        {
            currentFrameIndex = (currentFrameIndex + 1) % numOfFrames;
            
            legsAnimator.UpdateFrames(currentFrameIndex);
            torsoAnimator.UpdateFrames(currentFrameIndex);
            l_armAnimator.UpdateFrames(currentFrameIndex);
            r_armAnimator.UpdateFrames(currentFrameIndex);
            lastFrameTime = 0;

        }

        lastFrameTime += Time.deltaTime;
    }

    // TEMPORARY
    public void OnInput()
	{

	}


    public void SwitchAnimation()
	{
		switch (animationState)
		{
            case MechAnimationState.IDLE:
                //spriteRenderer.sprite = idles[(int)facing];
                break;
            default:
                break;
		}
	}


    public void ChangeFacing(int facingAmount)
	{
        var newFacing = (int)facing + facingAmount;
        if (newFacing > maxFacingValue) newFacing -= 32;
        else if (newFacing < 0) newFacing += 32;

        // i need to find the index of the animation at the current animation index with the new facing

	}

    public void ChangeAnimationState(MechAnimationState newState)
    {
        // check if the animation state can transition to the new state?
        // if yes, check if animation can transition immediate
        // transition to the new state at the exit point or immediate
        // if no, check if the animation can transition to another state closer to this state??
        // or do we just have forking decisions for transitions?
        // idle -> turn / start walk / fall down / shutdown / jump
        // start walk -> walk / fall down / jump? 
        // walk -> start run / stop(->idle) / fall down / jump?
        // start run -> run / fall down / jump
        // run -> run to walk / fall down / jump
        // fall down -> roll over (->get up) / get up (->idle)
    }

    public void BeginWalk()
	{
        Debug.Log("begin walk");
        animationState = MechAnimationState.BEGIN_WALK;
        pendingTransition = Walk;
        clearTransition = false;
        StartTransition();
        //UpdateAnimationWithCallback(Walk);
    }

    public void Walk()
	{
        Debug.Log("walk");
        animationState = MechAnimationState.WALK;
        StartAnimation();
        clearTransition = true;
	}

    public void BeginRun()
	{
        Debug.Log("begin run");
        animationState = MechAnimationState.BEGIN_RUN;
        pendingTransition = Run;
        transitionFactor = 1;
        clearTransition = false;
        StartTransition();
        //UpdateAnimationWithCallback(Run);
	}

    public void Run()
	{
        Debug.Log("run");
        animationState = MechAnimationState.RUN;
        StartAnimation();
        clearTransition = true;
    }

    public void RunToWalk()
	{
        animationState = MechAnimationState.RUN_TO_WALK;
        pendingTransition = Walk;
        transitionFactor = 1;
        clearTransition = false;
        StartTransition();
    }

    public void StopWalk()
	{
        animationState = MechAnimationState.STOP_WALK;
        pendingTransition = Stop;
        clearTransition = false;
        StartTransition();
    }

    public void Stop()
	{
        animationState = MechAnimationState.IDLE;
        legsAnimator.SetStateToSingleFrameStateAtID(1);
        legsAnimator.StartAnimator();

        torsoAnimator.SetStateToSingleFrameStateAtID(1);
        torsoAnimator.StartAnimator();

        l_armAnimator.SetStateToSingleFrameStateAtID(1);
        l_armAnimator.StartAnimator();

        r_armAnimator.SetStateToSingleFrameStateAtID(1);
        r_armAnimator.StartAnimator();

        clearTransition = true;

        numOfFrames = 1;
    }

    public void FallDown()// can be done anytime
	{
        // set speed to zero
        // trigger the fall animation that goes into the roll over and or getup animation
	}

    public void Jump()
	{
        // check if CANT_MOVE; store the command, return 
        if (unitState == UnitState.CANT_MOVE)
		{
            pendingCommand = Jump; // store this
		}

        // find the jump animation
	}

    public void ChangeFacing()
	{

        legsAnimator.UpdateFacingRounded((int)facing);
        legsAnimator.UpdateAnimator();

        torsoAnimator.UpdateFacingRounded((int)facing);
        torsoAnimator.UpdateAnimator();

        l_armAnimator.UpdateFacingRounded((int)facing);
        l_armAnimator.UpdateAnimator();

        r_armAnimator.UpdateFacingRounded((int)facing);
        r_armAnimator.UpdateAnimator();

        if (animationState < MechAnimationState.IDLE && l_armAnimator.CurrentFacingCount != 32)
        {
            torsoAnimator.AdjustZDepth(-.01f);
            l_armAnimator.AdjustZDepth(0);
            r_armAnimator.AdjustZDepth(-0.02f);
        }

        else if (facing > Facing.S && facing < Facing.N)
        {
            torsoAnimator.AdjustZDepth(-.01f);
            l_armAnimator.AdjustZDepth(0);
            r_armAnimator.AdjustZDepth(-0.02f);
        }
        else if (facing > Facing.N && facing <= Facing.SW_S_3)
        {
            torsoAnimator.AdjustZDepth(-.01f);
            l_armAnimator.AdjustZDepth(-0.02f);
            r_armAnimator.AdjustZDepth(0);
        }
    }

    public void StartTransition()
	{
        Debug.Log("starting " + animationState);
        currentFrameIndex = 0;

        legsAnimator.SetState((int)animationState);
        legsAnimator.StartAnimator();

        torsoAnimator.SetState((int)animationState);
        torsoAnimator.StartAnimator();

        l_armAnimator.SetState((int)animationState);
        r_armAnimator.SetState((int)animationState);
        if (facing > Facing.N)
		{
            
            l_armAnimator.StartTransition();
            r_armAnimator.StartTransition();
            
            return;
        }

        l_armAnimator.StartAnimator();
        r_armAnimator.StartAnimator();

        numOfFrames = legsAnimator.CurrentFrameCount;
    }

    public void StartAnimation()
	{
        Debug.Log("starting " + animationState);
        currentFrameIndex = 0;
        
        legsAnimator.SetState((int)animationState);
        legsAnimator.StartAnimator();

        torsoAnimator.SetState((int)animationState);
        torsoAnimator.StartAnimator();

        l_armAnimator.SetState((int)animationState);
        l_armAnimator.StartAnimator();

        r_armAnimator.SetState((int)animationState);
        r_armAnimator.StartAnimator();

        numOfFrames = legsAnimator.CurrentFrameCount;
    }

    public void UpdateAnimation()
	{

        legsAnimator.SetState((int)animationState);
        legsAnimator.UpdateAnimator();

        torsoAnimator.SetState((int)animationState);
        torsoAnimator.UpdateAnimator();

        l_armAnimator.SetState((int)animationState);
        l_armAnimator.UpdateAnimator();
        
        r_armAnimator.SetState((int)animationState);
        r_armAnimator.UpdateAnimator();

        numOfFrames = legsAnimator.CurrentFrameCount;
    }

    public void LoadResources()
	{
        //l_armSprites = GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + mechName + "/LARMS");
        //r_armSprites = GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + mechName + "/RARMS");
        /*
        legsAnimator.LoadSprites(GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + (int)mechID + "_" + mechID + "/LEGS"));
        torsoAnimator.LoadSprites(GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + (int)mechID + "_" + mechID + "/TORSOS"));
        l_armAnimator.LoadSprites(GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + (int)mechID + "_" + mechID + "/L_ARMS"));
        r_armAnimator.LoadSprites(GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + (int)mechID + "_" + mechID + "/R_ARMS"));
        */
        if (loader == null)
		{
            Debug.Log("no loader!");
            return;
		}

        animationData = loader.GetMechAnimationData(mechID);

    }

    public void TestLegsMesh()
	{
        if (animationData.Equals(default(MechAnimationData))) LoadResources();

        legsAnimator.TestMesh(animationData.legsAnimationData);
    }

    private Sprite[] GetSpritesInFolder(string path)
	{
        List<Sprite> tempSprites = new List<Sprite>();

        foreach (Sprite sprite in Resources.LoadAll<Sprite>(path))
        {
            tempSprites.Add(sprite);
            //else Debug.Log("sprite size too small, skipping " + sprite.name + " w " + sprite.rect.width + " x h " + sprite.rect.height);
        }
        //tempGroundTiles.Sort((x, y) => x.name.CompareTo(y.name));
        return tempSprites.ToArray();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MechAnimator))]
    public class MechAnimatorEditor : Editor
    {

        MechAnimator editor;

        public override void OnInspectorGUI()
        {
            editor = target as MechAnimator;

            DrawDefaultInspector();

            if (GUILayout.Button("Load Resources")) editor.LoadResources();
            if (GUILayout.Button("Test Legs Mesh")) editor.TestLegsMesh();
            if (GUILayout.Button("Update Anim")) editor.UpdateAnimation();
        }

    }
#endif
}

/*
 * what we can do is have all the updates at the start of the frame
 * input from player is first resolved - any new units that are to animate are added to the swapback array
 * non animating units might be swapped back
 * input from ai is resolved - the logic reacts to player
 * same adding of units that are to animate, removing of units that are no longer animating
 * we have 2 main arrays: moving units - for pathfinding and animating units to update animation
 * 
 * then we resolve any other damage or effect? falling? getting up? any units that are not navigating will skip their navigation call? should this precede input?
 * 
 * then we resolve anypath finding? in this case we just check the direction for the facing and the speed for the animation
 * the pathfinder will resolve its own data but we just need the direction and speed
 * also we will only call the pathfinders of units that are in the moving array
 * it might be faster to use built in calculators for direction
 * we need an algo to translate directoin vector2 to an index for facing and speed to transalte to an index for animation 
 * the movement array might have an index to the animating array since it does need updating based on movement?
 *
 * this allows us to update the animation with the appropriate index, calculated from the facing and animation state
 * we set the tick for the animation (currentFrame++) 
 * basically 2 things we update here, the tick and the state if necessary
 * - we still need some sort of array to reference what the animation index should be per facing and state
 *
 * then we iterate over all the units/objects that are to animate this turn
 * we go thru their parts which might be stored in parallel arrays? all legs, all torsos, all arms, all tanks, turrets etc
 * we might have to go thru 2 separate arrays for mechs and vehicles? since they have different part counts?
 * for each part, we plug in the values of animation
 * they have to retrieve from 
 * 
 * how to construct this data? 
 * we get this info from an index that should represent the index for every frame in the sheet
 * when we print the sheet, we need animation data that represents this info when the sprites are printed unto the sheet
 * 
 * at the end we need every animating part to get a 6 to 8 bytes that indicates the location of the sprite in the texture
 * x and y and width and height and its position in the world
 * we convert the x, y, width, height, and position, to world coords by * .01f 
 * we convert again x, y, width, height to uv each axis by * 1/textureAxisLength
 * 
 * then finally we pass all that to the rendering element which is a loop over all the corresponding meshes to resize their verts and adjust normals
 */