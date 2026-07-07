using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public class MechAnimator_OLD : MonoBehaviour
{

    // change publics later
    public MechSHPIndexTable mechID;
    public MechAnimationState animationState;
    public Facing facing;

    public int currentAnimationPosition; // used to transition between states of similar nature


    public PartAnimator_OLD legsAnimator;
    public PartAnimator_OLD torsoAnimator;
    public PartAnimator_OLD l_armAnimator;
    public PartAnimator_OLD r_armAnimator;

    //private MechAnimationData animationData;
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

        //animationData = loader.GetMechAnimationData(mechID);

        // CONVERT THE NEED FOR POSITION DATA TO VERTANDUVDATALoc = Path.Combine(legsAnimDataFileLoc, (int)mechName + "_" + mechName + "/" + mechName + "_" + "LEGS" + "_AnimData.Json");
        legsAnimDataFileLoc = Path.Combine(legsAnimDataFileLoc, (int)mechID + "_" + mechID + "/" + mechID + "_" + "LEGS" + "_AnimData.Json");
        string jsonString = File.ReadAllText(legsAnimDataFileLoc);
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
        
        
        Stop();
        Debug.Log("stopping");
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
        legsAnimator.ChangeCurrentAnimation(legsAnimData.singleDatas[1]);
        legsAnimator.StartAnimator();

        torsoAnimator.ChangeCurrentAnimation(torsoAnimData.singleDatas[1]);
        torsoAnimator.StartAnimator();

        l_armAnimator.ChangeCurrentAnimation(l_armAnimData.singleDatas[1]);
        l_armAnimator.StartAnimator();

        r_armAnimator.ChangeCurrentAnimation(r_armAnimData.singleDatas[1]);
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

        legsAnimator.ChangeCurrentAnimation(legsAnimData.animationDatas[(int)animationState]);
        legsAnimator.StartAnimator();

        torsoAnimator.ChangeCurrentAnimation(torsoAnimData.animationDatas[(int)animationState]);
        torsoAnimator.StartAnimator();

        l_armAnimator.ChangeCurrentAnimation(l_armAnimData.animationDatas[(int)animationState]);
        r_armAnimator.ChangeCurrentAnimation(r_armAnimData.animationDatas[(int)animationState]);
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

        legsAnimator.ChangeCurrentAnimation(legsAnimData.animationDatas[(int)animationState]);
        legsAnimator.StartAnimator();

        torsoAnimator.ChangeCurrentAnimation(torsoAnimData.animationDatas[(int)animationState]);
        torsoAnimator.StartAnimator();

        l_armAnimator.ChangeCurrentAnimation(l_armAnimData.animationDatas[(int)animationState]);
        l_armAnimator.StartAnimator();

        r_armAnimator.ChangeCurrentAnimation(r_armAnimData.animationDatas[(int)animationState]);
        r_armAnimator.StartAnimator();

        numOfFrames = legsAnimator.CurrentFrameCount;
    }

    public void UpdateAnimation()
    {

        legsAnimator.ChangeCurrentAnimation(legsAnimData.animationDatas[(int)animationState]);
        legsAnimator.UpdateAnimator();

        torsoAnimator.ChangeCurrentAnimation(torsoAnimData.animationDatas[(int)animationState]);
        torsoAnimator.UpdateAnimator();

        l_armAnimator.ChangeCurrentAnimation(l_armAnimData.animationDatas[(int)animationState]);
        l_armAnimator.UpdateAnimator();

        r_armAnimator.ChangeCurrentAnimation(r_armAnimData.animationDatas[(int)animationState]);
        r_armAnimator.UpdateAnimator();

        numOfFrames = legsAnimator.CurrentFrameCount;
    }

    public void LoadResources()
    {
        //l_armSprites = GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + mechName + "/LARMS");
        //r_armSprites = GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + mechName + "/RARMS");
        legsAnimator.LoadSprites(GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + (int)mechID + "_" + mechID + "/LEGS"));
        torsoAnimator.LoadSprites(GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + (int)mechID + "_" + mechID + "/TORSOS"));
        l_armAnimator.LoadSprites(GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + (int)mechID + "_" + mechID + "/L_ARMS"));
        r_armAnimator.LoadSprites(GetSpritesInFolder(dirOfMechsInResourceFolder + "/" + (int)mechID + "_" + mechID + "/R_ARMS"));

     
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
    [CustomEditor(typeof(MechAnimator_OLD))]
    public class MechAnimator_OLDEditor : Editor
    {

        MechAnimator_OLD editor;

        public override void OnInspectorGUI()
        {
            editor = target as MechAnimator_OLD;

            DrawDefaultInspector();

            if (GUILayout.Button("Load Resources")) editor.LoadResources();
            if (GUILayout.Button("Update Anim")) editor.UpdateAnimation();
        }

    }
#endif

}
