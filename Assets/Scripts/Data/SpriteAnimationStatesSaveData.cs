using UnityEngine;
using System;

/// <summary>
/// Similar to TileInfoSaveData, used to save the positions of the mcbitmaps on their respective atlas
/// </summary>
[Serializable]
public struct SpriteAnimationStatesSaveData
{
	// for every path to a mcbitmap file, we have an associated sprite array with info on sprites
	//public string[] paths;
	//public SpriteInfoArraySaveData[] spriteArrays;
	public string[] pathsToMCBitmap;// we save all of the paths to the mcbitmap .bytes file here 
	//public SpriteInfoSaveData[] spriteInfos;
	//public PartVertAndUVData[] vertsAndUvs;
	public SpriteAnimationStateSaveData[] animationStates;
	public SpriteAnimationStateSaveData[] singleFrameAnimationStates; // not sure how to implement this? but for now

}

[Serializable]
public struct SpriteInfoArraySaveData
{
	public SpriteInfoSaveData[] spriteInfos;
}


[Serializable]
public struct SpriteInfoSaveData
{
	// these are used to position the vert in game and the uv in the texture
	public Vector3[] verts;// for now... eventually will move out but the data is basically doubled
	public Vector2[] uvs;
	// these are the coords and dimensions of the sprite in the sheet / byte array
	public int x; // these might be changed but for now?
	public int y;
	public int width;
	public int height;
	public int sheetIndex;
	
	// half width? - only for center pivot images - shp files afaik will use top left pivot
}

[Serializable]
public struct SpriteAnimationStateSaveData
{
	public string stateName;
	public int startIndex;
	public int numOfFaces;
	public int numOfImages;
	public int offset;
	public int tick;
	public int playspeed;

	public SpriteAnimationStateSaveData(string name, PartAnimationStateData data)
	{
		stateName = name;
		startIndex = data.StartIndex;
		numOfFaces = data.FacingCount;
		numOfImages = data.Length;
		offset = data.Offset;
		tick = data.AnimationTick;
		playspeed = data.PlaySpeed;
	}

}