using UnityEngine;
using System;
[Serializable]
public struct PartAnimationDBSet
{
	public string partName;
	public int sheetStride;

	public AnimationDBSet[] singleDBsets;
	public AnimationDBSet[] animationDBsets; // JUST USE THIS

	public PositionData_2D_Byte[] partPositions;

	public PartAnimationDBSet(string p_partName, AnimationData[] p_singleDatas, AnimationData[] p_animationDatas, sbyte[] positionsX, sbyte[] positionsY, int p_sheetStride)
	{
		partName = p_partName;
		sheetStride = p_sheetStride;
		singleDBsets = new AnimationDBSet[p_singleDatas.Length];
		for (int i = 0; i < p_singleDatas.Length; i++)
		{
			singleDBsets[i] = new AnimationDBSet(p_singleDatas[i]);
		}
		animationDBsets = new AnimationDBSet[p_animationDatas.Length];
		for (int i = 0; i < p_animationDatas.Length; i++)
		{
			animationDBsets[i] = new AnimationDBSet(p_animationDatas[i]);
		}

		var longerArray = positionsX.Length >= positionsY.Length ? positionsX : positionsY;
		partPositions = new PositionData_2D_Byte[longerArray.Length];
		sbyte x, y;

		for (int i = 0; i < longerArray.Length; i++)
		{
			if (i >= positionsX.Length) x = 0;
			else x = positionsX[i];
			if (i >= positionsY.Length) y = 0;
			else y = positionsY[i];

			partPositions[i].x = x;
			partPositions[i].y = y;
		}
	}
}

[Serializable]
public struct AnimationDBSet
{
	public string name;
	public short indexOfStart;
	public byte numOfImages;
	public byte numOfFaces;

	public AnimationDBSet(AnimationData p_animationData)
	{
		name = p_animationData.Name;
		indexOfStart = p_animationData.IndexOfStart;
		numOfImages = p_animationData.NumOfImages;
		numOfFaces = p_animationData.NumOfFaces;
	}
}