using UnityEngine;
using System;
/// <summary>
/// Stores the vertex position and size of the mesh plus the uv coords for its sprite
/// Accessed in an array that is synced up to the sprite index in the sheet; accessing VertAndUVData[index] will refer to the sprite at [index]
/// </summary>
[Serializable] // compromise for now for ease of loading from json - we could also just write bytes but jsons are more convenient
public struct PartVertAndUVData
{
	// i think it would be better to just have the vectors in the struct... that way its safer to predict what layout it will use?
	// we have to remember that arrays are reference types so they might not be laid out in memory like we assume they would be
	public Vector2 vert_0; //8
	public Vector2 vert_1; //8
	public Vector2 vert_2; //8 // WE COULD TECHNICALLY ONLY STORE 2 CORNERS - LATER
	public Vector2 vert_3; //8 = 32 bytes total
	public Vector2 uv_0; //8
	public Vector2 uv_1; //8
	public Vector2 uv_2; //8
	public Vector2 uv_3; //8 = 32 bytes total
	public sbyte vert_z; //4 single float to carry a z value - we use an sbyte to make it smaller and just multiply it to a float later
	public byte textureIndex;
	// only the legs and torsos usually need a separate sheet BUT it can still be the case that we need a separate byte for it
	
	// total: 32 + 32 + 2 = 66 bytes of memory
	// we do this instead of Vector2[] so we can save all these contigous in memory instead of an array ref which is a pointer
	public PartVertAndUVData(Vector3[] verts, Vector2[] uvs, int textureIdx = 0, int zVert = 0)
	{
		vert_0 = verts[0];
		vert_1 = verts[1];
		vert_2 = verts[2];
		vert_3 = verts[3];

		uv_0 = uvs[0];
		uv_1 = uvs[1];
		uv_2 = uvs[2];
		uv_3 = uvs[3];

		vert_z = (sbyte)zVert;
		textureIndex = (byte)textureIdx;
	}
}

// everytime a mech is animating, we calculate its direction and speed from its navmesh agent (for now)
// then we translate that into indices for facing (0 to 31) and animation (speed is walk to run)
// we take in input first that creates a moving array for the player and the ai?
// we also have the main array of animating things
// there are 2 facings per mech (and tanks) as their torsos/turrets are independent of the legs
// atan2 is probably the fastest but it returns pi to negative pi but thats ok
// if we are just comparing values to get int for facing, atan2 is ok
// dot products if greater than 0 might mean it is in front
// we use atan2 (x, y) so the angle is relative to the y axis
// then we can just multiply the value of atan2 / pi (to get the normalized val) to our 32 then minus 1 to get the index of facing
// for forward motion we have to rely on the animation system to give us the moment when the acceleration happens
// navmesh agent speed is probably the variable to consider
// i think mechcommander functions on 3 run speeds - fast, med, slow i think
// still might have to store facing somewhere - like a animationInfoStruct that says facing and animation index? (byte plus ushort?)
// so we have a struct that contains the sprite info that tells the mech what sprite to play
// then we have the state struct which is modified everytime health, stability, speed etc change
// this state struct is what is read when we need to change 