using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MechCommanderUnity.API;
using MechCommanderUnity.Utility;

/// <summary>
/// THIS IS BASICALLY A PORT OF WHAT IRON ARTHUR DID BUT IN ONE SINGLE FILE!
/// my assumption is that it will be easier to understand the program flow if its all in here for now
/// This is very clever as it uses tightly packed sprites in a texture and meshes created at runtime to reduce the cost of sprite rendering
/// This is probably the way to go for all of the game but i haven't had time to try and implement it through out :( 
/// </summary>
public class MapLoader : MonoBehaviour
{
    // OVERALL this still needs to be optimized - loads in about 20 or so secs on my setup which is probably generous since it loads only terrain
    // BUT ONCE IT IS CACHED, its faster but on first compile OR first scene load, it takes a bit longer again
    [SerializeField]
    private GameFilesLoader gameFilesLoader;

    private FileManager fileManager;
    [SerializeField]
    private List<string> ListMaps = new List<string>();
    // these Dictionaries might be a bit much... but for just loading a map might be good enough?
    // but dictionaries IIRC are not contigous in memory so it might be a little slower to access specially when loading thousands of tiles
    // BUT the bottleneck is probably more in the creation of meshes and accessing the mesh filter classes?
    private Dictionary<int, MCBitmap> tileDict; // the tiles used
    private Dictionary<int, MCBitmap> tileOVDict;
    private Dictionary<string, TileInfo> tileInfoDict; // could be smaller but for now lets keep it as accurate to IronArthur's version
    private Dictionary<string, TileInfo> tileInfoOVDict;

    public Texture2D mainText;
    public Texture2D mainOVText;
    public Texture2D palText;

    public MapTilesLoader.MapTileSet tileSet;
    public string MapName;

    private string paletteDirectory = @"palette\HB.PAL";
    private string g_paletteDirectory = @"palette\GHB.PAL";
    
    private string terrainFileName, terrainTileFile;

    // these constants seem to be fixed in the game - the meters per elev is stored in the fit files but it returns some funky numbers :( 
    private const float tileHalfDistanceX = 2.28f / 2f;
    private const float tileHalfDistanceY = 1.28f / 2f;
    private const float metersPerElevation = 23f;
    //private const int maxKnownTileSizeX = 232;
    //private const int maxKnownTileSizeY = 180; // actually 179 but for eveness

    private int[] mapTileIndices;

    // this could be smaller but to keep the program stable, i kept it close to the way iron arthur did it
    public struct TileInfo
	{
        //public Rect rect;
        //public Vector2[] uvs; // 4 * 2 * 4 = 32 bytes 
        // LAID out like this, we ensure that the uvs are packed with our struct, arrays are ref types and might not be contiguous to the struct in memory
        public Vector2 uv_0;
        public Vector2 uv_1;
        public Vector2 uv_2;
        public Vector2 uv_3;
        public Vector2 pivot; // 4 * 2 = 8 bytes
        // im pretty sure these are all we need plus a pivot and uvs
        public float halfWidth; // 4 bytes
        public float height; // 4 bytes

        // this is a total of 32 + 8 + 4 + 4 = 48 bytes in length in memory

        public TileInfo(Vector2[] p_uvs, Vector2 p_pivot, float p_halfWidth, float p_height)
		{
            //uvs = p_uvs;
            pivot = p_pivot;
            halfWidth = p_halfWidth;
            height = p_height;

            uv_0 = p_uvs[0];
            uv_1 = p_uvs[1];
            uv_2 = p_uvs[2];
            uv_3 = p_uvs[3];
            //rect = new Rect();
		}
	}

    /// <summary>
    /// Serializable struct for saving the data into json files
    /// </summary>
    [Serializable]
    public struct TileInfoSave
    {
        public Vector2 pivot;
        public Vector2[] uvs;
        public string key; // these are stored like this for now BUT there should be a way to make these into a short/ushort
        public float halfWidth;
        public float height;
    }
    /// <summary>
    /// Wrapper to save the entire array of tileinfosaves into Json
    /// </summary>
    [Serializable]
    public struct TileInfoArraySave
	{
        public TileInfoSave[] tileInfos;
	}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadMap(string mapName)
	{
        SetupFileManager();

        var missionFileName = Path.ChangeExtension(mapName, "fit");

        //Read Mission Fit File
        var missionFile = new FITFile(fileManager.File(MCGExtensions.PathCombine(new string[] { "missions", missionFileName })));
        if (missionFile.SectionNumber == 0)
        {
            Debug.Log("Error Loading Mission FitFile :" + MCGExtensions.PathCombine(new string[] { gameFilesLoader.localMCGPath, "missions", missionFileName }));
        }
        missionFile.SeekSection("TerrainSystem");

        //string terrainFileName;
        missionFile.GetString("TerrainFileName", out terrainFileName);//System.IO.Path.ChangeExtension(missionName, "fit");

        // from Inside terrain
        string terrainFitFileName = Path.ChangeExtension(terrainFileName, "fit");

        FITFile terrainFitFile = new FITFile(fileManager.File(MCGExtensions.PathCombine(new string[] { "terrain", terrainFitFileName })));

        terrainFitFile.SeekSection("TerrainData");

        int verticesBlockSide, blocksMapSide;//, visibleVerticesPerSide;
        //float metersPerElv, metersPerVertex;

        terrainFitFile.GetInt("VerticesBlockSide", out verticesBlockSide);
        terrainFitFile.GetInt("BlocksMapSide", out blocksMapSide);
        /*
        terrainFitFile.GetFloat("MetersPerElevLevel", out metersPerElv);
        terrainFitFile.GetFloat("MetersPerVertex", out metersPerVertex);

        terrainFitFile.GetInt("VisibleVerticesPerSide", out visibleVerticesPerSide);
        */
        terrainFitFile.SeekSection("TileData");

        //string terrainTileFile;
        terrainFitFile.GetString("TerrainTileFile", out terrainTileFile);
        /*
        int realVerticesMapSide = verticesBlockSide * blocksMapSide;
        
        int halfVerticesMapSide = realVerticesMapSide / 2;

        float worldUnitsMapSide = realVerticesMapSide * metersPerVertex;

        int numObjBlocks = blocksMapSide * blocksMapSide;
        int numObjVertices = verticesBlockSide * verticesBlockSide;
        */

        // get elevvation file which contains the indices to the sprites in the pak
        string ElvFileName = Path.ChangeExtension(terrainFileName, "elv");

        MapElvFile terrainElvFile = new MapElvFile(fileManager.File(MCGExtensions.PathCombine(new string[] { "terrain", ElvFileName })), blocksMapSide, verticesBlockSide);
        MapElvFile.MCTile[] blocks = terrainElvFile.GetVertices();
        // get only the indices that are used by the map 
        mapTileIndices = terrainElvFile.GetDifferentTileIds().ToArray();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        // IF we have cached info, use that instead of loading from the pak file, which slow
        if (IsCached())
        {
            Debug.Log("atlas cache found, loading...");
            // go to the loading
            sw.Start();
            GenerateMapMeshes(blocks, blocksMapSide, verticesBlockSide);
            sw.Stop();
            Debug.Log("time to generate the map " + sw.ElapsedMilliseconds + " ms");
            return;
        }

        // THESE TAKES THE MAJORITY OF THE LOAD TIME
        Debug.Log("Creating atlas");

        PakFile pak;
        
        sw.Start();
        if (tileSet == MapTilesLoader.MapTileSet.ORIGINAL)
        {
            pak = new PakFile(Path.Combine(gameFilesLoader.localMCGPath, "TILES/TILES90.PAK"), fileManager.File(paletteDirectory));
        }
        else
        {
            pak = new PakFile(Path.Combine(gameFilesLoader.localMCGPath, "TILES/GTILES90.PAK"), fileManager.File(g_paletteDirectory));
        }
        sw.Stop();
        Debug.Log("time to finish decompression from pak " + sw.ElapsedMilliseconds + " ms");
        sw.Reset();

        // THIS PART IS THE SLOWEST as getting files from the inner pak files also requires LZdecomp
        sw.Start();
        tileDict = new Dictionary<int, MCBitmap>();
        tileOVDict = new Dictionary<int, MCBitmap>();
        // get all the bitmaps in the pack based on the indices used by the map
        for (int i = 0; i < mapTileIndices.Length; i++)
        {
            // this gets the sprite at the index
            var tiledata = pak.GetFileInner(mapTileIndices[i]);

            if (tiledata == null)
                continue;

            MCBitmap bitmap;
            if (tiledata[0] != 68 && tiledata[1] != 78)
            {
                var tile = new MCTileFile(tiledata);
                bitmap = tile.GetBitMap();
                if (bitmap == null)
                    continue;
                // FOR SOME REASON WE STORE INDICES as strings??? i guess we can refactor later to ID and bytes or ints
                bitmap.Name = mapTileIndices[i].ToString();//this.TerrainTileFile.ToUpper() + "-" + 

                tileDict.Add(mapTileIndices[i], bitmap);

            }
            else
            {
                var tile = new MCTileFileOverlay(tiledata);
                bitmap = tile.GetBitMap();
                if (bitmap == null)
                    continue;

                bitmap.Name = mapTileIndices[i].ToString();//this.TerrainTileFile.ToUpper() + "-" + 

                tileOVDict.Add(mapTileIndices[i], bitmap); // not sure if i will implement this now - it has bugs
            }

        }
        sw.Stop();
        Debug.Log("time to finish getting inner files from pak " + sw.ElapsedMilliseconds + " ms");
        sw.Reset();
        sw.Start();

        MCBitmap atlas;
        MCBitmap atlasOV;

        Dictionary<string, Rect> rectsDict;
        Dictionary<string, Rect> rectsOVDict;

        Debug.Log("Starting Terrain Atlas Generation after Tiles");
        //VERY COOL PACKER but it does leave a lot of space on the sheet - still it gets the job done really quickly and saves a lot of headaches!
        var result = ImageProcessing.CreateAtlas(tileDict.Values.ToArray(), out atlas, out rectsDict);
       
        CacheMap(atlas);

        tileInfoDict = new Dictionary<string, TileInfo>();
        foreach (var rect in rectsDict)
        {
            var bitmap = tileDict[int.Parse(rect.Key)];

            // pre calculate the uvs as these are fixed per tileInfo
            var tempUVS = new Vector2[]
			{
                new Vector2(rect.Value.x / atlas.Width, rect.Value.yMax / atlas.Height),
                new Vector2(rect.Value.xMax / atlas.Width, rect.Value.yMax / atlas.Height),
                new Vector2(rect.Value.x / atlas.Width, rect.Value.y / atlas.Height),
                new Vector2(rect.Value.xMax / atlas.Width, rect.Value.y / atlas.Height)

            };

            var halfWidth = rect.Value.width / 200; // 100 is ppu * 2 to get half
            //tileInfoDict.Add(rect.Key, new TileInfo(rect.Value, bitmap.Pivot));
            tileInfoDict.Add(rect.Key, new TileInfo(tempUVS, bitmap.Pivot, halfWidth, rect.Value.height / 100));
        }

        CacheTileInfo(tileInfoDict);

        if (result)
        {
            mainText = ImageProcessing.MakeIndexedTexture2D(atlas);
            palText = pak.Palette.ExportPaletteTexture();

            atlas.Dispose();

        }
        else
        {
            Debug.Log("Error Packing file " + result);
        }

        var resultOv = ImageProcessing.CreateAtlas(tileOVDict.Values.ToArray(), out atlasOV, out rectsOVDict);

        CacheMap(atlasOV, "OV"); // this one is very fast

        tileInfoOVDict = new Dictionary<string, TileInfo>();
        foreach (var rect in rectsOVDict)
        {
            var bitmap = tileOVDict[int.Parse(rect.Key)];

            var tempUVS = new Vector2[]
            {
                new Vector2(rect.Value.x / atlasOV.Width, rect.Value.yMax / atlasOV.Height),
                new Vector2(rect.Value.xMax / atlasOV.Width, rect.Value.yMax / atlasOV.Height),
                new Vector2(rect.Value.x / atlasOV.Width, rect.Value.y / atlasOV.Height),
                new Vector2(rect.Value.xMax / atlasOV.Width, rect.Value.y / atlasOV.Height)

            };

            var halfWidth = rect.Value.width / 200; // 100 is ppu * 2 to get half
            
            tileInfoOVDict.Add(rect.Key, new TileInfo(tempUVS, bitmap.Pivot, halfWidth, rect.Value.height / 100));
        }

        CacheTileInfo(tileInfoOVDict, "OV");

        if (resultOv)
        {
            mainOVText = ImageProcessing.MakeIndexedTexture2D(atlasOV);
            atlasOV.Dispose();
        }
        else
        {
            Debug.Log("Error Packing file " + resultOv);
        }

        foreach (var tile in tileDict)
        {
            tile.Value.Dispose();
        }

        foreach (var tile in tileOVDict)
        {
            tile.Value.Dispose();
        }
        sw.Stop();
        Debug.Log("time to generate the atlases " + sw.ElapsedMilliseconds + " ms");

        Debug.Log("Finish Init Terrain");
        sw.Start();
        GenerateMapMeshes(blocks, blocksMapSide, verticesBlockSide);
        sw.Stop();
        Debug.Log("time to generate the map " + sw.ElapsedMilliseconds + " ms");
    }

    // basic code to create sprite in game without having to create unity sprite sheets
    // this one uses the map elevation file to create blocks of vertices (each vertex is an individual sprite)
    // in order to save on renderers and gameobjects on screen, we use 1 renderer per block and just generate the meshes according to their size
    private void GenerateMapMeshes(MapElvFile.MCTile[] blocks, int blocksMapSide, int vertsBlockSide)
	{
        Debug.Log("GenerateMapIsoMeshes");
        
        string MapName = terrainFileName;
        GameObject mapParent = new GameObject(MapName + " Map");

        var Terrain = new GameObject();
        Terrain.name = "TerrainTiles-" + MapName;
        Terrain.transform.parent = mapParent.transform;

        var TerrainOV = new GameObject();
        TerrainOV.name = "TerrainTilesOverlay-" + MapName;
        TerrainOV.transform.parent = mapParent.transform;

        var index = 0;

        Shader shader = Shader.Find("MechCommanderUnity/PaletteSwapLookup");
        Material material = new Material(shader);
        material.mainTexture = mainText;
        material.SetTexture("_PaletteTex", palText);

        Material materialOV = new Material(shader);
        materialOV.mainTexture = mainOVText;
        materialOV.SetTexture("_PaletteTex", palText);
        Vector3 normal = Vector3.Normalize(Vector3.up + Vector3.forward);
        Vector3[] v3Values = new Vector3[]
        {
            Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero
        };

        Vector2[] v2Values = new Vector2[]
        {
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero
        };

        List<Vector3> LstVertex = new List<Vector3>();
        List<Vector3> LstNormals = new List<Vector3>();
        List<int> LstTriangles = new List<int>();
        List<Vector2> LstUVs = new List<Vector2>();
        for (int y = 0; y < blocksMapSide; y++)
        //for (int y = 0; y < 1; y++)
        {
            for (int x = 0; x < blocksMapSide; x++)
            //for (int x = 0; x < 1; x++)
            {
                int BlockNumber = (y * blocksMapSide) + x;
                GameObject block = new GameObject("Block" + (BlockNumber).ToString());
                block.transform.parent = Terrain.transform;

                MeshRenderer renderer = block.AddComponent<MeshRenderer>();
                MeshFilter meshFilter = block.AddComponent<MeshFilter>();

                //var info = tileInfoDict[LstTiles[index].Terrain.ToString()];

                renderer.sortingLayerName = "Terrain";
                renderer.sortingOrder = 0;
                renderer.sharedMaterial = material;

                // Create mesh for every block
                Mesh mesh = new Mesh();
                mesh.name = string.Format("TileBlockMesh");

                LstVertex.Clear();
                LstNormals.Clear();
                LstTriangles.Clear();
                LstUVs.Clear();

                for (int j = 0; j < vertsBlockSide; j++) //BlocksMapSide * 
                //for (int j = 0; j < 2; j++) //BlocksMapSide * 
                {
                    for (int i = 0; i < vertsBlockSide; i++) //BlocksMapSide *
                    //for (int i = 0; i < 2; i++) //BlocksMapSide *
                    {
                        // THIS STILL MIGHT BE SLOW - the calculations are a bit much per vert; can we precalculate??
                        // THE UVS are precalculated but i kept the orig code here commented out
                        int VertexNumber = j * vertsBlockSide + i;
                        // get the index of the sprite (Terrain property) and use it as a key to a tileInfo value
                        if (!tileInfoDict.ContainsKey(blocks[index].Terrain.ToString()))
                        {
                            Debug.LogError("No existe index: " + blocks[index].Terrain.ToString());
                        }
                        // get the rect info from the value
                        var info = tileInfoDict[blocks[index].Terrain.ToString()];
                        //Debug.Log("tile ineex " + LstTiles[index].Terrain.ToString());
                        //var rect = info.rect;
                        /*
                        //float width = rect.width / 100;
                        float height = rect.height / 100;
                        float halfWidth = rect.width / 200;
                        // get the position of the vertex (sprite to be drawn) in world coords
                        */
                        Vector3 IsoPosition = MapPositionToWorldCoords(BlockNumber, blocksMapSide,
                            VertexNumber, vertsBlockSide, blocks[index].Elevation);
                        //Debug.Log("position tile " + IsoPosition + " rect " + rect);
                        
                        // Start Creating the MESH
                        var idxTriangles = LstVertex.Count;
                        /*
                        v3Values[0].Set(IsoPosition.x - halfWidth, IsoPosition.y, IsoPosition.z); //top-left
                        v3Values[1].Set(IsoPosition.x + halfWidth, IsoPosition.y, IsoPosition.z); //top-right
                        v3Values[2].Set(IsoPosition.x - halfWidth, IsoPosition.y - height, IsoPosition.z); //bottom-left
                        v3Values[3].Set(IsoPosition.x + halfWidth, IsoPosition.y - height, IsoPosition.z); //bottom-right
                        */
                        v3Values[0].Set(IsoPosition.x - info.halfWidth, IsoPosition.y, IsoPosition.z); //top-left
                        v3Values[1].Set(IsoPosition.x + info.halfWidth, IsoPosition.y, IsoPosition.z); //top-right
                        v3Values[2].Set(IsoPosition.x - info.halfWidth, IsoPosition.y - info.height, IsoPosition.z); //bottom-left
                        v3Values[3].Set(IsoPosition.x + info.halfWidth, IsoPosition.y - info.height, IsoPosition.z); //bottom-right
                        LstVertex.AddRange(v3Values);

                        // tris
                        int[] triangles = new int[6]
                        {
                            idxTriangles, idxTriangles+1, idxTriangles+2,
                            idxTriangles+3, idxTriangles+2, idxTriangles+1,
                        };
                        LstTriangles.AddRange(triangles);
                        
                        // Normals
                        //Vector3 normal = Vector3.Normalize(Vector3.up + Vector3.forward);
                        v3Values[0] = normal;
                        v3Values[1] = normal;
                        v3Values[2] = normal;
                        v3Values[3] = normal;
                        LstNormals.AddRange(v3Values);

                        //uvs
                        /*
                        v2Values[0].Set(rect.x / mainText.width, rect.yMax / mainText.height);
                        v2Values[1].Set(rect.xMax / mainText.width, rect.yMax / mainText.height);
                        v2Values[2].Set(rect.x / mainText.width, rect.y / mainText.height);
                        v2Values[3].Set(rect.xMax / mainText.width, rect.y / mainText.height);
                        */
                        v2Values[0] = info.uv_0;
                        v2Values[1] = info.uv_1;
                        v2Values[2] = info.uv_2;
                        v2Values[3] = info.uv_3;

                        LstUVs.AddRange(v2Values);
                        //LstUVs.Add(info.uv_0); // becuase the uvs are precalculated, we can save on a bunch of cycles of division

                        var ovTileIndex = blocks[index].OverlayTile;

                        // THEN spawn overlays (roads, walls, trees etc)
                        // BUGGED spawning incorrectly - something to do with the coordinates
                        if (ovTileIndex != 41)// && DictIndexSprites.ContainsKey(LstTiles[index].OverlayTileId)
                        {
                            GameObject overtile = new GameObject("OV-" + ovTileIndex);

                            MeshRenderer rendererOV = overtile.AddComponent<MeshRenderer>();
                            MeshFilter meshFilterOV = overtile.AddComponent<MeshFilter>();

                            Vector3 BasePositionOV = new Vector3((x * vertsBlockSide) + i,
                                (y * vertsBlockSide) + j,
                                blocks[index].Elevation);

                            if (tileInfoOVDict.ContainsKey(ovTileIndex.ToString()))
                            {
                                var infoOV = tileInfoOVDict[ovTileIndex.ToString()];

                                //var rectOV = infoOV.rect;

                                rendererOV.sortingLayerName = "Overlays";
                                rendererOV.sortingOrder = (int)(BasePositionOV.x + BasePositionOV.y);
                                rendererOV.sharedMaterial = materialOV;

                                // Create mesh
                                Mesh meshOV = new Mesh();
                                meshOV.name = string.Format("TileOVMesh");

                                //Vector3[] verticesOV = new Vector3[4];
                                /*
                                var halfXOV1 = (rectOV.width / 100) * infoOV.pivot.x;
                                var halfXOV2 = (rectOV.width / 100) * (1 - infoOV.pivot.x);
                                var heightOV1 = (rectOV.height / 100) * (1 - infoOV.pivot.y);
                                var heightOV2 = (rectOV.height / 100) * infoOV.pivot.y;
                                */
                                var halfXOV1 = infoOV.halfWidth * 2 * infoOV.pivot.x;
                                var halfXOV2 = infoOV.halfWidth * 2 * (1 - infoOV.pivot.x);
                                var heightOV1 = infoOV.height * (1 - infoOV.pivot.y);
                                var heightOV2 = infoOV.height * infoOV.pivot.y;
                                
                                v3Values[0].Set(-halfXOV1, heightOV1, 0); //top-left
                                v3Values[1].Set(halfXOV2, heightOV1, 0); //top-right
                                v3Values[2].Set(-halfXOV1, -heightOV2, 0); //bottom-left
                                v3Values[3].Set(halfXOV2, -heightOV2, 0); //bottom-right
                                meshOV.vertices = v3Values;

                                // Indices
                                int[] trianglesOV = new int[6]
                                {
                                    0, 1, 2,
                                    3, 2, 1,
                                };
                                meshOV.triangles = trianglesOV;

                                // Normals
                                v3Values[0] = normal;
                                v3Values[1] = normal;
                                v3Values[2] = normal;
                                v3Values[3] = normal;
                                meshOV.normals = v3Values; // TEMP
                                //Vector2[] uvsOV = new Vector2[4];
                                // Debug.Log(rect);
                                /*
                                v2Values[0].Set(rectOV.x / mainOVText.width, rectOV.yMax / mainOVText.height);
                                v2Values[1].Set(rectOV.xMax / mainOVText.width, rectOV.yMax / mainOVText.height);
                                v2Values[2].Set(rectOV.x / mainOVText.width, rectOV.y / mainOVText.height);
                                v2Values[3].Set(rectOV.xMax / mainOVText.width, rectOV.y / mainOVText.height);
                                */
                                v2Values[0] = infoOV.uv_0;
                                v2Values[1] = infoOV.uv_1;
                                v2Values[2] = infoOV.uv_2;
                                v2Values[3] = infoOV.uv_3;

                                meshOV.uv = v2Values;
                                //meshOV.uv = infoOV.uvs;

                                // Assign mesh
                                meshFilterOV.sharedMesh = meshOV;
                                meshFilterOV.sharedMesh.uv = v2Values;
                                //meshFilterOV.sharedMesh.uv = infoOV.uvs;

                                Vector3 IsoPositionOV = Vector3.zero;
                                // THERE IS AN ERROR and it might be here as positions of OV tiles are off
                                IsoPositionOV.x = (BasePositionOV.x - BasePositionOV.y) * tileHalfDistanceX;
                                IsoPositionOV.y = ((BasePositionOV.x + BasePositionOV.y) * -tileHalfDistanceX) + (BasePositionOV.z * (metersPerElevation / 100f));

                                IsoPositionOV.z = 0;// (BasePosition.z * ((float)MetersPerElevLevel / 100));

                                //Vector3 IsoPositionOV2 = MapPositionToWorldCoords(BlockNumber, blocksMapSide,
                                //    VertexNumber, verticesBlockSide, LstTiles[index].Elevation);


                                overtile.transform.position = IsoPositionOV;

                                overtile.transform.parent = TerrainOV.transform;
                            }
                        }
                        index++;
                    }
                }

                mesh.vertices = LstVertex.ToArray();
                mesh.triangles = LstTriangles.ToArray();
                mesh.normals = LstNormals.ToArray();
                mesh.uv = LstUVs.ToArray();
                // Assign mesh
                meshFilter.sharedMesh = mesh;
                meshFilter.sharedMesh.uv = LstUVs.ToArray();

            }
        }
    }

    public Vector3 MapPositionToWorldCoords(int block, int blocksMapSide, int vertex, int vertsBlockSide, int elv, int pixelOffsetX = 0, int pixelOffsetY = 0)
    {
        var x = block % blocksMapSide;
        var y = block / blocksMapSide; // we can replace this div with a 1 / blockMapSide whenever the blockside/vertside is loaded

        var i = vertex % vertsBlockSide;
        var j = vertex / vertsBlockSide;

        var isoX = (x * vertsBlockSide) + i;
        var isoY = (y * vertsBlockSide) + j;
        Vector3 IsoPosition = Vector3.zero;

        IsoPosition.x = ((isoX - isoY) * tileHalfDistanceX) + (pixelOffsetX / 100f);// reduce all of the divisions to equivalent float mult
        IsoPosition.y = ((isoX + isoY) * -tileHalfDistanceY) + (elv * (23f / 100f)) - (pixelOffsetY / 100f); // 
        
        return IsoPosition;
    }


    public void SetupFileManager()
    {
        if (fileManager == null)
        {
            fileManager = gameFilesLoader.FileManager; // important to get at files in structure but will read thru and extract important
        }

        if (fileManager == null)
		{
            fileManager = new FileManager(gameFilesLoader.localMCGPath);
		}
    }

    private bool ValidateDataPath()
    {
        if (gameFilesLoader == null) return false;
        else return gameFilesLoader.ValidateMCGAssetPath(gameFilesLoader.localMCGPath);
    }

    public void SetupMapList()
    {
        SetupFileManager();

        if (ValidateDataPath())
        {
            ListMaps = new List<string>();

            var allMaps = fileManager.Files(s => s.StartsWith("MISSIONS") && s.EndsWith(".FIT"));
            ListMaps.AddRange(allMaps.Keys.Select(Path.GetFileNameWithoutExtension));
        }
    }

    // from iron arthur, just speeds up the loading if it was previously cached
    private bool IsCached()
    {
        string MapDirPath = Path.Combine("Assets", "SpritesData", "Maps");
        Directory.CreateDirectory(MapDirPath);

        string MapFilePath = Path.Combine(MapDirPath, terrainFileName);

        if (!File.Exists(MapFilePath + ".bytes"))
            return false;

        if (!File.Exists(MapFilePath + "OV.bytes"))
            return false;
        /*
        if (!File.Exists(MapFilePath + ".map"))
            return false;

        if (!File.Exists(MapFilePath + "OV.map"))
            return false;
        */
        if (!File.Exists(MapFilePath + "_mapData.Json"))
            return false;

        if (!File.Exists(MapFilePath + "OV_mapData.Json"))
            return false;

        var atlas = MCBitmap.Unserialize(MapFilePath + ".bytes");
        mainText = ImageProcessing.MakeIndexedTexture2D(atlas);
        atlas.Dispose();

        var atlasOv = MCBitmap.Unserialize(MapFilePath + "OV.bytes");
        mainOVText = ImageProcessing.MakeIndexedTexture2D(atlasOv);
        atlasOv.Dispose();
        /*
        tileInfoDict = GetCacheTileInfo(MapFilePath + ".map"); // looking for wrong type

        tileInfoOVDict = GetCacheTileInfo(MapFilePath + "OV.map");
        */
        tileInfoDict = GetCacheTileInfo(MapFilePath); // looking for wrong type

        tileInfoOVDict = GetCacheTileInfo(MapFilePath, "OV");
        if (terrainTileFile.ToUpper() == "Tiles".ToUpper())
        {
            var pal = new MCPalette(fileManager.File(paletteDirectory));
            palText = pal.ExportPaletteTexture();
        }
        else
        {
            var pal = new MCPalette(fileManager.File(g_paletteDirectory));
            palText = pal.ExportPaletteTexture();
        }

        return true;
    }

    private void CacheMap(MCBitmap bitmap, string suffix = "")
    {
        string MapDirPath = Path.Combine("Assets", "SpritesData", "Maps");
        Directory.CreateDirectory(MapDirPath);

        string MapFilePath = Path.Combine(MapDirPath, this.terrainFileName) + suffix + ".bytes";

        bitmap.Serialize(MapFilePath);
    }

    // MODIFIED FROM IRON ARTHUR TO SAVE TO JSON
    private void CacheTileInfo(Dictionary<string, TileInfo> dictTileInfo, string suffix = "")
    {
        string MapDirPath = Path.Combine("Assets", "SpritesData", "Maps");
        Directory.CreateDirectory(MapDirPath);

        //string MapFilePath = Path.Combine(MapDirPath, terrainFileName) + suffix + ".map";

        string[] keys = dictTileInfo.Keys.ToArray();
        TileInfo[] infos = dictTileInfo.Values.ToArray();
        /*
        using (StreamWriter writer = new StreamWriter(MapFilePath, true))
        {
            foreach (var tileInfo in dictTileInfo)
            {
                // get the destination rectangle
                Rect destination = tileInfo.Value.rect;
                Vector2 pivot = tileInfo.Value.pivot;

                // write out the destination rectangle for this bitmap
                writer.WriteLine(string.Format(
                    "{0};{1};{2};{3};{4};{5};{6}",
                    tileInfo.Key,
                    destination.x,
                    destination.y,
                    destination.width,
                    destination.height,
                    pivot.x,
                    pivot.y));
            }
        }*/

        TileInfoArraySave saveData = new TileInfoArraySave();
        saveData.tileInfos = new TileInfoSave[infos.Length];

		for (int i = 0; i < infos.Length; i++)
		{
            saveData.tileInfos[i].key = keys[i];
            saveData.tileInfos[i].halfWidth = infos[i].halfWidth;
            saveData.tileInfos[i].height = infos[i].height;
            saveData.tileInfos[i].pivot = infos[i].pivot;
            saveData.tileInfos[i].uvs = new Vector2[]
            {
                infos[i].uv_0, infos[i].uv_1, infos[i].uv_2, infos[i].uv_3,
            };

		}

        File.WriteAllText(Path.Combine(MapDirPath, terrainFileName + suffix + "_mapData.Json"), JsonUtility.ToJson(saveData, true));
    }

    // MODIFIED FROM IRON ARTHUR TO LOAD FROM JSON
    //private Dictionary<string, TileInfo> GetCacheTileInfo(string path, string suffix = "")
    private Dictionary<string, TileInfo> GetCacheTileInfo(string path, string suffix = "")
    {
        var result = new Dictionary<string, TileInfo>();
        /*
        using (StreamReader reader = new StreamReader(path))
        {
            while (reader.Peek() >= 0)
            {
                var str = reader.ReadLine();
                string[] strArray = str.Split(';');

                if (strArray.Length < 7)
                {
                    continue;
                }

                string key = strArray[0];

                Rect rect = new Rect();

                rect.x = float.Parse(strArray[1]);
                rect.y = float.Parse(strArray[2]);
                rect.width = float.Parse(strArray[3]);
                rect.height = float.Parse(strArray[4]);

                var pivot = new Vector2(float.Parse(strArray[5]), float.Parse(strArray[6]));

                var tileInfo = new TileInfo { rect = rect, pivot = pivot };

                result.Add(key, tileInfo);
            }
        }*/
        
        string jsonString = File.ReadAllText(path + suffix +  "_mapData.Json");
        var tileInfoSaves = JsonUtility.FromJson<TileInfoArraySave>(jsonString);
		for (int i = 0; i < tileInfoSaves.tileInfos.Length; i++)
		{
            var tempTileInfo = new TileInfo();

            tempTileInfo.halfWidth = tileInfoSaves.tileInfos[i].halfWidth;
            tempTileInfo.height = tileInfoSaves.tileInfos[i].height;
            tempTileInfo.pivot = tileInfoSaves.tileInfos[i].pivot;
            tempTileInfo.uv_0 = tileInfoSaves.tileInfos[i].uvs[0];
            tempTileInfo.uv_1 = tileInfoSaves.tileInfos[i].uvs[1];
            tempTileInfo.uv_2 = tileInfoSaves.tileInfos[i].uvs[2];
            tempTileInfo.uv_3 = tileInfoSaves.tileInfos[i].uvs[3];

            result.Add(tileInfoSaves.tileInfos[i].key, tempTileInfo);
        }

        return result;
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(MapLoader))]
    public class MapLoaderEditor : Editor
    {
        MapLoader editor;

        private const string showOptionsFoldout = "MechCommanderUnity_ShowOptionsFoldout";
        private static bool ShowOptionsFoldout
        {
            get { return EditorPrefs.GetBool(showOptionsFoldout, false); }
            set { EditorPrefs.SetBool(showOptionsFoldout, value); }
        }
        SerializedProperty Prop(string name)
        {
            return serializedObject.FindProperty(name);
        }

        public override void OnInspectorGUI()
        {
            editor = target as MapLoader;

            DrawDefaultInspector();

            if (!editor.ValidateDataPath())
            {
                EditorGUILayout.HelpBox("Please set the MCG path of your MechCommander Gold installation.", MessageType.Info);
                return;
            }
            EditorGUILayout.Space();
            ShowOptionsFoldout = GUILayoutHelper.Foldout(ShowOptionsFoldout, new GUIContent("Maps Importer"), () => {
                GUILayoutHelper.Indent(() =>
                {
                    //var propMapIndex = Prop("Map_ModelIndex");
                    var propMapName = Prop("MapName");

                    EditorGUILayout.Space();

                    if (editor.ListMaps.Count > 0)
                    {
                        GUILayoutHelper.Horizontal(() =>
                        {
                            GUILayout.Label("Map tools", EditorStyles.boldLabel);
                            GenericMenu menu = new GenericMenu();
                            
                            if (GUILayout.Button("Select Map to Scene Load"))
                            {
                                foreach (var map in editor.ListMaps)
                                {
                                    menu.AddItem(new GUIContent(map), map == propMapName.stringValue, LoadMap, map);
                                }

                                menu.ShowAsContext();
                            }
                        });

                        EditorGUILayout.Space();
                    }

                    else if (GUILayout.Button("Setup Map List")) editor.SetupMapList();
                });
            });
        }

        private void LoadMap(object o)
		{
            string mapname = (string)o;
            editor.LoadMap(mapname);
        }

    }
#endif
}


/// <summary>
/// MOST OF THE ORIG CODE STILL HERE:
/// </summary>
/*
internal IEnumerator GenerateMapIsoMeshes()
{
    Debug.Log("GenerateMapIsoMeshes");
    var tilePrefab = Resources.Load("TileIsoMesh");
    var OvertilePrefab = Resources.Load("OverlayTileIso");

    var TerrainObj = this.Terrain;

    int BlocksMapSide = (int)TerrainObj.blocksMapSide;
    int VerticesBlockSide = (int)TerrainObj.verticesBlockSide;

    float MetersPerVertex = TerrainObj.MetersPerVertex;
    float MetersPerElevLevel = TerrainObj.MetersPerElevLevel;
    string MapName = TerrainObj.terrainName;

    var MainTexture = TerrainObj.TerrainTiles.MainText;
    var MainOVTexture = TerrainObj.TerrainTiles.MainOVText;
    var PalTexture = TerrainObj.TerrainTiles.PalText;

    var DictSpriteInfo = TerrainObj.TerrainTiles.DictTileInfo;
    var DictSpriteOVInfo = TerrainObj.TerrainTiles.DictTileOVInfo;

    var LstTiles = TerrainObj.MapBlock.Tiles;

    float TileWidth = 2.28f;

    var HalfTileWidth = TileWidth / 2;
    var HalfTileHeigth = (float)MetersPerVertex / 200;
    var TileHeigth = HalfTileHeigth * 2;

    var Map = this.MapGO;
    Map.name = MapName;
    Debug.Log("tile height " + HalfTileHeigth + " metersPerVert " + MetersPerVertex);
    Map.layer = LayerMask.NameToLayer("Terrain");

    /*var grid = Map.AddComponent<MapGrid>();
    //Map.AddComponent<Pathfinding>();

    grid.displayGridGizmos = true;
    grid.gridWorldSize = new Vector2(BlocksMapSide * VerticesBlockSide, BlocksMapSide * VerticesBlockSide);
    grid.gridWorldBlocks = new Vector2(BlocksMapSide, BlocksMapSide);
    grid.gridWorldVertexPerBlock = new Vector2(VerticesBlockSide, VerticesBlockSide);
    grid.tileWidth = TileWidth;
    grid.tileHeigth = (float)MetersPerVertex / 100f;
    grid.tileZ = (MetersPerElevLevel / 100f);
    //grid.TileMap = LstTiles;
    */
// grid.nodeRadius = TileWidth;

/*
 * //var tilePrefab = Resources.Load("TileIsoMesh");
        //var OvertilePrefab = Resources.Load("OverlayTileIso");

        //var TerrainObj = this.Terrain;

        //int BlocksMapSide = blocksMapSide;
        //int VerticesBlockSide = verticesBlockSide;

        //float MetersPerVertex = metersPerVertex;
        //float MetersPerElevLevel = metersPerElv;
        

        //var MainTexture = mainText;
        //var MainOVTexture = mainOVText;
        //var PalTexture = palText;

        //var DictSpriteInfo = tileInfoDict;
        //var DictSpriteOVInfo = tileInfoOVDict;
        /*foreach (var info in DictSpriteInfo)
		{
            Debug.Log(info.Value.rect);
        }
        return;*/
//var LstTiles = TerrainObj.MapBlock.Tiles;
//var LstTiles = blocks;

//float TileWidth = 2.28f;

//var HalfTileWidth = TileWidth / 2;
//var HalfTileHeigth = (float)MetersPerVertex / 200;
//var TileHeigth = HalfTileHeigth * 2;

/*var Map = this.MapGO;
Map.name = MapName;
Debug.Log("tile height " + HalfTileHeigth + " metersPerVert " + MetersPerVertex);
Map.layer = LayerMask.NameToLayer("Terrain");
*/

//var grid = Map.AddComponent<MapGrid>();
//Map.AddComponent<Pathfinding>();
/*
grid.displayGridGizmos = true;
grid.gridWorldSize = new Vector2(BlocksMapSide * VerticesBlockSide, BlocksMapSide * VerticesBlockSide);
grid.gridWorldBlocks = new Vector2(BlocksMapSide, BlocksMapSide);
grid.gridWorldVertexPerBlock = new Vector2(VerticesBlockSide, VerticesBlockSide);
grid.tileWidth = TileWidth;
grid.tileHeigth = (float)MetersPerVertex / 100f;
grid.tileZ = (MetersPerElevLevel / 100f);
//grid.TileMap = LstTiles;

// grid.nodeRadius = TileWidth;
var Terrain = new GameObject();
    Terrain.name = "TerrainTiles-" + MapName;
    Terrain.transform.parent = Map.transform;

    var TerrainOV = new GameObject();
    TerrainOV.name = "TerrainTilesOverlay-" + MapName;
    TerrainOV.transform.parent = Map.transform;


    var index = 0;

    Shader shader = Shader.Find("MechCommanderUnity/PaletteSwapLookup");
    Material material = new Material(shader);
    material.mainTexture = MainTexture;
    material.SetTexture("_PaletteTex", PalTexture);

    Material materialOV = new Material(shader);
    materialOV.mainTexture = MainOVTexture;
    materialOV.SetTexture("_PaletteTex", PalTexture);


    for (int y = 0; y < BlocksMapSide; y++)
    {
        for (int x = 0; x < BlocksMapSide; x++)
        {
            int BlockNumber = (y * BlocksMapSide) + (x);
            GameObject block = new GameObject("Block" + (BlockNumber).ToString());
            block.transform.parent = Terrain.transform;

            MeshRenderer renderer = block.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = block.AddComponent<MeshFilter>();

            var info = DictSpriteInfo[LstTiles[index].Terrain.ToString()];

            renderer.sortingLayerName = "Terrain";
            renderer.sortingOrder = 0;
            renderer.sharedMaterial = material;

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.name = string.Format("TileBlockMesh");

            List<Vector3> LstVertex = new List<Vector3>();
            List<Vector3> LstNormals = new List<Vector3>();
            List<int> LstTriangles = new List<int>();

            List<Vector2> LstUVs = new List<Vector2>();



            for (int j = 0; j < VerticesBlockSide; j++) //BlocksMapSide * 
            {

                for (int i = 0; i < VerticesBlockSide; i++) //BlocksMapSide *
                {
                    int VertexNumber = j * VerticesBlockSide + i;

                    if (!DictSpriteInfo.ContainsKey(LstTiles[index].Terrain.ToString()))
                    {
                        Debug.LogError("No existe index: " + LstTiles[index].Terrain.ToString());
                    }
                    info = DictSpriteInfo[LstTiles[index].Terrain.ToString()];

                    var rect = info.rect;

                    float width = rect.width / 100;
                    float height = rect.height / 100;

                    float halfX = width / 2f;
                    float halfY = height / 2f;

                    float halfX2 = HalfTileWidth;
                    float halfY2 = HalfTileHeigth;

                    Vector3 BasePosition = new Vector3((x * VerticesBlockSide) + i,
                    (y * VerticesBlockSide) + j,
                    LstTiles[index].Elevation);

                    Vector3 IsoPosition = TerrainObj.MapPositionToWorldCoords(BlockNumber,
                        VertexNumber);

                    Vector3[] vertices = new Vector3[4];

                    vertices[0] = new Vector3(IsoPosition.x - halfX, IsoPosition.y, IsoPosition.z); //top-left
                    vertices[1] = new Vector3(IsoPosition.x + halfX, IsoPosition.y, IsoPosition.z); //top-right
                    vertices[2] = new Vector3(IsoPosition.x - halfX, IsoPosition.y - height, IsoPosition.z); //bottom-left
                    vertices[3] = new Vector3(IsoPosition.x + halfX, IsoPosition.y - height, IsoPosition.z); //bottom-right

                    var idxTriangles = LstVertex.Count;
                    // Indices
                    int[] triangles = new int[6]
                        {
                                idxTriangles, idxTriangles+1, idxTriangles+2,
                                idxTriangles+3, idxTriangles+2, idxTriangles+1,
                        };

                    // Normals
                    Vector3 normal = Vector3.Normalize(Vector3.up + Vector3.forward);
                    Vector3[] normals = new Vector3[4];
                    normals[0] = normal;
                    normals[1] = normal;
                    normals[2] = normal;
                    normals[3] = normal;

                    Vector2[] uvs = new Vector2[4];

                    uvs[0] = new Vector2(rect.x / MainTexture.width, rect.yMax / MainTexture.height);
                    uvs[1] = new Vector2(rect.xMax / MainTexture.width, rect.yMax / MainTexture.height);
                    uvs[2] = new Vector2(rect.x / MainTexture.width, rect.y / MainTexture.height);
                    uvs[3] = new Vector2(rect.xMax / MainTexture.width, rect.y / MainTexture.height);

                    LstVertex.AddRange(vertices);
                    LstTriangles.AddRange(triangles);
                    LstNormals.AddRange(normals);
                    LstUVs.AddRange(uvs);


                    //tile.transform.parent = row.transform;

                    var ovTileIndex = LstTiles[index].OverlayTile;

                    if (ovTileIndex != 41)// && DictIndexSprites.ContainsKey(LstTiles[index].OverlayTileId)
                    {
                        GameObject overtile = new GameObject("OV-" + ovTileIndex);

                        MeshRenderer rendererOV = overtile.AddComponent<MeshRenderer>();
                        MeshFilter meshFilterOV = overtile.AddComponent<MeshFilter>();

                        Vector3 BasePositionOV = new Vector3((x * VerticesBlockSide) + i,
                          (y * VerticesBlockSide) + j,
                          LstTiles[index].Elevation);

                        if (DictSpriteOVInfo.ContainsKey(ovTileIndex.ToString()))
                        {
                            var infoOV = DictSpriteOVInfo[ovTileIndex.ToString()];

                            var rectOV = infoOV.rect;

                            rendererOV.sortingLayerName = "Overlays";
                            rendererOV.sortingOrder = (int)(BasePositionOV.x + BasePositionOV.y);
                            rendererOV.sharedMaterial = materialOV;

                            // Create mesh
                            Mesh meshOV = new Mesh();
                            meshOV.name = string.Format("TileOVMesh");

                            Vector3[] verticesOV = new Vector3[4];


                            var halfXOV1 = (rectOV.width / 100) * infoOV.pivot.x;
                            var halfXOV2 = (rectOV.width / 100) * (1 - infoOV.pivot.x);
                            var heightOV1 = (rectOV.height / 100) * (1 - infoOV.pivot.y);
                            var heightOV2 = (rectOV.height / 100) * infoOV.pivot.y;

                            verticesOV[0] = new Vector3(-halfXOV1, heightOV1, 0); //top-left
                            verticesOV[1] = new Vector3(halfXOV2, heightOV1, 0); //top-right
                            verticesOV[2] = new Vector3(-halfXOV1, -heightOV2, 0); //bottom-left
                            verticesOV[3] = new Vector3(halfXOV2, -heightOV2, 0); //bottom-right

                            // Indices
                            int[] trianglesOV = new int[6]
                                {
                                    0, 1, 2,
                                    3, 2, 1,
                                };

                            // Normals


                            Vector2[] uvsOV = new Vector2[4];

                            // Debug.Log(rect);
                            uvsOV[0] = new Vector2(rectOV.x / MainOVTexture.width, rectOV.yMax / MainOVTexture.height);
                            uvsOV[1] = new Vector2(rectOV.xMax / MainOVTexture.width, rectOV.yMax / MainOVTexture.height);
                            uvsOV[2] = new Vector2(rectOV.x / MainOVTexture.width, rectOV.y / MainOVTexture.height);
                            uvsOV[3] = new Vector2(rectOV.xMax / MainOVTexture.width, rectOV.y / MainOVTexture.height);

                            meshOV.vertices = verticesOV;
                            meshOV.triangles = trianglesOV;
                            meshOV.normals = normals;
                            meshOV.uv = uvsOV;
                            // Assign mesh
                            meshFilterOV.sharedMesh = meshOV;
                            meshFilterOV.sharedMesh.uv = uvsOV;

                            Vector3 IsoPositionOV = Vector3.zero;

                            IsoPositionOV.x = (BasePositionOV.x - BasePositionOV.y) * halfX2;
                            IsoPositionOV.y = ((BasePositionOV.x + BasePositionOV.y) * -halfY2) + ((BasePositionOV.z) * (MetersPerElevLevel / 100));

                            IsoPositionOV.z = 0;// (BasePosition.z * ((float)MetersPerElevLevel / 100));


                            Vector3 IsoPositionOV2 = TerrainObj.MapPositionToWorldCoords(BlockNumber,
                                VertexNumber);



                            overtile.transform.position = IsoPositionOV;

                            overtile.transform.parent = TerrainOV.transform;
                        }
                    }
                    index++;
                }
            }

            mesh.vertices = LstVertex.ToArray();
            mesh.triangles = LstTriangles.ToArray();
            mesh.normals = LstNormals.ToArray();
            mesh.uv = LstUVs.ToArray();
            // Assign mesh
            meshFilter.sharedMesh = mesh;
            meshFilter.sharedMesh.uv = LstUVs.ToArray();


            yield return new WaitForFixedUpdate();
        }
    }
}


private IEnumerator GenerateMapObj()
{
    yield return null;

    Debug.Log(System.DateTime.Now.ToLongTimeString() + " : Init Painting MapObjects");

    string MapName = this.Terrain.terrainName;
    var MapObjects = this.Terrain.ObjBlock.objDataBlock;

    var MapGameObject = new GameObject("MapObjects");

    if (this.MapGO != null)
    {
        MapGameObject.transform.parent = this.MapGO.transform;
    }

    var objMGR = this.ObjectManager;

    var PalTexture = MechCommanderUnity.Instance.ContentReader.ShapesPakFile.Palette.ExportPaletteTexture();

    if (LoadTerrainObjects)
    {
        var MapGameObjectTerrain = new GameObject("MapObjects-TerrainObj");
        MapGameObjectTerrain.transform.parent = MapGameObject.transform;

        foreach (var terrObj in objMGR.terrainObjects)
        {
            terrObj.transform.parent = MapGameObjectTerrain.transform;
        }

        yield return new WaitForFixedUpdate();
        //                Debug.Log(System.DateTime.Now.ToLongTimeString() + " : Finish Painting TerrainObj");
    }


    if (LoadBuildingObjects)
    {
        //                Debug.Log(System.DateTime.Now.ToLongTimeString() + " : Init Painting Buildings");

        var MapGameObjectBuildings = new GameObject("MapObjects-Building");
        MapGameObjectBuildings.transform.parent = MapGameObject.transform;
        foreach (var bld in objMGR.buildings)
        {
            bld.transform.parent = MapGameObjectBuildings.transform;
        }

        //                Debug.Log(System.DateTime.Now.ToLongTimeString() + " : Finish Painting MapObjects");

        yield return new WaitForFixedUpdate();
    }

    if (LoadTurretsObjects)
    {
        //                Debug.Log(System.DateTime.Now.ToLongTimeString() + " : Init Painting Turrets");
        var MapGameObjectTurrets = new GameObject("MapObjects-Turrets");
        MapGameObjectTurrets.transform.parent = MapGameObject.transform;
        foreach (var tur in objMGR.turrets)
        {
            // GameObjectHelper.CreateMapObj(tur, MapGameObjectTurrets.transform, PalTexture);
            tur.transform.parent = MapGameObjectTurrets.transform;
        }
        foreach (var tur in objMGR.test)
        {
            tur.transform.parent = MapGameObjectTurrets.transform;
        }


        //                Debug.Log(System.DateTime.Now.ToLongTimeString() + " : Finish Painting Turrets");
        yield return new WaitForFixedUpdate();
    }

}*/
