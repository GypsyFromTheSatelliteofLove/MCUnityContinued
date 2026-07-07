using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using MechCommanderUnity.API;
using MechCommanderUnity.Utility;

public class MechSpriteLoader_OLD : MonoBehaviour
{
    [SerializeField]
    private GameFilesLoader gameFilesLoader;
    private FileManager fileManager;

    private string[] pakFilesToCopy = new string[]
    {
        "SPRITES/LARMS90.PAK",
        "SPRITES/LEGS90.PAK",
        "SPRITES/RARMS90.PAK",
        "SPRITES/TORSOS90.PAK"
    };
    
    public MechSHPIndexTable mechIndex;
    private MechsLegsSHPIndexTable mechLegsIndex; // automatically update when selecting a mechindex

    public List<int> validLarmsIndices = new List<int>();
    public List<int> validLegsIndices = new List<int>();
    public List<int> validRarmsIndices = new List<int>();
    public List<int> validTorsoIndices = new List<int>();

    private List<MCBitmap> bitmaps = new List<MCBitmap>();
    private List<sbyte> positionsX = new List<sbyte>();
    private List<sbyte> positionsY = new List<sbyte>();


    private Func<int, int, Texture2D> currentTextureGetter;
    private Action<int, int, string> continueBitmapIterationOnFullAtlas;

    public PartIndex atlasStartPartIndex;
    public PartIndex currentPartIndex;
    public int tileSizeX;
    public int tileSizeY;
    public int targetAtlasSize = 4096;

    public bool generateIndexTextFiles = false;
    public bool displayInfoText = false;

    private string paletteDirectory = @"palette\HB.PAL";
    private string g_paletteDirectory = @"palette\GHB.PAL";

    private const int shpID = 0x30312E31; // constant id

    private readonly string[] animationNames = new string[]
    {
        "SHUTDOWN",     //0
        "BEGIN_WALK",   //1
        "WALK",         //2
        "STOP",         //3
        "BEGIN_RUN",    //4
        "RUN",          //5
        "RUN_TO_WALK",  //6
        "HOBBLE",       //7
        "FALL_B_0",     //8
        "FALL_F_0",     //9
        "FALL_B_1",     //10
        "FALL_F_1",     //11
        "FALL_B_2",     //12
        "FALL_F_2",     //13
        "JUMP",         //14
        "ROLL_OVER",    //15
        "GET_UP"        //16
    };

    public string pathToResourceFolder = "Assets/Resources/Sprites/Mechs";
    private const string spriteDataFileExt = "_spriteData.Json";
    private const string spriteInfosFileExt = "_spriteInfos.Json";
    private List<string> pathsToSpriteSheets = new List<string>();

    private const float pixelsPerUnit = 1f / 100f;
    //private List<SpriteInfoArraySaveData> spriteInfoArrays = new List<SpriteInfoArraySaveData>(); 
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void CreateMechSprites() // prints 1 mech at a time 
    {
        SetupFileManager();
        pathsToSpriteSheets.Clear();

        currentPartIndex = PartIndex.L_ARMS;
        GetValidIndices(validLarmsIndices, pakFilesToCopy[(int)PartIndex.L_ARMS]);
        PrintBitmaps();

        currentPartIndex = PartIndex.LEGS;
        GetValidIndices(validLegsIndices, pakFilesToCopy[(int)PartIndex.LEGS]);
        PrintBitmaps();

        currentPartIndex = PartIndex.R_ARMS;
        GetValidIndices(validRarmsIndices, pakFilesToCopy[(int)PartIndex.R_ARMS]);
        PrintBitmaps();

        currentPartIndex = PartIndex.TORSOS;
        GetValidIndices(validTorsoIndices, pakFilesToCopy[(int)PartIndex.TORSOS]);        
        PrintBitmaps();
    }

    public void CreateMechAnimData()
	{
        SetupFileManager();

        currentPartIndex = PartIndex.L_ARMS;
        GetValidIndices(validLarmsIndices, pakFilesToCopy[(int)PartIndex.L_ARMS]);
        
        currentPartIndex = PartIndex.LEGS;
        GetValidIndices(validLegsIndices, pakFilesToCopy[(int)PartIndex.LEGS]);
        
        currentPartIndex = PartIndex.R_ARMS;
        GetValidIndices(validRarmsIndices, pakFilesToCopy[(int)PartIndex.R_ARMS]);
        
        currentPartIndex = PartIndex.TORSOS;
        GetValidIndices(validTorsoIndices, pakFilesToCopy[(int)PartIndex.TORSOS]);
    }

    public void CreateContinuousPartSheet()
	{
        SetupFileManager();

        List<int> currentIndices = validLarmsIndices;
        switch (currentPartIndex)
        {
            case PartIndex.LEGS:
                currentIndices = validLegsIndices;
                break;
            case PartIndex.R_ARMS:
                currentIndices = validRarmsIndices;
                break;
            case PartIndex.TORSOS:
                currentIndices = validTorsoIndices;
                break;
            default:
                break;
        }

        var maxPart = currentPartIndex == PartIndex.LEGS ? MechSHPIndexTable.STILLETO : MechSHPIndexTable.BUSHWACKER; // there are only 19 legs and 24 mechs
        MechSHPIndexTable origMechIndex = mechIndex;
        //mechIndex = MechSHPIndexTable.AWESOME;
        currentTextureGetter = GetTextureDynamicSpacing;
        continueBitmapIterationOnFullAtlas = null;
        bitmaps.Clear();
        currentIndices.Clear();
		while(mechIndex <= maxPart)
		{
            // test because raven at index 14 throws error
            //if (mechIndex != MechSHPIndexTable.RAVEN && mechIndex != MechSHPIndexTable.VULTURE) 
              
            GetValidIndices(currentIndices, pakFilesToCopy[(int)currentPartIndex], true);
            mechIndex++;
		}
        PrintBitmaps(0, 0, pathToResourceFolder);
        currentTextureGetter = null;
        mechIndex = origMechIndex;
    }

    public void CreateAllPartsInFolders()
	{
        SetupFileManager();

        List<int> currentIndices = validLarmsIndices;
        switch (currentPartIndex)
        {
            case PartIndex.LEGS:
                currentIndices = validLegsIndices;
                break;
            case PartIndex.R_ARMS:
                currentIndices = validRarmsIndices;
                break;
            case PartIndex.TORSOS:
                currentIndices = validTorsoIndices;
                break;
            default:
                break;
        }

        MechSHPIndexTable origMechIndex = mechIndex;
        //mechIndex = MechSHPIndexTable.AWESOME;
        currentTextureGetter = null;
        continueBitmapIterationOnFullAtlas = null;
        bitmaps.Clear();
        currentIndices.Clear();
        while (mechIndex <= MechSHPIndexTable.BUSHWACKER) // we dont need to check for legs here as the getvalidindices will find the legs folder
        {
            // test because raven at index 14 throws error
            //if (mechIndex != MechSHPIndexTable.RAVEN && mechIndex != MechSHPIndexTable.VULTURE) 
            GetValidIndices(currentIndices, pakFilesToCopy[(int)currentPartIndex]);
            Debug.Log("printing " + mechIndex + " " + currentPartIndex);
            PrintBitmaps(0, 0);
            mechIndex++;
        }
        mechIndex = origMechIndex;
    }

    public void AssesAllParts()
	{
        SetupFileManager();

        List<int> currentIndices = validLarmsIndices;
        switch (currentPartIndex)
        {
            case PartIndex.LEGS:
                currentIndices = validLegsIndices;
                break;
            case PartIndex.R_ARMS:
                currentIndices = validRarmsIndices;
                break;
            case PartIndex.TORSOS:
                currentIndices = validTorsoIndices;
                break;
            default:
                break;
        }

        MechSHPIndexTable origMechIndex = mechIndex;
        //mechIndex = MechSHPIndexTable.AWESOME;
        currentTextureGetter = GetTextureDynamicSpacing;
        bitmaps.Clear();
        currentIndices.Clear();
        continueBitmapIterationOnFullAtlas = AssesBitmaps;
        while (mechIndex <= MechSHPIndexTable.BUSHWACKER) // we dont need to check for legs here as the getvalidindices will find the legs folder
        {
            // test because raven at index 14 and vulture at index 15 throws error
            //if (mechIndex != MechSHPIndexTable.RAVEN && mechIndex != MechSHPIndexTable.VULTURE) 
            GetValidIndices(currentIndices, pakFilesToCopy[(int)currentPartIndex]);
            Debug.Log("printing " + mechIndex + " " + currentPartIndex);
            //currentTextureGetter = null;
            //AssesBitmaps(0, 0);
            AssesBitmaps(0, 0);
            mechIndex++;
        }
        continueBitmapIterationOnFullAtlas = null;
        currentTextureGetter = null;
        mechIndex = origMechIndex;
    }

    public void CreateDirectoryStructure()
	{
        MechSHPIndexTable allMechs = MechSHPIndexTable.AWESOME;
        PartIndex allParts;

        string currentDir;
        for (int i = 0; i <= (int)MechSHPIndexTable.BUSHWACKER; i++)
		{
            currentDir = Path.Combine(pathToResourceFolder, (int)allMechs + "_" + allMechs.ToString());
            Directory.CreateDirectory(currentDir);
            allParts = PartIndex.L_ARMS;
			for (int j = 0; j <= (int)PartIndex.TORSOS; j++)
			{
                Directory.CreateDirectory(Path.Combine(currentDir, allParts.ToString()));
                allParts++;
            }

            allMechs++;
        }
	}

    public void PrintBitmaps(int startIndex = 0, int count = 0, string p_path = "")
    {
        // these are the doing the heavy lifting in terms of bytes to image
        var mainTex = currentTextureGetter == null ? GetTexture(startIndex, count) : currentTextureGetter.Invoke(startIndex, count);
        mainTex.filterMode = FilterMode.Point;
        
        byte[] imageBytes = mainTex.EncodeToPNG();
        DestroyImmediate(mainTex);

        if (p_path != "" && Directory.Exists(p_path))
		{
            File.WriteAllBytes(p_path + "/" + currentPartIndex.ToString() + "_" + count + ".png", imageBytes);
            Debug.Log("printing to " + p_path + "/" + currentPartIndex.ToString() + "_" + count + ".png");
            return;
		}
        
        // Define save path in Assets folder
        string folder = (int)mechIndex + "_" + mechIndex.ToString() + "/" + currentPartIndex.ToString();
        string path = Path.Combine(pathToResourceFolder, folder);
        path = Path.Combine(path, currentPartIndex.ToString() + "_" + count + ".png");
        File.WriteAllBytes(path, imageBytes);
        Debug.Log("printing to " + path);
    }

    public void AssesBitmaps(int startIndex = 0, int count = 0, string path = "")
	{
        var mainTex = currentTextureGetter == null ? GetTexture(startIndex, count) : currentTextureGetter.Invoke(startIndex, count);
        DestroyImmediate(mainTex);
    }

    // should check if a cached texture exists and return that before getting the texture from file
    public Texture2D GetTextureForMechPart(MechSHPIndexTable mech, PartIndex part)
	{
        SetupFileManager();

        List<int> currentIndices = validLarmsIndices;
		switch (part)
		{
            case PartIndex.LEGS:
                currentIndices = validLegsIndices;
                break;
            case PartIndex.R_ARMS:
                currentIndices = validRarmsIndices;
                break;
            case PartIndex.TORSOS:
                currentIndices = validTorsoIndices;
                break;
            default:
                break;
		}

        currentPartIndex = PartIndex.L_ARMS;
        GetValidIndices(currentIndices, pakFilesToCopy[(byte)part]);
        // WE still need the dictionary of int plus rect?
        // OR since we made them uniform sizes, maybe we just cut in uniform as well? this also saves the mesh from having to be resized??
        // if we can get info for tile size x, tile size y and atlas sizes, we can just calculate where the sprite is based on the index
        // y = index / stride
        // x = index % stride
        // this breaks if we need 2 tiles; but we just store the max index per sheet/texture
        return GetTexture();
    }

    // we have to be able to cache the images and return the cached image if it exists
    public Texture2D GetTexture(int startIndex = 0, int count = 0)
	{
        MCBitmap outputBitmap;
        //Debug.Log("total number of valid files " + bitmaps.Count);
        int tileMaxY = tileSizeY;
        int tileMaxX = tileSizeX;
        int yEndPadding = 0;
        int xEndPadding = 0;
        int atlasMaxX = targetAtlasSize / (tileMaxX + xEndPadding);
        int atlasMaxY = targetAtlasSize / (tileMaxY + yEndPadding);

        Debug.Log("will print " + atlasMaxX + " x " + atlasMaxY + " maxWidth " + tileMaxX + " x max Height " + tileMaxY);
        // just make atlas size maybe??
        outputBitmap = new MCBitmap((tileMaxX + xEndPadding) * atlasMaxX, (tileMaxY + yEndPadding) * atlasMaxY);

        int bytes = outputBitmap.Stride * outputBitmap.Height;
        var rgbValues = new byte[bytes];
        //Debug.Log("number of bytes in sheet " + bytes);
        // it does start at top??
        //int outputX = outputBitmap.Stride - (tileMaxX + xEndPadding), outputY = 0, currentRgbIndex, currentBitMapIndex;
        int outputX = 0, outputY = 0, currentRgbIndex, currentBitMapIndex;
        //int outputX = 0, outputY = outputBitmap.Height - (tileMaxX + xEndPadding), currentRgbIndex, currentBitMapIndex;

        for (int i = startIndex; i < bitmaps.Count; i++)
        {
            for (int x = 0; x < bitmaps[i].Width; x++)
            {
                for (int y = 0; y < bitmaps[i].Height; y++)
                {
                    currentRgbIndex = (outputY + y) * outputBitmap.Stride + outputX + x;
                    currentBitMapIndex = (y * bitmaps[i].Stride) + x;

                    rgbValues[currentRgbIndex] = bitmaps[i].Data[currentBitMapIndex];

                }

            }
            //if (i == 200) break;//testing
            outputX += tileMaxX + xEndPadding;// instead we can just pass in the width of the current?
            //if (outputX < 0)
            if (outputX >= outputBitmap.Stride)
            {
                //if (outputY - (tileMaxY + yEndPadding) < 0) break;
                if (outputY + tileMaxY + yEndPadding >= outputBitmap.Height)
                {
                    Debug.Log("reached end of tile sheet with " + (bitmaps.Count - i - 1) + " remaining");
                    if (i < bitmaps.Count - 1) PrintBitmaps(i + 1, count + 1); // if were not at the end but the sheet is filled, print on next sheet
                    break;
                }

                //outputX = outputBitmap.Stride - (tileMaxX + xEndPadding);
                outputX = 0;
                //outputY -= tileMaxY + yEndPadding;
                outputY += tileMaxY + yEndPadding; // instead we just pass in the max height of the line?
            }

            // we need to add a check for the width and height - if the next sprite will go over the sheet, we go to the next line
            // if the next line can no longer print the next image, go to the next sheet

        }

        Debug.Log("Max output Y " + outputY);

        outputBitmap.Data = rgbValues;
        // we can cache here
        // outputBitmap.Serialize(path)
        // these are the doing the heavy lifting in terms of bytes to image; theres a reversing of the data to raw data that has to happen
        return ImageProcessing.MakeIndexedTexture2D(outputBitmap);
    }

    public Texture2D GetTextureDynamicSpacing(int startIndex = 0, int count = 0)
    {
        MCBitmap outputBitmap;
        //Debug.Log("total number of valid files " + bitmaps.Count);
        int tileMaxY = 0;
        //int tileMaxX = tileSizeX;
        int yEndPadding = 0;
        int xEndPadding = 0;
        //int atlasMaxX = targetAtlasSize / (tileMaxX + xEndPadding);
        //int atlasMaxY = targetAtlasSize / (tileMaxY + yEndPadding);

        //Debug.Log("will print " + atlasMaxX + " x " + atlasMaxY + " maxWidth " + tileMaxX + " x max Height " + tileMaxY);
        // just make atlas size maybe??
        outputBitmap = new MCBitmap(targetAtlasSize, targetAtlasSize);

        int bytes = outputBitmap.Stride * outputBitmap.Height;
        var rgbValues = new byte[bytes];
        //Debug.Log("number of bytes in sheet " + bytes);
        // it does start at top??
        //int outputX = outputBitmap.Stride - (tileMaxX + xEndPadding), outputY = 0, currentRgbIndex, currentBitMapIndex;
        int outputX = 0, outputY = 0, currentRgbIndex, currentBitMapIndex;
        //int outputX = 0, outputY = outputBitmap.Height - (tileMaxX + xEndPadding), currentRgbIndex, currentBitMapIndex;

        for (int i = startIndex; i < bitmaps.Count; i++)
        {
            //if (outputY - (tileMaxY + yEndPadding) < 0) break;
            if (outputY + bitmaps[i].Height > outputBitmap.Height)
            {
                Debug.Log("reached end of tile sheet with " + (bitmaps.Count - i - 1) + " remaining");
                // if were not at the end but the sheet is filled, print on next sheet
                if (continueBitmapIterationOnFullAtlas == null) PrintBitmaps(i + 1, count + 1, pathToResourceFolder);
                else continueBitmapIterationOnFullAtlas.Invoke(i + 1, count + 1, "");
                break;
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

            if (i >= bitmaps.Count - 1) // if we managed to set the data without going past the ylimit, we have printed the last index
			{
                break;
			}
            //if (i == 200) break;//testing
            outputX += bitmaps[i].Width + xEndPadding;// we just pass in the width of the current
            //if (outputX < 0)
            if (outputX + bitmaps[i + 1].Width > outputBitmap.Stride)
            {
                //outputX = outputBitmap.Stride - (tileMaxX + xEndPadding);
                outputX = 0;
                //outputY -= tileMaxY + yEndPadding;
                outputY += tileMaxY + yEndPadding; // we pass the largest height we found on the line
                // should i not reset tileMaxY???
                tileMaxY = 0;
            }

            // we need to add a check for the width and height - if the next sprite will go over the sheet, we go to the next line
            // if the next line can no longer print the next image, go to the next sheet
            // this is probably where we also have to create our MeshVert and UV data
            // the mesh vert is just the local position of the sprite and its width and height 

        }
        Debug.Log("Max output Y dynamic " + outputY);
        outputBitmap.Data = rgbValues;
        // we can cache here
        // outputBitmap.Serialize(path)
        // these are the doing the heavy lifting in terms of bytes to image; theres a reversing of the data to raw data that has to happen
        return ImageProcessing.MakeIndexedTexture2D(outputBitmap);
    }

    // prints only per part
    public void PrintAllTexturesFromCache()
	{

        currentPartIndex = PartIndex.L_ARMS;
        while(currentPartIndex <= PartIndex.TORSOS)
		{
            PrintAllPartTexturesFromCache();
            currentPartIndex++;
		}
        currentPartIndex = PartIndex.L_ARMS;
    }

    public void PrintAllPartTexturesFromCache()
	{
        MechSHPIndexTable origMechIndex = mechIndex;
        int sheetCount;

        // we dont have to check the legs... when the sprite data is generated, a separate bmp bytes is made for each mech
        while (mechIndex <= MechSHPIndexTable.BUSHWACKER)
        {
            sheetCount = 0;
            string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
            var tempText = LoadTextureFromCache(Path.Combine(folder, currentPartIndex.ToString() + "_" + sheetCount + ".bytes"));

            while (tempText != null)
            {
                byte[] imageBytes = tempText.EncodeToPNG();
                DestroyImmediate(tempText);

                var path = Path.Combine(folder, currentPartIndex.ToString() + "_" + sheetCount++ + ".png");
                File.WriteAllBytes(path, imageBytes);
                Debug.Log("printing to " + path);
                tempText = LoadTextureFromCache(Path.Combine(folder, currentPartIndex.ToString() + "_" + sheetCount + ".bytes"));
            }

            mechIndex++;
        }
        mechIndex = origMechIndex;
    }

    public void PrintTextureFromCurrentMechCache()
	{
        currentPartIndex = PartIndex.L_ARMS;
        int sheetCount;
        while (currentPartIndex <= PartIndex.TORSOS)
        {
            sheetCount = 0;
            string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
            var tempText = LoadTextureFromCache(Path.Combine(folder, currentPartIndex.ToString() + "_" + sheetCount + ".bytes"));

            while (tempText != null)
            {
                byte[] imageBytes = tempText.EncodeToPNG();
                DestroyImmediate(tempText);

                var path = Path.Combine(folder, currentPartIndex.ToString() + "_" + sheetCount++ + ".png");
                File.WriteAllBytes(path, imageBytes);
                Debug.Log("printing to " + path);
                tempText = LoadTextureFromCache(Path.Combine(folder, currentPartIndex.ToString() + "_" + sheetCount + ".bytes"));
            }
            currentPartIndex++;
        }
        currentPartIndex = PartIndex.L_ARMS;
    }

    public void AssesDataForLastPixelYCoord()
	{
        MechSHPIndexTable origMechIndex = mechIndex;
        int sheetCount;
        
        while (mechIndex <= MechSHPIndexTable.BUSHWACKER) // we dont need to check for legs here as the getvalidindices will find the legs folder
        {
            sheetCount = 0;
            string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
            var tempData = LoadSpriteDataFromCache(Path.Combine(folder, currentPartIndex.ToString() + "_" + sheetCount + spriteDataFileExt));

            // OLD - not considering the string array but fixed by using idx 0 for now
            while (!tempData.Equals(default(SpriteInfoArraySaveData)))
			{
                var lastIndex = tempData.spriteInfos.Length - 1;
                var heightOfLast = tempData.spriteInfos[lastIndex].y + tempData.spriteInfos[lastIndex].height;
                Debug.Log(mechIndex + " has last image at " + heightOfLast + " y pixel coord for sheet " + sheetCount++);

                tempData = LoadSpriteDataFromCache(Path.Combine(GetFolderPathOfMech(), currentPartIndex.ToString() + "_" + sheetCount + spriteDataFileExt));
            }

            mechIndex++;
        }
        mechIndex = origMechIndex;
    }

    public void CombineCachedMechDataIntoAtlas()
	{
        // we have to find the last byte written and append?? 
        // we can use the data to tell us the last x position that we start at
        int sheetCount;
        string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
        List<SpriteAnimationStatesSaveData> allPartData = new List<SpriteAnimationStatesSaveData>();
        
        PartIndex part = PartIndex.L_ARMS;

		while(part <= PartIndex.TORSOS)
		{
            sheetCount = 0;
            var tempData = LoadDataFromCache(Path.Combine(folder, part.ToString() + "_" + sheetCount + spriteDataFileExt));
            
            // idx 0 is temp
            while (!string.IsNullOrEmpty(tempData.pathsToMCBitmap[0]))
            {
                allPartData.Add(tempData);
                
                tempData = LoadDataFromCache(Path.Combine(GetFolderPathOfMech(), part.ToString() + "_" + sheetCount + spriteDataFileExt));
            }

            part++;
        }
	}

    public void PrintCurrentMechAtlas()
    {
        int sheetCount = 0;
        string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());

        var toPrint = LoadTextureFromCache(Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + ".bytes"));

        while (toPrint != null)
        {
            byte[] imageBytes = toPrint.EncodeToPNG();
            DestroyImmediate(toPrint);

            var path = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + sheetCount++ + ".png");
            File.WriteAllBytes(path, imageBytes);
            Debug.Log("printing to " + path);
            toPrint = LoadTextureFromCache(Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + sheetCount + ".bytes"));
        }
        
    }

    public int FindGreatestYOfLastLine(SpriteInfoArraySaveData saveData)
	{
        int lastYMax = 0;
        int currentY = saveData.spriteInfos[saveData.spriteInfos.Length - 1].y;
        int lastLineY = currentY;
        int i = saveData.spriteInfos.Length - 1;

        while (currentY == lastLineY)
		{
            if (lastYMax < saveData.spriteInfos[i].height) lastYMax = saveData.spriteInfos[i].height;

            i--;
            currentY = saveData.spriteInfos[i].y;
		}
        
        return lastYMax;
	}

    public void DisplayYSpaceLeftAndYMaxOfPart()
	{
        string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
        var partData = LoadSpriteDataFromCache(Path.Combine(folder, currentPartIndex.ToString() + "_" + 0 + spriteDataFileExt));

        var yMax = FindGreatestYOfLastLine(partData) + partData.spriteInfos[partData.spriteInfos.Length - 1].y;
        var spaceLeftY = targetAtlasSize - yMax;

        Debug.Log(mechIndex + " " + currentPartIndex + " has " + spaceLeftY + " y Space Left");
        Debug.Log(mechIndex + " " + currentPartIndex + " has " + yMax + " y Max");
    }
    // we can collapse these 3 functions into each other
    // we might have to work on naming the atlases but for now this is ok
    public void AppendArmsOfCurrentMech()
	{
        string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
        string pathBytes = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + ".bytes"); // we will store this for the save
        string pathData = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + spriteDataFileExt);

        var origData = LoadDataFromCache(Path.Combine(folder, PartIndex.L_ARMS + "_" + 0 + spriteDataFileExt));
        var appendData = LoadDataFromCache(Path.Combine(folder, PartIndex.R_ARMS + "_" + 0 + spriteDataFileExt));
        var origSpriteInfo = LoadSpriteDataFromCache(Path.Combine(folder, PartIndex.L_ARMS + "_" + 0 + spriteInfosFileExt));
        var appendSpriteInfo = LoadSpriteDataFromCache(Path.Combine(folder, PartIndex.R_ARMS + "_" + 0 + spriteInfosFileExt));

        SpriteAnimationStatesSaveData saveData;
        SpriteInfoArraySaveData spriteSaveData;

        var atlas = AppendBitmap(origData.pathsToMCBitmap[0], appendData.pathsToMCBitmap[0], origSpriteInfo, appendSpriteInfo, out saveData, out spriteSaveData);

        atlas.Serialize(pathBytes);
        saveData.pathsToMCBitmap = new string[] { pathBytes }; // ALL INSTANCES OF THIS ARE REFACTORED TO ARRAYS BUT WILL ONLY EVER USE idx 0
        File.WriteAllText(pathData, JsonUtility.ToJson(saveData, true));
    }
    
    public void AppendTorsoToLegsOfCurrentMech()
	{
        string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
        string pathBytes = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 1 + ".bytes"); // we will store this for the save
        string pathData = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 1 + spriteDataFileExt);

        var origData = LoadDataFromCache(Path.Combine(folder, PartIndex.L_ARMS + "_" + 0 + spriteDataFileExt));
        var appendData = LoadDataFromCache(Path.Combine(folder, PartIndex.R_ARMS + "_" + 0 + spriteDataFileExt));
        var origSpriteInfo = LoadSpriteDataFromCache(Path.Combine(folder, PartIndex.L_ARMS + "_" + 0 + spriteInfosFileExt));
        var appendSpriteInfo = LoadSpriteDataFromCache(Path.Combine(folder, PartIndex.R_ARMS + "_" + 0 + spriteInfosFileExt));

        SpriteAnimationStatesSaveData saveData;
        SpriteInfoArraySaveData spriteSaveData;

        var atlas = AppendBitmap(origData.pathsToMCBitmap[0], appendData.pathsToMCBitmap[0], origSpriteInfo, appendSpriteInfo, out saveData, out spriteSaveData);

        atlas.Serialize(pathBytes);
        saveData.pathsToMCBitmap = new string[] { pathBytes };
        File.WriteAllText(pathData, JsonUtility.ToJson(saveData, true));
    }

    public void AppendToLastAtlasCreated()
	{
        string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
        string pathBytes = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + ".bytes"); // we will store this for the save
        string pathData = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + spriteDataFileExt);
        string pathSpriteData = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + spriteInfosFileExt);

        var origData = LoadDataFromCache(pathData);
        var origSpriteInfo = LoadSpriteDataFromCache(pathSpriteData);

        if (string.IsNullOrEmpty(origData.pathsToMCBitmap[0]))
		{
            origData = LoadDataFromCache(Path.Combine(folder, atlasStartPartIndex.ToString() + "_" + 0 + spriteDataFileExt));
            origSpriteInfo = LoadSpriteDataFromCache(Path.Combine(folder, atlasStartPartIndex.ToString() + "_" + 0 + spriteInfosFileExt));

        }
        var appendData = LoadDataFromCache(Path.Combine(folder, currentPartIndex.ToString() + "_" + 0 + spriteDataFileExt));
        var appendSpriteInfo = LoadSpriteDataFromCache(Path.Combine(folder, currentPartIndex.ToString() + "_" + 0 + spriteInfosFileExt));

        SpriteAnimationStatesSaveData saveData;
        SpriteInfoArraySaveData spriteSaveData;

        var atlas = AppendBitmap(origData.pathsToMCBitmap[0], appendData.pathsToMCBitmap[0], origSpriteInfo, appendSpriteInfo, out saveData, out spriteSaveData);

        //Debug.Log("Greatest Y " + FindGreatestYOfLastLine(saveData));
        atlas.Serialize(pathBytes);
        saveData.pathsToMCBitmap = new string[] { pathBytes };
        File.WriteAllText(pathData, JsonUtility.ToJson(saveData, true));
    }

    public MCBitmap AppendBitmap(string pathToOrigData, string  pathToDataToAppend, 
        SpriteInfoArraySaveData origSpriteInfo, SpriteInfoArraySaveData spriteInfoToAppend, out SpriteAnimationStatesSaveData saveData, 
        out SpriteInfoArraySaveData spriteInfo)
	{
        int tileMaxY = FindGreatestYOfLastLine(origSpriteInfo);
        int outputX = origSpriteInfo.spriteInfos[origSpriteInfo.spriteInfos.Length - 1].x + origSpriteInfo.spriteInfos[origSpriteInfo.spriteInfos.Length - 1].width;
        int outputY = origSpriteInfo.spriteInfos[origSpriteInfo.spriteInfos.Length - 1].y, currentRgbIndex, currentBitMapIndex;

        MCBitmap origBMP = MCBitmap.Unserialize(pathToOrigData);
        MCBitmap bmpToAppend = MCBitmap.Unserialize(pathToDataToAppend);

        saveData = new SpriteAnimationStatesSaveData();
        spriteInfo = new SpriteInfoArraySaveData();
        List<SpriteInfoSaveData> spriteInfos = new List<SpriteInfoSaveData>();
        spriteInfos.AddRange(origSpriteInfo.spriteInfos);
        Debug.Log("attempting to append starting at " + outputY + " to file " + pathToOrigData + " from file " + pathToDataToAppend);
        for (int i = 0; i < spriteInfoToAppend.spriteInfos.Length; i++)
        {
            if (outputY + spriteInfoToAppend.spriteInfos[i].height > origBMP.Height)
            {
                Debug.Log("reached end of tile sheet with " + (spriteInfoToAppend.spriteInfos.Length - i - 1) + " remaining");
                spriteInfo.spriteInfos = spriteInfos.ToArray();
                
                // serialize but do nothing for now... we  just leave the rest of the data unfinished - plan for this in future
                return origBMP;
            }

            for (int x = 0; x < spriteInfoToAppend.spriteInfos[i].width; x++)
            {
                for (int y = 0; y < spriteInfoToAppend.spriteInfos[i].height; y++)
                {
                    currentRgbIndex = (outputY + y) * origBMP.Stride + outputX + x;
                    currentBitMapIndex = (spriteInfoToAppend.spriteInfos[i].y + y) * bmpToAppend.Stride + spriteInfoToAppend.spriteInfos[i].x + x;

                    origBMP.Data[currentRgbIndex] = bmpToAppend.Data[currentBitMapIndex];

                }
            }
            
            if (tileMaxY < spriteInfoToAppend.spriteInfos[i].height) tileMaxY = spriteInfoToAppend.spriteInfos[i].height;

            // we have to modify these to stay consistent with the new atlas
            spriteInfoToAppend.spriteInfos[i].x = outputX;
            spriteInfoToAppend.spriteInfos[i].y = outputY;

            spriteInfos.Add(spriteInfoToAppend.spriteInfos[i]);

            // if we managed to set the data without going past the ylimit, we have printed the last index - needed to not access i + 1
            if (i >= spriteInfoToAppend.spriteInfos.Length - 1) break;

            outputX += spriteInfoToAppend.spriteInfos[i].width;// we just pass in the width of the current
            if (outputX + spriteInfoToAppend.spriteInfos[i + 1].width > origBMP.Stride)
            {
                outputX = 0;
                outputY += tileMaxY; // we pass the largest height we found on the line
                tileMaxY = 0;
            }

        }

        //origBMP.Serialize(pathBytes);

        //saveData.pathToMCBitmap = pathBytes;// this should be the path to the corresponding sheet

        Debug.Log("finished append at line " + (outputY + tileMaxY));
        spriteInfoToAppend.spriteInfos = spriteInfos.ToArray();

        //File.WriteAllText(pathData, JsonUtility.ToJson(saveData, true));

        return origBMP;
    }


    public Texture2D LoadTextureFromCache(string path)
	{
        if (path.Contains(".bytes") && File.Exists(path))
		{
            return ImageProcessing.MakeIndexedTexture2D(MCBitmap.Unserialize(path));

        }
        Debug.Log("bytes file not found at " + path);
        return null;
	}

    public SpriteInfoArraySaveData LoadSpriteDataFromCache(string path)
	{
        if (path.Contains(spriteDataFileExt) && File.Exists(path))
        {
            string jsonString = File.ReadAllText(path);
            return JsonUtility.FromJson<SpriteInfoArraySaveData>(jsonString);
        }
        Debug.Log("sprite data file not found at " + path);
        return new SpriteInfoArraySaveData();
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

    public void CacheData()
	{
        //pathsToSpriteSheets.Clear();
        //spriteInfoArrays.Clear();
        SetupFileManager();
        currentPartIndex = PartIndex.L_ARMS;
        while(currentPartIndex <= PartIndex.TORSOS)
		{
            CacheDataForPart();
            currentPartIndex++;
		}

        currentPartIndex = PartIndex.L_ARMS;
        /*
        SpriteInfoArraySaveData currentSaveData = new SpriteInfoArraySaveData();

        currentSaveData.paths = pathsToSpriteSheets.ToArray();
        currentSaveData.spriteArrays = spriteInfoArrays.ToArray();
        
        //save the file
        File.WriteAllText(Path.Combine(pathToResourceFolder, currentPartIndex + spriteDataFileExt), JsonUtility.ToJson(currentSaveData, true));
        */
    }

    public void CacheDataForPart()
	{
        List<int> currentIndices = validLarmsIndices;
        switch (currentPartIndex)
        {
            case PartIndex.LEGS:
                currentIndices = validLegsIndices;
                break;
            case PartIndex.R_ARMS:
                currentIndices = validRarmsIndices;
                break;
            case PartIndex.TORSOS:
                currentIndices = validTorsoIndices;
                break;
            default:
                break;
        }

        MechSHPIndexTable origMechIndex = mechIndex;
        while (mechIndex <= MechSHPIndexTable.BUSHWACKER) // we dont need to check for legs here as the getvalidindices will find the legs folder
        {
            GetValidIndices(currentIndices, pakFilesToCopy[(int)currentPartIndex]);
            // will save the bitmaps and create their appropriate bitmap bytes file
            CacheDynamicallyPlacedMCBitmap(0, 0);
            mechIndex++;
        }
        mechIndex = origMechIndex;
    }

    public void CacheDynamicallyPlacedMCBitmap(int startIndex, int count)
	{
        MCBitmap outputBitmap;
        int tileMaxY = 0;
        int yEndPadding = 0;
        int xEndPadding = 0;

        outputBitmap = new MCBitmap(targetAtlasSize, targetAtlasSize);

        int bytes = outputBitmap.Stride * outputBitmap.Height;
        var rgbValues = new byte[bytes];
        int outputX = 0, outputY = 0, currentRgbIndex, currentBitMapIndex;

        string path = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
        string pathBytes = Path.Combine(path, currentPartIndex.ToString() + "_" + count + ".bytes"); // we will store this for the save
        string pathData = Path.Combine(path, currentPartIndex.ToString() + "_" + count + spriteDataFileExt);
        string pathSpriteInfos = Path.Combine(path, currentPartIndex.ToString() + "_" + count + spriteInfosFileExt);
        var saveData = new SpriteAnimationStatesSaveData();
        var spriteInfoSaveData = new SpriteInfoArraySaveData();
        List<SpriteInfoSaveData> spriteInfos = new List<SpriteInfoSaveData>();

        for (int i = startIndex; i < bitmaps.Count; i++)
        {
            if (outputY + bitmaps[i].Height > outputBitmap.Height)
            {
                Debug.Log("reached end of tile sheet with " + (bitmaps.Count - i - 1) + " remaining");
                outputBitmap.Data = rgbValues;

                // save here if we reach the end of sheet but there are still sprites remaining
                outputBitmap.Serialize(pathBytes);

                saveData.pathsToMCBitmap = new string[] { pathBytes };
                spriteInfoSaveData.spriteInfos = spriteInfos.ToArray();
                File.WriteAllText(pathData, JsonUtility.ToJson(saveData, true));
                File.WriteAllText(pathSpriteInfos, JsonUtility.ToJson(spriteInfoSaveData, true));

                // if were not at the end but the sheet is filled, cache the next sheet
                CacheDynamicallyPlacedMCBitmap(i + 1, count + 1);
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
            float textSize = targetAtlasSize;
            spriteInfo.uvs = new Vector2[]
            {
                new Vector2(outputX / textSize, outputY + bitmaps[i].Height / textSize),
                new Vector2(outputX + bitmaps[i].Width / textSize, outputY + bitmaps[i].Height / textSize),
                new Vector2(outputX / textSize, outputY / textSize),
                new Vector2(outputX + bitmaps[i].Width / textSize, outputY / textSize)

            };

            spriteInfo.verts = new Vector3[]
            {
                new Vector3(positionsX[i] * pixelsPerUnit, positionsY[i] * pixelsPerUnit, 0), //top-left
                new Vector3((positionsX[i] + bitmaps[i].Width) * pixelsPerUnit, positionsY[i] * pixelsPerUnit, 0), //top-right
                new Vector3(positionsX[i] * pixelsPerUnit, (positionsY[i] - bitmaps[i].Height) * pixelsPerUnit, 0), //bottom-left
                new Vector3((positionsX[i] + bitmaps[i].Width) * pixelsPerUnit, (positionsY[i] - bitmaps[i].Height) * pixelsPerUnit, 0) //bottom-right
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
        outputBitmap.Serialize(pathBytes);

        saveData.pathsToMCBitmap = new string[] { pathBytes }; // this should be the path to the corresponding sheet
        spriteInfoSaveData.spriteInfos = spriteInfos.ToArray();
        File.WriteAllText(pathData, JsonUtility.ToJson(saveData, true));
        File.WriteAllText(pathSpriteInfos, JsonUtility.ToJson(spriteInfoSaveData, true));
    }

    // the final version of this must be a function that returns both a Texture2D[], an PartAnimationState[], and a PartVertAndUVData[]
    // we have to do this for every part... so do we just automatically set?
    // we could also return some struct contianing all of the above??
    // 
    private void GetValidIndices(List<int> validIndices, string partPath, bool keepLists = false)
    {
        if (!keepLists)
		{
            validIndices.Clear();
            bitmaps.Clear();
            positionsX.Clear();
            positionsY.Clear();
        }

        var currentPaletteDir = paletteDirectory;
        int mechIndexAdjustedForPart = (int)mechIndex;
        if (!keepLists && partPath.Contains("LEGS"))
        {
            // adjust for legs
            if (mechIndex == MechSHPIndexTable.HUNCHBACK_IIC) mechIndexAdjustedForPart = 1; // if mechindex == 16, legsindex = 1
            else if (mechIndex == MechSHPIndexTable.FIRESTARTER) mechIndexAdjustedForPart = 7; // if mechindex == 11, legsindex = 7
            else if (mechIndex >= MechSHPIndexTable.VULTURE) mechIndexAdjustedForPart -= 5;// if mechindex >= 15, legsindex = mechindex - 5
            else if (mechIndex >= MechSHPIndexTable.MASAKARI) mechIndexAdjustedForPart -= 3; // if mechindex >= 12, legsindex = mechindex - 3
            else if (mechIndex >= MechSHPIndexTable.ULLER) mechIndexAdjustedForPart -= 2; // if mechindex >= 6, legsindex = mechindex - 2
        }
        //if (dirForLocalTileSpriteFiles.Contains("GTILE")) currentPaletteDir = g_paletteDirectory;
        var outerPak = new PakFile(Path.Combine(gameFilesLoader.localMCGPath, partPath), fileManager.File(currentPaletteDir));
        var mechPartPak = new PakFile(outerPak.GetFileInner(mechIndexAdjustedForPart), fileManager.File(currentPaletteDir));
        var length = mechPartPak.PakInnerFileCount;
        tileSizeX = 0;
        tileSizeY = 0;
        int minTileSizeX = 0;
        int minTileSizeY = 0;
        int headerLength = 6;
        int numOfSingleBMPFiles = 0;
        int numOfMultiBMPFiles = 0;
        int totalImageCount = 0;
        string allSingleSpritesText = ""; // string we can print later to identify indices of single
        string allMultiSpritesText = ""; // string we can print later to id multi
        string allMultiImageCounts = "";
        byte[] mechData;
        //List<sbyte> positionsX = new List<sbyte>();
        //List<sbyte> positionsY = new List<sbyte>();
        List<AnimationData> animationDatas = new List<AnimationData>();
        List<AnimationData> singleFrameDatas = new List<AnimationData>();
        int indexOfAnimationStart = 0;
        int numOfImages = 0;
        int numOfFaces = 0;
        int animationNameIndex = 0;
        int singleImagesIndex = 0;
        bool singleImagesStart = false;
        MCBitmap outputBitmap;
        Debug.Log("attempting to get " + mechIndex + " from " + partPath + " pak count " + length);

        // here add a check if the mech is already cached; 

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
            //Debug.Log("testing at " + i);
            var mech = new ShpFile(mechData);
            if (mech.ImgLength == 1)
            {
                allSingleSpritesText += validIndices.Count + "\n";
                numOfSingleBMPFiles++;
                if (!singleImagesStart && (numOfImages > 1 || i == 0))
                {
                    singleImagesStart = true;
                    singleImagesIndex = validIndices.Count;
                }
            }
            else if (mech.ImgLength > 1)
            {
                allMultiSpritesText += validIndices.Count + "\n";
                allMultiImageCounts += mech.ImgLength + "\n";
                numOfMultiBMPFiles++;

                if (singleImagesStart) // we only put in the animation if the count changes
                {
                    singleImagesStart = false;
                    string singleFrameName = "SHUTDOWN"; // we assume the first set of singles are shutdown
                    if (singleFrameDatas.Count == 1) singleFrameName = "IDLE"; // 2nd set is idle
                    else if (singleFrameDatas.Count > 1) singleFrameName = "SINGLE_" + (singleFrameDatas.Count - 2); // some contain other singles but unknown

                    singleFrameDatas.Add(new AnimationData(singleFrameName, (short)singleImagesIndex, 1, (byte)(validIndices.Count - singleImagesIndex)));
                }

                // whenever the count of frames changes, we are (probably) in a new set of animations - make sure??
                if ((numOfImages != mech.ImgLength && numOfFaces >= 4) || // 4 is the minimum? will have to check further
                    (animationNameIndex > 0 && animationNameIndex <= 7 && numOfFaces >= animationDatas[0].NumOfFaces)) // sometimes we go past because same count 
                {
                    animationNameIndex = Mathf.Min(animationNameIndex, animationNames.Length - 1);
                    animationDatas.Add(new AnimationData(animationNames[animationNameIndex++], (short)indexOfAnimationStart, (byte)numOfImages, (byte)numOfFaces));
                    numOfFaces = 0; // restart
                    indexOfAnimationStart = validIndices.Count;
                }
                else if (numOfImages != mech.ImgLength) indexOfAnimationStart = validIndices.Count;

                numOfFaces++;
                numOfImages = mech.ImgLength;

                totalImageCount += mech.ImgLength;
            }
            //outputBitmap = mech.GetBitMap(0); // assign a default
            // add all the bitmaps to the main list
            for (int j = 0; j < mech.ImgLength; j++)
            {
                outputBitmap = mech.GetBitMap(j); // this is the index of the animation

                if (outputBitmap == null)
                {
                    //Debug.Log("bmp is null");
                    continue;
                }

                validIndices.Add(i); // this will total the number of images and mark animations as duplicate indices
                bitmaps.Add(outputBitmap);
                // THESE positions have to be considered when we make the uv/mesh array
                positionsX.Add((sbyte)mech.GetHeaderStartX(j));
                positionsY.Add((sbyte)mech.GetHeaderStartY(j));

                tileSizeY = Mathf.Max(tileSizeY, outputBitmap.Height);
                tileSizeX = Mathf.Max(tileSizeX, outputBitmap.Width);

                if (minTileSizeX == 0) minTileSizeX = outputBitmap.Width;
                if (minTileSizeY == 0) minTileSizeY = outputBitmap.Height;

                minTileSizeY = Mathf.Min(tileSizeY, outputBitmap.Height);
                minTileSizeX = Mathf.Min(tileSizeX, outputBitmap.Width);

                //tileInfoDict.Add(i, new Vector3(outputBitmap.Width, outputBitmap.Height, 0));
                // test

            }

        }

        // last check if an animation was found at the end of the loop
        if (indexOfAnimationStart != animationDatas[animationDatas.Count - 1].IndexOfStart)
        {
            animationNameIndex = Mathf.Min(animationNameIndex, animationNames.Length - 1);
            animationDatas.Add(new AnimationData(animationNames[animationNameIndex], (short)indexOfAnimationStart, (byte)numOfImages, (byte)numOfFaces));
        }

        // we should also add the animation state for reverse which is walk at negative speed
        // we should also add the idle to reverse which is idle to walk at negative
        // we should also add reverse to idle

        var pathToMechFolder = Path.Combine(pathToResourceFolder, (int)mechIndex + "_" + mechIndex.ToString());
        // IF WE WERE TO SKIP SAVING TO FILE... we can just get all data into a struct that we can return:
        // all position and animation data and the Texture2D
        var stride = targetAtlasSize / tileSizeX; // no padding??
        var saveDB = new PartAnimationDBSet(mechName, singleFrameDatas.ToArray(), animationDatas.ToArray(), positionsX.ToArray(), positionsY.ToArray(), stride);
        File.WriteAllText(Path.Combine(pathToMechFolder, mechName + "_AnimData.Json"), JsonUtility.ToJson(saveDB, true));
        
        if (!generateIndexTextFiles) return;

        File.WriteAllText(Path.Combine(pathToMechFolder, mechName + "_allSingleIndices.txt"), allSingleSpritesText);
        File.WriteAllText(Path.Combine(pathToMechFolder, mechName + "_allMultiIndices.txt"), allMultiSpritesText);
        File.WriteAllText(Path.Combine(pathToMechFolder, mechName + "_allMutliImageCounts.txt"), allMultiImageCounts);

        if (!displayInfoText) return;
        Debug.Log("max tile size X " + tileSizeX + " max tile size Y " + tileSizeY + " atlas size " + (4096 / (tileSizeX + 2)) + " x " + (4096 / (tileSizeY + 2)));
        Debug.Log("min tile size X  " + minTileSizeX + " min tile size Y " + minTileSizeY);
        Debug.Log("single image count = " + numOfSingleBMPFiles + " multi image count " + numOfMultiBMPFiles);
        Debug.Log("total images " + (numOfSingleBMPFiles + totalImageCount));

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


    public void CreateBlankSpriteSheetAtAtlasSize()
    {
        //if (validIndices == null || validIndices.Count == 0) CreateValidSpriteIndices();
        //Debug.Log("valid ind " + validIndices.Count + " length to get " + length + " total sheets " + (validIndices.Count * 1f / (length * 1f)));
        //int numberOfSheets = Mathf.CeilToInt(validIndices.Count * 1f / (length * 1f));
        Debug.Log("number of sheets " + pakFilesToCopy.Length); // might change later to something dynamic
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

    public bool ValidateMCGAssetPath(string path)
    {
        var pathToData = Path.Combine(path, "DATA");
        if (!Directory.Exists(pathToData))
        {
            Debug.Log("Can´t find: " + pathToData);
            return false;
        }

        string pathToAsset;
        for (int i = 0; i < pakFilesToCopy.Length; i++)
        {
            pathToAsset = Path.Combine(pathToData, pakFilesToCopy[i]);
            if (!File.Exists(pathToAsset))
            {
                Debug.Log("Can´t find: " + pathToAsset);
                return false;
            }
        }

        Debug.Log("file found " + path);
        return true;
    }

    public List<string> GetAllTexturePaths()
	{
        MechSHPIndexTable mechs = MechSHPIndexTable.AWESOME;
        PartIndex parts = PartIndex.L_ARMS;
        string currentDir, partDir;
        pathsToSpriteSheets.Clear();

        for (int i = 0; i <= (int)MechSHPIndexTable.BUSHWACKER; i++)
        {
            currentDir = (int)mechs + "_" + mechs.ToString();

            parts = PartIndex.L_ARMS;
            for (int j = 0; j <= (int)PartIndex.TORSOS; j++)
            {
                partDir = Path.Combine(currentDir, parts.ToString() + "/" + parts.ToString());
                pathsToSpriteSheets.Add(Path.Combine(pathToResourceFolder, partDir + "_0.png"));
                pathsToSpriteSheets.Add(Path.Combine(pathToResourceFolder, partDir + "_1.png")); // some parts need two sheets


                parts++;
            }
            mechs++;
        }

        return pathsToSpriteSheets;
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

#if UNITY_EDITOR
    [CustomEditor(typeof(MechSpriteLoader_OLD))]
    public class SpriteLoaderEditor : Editor
    {

        MechSpriteLoader_OLD editor;

        public override void OnInspectorGUI()
        {
            editor = target as MechSpriteLoader_OLD;

            DrawDefaultInspector();

            if (GUILayout.Button("Create Directories")) editor.CreateDirectoryStructure();
            //if (GUILayout.Button("Create Blank Sprite Sheets")) editor.CreateBlankSpriteSheetAtAtlasSize();
            //if (GUILayout.Button("Create Mech Sprite files")) editor.CreateMechSprites();
            //if (GUILayout.Button("Create Mech Anim files")) editor.CreateMechAnimData();
            //if (GUILayout.Button("Create 1 Sheet For Parts")) editor.CreateContinuousPartSheet();
            //if (GUILayout.Button("Create Sheets For Parts")) editor.CreateAllPartsInFolders();
            if (GUILayout.Button("Asses Sheets For Parts")) editor.AssesAllParts();
            if (GUILayout.Button("Cache Data")) editor.CacheData();
            if (GUILayout.Button("Asses Last Y Coord for Cached Part")) editor.AssesDataForLastPixelYCoord();
            if (GUILayout.Button("Display YMax and SpaceLeft of Part")) editor.DisplayYSpaceLeftAndYMaxOfPart();
            if (GUILayout.Button("Print all Cached Parts")) editor.PrintAllTexturesFromCache();
            if (GUILayout.Button("Print Cached Mech")) editor.PrintTextureFromCurrentMechCache();
            if (GUILayout.Button("Create Arms Atlas of Mech")) editor.AppendArmsOfCurrentMech();
            if (GUILayout.Button("Create LegsTorso Atlas of Mech")) editor.AppendTorsoToLegsOfCurrentMech();
            if (GUILayout.Button("Append Part to Atlas of Mech")) editor.AppendToLastAtlasCreated();
            if (GUILayout.Button("Print Atlas of Mech")) editor.PrintCurrentMechAtlas();
            //if (GUILayout.Button("Print Mech BMP")) editor.PrintBitmaps();
            //if (GUILayout.Button("Prep All Sheets")) ModifyTextures();

        }

        private void ModifyTextures()
        {
            foreach(string assetPath in editor.GetAllTexturePaths())
			{
                Debug.Log("try get " + assetPath);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    // Set the global max size limit
                    importer.maxTextureSize = editor.targetAtlasSize;
                    //importer.textureFormat = TextureImporterFormat.Alpha8;
                    importer.filterMode = FilterMode.Point;
                    // Save changes and re-import the asset to apply the new size
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

    }
#endif

}
// can we try to print all the bitmaps for a part into sheets? 
// if we were to use the packer, we would still need to be able to get all the bitmaps into several sheets which im not sure the packer is able to do
// we could for now just keep printing per part image and just adjust the next y by the max image in the line 
// i think we have to dynamically create the textures instead of loading them all from disk...
// best practice is probably just creating 4096 textures as needed and packing them all in with 1 mech at a time
// so the same with the map, we get all the mech indices that are loaded this map, and we print ONLY the tile sheets needed for those mechs
// i think we go from mech1 legs -> mech1 torso -> mech1 larm - > mech1 rarm before going to mech2 
// the max is around 37 or sprite sheets for all of the mechs which is absurd but it will have to do ...
// BUT if we limit unit count we might be able to get away with less

// we might want to just limit the sheets we have to generate at runtime
// we already have the data for all the mechs / parts laid out in disk, we load every mech we need sequentially and look at the space left as we try to compact 
// the way we do this is by testing first if the new save has enough space to load in all of the texture sheet = FOR NOW
// the fastest way to do this is to just keep packing the sheets into the previous but you will have to deal with texture switching
// its best not to split the animation in the middle tho... so we have to be aware what animation should be used?
// we should use materialpropertyblock as i think it prevents hitches from the gc to assign the texture
// for every unit, we have its own material with its own texture, then we just use the materialpropertyblock to change the texture
// modifying the mesh directly is still quicker for uvs and verts
// FOR NOW, lets just pack the mechs as small as we can maybe 2 sheets per
// this gets us to make a demo at least for the mean time... lets optimize the sheets  LATER

/*
public void AppendAllPartsOfCurrentMech()
{
    string folder = Path.Combine(pathToResourceFolder, GetFolderPathOfMech());
    string pathBytes = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + ".bytes"); // we will store this for the save
    string pathData = Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + spriteDataFileExt);

    var origData = LoadDataFromCache(Path.Combine(folder, PartIndex.LEGS + "_" + 0 + spriteDataFileExt));
    var larmData = LoadDataFromCache(Path.Combine(folder, PartIndex.L_ARMS + "_" + 0 + spriteDataFileExt));
    var rarmData = LoadDataFromCache(Path.Combine(folder, PartIndex.R_ARMS + "_" + 0 + spriteDataFileExt));
    var torsoData = LoadDataFromCache(Path.Combine(folder, PartIndex.TORSOS + "_" + 0 + spriteDataFileExt));

    SpriteInfoArraySaveData saveData;

    AppendBitmap(origData, larmData, out saveData);
    //origData = LoadDataFromCache(Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + spriteDataFileExt));
    AppendBitmap(saveData, rarmData, out saveData);
    //origData = LoadDataFromCache(Path.Combine(folder, mechIndex.ToString() + "_Atlas_" + 0 + spriteDataFileExt));
    var atlas = AppendBitmap(saveData, torsoData, out saveData);

    atlas.Serialize(pathBytes);
    saveData.pathToMCBitmap = pathBytes;
    File.WriteAllText(pathData, JsonUtility.ToJson(saveData, true));
}
*/