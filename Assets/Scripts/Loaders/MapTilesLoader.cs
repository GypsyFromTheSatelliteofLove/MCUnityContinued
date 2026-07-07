using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MechCommanderUnity.API;
using MechCommanderUnity.Utility;
/// <summary>
/// This is a simple way to get the ground tile sprites
/// it just iterates thru the pak files to find all the relevant sprites and print them to preset sprite sheets considering their max and min sizes
/// for now this has no packing, it just takes the max sizes and uses that to create uniform spacing
/// IT IS BEST TO CREATE BLANK SHEETS FIRST - you modify their size and settings before printing the data on to them (automatically overwrites blanks)
/// if the blank sprite sheet is not set to 4096 or whatever preset size the sheet is supposed to be, the indexing will get messed up (i think)
/// </summary>

//OLD NOTES
// with this we have proven we can read the data and load a game type envi out of it
// BUT the effeciency is still lacking... and the iron arthur solution is probs the best (making meshes and then using tiles as textures)
// can we make that into a map creator system using just the uvs? its a good challenge...
// the rest we can still do with traditional sprite systems in unity as that would be easiest to control?

// Sprite sheets with palettes are now ok... we just need to save them as png without mipmaps, filters, and alpha8 format for main tex; RGBA8 is fine for palettes

// OK hit a snag with the GMM file because the API never fully decoded it...
// so ... what would be more realistic? decoding the GMM FILE by trawling the raw binary data?
// - the 1st idea here would be to create custom maps with very limited info and try decoding the gmm for that
// - the problem is that we have to get the editor to work first
// THE other option is to just recreate the game itself
// - pathfinding will be done with unity systems... but this might have problems in itself?
// - the animation system has to be automated
// - mech lab and shop system
// - pilot and and other modifiers
// - explosions and weapon effects
// - i dont know if it would work... the game would be absolutely buggy probably hehe - and it might not be worth the effort for now :( 
// - but a quick and dirty working example thats open source might still be worth something?
// - to recreate the menu stuff is probs the easiest 
// - to recreate the weapon effects and other things would be hard
// - the navigation would be pathfinder based probably... but maybe we can use pixel based movement 
public class MapTilesLoader : MonoBehaviour
{
    [SerializeField]
    private GameFilesLoader gameFilesLoader;
    private FileManager fileManager;

    public GameObject testTileSpritePrefab;
    public Material origPaletteMat;
    public Material expPaletteMat;
    public Shader shader;

    public List<int> validIndices = new List<int>(); // stores all the valid pak file sprite indices
    public List<int> validExpIndices = new List<int>(); // stores all the valid pak file sprite indices
    public List<string> mapList = new List<string>();
    //public List<GetMaterialFromObject> printers = new List<GetMaterialFromObject>();

    private string missionName;
    private string dirForLocalTileSpriteFiles;
    public string pathToResourceFolder = "Assets/Resources/GroundTiles";

    public enum MapTileSet
	{
        ORIGINAL = 0,
        EXPANSION = 1
	};

    public MapTileSet currentTileSet;

    //public int startIndex;
    // this is how many sprites we plan to print on one sheet... 374 seems to be a good num for 4096 at uniform size
    public int length = 374;

    public int tileSizeX;
    public int tileSizeY;

    public int targetAtlasSize = 4096;

    public Sprite[] allGroundTiles;
    public Sprite[] allExpGroundTiles;

    // can be checked with valid index checker below
    private const int maxKnownTileSizeX = 232;
    private const int maxKnownTileSizeY = 180; // actually 179 but for eveness

    private string paletteDirectory = @"palette\HB.PAL";
    private string g_paletteDirectory = @"palette\GHB.PAL";
    private string localDir;

    // these constants seem to be fixed in the game - the meters per elev is stored in the fit files but it returns some funky numbers :( 
    private const float tileHalfDistanceX = 2.28f / 2f;
    private const float tileHalfDistanceY = 1.28f / 2f;
    private const float metersPerElevation = 23f; 

    // we try to store the pak index, width height plus 1 more clue? // i dont know what the other clue will be yet
    private Dictionary<int, Vector3> tileInfoDict
        = new Dictionary<int, Vector3>();

    //public bool useRawData;
    public bool useAltPalette;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SetupFileManager()
    {
        if (fileManager == null && gameFilesLoader != null)
        {
            fileManager = gameFilesLoader.FileManager; // important to get at files in structure but will read thru and extract important
        }

        if (fileManager == null)
        {
            fileManager = new FileManager(gameFilesLoader.localMCGPath);
        }

        localDir = gameFilesLoader.localMCGPath;
    }

    public void SetupMapList()
    {
        SetupFileManager();

        if (gameFilesLoader != null && gameFilesLoader.ValidateMCGAssetPath(gameFilesLoader.localMCGPath))
        {
            mapList.Clear();

            var allMaps = fileManager.Files(s => s.StartsWith("MISSIONS") && s.EndsWith(".FIT"));
            mapList.AddRange(allMaps.Keys.Select(Path.GetFileNameWithoutExtension));
        }
    }
    public void CreateBlankSpriteSheetAtAtlasSize()
    {
        currentTileSet = MapTileSet.ORIGINAL;
        
        CreateValidSpriteIndices();
        Debug.Log("valid ind " + validIndices.Count + " length to get " + length + " total sheets " + (validIndices.Count / (length * 1f)));
        int numberOfSheets = Mathf.CeilToInt(validIndices.Count / (length * 1f));
        Debug.Log("number of sheets for orig " + numberOfSheets);
        for (int i = 0; i < numberOfSheets; i++)
        {
            CreateBlankSpriteSheetAtAtlasSize(targetAtlasSize, "Original/GroundTileSheet_" + i);
        }

        currentTileSet = MapTileSet.EXPANSION;
        
        CreateValidSpriteIndices();
        Debug.Log("valid ind " + validExpIndices.Count + " length to get " + length + " total sheets " + (validExpIndices.Count / (length * 1f)));
        numberOfSheets = Mathf.CeilToInt(validExpIndices.Count / (length * 1f));
        Debug.Log("number of sheets for orig " + numberOfSheets);
        for (int i = 0; i < numberOfSheets; i++)
        {
            CreateBlankSpriteSheetAtAtlasSize(targetAtlasSize, "Expansion/GroundTileSheet_" + i);
        }

    }

    public void CreateBlankSpriteSheetAtAtlasSize(int targetAtlasSize = 4096, string fileName = "Sheet_")
    {
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
        DestroyImmediate(texture);

        // Define save path in Assets folder
        string path = Path.Combine(pathToResourceFolder, fileName + ".png");
        Debug.Log("printing to path " + path);
        File.WriteAllBytes(path, bytes);

    }

    public void CreateValidSpriteIndices()
	{
        CreateValidSpriteIndices("TILES/TILES90.PAK", validIndices);
        CreateValidSpriteIndices("TILES/GTILES90.PAK", validExpIndices);
	}

    public void CreateValidSpriteIndices(string filePath, List<int> validList)
    {
        tileInfoDict.Clear();

        SetupFileManager();
        validList.Clear();

        var currentPaletteDir = paletteDirectory;
        if (dirForLocalTileSpriteFiles.Contains("GTILE")) currentPaletteDir = g_paletteDirectory;
        var pak = new PakFile(Path.Combine(localDir, dirForLocalTileSpriteFiles), fileManager.File(currentPaletteDir));
        int length = pak.PakInnerFileCount;
        tileSizeX = 0;
        tileSizeY = 0;
        int minTileSizeX = 0;
        int minTileSizeY = 0;
        byte[] tileData;
        MCBitmap outputBitmap;

        for (int i = 0; i < length; i++)
        {
            tileData = pak.GetFileInner(i); // test
            if (tileData == null)
            {
                //Debug.Log("tile dat is null");
                continue;
            }
            if (tileData[0] == 68 && tileData[1] == 78) continue;

            var tile = new MCTileFile(tileData);
            outputBitmap = tile.GetBitMap();

            if (outputBitmap == null)
            {
                Debug.Log("bmp is null");
                continue;
            }

            validList.Add(i);

            tileSizeY = Mathf.Max(tileSizeY, outputBitmap.Height);
            tileSizeX = Mathf.Max(tileSizeX, outputBitmap.Width);

            if (minTileSizeX == 0) minTileSizeX = outputBitmap.Width;
            if (minTileSizeY == 0) minTileSizeY = outputBitmap.Height;

            minTileSizeY = Mathf.Min(tileSizeY, outputBitmap.Height);
            minTileSizeX = Mathf.Min(tileSizeX, outputBitmap.Width);

            tileInfoDict.Add(i, new Vector3(outputBitmap.Width, outputBitmap.Height, 0));

        }

        Debug.Log("max tile size X " + tileSizeX + " max tile size Y " + tileSizeY + " atlas size " + (4096 / (tileSizeX + 2)) + " x " + (4096 / (tileSizeY + 2)));
        Debug.Log("min tile size X  " + minTileSizeX + " min tile size Y " + minTileSizeY);
        Debug.Log("number of tiles dict " + tileInfoDict.Count);
    }

    public void GetSpritesFromValidIndices()
	{
        currentTileSet = MapTileSet.ORIGINAL;
        GetSpritesFromValidIndices("TILES/TILES90.PAK", validIndices);

        currentTileSet = MapTileSet.EXPANSION;
        GetSpritesFromValidIndices("TILES/GTILES90.PAK", validExpIndices);
	}

    public void GetSpritesFromValidIndices(string filePath, List<int> validList)
    {
        if (validList == null || validList.Count == 0)
		{
            Debug.Log("list is invalid!");
            return;
		}
        SetupFileManager();

        //printers.Clear();
        // this is decompressing paks... this one is important
        var pak = new PakFile(Path.Combine(localDir, filePath), fileManager.File(paletteDirectory));

        byte[] tileData;
        MCBitmap outputBitmap;
        List<MCBitmap> bitmaps = new List<MCBitmap>();
        int numberOfPrints = 0;
        Vector3 printerPos = Vector3.zero;

        for (int i = 0; i < validList.Count; i++)
        {
            tileData = pak.GetFileInner(validList[i]); // test
            if (tileData == null)
            {
                //Debug.Log("tile dat is null");
                continue;
            }

            if (tileData[0] == 68 && tileData[1] == 78) continue;

            var tile = new MCTileFile(tileData);
            outputBitmap = tile.GetBitMap();

            if (outputBitmap == null)
            {
                Debug.Log("bmp is null");
                continue;
            }

            bitmaps.Add(outputBitmap);

            // check if we have reached the length of sprites to fill a sheet
            if (i > 0 && ((i + 1) % length == 0 || i >= validIndices.Count - 1))
            {
                // output the current bitmaps
                PrintBitmaps(bitmaps, printerPos, "GroundTileSheet_" + numberOfPrints++);
                
                bitmaps.Clear();
                printerPos.x += 20;
            }

        }

    }

    public void PrintBitmaps(List<MCBitmap> bitmaps, Vector3 printerPos, string fileName = "Sheet_")
    {
        MCBitmap outputBitmap;
        //Debug.Log("total number of valid files " + bitmaps.Count);
        int tileMaxY = maxKnownTileSizeY;
        int tileMaxX = maxKnownTileSizeX;
        int yEndPadding = 2;
        int xEndPadding = 2;
        int atlasMaxX = targetAtlasSize / (tileMaxX + xEndPadding);
        int atlasMaxY = targetAtlasSize / (tileMaxY + yEndPadding);


        Debug.Log("will print " + atlasMaxX + " x " + atlasMaxY + " maxWidth " + tileMaxX + " x max Height " + tileMaxY);

        outputBitmap = new MCBitmap((tileMaxX + xEndPadding) * atlasMaxX, (tileMaxY + yEndPadding) * atlasMaxY);

        int bytes = outputBitmap.Stride * outputBitmap.Height;
        var rgbValues = new byte[bytes];

        int outputX = 0, outputY = 0, currentRgbIndex, currentBitMapIndex;

        for (int i = 0; i < bitmaps.Count; i++)
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
            outputX += tileMaxX + xEndPadding;
            //if (outputX < 0)
            if (outputX >= outputBitmap.Stride)
            {
                //if (outputY - (tileMaxY + yEndPadding) < 0) break;
                if (outputY + tileMaxY + yEndPadding >= outputBitmap.Height) break;

                //outputX = outputBitmap.Stride - (tileMaxX + xEndPadding);
                outputX = 0;
                //outputY -= tileMaxY + yEndPadding;
                outputY += tileMaxY + yEndPadding;
            }

        }

        outputBitmap.Data = rgbValues;

        // these are the doing the heavy lifting in terms of bytes to image
        var mainTex = ImageProcessing.MakeIndexedTexture2D(outputBitmap);

        // Encode to PNG
        byte[] imageBytes = mainTex.EncodeToPNG();
        DestroyImmediate(mainTex);

        // Define save path in Assets folder
        string folder = currentTileSet == MapTileSet.ORIGINAL ? "Original" : "Expansion";
        string path = Path.Combine(pathToResourceFolder, folder);
        path = Path.Combine(path, fileName + ".png");
        File.WriteAllBytes(path, imageBytes);

    }


    public void GenerateMapFromTilesInResources()
    {
        SetupFileManager();
        Debug.Log("checking map elv for " + missionName);

        // from inside mission
        var missionFileName = Path.ChangeExtension(missionName, "fit");

        var missionFile = new FITFile(fileManager.File(MCGExtensions.PathCombine(new string[] { "missions", missionFileName })));
        if (missionFile.SectionNumber == 0)
        {
            Debug.Log("invalid mission file");
            return;
        }

        missionFile.SeekSection("TerrainSystem");

        string terrainFileName;
        missionFile.GetString("TerrainFileName", out terrainFileName);//System.IO.Path.ChangeExtension(missionName, "fit");

        // from Inside terrain
        string terrainFitFileName = Path.ChangeExtension(terrainFileName, "fit");

        FITFile terrainFitFile = new FITFile(fileManager.File(MCGExtensions.PathCombine(new string[] { "terrain", terrainFitFileName })));

        terrainFitFile.SeekSection("TerrainData");

        int verticesBlockSide, blocksMapSide, visibleVerticesPerSide;
        float metersPerElv, metersPerVertex;

        terrainFitFile.GetInt("VerticesBlockSide", out verticesBlockSide);
        terrainFitFile.GetInt("BlocksMapSide", out blocksMapSide);

        terrainFitFile.GetFloat("MetersPerElevLevel", out metersPerElv);
        terrainFitFile.GetFloat("MetersPerVertex", out metersPerVertex);

        terrainFitFile.GetInt("VisibleVerticesPerSide", out visibleVerticesPerSide);

        terrainFitFile.SeekSection("TileData");

        string terrainTileFile;
        terrainFitFile.GetString("TerrainTileFile", out terrainTileFile);

        int realVerticesMapSide = verticesBlockSide * blocksMapSide;
        /*
        int halfVerticesMapSide = realVerticesMapSide / 2;

        float worldUnitsMapSide = realVerticesMapSide * metersPerVertex;

        int numObjBlocks = blocksMapSide * blocksMapSide;
        int numObjVertices = verticesBlockSide * verticesBlockSide;
        */
        GameObject mapParent = new GameObject(missionName + " Map");

        // from inside terrainTiles
        string ElvFileName = Path.ChangeExtension(terrainFileName, "elv");

        MapElvFile terrainElvFile = new MapElvFile(fileManager.File(MCGExtensions.PathCombine(new string[] { "terrain", ElvFileName })), blocksMapSide, verticesBlockSide);

        MapElvFile.MCTile[] blocks = terrainElvFile.GetVertices();

        Debug.Log("blocks length " + blocks.Length + " blocksMapSide " + blocksMapSide + " vertBlockside " + verticesBlockSide);

        // the blocks are also used to get the position... basically per block, there are vertices that are printed 
        // for x  for y blocks, for i for j vertices print block thru conversion of index
        GameObject mapTile;
        Vector3 tilePos = Vector3.zero;
        float elv = 0;

        Material currentMaterial = currentTileSet == MapTileSet.ORIGINAL ? origPaletteMat : expPaletteMat;
        var currentGroundTiles = currentTileSet == MapTileSet.ORIGINAL ? allGroundTiles : allExpGroundTiles;
        // iterate thru the blocks... get the terrain sprite from blocks[i].Terrain
        for (int x = 0; x < blocksMapSide; x++)
        {
            for (int y = 0; y < blocksMapSide; y++)
            {
                int blockIndex = y * blocksMapSide + x;
                //mapTile = new GameObject("Block " + blockIndex.ToString());


                for (int vX = 0; vX < verticesBlockSide; vX++)
                {
                    for (int vY = 0; vY < verticesBlockSide; vY++)
                    {
                        int vertIndex = vX * verticesBlockSide + vY;
                        elv = blocks[blockIndex * verticesBlockSide * verticesBlockSide + vertIndex].Elevation;
                        //Debug.Log("elv " + elv);
                        mapTile = Instantiate(testTileSpritePrefab, mapParent.transform);
                        mapTile.name = "Tile " + (blockIndex * verticesBlockSide * verticesBlockSide + vertIndex);
                        tilePos = MapPositionToWorldCoords(blockIndex, blocksMapSide, vertIndex, verticesBlockSide, elv, metersPerElv);

                        mapTile.transform.position = tilePos;

                        int spriteIndex = validIndices.IndexOf(blocks[blockIndex * verticesBlockSide * verticesBlockSide + vertIndex].Terrain);
                        spriteIndex = Mathf.Max(spriteIndex, 0);
                        if (spriteIndex >= currentGroundTiles.Length)
                        {
                            Debug.Log("sprite index oob " + spriteIndex);
                            spriteIndex = currentGroundTiles.Length - 1;
                        }

                        mapTile.GetComponent<SpriteRenderer>().sprite = currentGroundTiles[spriteIndex];
                        mapTile.GetComponent<SpriteRenderer>().sharedMaterial = currentMaterial; // this can be changed for the expansion stuff? but not yet implemented

                        /*
                        if (vX > 2 && vY > 2)
                        {
                            return; // test
                        }*/
                    }

                }
                /*
                if (x == 0 && y > 0) // test ... theres a lot of blocks to be painted... maybe paint only the first block
                {
                    // return; // test
                }*/

            }
        }
        // print the block as texture? or convert as sprite
        // instantiate go and add texture thru mat or sprite
        // position based on block
    }

    public Vector3 MapPositionToWorldCoords(int block, int blocksMapSide, int vertex, int vertBlockSide, float elv, float metersPerElv, int pixelOffsetX = 0, int pixelOffsetY = 0)
    {
        var x = block % blocksMapSide;
        var y = block / blocksMapSide;

        var i = vertex % vertBlockSide;
        var j = vertex / vertBlockSide;

        Vector3 BasePosition = new Vector3((x * vertBlockSide) + i,
                  (y * vertBlockSide) + j,
                  elv); // elv is not needed :/

        Vector3 IsoPosition = Vector3.zero;

        IsoPosition.x = ((BasePosition.x - BasePosition.y) * tileHalfDistanceX) + (pixelOffsetX / 100f);// 
        IsoPosition.y = ((BasePosition.x + BasePosition.y) * -tileHalfDistanceY) + (BasePosition.z * (metersPerElevation / 100f)) - (pixelOffsetY / 100f); // 
        // FIX TO IRON ARTHURS VERSION: WE USE 23 pixels as number of meters per elev - the orig meters per elev returns some large numbers


        IsoPosition.z = 0;// (BasePosition.z * ((float)MetersPerElevLevel / 100));

        return IsoPosition;
    }

    // this just prints the images straight to a tile without using the resources 
    public void GenerateMapFromElvFileAndPAK()
    {
        SetupFileManager();
        Debug.Log("checking map elv for " + missionName);

        // from inside mission
        var missionFileName = Path.ChangeExtension(missionName, "fit");

        var missionFile = new FITFile(fileManager.File(MCGExtensions.PathCombine(new string[] { "missions", missionFileName })));
        if (missionFile.SectionNumber == 0)
        {
            Debug.Log("invalid mission file");
            return;
        }

        missionFile.SeekSection("TerrainSystem");

        string terrainFileName;
        missionFile.GetString("TerrainFileName", out terrainFileName);//System.IO.Path.ChangeExtension(missionName, "fit");

        // from Inside terrain
        string terrainFitFileName = Path.ChangeExtension(terrainFileName, "fit");

        FITFile terrainFitFile = new FITFile(fileManager.File(MCGExtensions.PathCombine(new string[] { "terrain", terrainFitFileName })));

        terrainFitFile.SeekSection("TerrainData");

        int verticesBlockSide, blocksMapSide, visibleVerticesPerSide;
        float metersPerElv, metersPerVertex;

        terrainFitFile.GetInt("VerticesBlockSide", out verticesBlockSide);
        terrainFitFile.GetInt("BlocksMapSide", out blocksMapSide);

        terrainFitFile.GetFloat("MetersPerElevLevel", out metersPerElv);
        terrainFitFile.GetFloat("MetersPerVertex", out metersPerVertex);

        terrainFitFile.GetInt("VisibleVerticesPerSide", out visibleVerticesPerSide);

        terrainFitFile.SeekSection("TileData");

        string terrainTileFile;
        terrainFitFile.GetString("TerrainTileFile", out terrainTileFile);

        int realVerticesMapSide = verticesBlockSide * blocksMapSide;
        /*
        int halfVerticesMapSide = realVerticesMapSide / 2;

        float worldUnitsMapSide = realVerticesMapSide * metersPerVertex;

        int numObjBlocks = blocksMapSide * blocksMapSide;
        int numObjVertices = verticesBlockSide * verticesBlockSide;
        */
        GameObject mapParent = new GameObject(missionName + " Map");

        // from inside terrainTiles
        string ElvFileName = Path.ChangeExtension(terrainFileName, "elv");

        MapElvFile terrainElvFile = new MapElvFile(fileManager.File(MCGExtensions.PathCombine(new string[] { "terrain", ElvFileName })), blocksMapSide, verticesBlockSide);

        MapElvFile.MCTile[] blocks = terrainElvFile.GetVertices();

        Debug.Log("blocks length " + blocks.Length + " blocksMapSide " + blocksMapSide + " vertBlockside " + verticesBlockSide);

        // the blocks are also used to get the position... basically per block, there are vertices that are printed 
        // for x for y blocks, for i for j vertices print block thru conversion of index
        GameObject mapTile;
        Vector3 tilePos = Vector3.zero;
        float elv = 0;

        var currentTileDir = terrainTileFile.ToUpper() == "tiles".ToUpper() ? "TILES/TILES90.PAK" : "TILES/GTILES90.PAK";
        string currentPaletteDir = currentTileDir.Contains("GTILE") ? g_paletteDirectory : paletteDirectory;

        if (useAltPalette) currentPaletteDir = g_paletteDirectory;

        var pak = new PakFile(Path.Combine(localDir, currentTileDir), fileManager.File(currentPaletteDir));
        byte[] tileData;
        MCBitmap outputBitmap;
        Shader shader = Shader.Find("MechCommanderUnity/PaletteSwapLookup");
        var palTex = pak.Palette.ExportPaletteTexture();
        Material sharedMat = new Material(shader);
        sharedMat.SetTexture("_PaletteTex", palTex);

        // iterate thru the blocks... get the terrain sprite from blocks[i].Terrain
        for (int x = 0; x < blocksMapSide; x++)
        {
            for (int y = 0; y < blocksMapSide; y++)
            {
                int blockIndex = y * blocksMapSide + x;

                for (int vX = 0; vX < verticesBlockSide; vX++)
                {
                    for (int vY = 0; vY < verticesBlockSide; vY++)
                    {
                        int vertIndex = vX * verticesBlockSide + vY;
                        elv = blocks[blockIndex * verticesBlockSide * verticesBlockSide + vertIndex].Elevation;

                        mapTile = Instantiate(testTileSpritePrefab, mapParent.transform);

                        tileData = pak.GetFileInner(blocks[(blockIndex * verticesBlockSide * verticesBlockSide) + vertIndex].Terrain); // test

                        var tile = new MCTileFile(tileData);
                        outputBitmap = tile.GetBitMap();

                        Material material = new Material(shader);
                        var mainTexture = ImageProcessing.MakeIndexedTexture2D(outputBitmap);
                        material.mainTexture = mainTexture;
                        material.SetTexture("_PaletteTex", palTex);
                        
                        var rect = new Rect(0, 0, mainTexture.width, mainTexture.height);
                        var pivot = new Vector2(0.5f, 1f);
                        Sprite.Create(mainTexture, rect, pivot, 100f);
                        mapTile.name = "Tile " + (blockIndex * verticesBlockSide * verticesBlockSide + vertIndex);
                        tilePos = MapPositionToWorldCoords(blockIndex, blocksMapSide, vertIndex, verticesBlockSide, elv, metersPerElv);

                        mapTile.transform.position = tilePos;
                        mapTile.GetComponent<SpriteRenderer>().sprite = Sprite.Create(mainTexture, rect, pivot, 100f);
                        mapTile.GetComponent<SpriteRenderer>().material = sharedMat;

                        /*if (vX > 2 && vY > 2)
                        {
                            return; // test
                        }*/
                    }

                }
                /*
                if (x == 0 && y > 0) // test ... theres a lot of blocks to be painted... maybe paint only the first block
                {
                    // return; // test
                }*/

            }
        }
    }

    public void GetAllSpritesInResourceFolder()
    {
        List<Sprite> tempGroundTiles = new List<Sprite>();
        
        foreach (Sprite sprite in Resources.LoadAll<Sprite>("GroundTiles/" + "Original"))
        {
            if (sprite.rect.width >= 140f && sprite.rect.height >= 64f) tempGroundTiles.Add(sprite);
            else Debug.Log("sprite size too small, skipping " + sprite.name + " w " + sprite.rect.width + " x h " + sprite.rect.height);
        }
        //tempGroundTiles.Sort((x, y) => x.name.CompareTo(y.name));
        allGroundTiles = tempGroundTiles.ToArray();

        tempGroundTiles.Clear();
        foreach (Sprite sprite in Resources.LoadAll<Sprite>("GroundTiles/" + "Expansion"))
        {
            if (sprite.rect.width >= 140f && sprite.rect.height >= 64f) tempGroundTiles.Add(sprite);
            else Debug.Log("sprite size too small, skipping " + sprite.name + " w " + sprite.rect.width + " x h " + sprite.rect.height);
        }
        //tempGroundTiles.Sort((x, y) => x.name.CompareTo(y.name));
        allExpGroundTiles = tempGroundTiles.ToArray();

    }

    public void PrintPalette()
	{
        SetupFileManager();

        var currentTileDir = currentTileSet == MapTileSet.ORIGINAL ? "TILES/TILES90.PAK" : "TILES/GTILES90.PAK";
        string currentPaletteDir = currentTileSet == MapTileSet.ORIGINAL ? paletteDirectory : g_paletteDirectory;

        var pak = new PakFile(Path.Combine(localDir, currentTileDir), fileManager.File(currentPaletteDir));
        var palTex = pak.Palette.ExportPaletteTexture();

        byte[] bytes = palTex.EncodeToPNG();
        DestroyImmediate(palTex);

        // Define save path in Assets folder
        string fileName = currentTileSet == MapTileSet.ORIGINAL ? "Orig_Palette" : "Exp_Palette";
        string path = Path.Combine("Assets", fileName + ".png");
        Debug.Log("printing to path " + path);
        File.WriteAllBytes(path, bytes);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MapTilesLoader))]
    public class MapTilesLoaderEditor : Editor
    {

        MapTilesLoader editor;
        
        public override void OnInspectorGUI()
        {
            editor = target as MapTilesLoader;

            DrawDefaultInspector();

            if (GUILayout.Button("Setup Map List")) editor.SetupMapList();
            if (GUILayout.Button("Check Valid Indices")) editor.CreateValidSpriteIndices();

            if (GUILayout.Button("Make Sprite Sheets")) editor.GetSpritesFromValidIndices();
            if (GUILayout.Button("Make Blank Sprite Sheet")) editor.CreateBlankSpriteSheetAtAtlasSize();
            if (GUILayout.Button("Get All Sprites in Resource")) editor.GetAllSpritesInResourceFolder();
            
            if (GUILayout.Button("Print Palette")) editor.PrintPalette();

            if (editor.mapList.Count > 0)
			{
                GUILayout.Label("Map tools", EditorStyles.boldLabel);
                GenericMenu menu = new GenericMenu();

                if (GUILayout.Button("Select Map to Load with Tiles"))
                {
                    foreach (var map in editor.mapList)
                    {
                        menu.AddItem(new GUIContent(map), map == editor.missionName, LoadMap, map);
                    }

                    menu.ShowAsContext();
                }

                GenericMenu menu2 = new GenericMenu();

                if (GUILayout.Button("Select Map to Load from Pak"))
                {
                    foreach (var map in editor.mapList)
                    {
                        menu2.AddItem(new GUIContent(map), map == editor.missionName, LoadMapFromPak, map);
                    }

                    menu2.ShowAsContext();
                }
            }

            
        }
        private void LoadMap(object o)
        {
            editor.missionName = (string)o;
            editor.GenerateMapFromTilesInResources();
        }

        private void LoadMapFromPak(object o)
		{
            editor.missionName = (string)o;
            editor.GenerateMapFromElvFileAndPAK();
        }
    }
#endif

}
