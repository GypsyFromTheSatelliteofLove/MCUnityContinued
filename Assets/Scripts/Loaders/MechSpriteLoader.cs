using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using MechCommanderUnity.API;
using MechCommanderUnity.Utility;
/// <summary>
/// Can load and get sprites for mechs; can also print them if needed into pngs
/// </summary>
public class MechSpriteLoader : MonoBehaviour
{
    // STILL VERY UNOPTIMIZED AT THE MOMENT
    // we need some way to atlas the textures and only size it to the size of the needed mechs - this is harder as the indices will be dynamic per animation
    [SerializeField]
    private GameFilesLoader gameFilesLoader;
    private FileManager fileManager;

    private string[] mechPakFiles = new string[]
    {
        "SPRITES/LARMS90.PAK",
        "SPRITES/LEGS90.PAK",
        "SPRITES/RARMS90.PAK",
        "SPRITES/TORSOS90.PAK"
    };

    public MechSHPIndexTable mechIndex; // allows us to set the mech to load in the inspector

    private List<MCBitmap> bitmaps = new List<MCBitmap>();
    private List<sbyte> positionsX = new List<sbyte>();
    private List<sbyte> positionsY = new List<sbyte>();

    public PartIndex[] partsToMergeForAtlas;
    public PartIndex currentPartIndex;
    public int targetAtlasSize = 4096;
    public string pathToResourceFolder = "Assets/Resources/Sprites/Mechs";
    
    private const string spriteDataFileExt = "_spriteData.Json";
    private const string spriteInfoFIleExt = "_spriteInfo.Json";
    private const string vertsAndUVFileExt = "_vertsAndUV.bytes";
    private const string paletteDirectory = @"palette\HB.PAL";
    private const float pixelsPerUnit = 1f / 100f;
    private const int shpID = 0x30312E31; // checks if the pak file is an shp

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // loader that goes thru all the mech parts in the pak files and just gets a MCbitmap list
    // IN FUTURE we can cache once we get the bitmaps and arrange them in an atlas (a basic atlas without any packing)
    // how to split up atlas? at the start do we just make one per part?? - might be best for now tho it will need optimizing eventually
    // we have to learn more about the files as well... 
    
    // create another method to return mech animation data
    public MechAnimationData GetMechAnimationData(MechSHPIndexTable mechToGet)
    {
        mechIndex = mechToGet;

        currentPartIndex = PartIndex.LEGS;
        var tempLegs = GetPartAnimationData(mechPakFiles[(int)currentPartIndex]);
        currentPartIndex = PartIndex.TORSOS;
        var tempTorsos = GetPartAnimationData(mechPakFiles[(int)currentPartIndex]);
        currentPartIndex = PartIndex.L_ARMS;
        var tempL_arms = GetPartAnimationData(mechPakFiles[(int)currentPartIndex]);
        currentPartIndex = PartIndex.R_ARMS;
        var tempR_arms = GetPartAnimationData(mechPakFiles[(int)currentPartIndex]);

        var tempdata = new MechAnimationData(tempLegs, tempTorsos, tempL_arms, tempR_arms);

        return tempdata;
	}

    // return part animation data
    private PartAnimationData GetPartAnimationData(string partPath)
    {
        string path = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
        string pathData = Path.Combine(path, currentPartIndex.ToString() + spriteDataFileExt);
        string pathVertsAndUV = Path.Combine(path, currentPartIndex.ToString() + vertsAndUVFileExt);
        string pathSpriteInfos = Path.Combine(path, currentPartIndex.ToString() + spriteInfoFIleExt);

        // if no directory exists, receate the directories (will not override any existing)
        if (!Directory.Exists(path))
		{
            CreateDirectoryStructure();
		}
        // here add a check if the mech is already cached; 
        else if (File.Exists(pathData) && File.Exists(pathVertsAndUV))
		{
            // we have a cahced file, retrieve and return;
            Debug.Log("Cache found! loading from cache");
            string jsonString = File.ReadAllText(pathData);
            if (!string.IsNullOrEmpty(jsonString))
			{
                SpriteAnimationStatesSaveData cachedData = JsonUtility.FromJson<SpriteAnimationStatesSaveData>(jsonString);
                PartVertAndUVData[] cachedVertsAndUvs = DeserializeVertsAndUV(pathVertsAndUV);
                
                return ConvertSpriteInfoArrayToPartAnimation(cachedData, cachedVertsAndUvs);
            }

        }
        // only setup a file manager if it is absolutely necessary! it takes up to 21 secs or more to load one
        SetupFileManager();

        // if no cached, retrieve data from the pak files (SLOW)
        Debug.Log("no cache found, loading from pak");
        bitmaps.Clear();
        positionsX.Clear();
        positionsY.Clear();
        
        var currentPaletteDir = paletteDirectory;
        int mechIndexAdjustedForPart = (int)mechIndex;
        if (partPath.Contains("LEGS"))
        {
            // adjust for legs
            if (mechIndex == MechSHPIndexTable.HUNCHBACK_IIC) mechIndexAdjustedForPart = 1; // if mechindex == 16, legsindex = 1
            else if (mechIndex == MechSHPIndexTable.FIRESTARTER) mechIndexAdjustedForPart = 7; // if mechindex == 11, legsindex = 7
            else if (mechIndex >= MechSHPIndexTable.VULTURE) mechIndexAdjustedForPart -= 5;// if mechindex >= 15, legsindex = mechindex - 5
            else if (mechIndex >= MechSHPIndexTable.MASAKARI) mechIndexAdjustedForPart -= 3; // if mechindex >= 12, legsindex = mechindex - 3
            else if (mechIndex >= MechSHPIndexTable.ULLER) mechIndexAdjustedForPart -= 2; // if mechindex >= 6, legsindex = mechindex - 2
        }

        var outerPak = new PakFile(Path.Combine(gameFilesLoader.localMCGPath, partPath), fileManager.File(currentPaletteDir));
        var mechPartPak = new PakFile(outerPak.GetFileInner(mechIndexAdjustedForPart), fileManager.File(currentPaletteDir));
        var length = mechPartPak.PakInnerFileCount;
     
        int headerLength = 6; // this is the header length for mechs (6 bytes dedicated to some info)
        byte[] mechData;

        int indexOfAnimationStart = 0;
        int singleFrameStartIdx = 0;
        int numOfImages = 0; // this is the number of sprites in an animation
        int numOfFaces = 0; // this the current facing and will also give the final number of faces
        bool singleImagesStart = false;
        MechAnimationState animationState = MechAnimationState.SHUT_DOWN;
        List<int> validIndices = new List<int>(); // just to keep track of count when there are null pakfiles - used to have more function but kept for now
        List<PartAnimationStateData> animationDatas = new List<PartAnimationStateData>();
        List<PartAnimationStateData> singleFrameAnimDatas = new List<PartAnimationStateData>();

        MCBitmap spriteBmp;
        Debug.Log("attempting to get " + mechIndex + " from " + partPath + " pak count " + length);

        var mechName = mechIndex.ToString() + "_" + currentPartIndex.ToString();
        for (int i = 0; i < length; i++)
        {
            mechData = mechPartPak.GetFileInner(i); // main function to get the inner content
            if (mechData == null)
            {
                //Debug.Log("mech dat is null");
                continue;
            }
            // test if the file is a Shp file using unique ID: 0x30312E31
            if (BitConverter.ToInt32(mechData.AsSpan(0, 4)) != shpID && BitConverter.ToInt32(mechData.AsSpan(headerLength, 4)) != shpID)
            {
                Debug.Log("shp ID error " + shpID + " vs " + BitConverter.ToInt32(mechData.AsSpan(headerLength, 4)));
                continue;
            }

            var mech = new ShpFile(mechData);
            if (mech.ImgLength == 1)
            {
                if (!singleImagesStart && (numOfImages > 1 || i == 0))
                {
                    singleImagesStart = true;
                    singleFrameStartIdx = validIndices.Count;
                }
            }

            else if (mech.ImgLength > 1)
            {
                // THINK ABOUT THIS: how are animation states called back up? we dont have labels per se... do we need to depend on indexing?
                if (singleImagesStart) // we only put in the animation if the count changes
                {
                    singleImagesStart = false;
                    singleFrameAnimDatas.Add(new PartAnimationStateData((ushort)singleFrameStartIdx, 1, (byte)(validIndices.Count - singleFrameStartIdx)));
               
                }
                // whenever the count of frames changes, we are (probably) in a new set of animations - make sure??
                if ((numOfImages != mech.ImgLength && numOfFaces >= 4) || // 4 is the minimum? will have to check further
                    (animationState > 0 && animationState <= MechAnimationState.HOBBLE && numOfFaces >= animationDatas[0].FacingCount)) // sometimes we go past because same count 
                {
                    animationDatas.Add(new PartAnimationStateData((ushort)indexOfAnimationStart, (byte)numOfImages, (byte)numOfFaces));
                    numOfFaces = 0; // restart
                    indexOfAnimationStart = validIndices.Count;
                    animationState++;
                }
                else if (numOfImages != mech.ImgLength) indexOfAnimationStart = validIndices.Count;

                numOfFaces++;
                numOfImages = mech.ImgLength;
            }
            //outputBitmap = mech.GetBitMap(0); // assign a default
            // add all the bitmaps to the main list
            for (int j = 0; j < mech.ImgLength; j++)
            {
                spriteBmp = mech.GetBitMap(j); // this is the index of the animation

                if (spriteBmp == null)
                {
                    //Debug.Log("bmp is null");
                    continue;
                }

                validIndices.Add(i); // this will total the number of images and mark animations as duplicate indices
                bitmaps.Add(spriteBmp);
                // THESE positions have to be considered when we make the uv/mesh array
                positionsX.Add((sbyte)mech.GetHeaderStartX(j));
                positionsY.Add((sbyte)mech.GetHeaderStartY(j));
                
            }

        }

        // last check if an animation was found at the end of the loop
        if (indexOfAnimationStart != animationDatas[animationDatas.Count - 1].StartIndex)
        {
            animationDatas.Add(new PartAnimationStateData((ushort)indexOfAnimationStart, (byte)numOfImages, (byte)numOfFaces));
        }

        // we should also add the animation state for reverse which is walk at negative speed
        // we should also add the idle to reverse which is idle to walk at negative
        // we should also add reverse to idle

        // if we reach this point we cache the bitmaps - we use references to get the spriteInfos and bitmaps out
        List<MCBitmap> sheetBitmaps = new List<MCBitmap>();
        List<SpriteInfoSaveData> spriteInfos = new List<SpriteInfoSaveData>();

        CacheDynamicallyPlacedMCBitmap(0, 0, ref sheetBitmaps, ref spriteInfos);

        PartVertAndUVData[] vertsAndUVs = new PartVertAndUVData[spriteInfos.Count];

		for (int i = 0; i < spriteInfos.Count; i++)
		{
            var tempVertAndUV = new PartVertAndUVData();
            tempVertAndUV.topLeftVert = spriteInfos[i].verts[0];
            tempVertAndUV.bottomRightVert = spriteInfos[i].verts[3];
            //tempVertAndUV.vert_2 = spriteInfos[i].verts[2];
            //tempVertAndUV.vert_3 = spriteInfos[i].verts[3];

            tempVertAndUV.uv_0 = spriteInfos[i].uvs[0];
            tempVertAndUV.uv_1 = spriteInfos[i].uvs[1];
            tempVertAndUV.uv_2 = spriteInfos[i].uvs[2];
            tempVertAndUV.uv_3 = spriteInfos[i].uvs[3];

            tempVertAndUV.textureIndex = (byte)spriteInfos[i].sheetIndex;
            // WE HAVE TO ADD depth placement per part here??
            vertsAndUVs[i] = tempVertAndUV;
		}

        // we can move these following coversions to its own function later
        Texture2D[] textures = new Texture2D[sheetBitmaps.Count];
		for (int i = 0; i < sheetBitmaps.Count; i++)
		{
            textures[i] = ImageProcessing.MakeIndexedTexture2D(sheetBitmaps[i]);
		}

        var saveData = new SpriteAnimationStatesSaveData();
        saveData.pathsToMCBitmap = new string[sheetBitmaps.Count];
		for (int i = 0; i < sheetBitmaps.Count; i++)
		{
            var savePath = Path.Combine(path, currentPartIndex.ToString() + "_" + i + ".bytes"); // we will store this for the save;
            saveData.pathsToMCBitmap[i] = savePath;
            sheetBitmaps[i].Serialize(savePath); // save the bitmaps to their own files
        }

        saveData.animationStates = new SpriteAnimationStateSaveData[animationDatas.Count];
        animationState = MechAnimationState.SHUT_DOWN; // NOT YET FIXED
        for (int i = 0; i < animationDatas.Count; i++)
		{
            saveData.animationStates[i] = new SpriteAnimationStateSaveData(animationState.ToString(), animationDatas[i]);
            animationState++; // bug here since we dont really know how to decipher all the animation states in a pak file yet
		}

        saveData.singleFrameAnimationStates = new SpriteAnimationStateSaveData[singleFrameAnimDatas.Count];
        animationState = MechAnimationState.SHUTDOWN_IDLE; // NOT YET FIXED - used to label the single frame anim states
        for (int i = 0; i < singleFrameAnimDatas.Count; i++)
		{
            saveData.singleFrameAnimationStates[i] = new SpriteAnimationStateSaveData(animationState.ToString(), singleFrameAnimDatas[i]);
            animationState++; // bug here since we dont really know how to decipher all the animation states in a pak file yet
		}

        //saveData.spriteInfos = spriteInfos.ToArray();
        SpriteInfoArraySaveData spriteInfoSave = new SpriteInfoArraySaveData();
        spriteInfoSave.spriteInfos = spriteInfos.ToArray();

        File.WriteAllText(pathSpriteInfos, JsonUtility.ToJson(spriteInfoSave, true)); // save for later
        File.WriteAllText(pathData, JsonUtility.ToJson(saveData, true)); // save the entire part data in one file

        SerializeVertsAndUvs(vertsAndUVs, pathVertsAndUV); 

        return new PartAnimationData(animationDatas.ToArray(), singleFrameAnimDatas.ToArray(), textures, vertsAndUVs); 

        // HERE we have to call the merging of the data and MCBitmap
        // we go thru all the bitmaps, calculate uv and mesh from position data in shp
        // we create the initial Texture2D by just cramming as many sprites in a sheet as we can (never mind the low volume sheets for now)
        // then we tell the PartAnimationState to store what index the texture is (other info should be initialized before)
        // then we also tell the PartVertAndUVData what the positions + dimensions and uvs are
        // the how to sync the PartVertAndUVData with multiple array indices?
        // we might not have to ... since the index of animation is still assuming we start from 0, the first step is to get the AnimationStateData?
        // this will tell us what sheet and index we need to play the animation; the index goes into the PartVertAndUVData[] and the sheet goes into Texture2D[]
        // it shouldnt matter where the sheet was separated, as long as the animationStateData knows which PartVert and Texture to get?
        // this means tho that we shouldnt separate animations per sheet --- this is a limitation we have to consider 
        // IF we were to optimize later, we have to be able to say what sheet the PartVertAndUVData is in (basically we'd have to add a byte to PartVertAndUV)
        // the PartVertAndUV has been reduced to around 66 to 68 bytes of memory but its still big; only 1 sbyte for z value and has byte for texture index

        // deciphering the animation needs work as well!!! - AFTER the motive animations its all up in the air... 
        // we need to trawl thru all the mechs and determine how many frames each are used for the last sets of animations
    }

    public PartAnimationData ConvertSpriteInfoArrayToPartAnimation(SpriteAnimationStatesSaveData saveData, PartVertAndUVData[] vertsAndUvs)
	{
        var tempStateDatas = new PartAnimationStateData[saveData.animationStates.Length];
    
		for (int i = 0; i < tempStateDatas.Length; i++)
		{
            var saveAnimState = saveData.animationStates[i];
            tempStateDatas[i] = new PartAnimationStateData((ushort)saveAnimState.startIndex,
                (byte)saveAnimState.numOfImages, (byte)saveAnimState.numOfFaces, (byte)saveAnimState.offset, (sbyte)saveAnimState.playspeed);
		}

        var tempSingleDatas = new PartAnimationStateData[saveData.singleFrameAnimationStates.Length];
		for (int i = 0; i < tempSingleDatas.Length; i++)
		{
            var saveAnimState = saveData.singleFrameAnimationStates[i];
            tempSingleDatas[i] = new PartAnimationStateData((ushort)saveAnimState.startIndex,
                (byte)saveAnimState.numOfImages, (byte)saveAnimState.numOfFaces, (byte)saveAnimState.offset, (sbyte)saveAnimState.playspeed);
        }

        var tempTextures = new Texture2D[saveData.pathsToMCBitmap.Length];

		for (int i = 0; i < tempTextures.Length; i++)
		{
            var bitmap = MCBitmap.Unserialize(saveData.pathsToMCBitmap[i]);
            tempTextures[i] = ImageProcessing.MakeIndexedTexture2D(bitmap); 
		}

        return new PartAnimationData(tempStateDatas, tempSingleDatas, tempTextures, vertsAndUvs);
    }

    // can recursively call another cache if the targetAtlasSize is exceeded
    public void CacheDynamicallyPlacedMCBitmap(int startIndex, int sheetIndex, ref List<MCBitmap> sheetBitmapsRef, ref List<SpriteInfoSaveData> spriteInfosRef)
    {
        MCBitmap outputBitmap;
        int tileMaxY = 0;
        int yEndPadding = 0;
        int xEndPadding = 0;

        outputBitmap = new MCBitmap(targetAtlasSize, targetAtlasSize);

        int bytes = outputBitmap.Stride * outputBitmap.Height;
        var rgbValues = new byte[bytes];
        int outputX = 0, outputY = 0, currentRgbIndex, currentBitMapIndex;

        List<SpriteInfoSaveData> spriteInfos = new List<SpriteInfoSaveData>();

        for (int i = startIndex; i < bitmaps.Count; i++)
        {
            if (outputY + bitmaps[i].Height > outputBitmap.Height)
            {
                Debug.Log("reached end of tile sheet with " + (bitmaps.Count - i - 1) + " remaining");
                outputBitmap.Data = rgbValues;

                // save here if we reach the end of sheet but there are still sprites remaining
                sheetBitmapsRef.Add(outputBitmap);
                spriteInfosRef.AddRange(spriteInfos);

                // if were not at the end but the sheet is filled, cache the next sheet
                CacheDynamicallyPlacedMCBitmap(i + 1, sheetIndex + 1, ref sheetBitmapsRef, ref spriteInfosRef);
                return;
            }

            for (int x = 0; x < bitmaps[i].Width; x++)
            {
                for (int y = 0; y < bitmaps[i].Height; y++)
                {
                    currentRgbIndex = (outputY + y) * outputBitmap.Stride + outputX + x;
                    currentBitMapIndex = (y * bitmaps[i].Stride) + x;

                    rgbValues[currentRgbIndex] = bitmaps[i].Data[currentBitMapIndex];
                }
            }

            if (tileMaxY < bitmaps[i].Height) tileMaxY = bitmaps[i].Height;
            // add the info here after we add the bytes to the array - this should also make the uvs
            SpriteInfoSaveData spriteInfo = new SpriteInfoSaveData();
            spriteInfo.x = outputX;
            spriteInfo.y = outputY;
            spriteInfo.width = bitmaps[i].Width;
            spriteInfo.height = bitmaps[i].Height;
            spriteInfo.sheetIndex = sheetIndex;
            float textSize = targetAtlasSize; // should have no rounding errors if targetAtlasSize is a multiple of 2
            float uvYOrigin = 1 - outputY / textSize;
            float uvYSize = bitmaps[i].Height / textSize;

            spriteInfo.uvs = new Vector2[]
            {
                new Vector2(outputX / textSize, uvYOrigin),
                new Vector2((outputX + bitmaps[i].Width) / textSize, uvYOrigin),
                new Vector2(outputX / textSize, uvYOrigin - uvYSize),
                new Vector2((outputX + bitmaps[i].Width) / textSize, uvYOrigin - uvYSize)

            };

            float invertY = -positionsY[i];

            spriteInfo.verts = new Vector3[]
            {
                //new Vector3(positionsX[i] * pixelsPerUnit, positionsY[i] * pixelsPerUnit, 0), //top-left
                new Vector3(positionsX[i] * pixelsPerUnit, invertY * pixelsPerUnit, 0), //top-left
                //new Vector3((positionsX[i] + bitmaps[i].Width) * pixelsPerUnit, positionsY[i] * pixelsPerUnit, 0), //top-right
                new Vector3((positionsX[i] + bitmaps[i].Width) * pixelsPerUnit, invertY * pixelsPerUnit, 0), //top-right
                //new Vector3(positionsX[i] * pixelsPerUnit, (positionsY[i] - bitmaps[i].Height) * pixelsPerUnit, 0), //bottom-left
                new Vector3(positionsX[i] * pixelsPerUnit, (invertY - bitmaps[i].Height) * pixelsPerUnit, 0), //bottom-left
                //new Vector3((positionsX[i] + bitmaps[i].Width) * pixelsPerUnit, (positionsY[i] - bitmaps[i].Height) * pixelsPerUnit, 0) //bottom-right
                new Vector3((positionsX[i] + bitmaps[i].Width) * pixelsPerUnit, (invertY - bitmaps[i].Height) * pixelsPerUnit, 0) //bottom-right
            };

            spriteInfos.Add(spriteInfo);

            // if we managed to set the data without going past the ylimit, we have printed the last index - needed to not access i + 1
            if (i >= bitmaps.Count - 1) break;

            outputX += bitmaps[i].Width + xEndPadding;// we just pass in the width of the current
            if (outputX + bitmaps[i + 1].Width > outputBitmap.Stride)
            {
                outputX = 0;
                outputY += tileMaxY + yEndPadding; // we pass the largest height we found on the line
                tileMaxY = 0; // we have to reset the maxY here so that the next line uses the maxY of only the largest sprite
            }

        }
        Debug.Log("Max output Y dynamic " + outputY);
        outputBitmap.Data = rgbValues;

        // save here if we reach end
        sheetBitmapsRef.Add(outputBitmap);
        spriteInfosRef.AddRange(spriteInfos);
    }

    public SpriteAnimationStatesSaveData LoadDataFromCache(string path)
    {
        if (path.Contains(spriteDataFileExt) && File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            return JsonUtility.FromJson<SpriteAnimationStatesSaveData>(jsonString);
        }
        Debug.Log("sprite data file not found at " + path);
        return new SpriteAnimationStatesSaveData();
    }

    public void SerializeVertsAndUvs(PartVertAndUVData[] vertsAndUvs, string path)
    {
        using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
        {
            int len = vertsAndUvs.Length;
            writer.Write(len); // write the length as int
			for (int i = 0; i < len; i++)
			{
                writer.Write(vertsAndUvs[i].topLeftVert.x);
                writer.Write(vertsAndUvs[i].topLeftVert.y);

                writer.Write(vertsAndUvs[i].bottomRightVert.x);
                writer.Write(vertsAndUvs[i].bottomRightVert.y);

                //writer.Write(vertsAndUvs[i].vert_2.x);
                //writer.Write(vertsAndUvs[i].vert_2.y);
                
                //writer.Write(vertsAndUvs[i].vert_3.x);
                //writer.Write(vertsAndUvs[i].vert_3.y);

                writer.Write(vertsAndUvs[i].uv_0.x);
                writer.Write(vertsAndUvs[i].uv_0.y);

                writer.Write(vertsAndUvs[i].uv_1.x);
                writer.Write(vertsAndUvs[i].uv_1.y);

                writer.Write(vertsAndUvs[i].uv_2.x);
                writer.Write(vertsAndUvs[i].uv_2.y);

                writer.Write(vertsAndUvs[i].uv_3.x);
                writer.Write(vertsAndUvs[i].uv_3.y);

                writer.Write(vertsAndUvs[i].vert_z);

                writer.Write(vertsAndUvs[i].textureIndex);
            }
        }
    }

    public PartVertAndUVData[] DeserializeVertsAndUV(string path)
	{
        PartVertAndUVData[] vertsAndUvs;
        int len;
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            len = reader.ReadInt32();
            vertsAndUvs = new PartVertAndUVData[len];
			for (int i = 0; i < len; i++)
			{
                var vertAndUV = new PartVertAndUVData();
                vertAndUV.topLeftVert = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                vertAndUV.bottomRightVert = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                //vertAndUV.vert_2 = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                //vertAndUV.vert_3 = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                
                vertAndUV.uv_0 = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                vertAndUV.uv_1 = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                vertAndUV.uv_2 = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                vertAndUV.uv_3 = new Vector2(reader.ReadSingle(), reader.ReadSingle());

                vertAndUV.vert_z = reader.ReadSByte();
                vertAndUV.textureIndex = reader.ReadByte();

                vertsAndUvs[i] = vertAndUV;
			}
        }
        return vertsAndUvs;
    }
    // refactor
    public void CreateBlankSpriteSheetAtAtlasSize()
    {
        //if (validIndices == null || validIndices.Count == 0) CreateValidSpriteIndices();
        //Debug.Log("valid ind " + validIndices.Count + " length to get " + length + " total sheets " + (validIndices.Count * 1f / (length * 1f)));
        //int numberOfSheets = Mathf.CeilToInt(validIndices.Count * 1f / (length * 1f));
        Debug.Log("number of sheets " + mechPakFiles.Length); // might change later to something dynamic
        Texture2D texture = new Texture2D(targetAtlasSize, targetAtlasSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[targetAtlasSize * targetAtlasSize];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear; // Sets alpha to 0 (completely transparent)
        }

        texture.SetPixels(pixels);
        texture.Apply();
        // Encode to PNG
        byte[] bytes = texture.EncodeToPNG();

        MechSHPIndexTable mechs = MechSHPIndexTable.AWESOME;
        PartIndex parts;
        string currentDir, partDir;

        for (int i = 0; i <= (int)MechSHPIndexTable.BUSHWACKER; i++)
        {
            currentDir = (int)mechs + "_" + mechs.ToString();

            parts = PartIndex.L_ARMS;
            for (int j = 0; j <= (int)PartIndex.TORSOS; j++)
            {
                partDir = Path.Combine(currentDir, parts.ToString() + "/" + parts.ToString());
                File.WriteAllBytes(Path.Combine(pathToResourceFolder, partDir + "_0.png"), bytes);
                File.WriteAllBytes(Path.Combine(pathToResourceFolder, partDir + "_1.png"), bytes); // some parts need two sheets


                parts++;
            }
            mechs++;
        }

        DestroyImmediate(texture);

    }

    private Facing ConvertFacing(int numOfFaces, int facing)
    {
        // the logic here is that the num of faces can be a set of images for half or a whole rotation
        // if it is 17, it is half
        // if it is 32 it is whole
        // if it is 5 or 9, its half but have to round
        // if its 8 or 16, its whole but have to round

        int divAmount = 1;
        if (numOfFaces <= 8) divAmount = 4;
        else if (numOfFaces <= 16) divAmount = 2;

        int newFacing = Mathf.RoundToInt(facing / divAmount);
        
        if (newFacing >= numOfFaces)
            return (Facing)(numOfFaces + numOfFaces - 2 - newFacing);
        
        return (Facing)newFacing;
    }

    private sbyte GetDepthPerPartAndFacing(PartIndex part, Facing facing)
	{
		switch (part)
		{
            case PartIndex.TORSOS:
                return 1;
            case PartIndex.L_ARMS:
                return GetDepthForL_Arms(facing);
            case PartIndex.R_ARMS:
                return GetDepthForR_Arms(facing);
            default:
                return 0; // legs are at 0
		}
	}

    private sbyte GetDepthForL_Arms(Facing facing)
	{
        if (facing >= 0 && facing <= Facing.N) return -1;
        else if (facing > Facing.N && facing <= Facing.SW_S_3) return 1;
        return -1;
	}

    private sbyte GetDepthForR_Arms(Facing facing)
	{
        if (facing > 0 && facing < Facing.N) return 1;
        else if (facing >= Facing.N && facing <= Facing.SW_S_3 || facing == Facing.S) return -1;
        return -1;
    }

    public void CreateDirectoryStructure()
    {
        MechSHPIndexTable origMech = mechIndex;
        mechIndex = MechSHPIndexTable.AWESOME;

        while(mechIndex <= MechSHPIndexTable.BUSHWACKER)
        {
            Directory.CreateDirectory(Path.Combine(pathToResourceFolder, GetFolderPathOfMech()));
            
            mechIndex++;
        }

        mechIndex = origMech;
    }

    public bool ValidateMCGAssetPath(string path)
    {
        var pathToData = Path.Combine(path, "DATA");
        if (!Directory.Exists(pathToData))
        {
            Debug.Log("Can´t find: " + pathToData);
            return false;
        }

        string pathToAsset;
        for (int i = 0; i < mechPakFiles.Length; i++)
        {
            pathToAsset = Path.Combine(pathToData, mechPakFiles[i]);
            if (!File.Exists(pathToAsset))
            {
                Debug.Log("Can´t find: " + pathToAsset);
                return false;
            }
        }

        Debug.Log("file found " + path);
        return true;
    }


    public void SetupFileManager()
    {
        if (gameFilesLoader == null)
        {
            Debug.Log("no file loader!");
            return;
        }

        if (fileManager == null)
        {
            // important to get at files in structure but will read thru and extract important
            fileManager = gameFilesLoader.FileManager == null ? new FileManager(gameFilesLoader.localMCGPath) : gameFilesLoader.FileManager;

        }

    }

    public string GetFolderPathOfMechAndPart()
    {
        return (int)mechIndex + "_" + mechIndex.ToString() + "/" + currentPartIndex.ToString();
    }

    public string GetFolderPathOfMech()
    {
        return (int)mechIndex + "_" + mechIndex.ToString();
    }

}
