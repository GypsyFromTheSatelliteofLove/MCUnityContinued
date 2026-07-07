using System.Collections.Generic;

/// <summary>
/// Every part has an array of animation data that describes where the animations are in the array of sprites
/// Ex. Awesome Legs has BeginWalk anim start at 162, with numOfImages 10, and has 9 faces
/// Faces are always 9, 17, or 32
/// </summary>
public struct PartAnimationData_OLD
{
	public string PartName { get; private set; }
	public int SheetStride { get; private set; }
	public IReadOnlyList<AnimationData> singleDatas;
	public IReadOnlyList<AnimationData> animationDatas;

	public PartAnimationData_OLD(string p_partName, AnimationData[] p_singleDatas, AnimationData[] p_animationDatas, int p_sheetStride)
	{
		PartName = p_partName;
		SheetStride = p_sheetStride;
		singleDatas = (AnimationData[])p_singleDatas.Clone();
		animationDatas = (AnimationData[])p_animationDatas.Clone();

	}

	public PartAnimationData_OLD(PartAnimationDBSet dbSet)
	{
		PartName = dbSet.partName;
		SheetStride = dbSet.sheetStride;
		var tempSingleDatas = new AnimationData[dbSet.singleDBsets.Length];
		for (int i = 0; i < tempSingleDatas.Length; i++)
		{
			tempSingleDatas[i] =
				new AnimationData(dbSet.singleDBsets[i].name, dbSet.singleDBsets[i].indexOfStart, 
				dbSet.singleDBsets[i].numOfImages, dbSet.singleDBsets[i].numOfFaces);
		}
		var tempAnimationDatas = new AnimationData[dbSet.animationDBsets.Length];
		for (int i = 0; i < tempAnimationDatas.Length; i++)
		{
			tempAnimationDatas[i] =
				new AnimationData(dbSet.animationDBsets[i].name, dbSet.animationDBsets[i].indexOfStart, 
				dbSet.animationDBsets[i].numOfImages, dbSet.animationDBsets[i].numOfFaces);
		}

		singleDatas = (AnimationData[])tempSingleDatas.Clone();
		animationDatas = (AnimationData[])tempAnimationDatas.Clone();
	}
}

public struct AnimationData
{
	private string name; // change to enum to reduce bytes but not readability; might remove in fact, save space - leave the name in the db
	private short indexOfStart; // should be a ushort since indices dont become negative
	private byte numOfImages;
	private byte numOfFaces;

	public string Name { get { return name; } }
	public short IndexOfStart { get { return indexOfStart; } }
	public byte NumOfImages { get { return numOfImages; } }
	public byte NumOfFaces { get { return numOfFaces; } }

	public AnimationData(string p_name, short p_indexOfStart, byte p_numOfImages, byte p_numOfFaces)
	{
		name = p_name;
		indexOfStart = p_indexOfStart;
		numOfImages = p_numOfImages;
		numOfFaces = p_numOfFaces;
	}

}

