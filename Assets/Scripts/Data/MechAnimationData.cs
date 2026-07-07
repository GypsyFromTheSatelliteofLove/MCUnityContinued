using UnityEngine;
using System.Collections.Generic;
public struct MechAnimationData
{
	public PartAnimationData legsAnimationData;
	public PartAnimationData torsoAnimationData;
	public PartAnimationData l_armAnimationData;
	public PartAnimationData r_armAnimationData;

	public MechAnimationData(PartAnimationData legs, PartAnimationData torso, PartAnimationData l_arm, PartAnimationData r_arm)
	{
		legsAnimationData = legs;
		torsoAnimationData = torso;
		l_armAnimationData = l_arm;
		r_armAnimationData = r_arm;
	}
}

public struct PartAnimationData
{
	public IReadOnlyList<PartAnimationStateData> animationStates;
	public IReadOnlyList<PartAnimationStateData> singleFrameAnimationStates;
	public IReadOnlyList<Texture2D> textures;
	public IReadOnlyList<PartVertAndUVData> vertsAndUVs;

	public PartAnimationData(PartAnimationStateData[] animStates, PartAnimationStateData[] singleStates, Texture2D[] texts, PartVertAndUVData[] vertsUvs)
	{
		animationStates = (PartAnimationStateData[])animStates.Clone();
		singleFrameAnimationStates = singleStates;
		textures = (Texture2D[])texts.Clone();
		vertsAndUVs = (PartVertAndUVData[])vertsUvs.Clone();
	}
}