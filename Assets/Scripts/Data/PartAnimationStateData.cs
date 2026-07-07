using UnityEngine;

/// <summary>
/// For every part, we update this struct to reflect what facing and animation (startIndex) we need, the length of the animation (for ends and loops) 
/// and the current tick (meaning the index of the animation is startIndex + tick)
/// </summary>

public struct PartAnimationStateData // we could store a name field but it takes up so much space - alternative we just name these in the Animator
{
	public ushort StartIndex { get;}

	public byte TextureIndex { get;} // this will tell us if we need to change textures to get to the animation??

	public byte Length { get; }

	public byte FacingCount { get; } // we still have to store this here as we need a way to calculate how many facings this state has

	public byte Offset { get; } // this makes the animation start at the offset but still loop back around the startIndex and length

	public byte AnimationTick { get; set; } // we could store the playspeed direction in here in a sign bit but leave it for future optimization

	// we dont want to waste the 4 bytes for a float yet but it could still be useful if we want to 2x the speed
	public sbyte PlaySpeed { get; } // I DONT HAVE TO WASTE THE BYTE here but for now, for readability

	public PartAnimationStateData(ushort startIdx, byte len, byte faceCount, byte p_offset = 0, sbyte speed = 1)
	{
		StartIndex = startIdx;
		TextureIndex = 0;
		Length = len;
		FacingCount = faceCount;
		Offset = p_offset;
		AnimationTick = 1;
		PlaySpeed = speed;
	}
	
	// total of 8 bytes per part 
	// if we want to keep this at 4 bytes, were going to have to combine properties like offset and animation tick but will cut down on readability
}
// In very animator, we will have an array of these but to access them, we could still create some naming system either via enum or constants
// its probs best to just use the enum so we can just say states[AnimationEnum.Run] to play the next one

// when we change the state, we update the animationState to reflect its aniamtion
// a struct for state might still be useful?
// defo something like health and stability will be checked - if health is 0, remove from lists
// if stability is 0, fall - remove from moving as well?
// falling mechs will stop (navmesh agent) and play an animation for the fall then the get up
// jumping is another thing but it might just be a moveto after some interpolated animation position

// weapon attacks are prerolled - meaning a hit will always be a hit
// but we have to wait for the animation to be done before we call the damage
// waiting for animation to be done is something we have to keep doing... -- for turning to attack, walking to running, and weapon hits
// solve for later but actions will probably do... or we just keep making arrays for waiting
// ex if an attack is being processed but the animation is still playing, we create the animation struct for the weapon
// then if we reach the target after moving etc, we add the attack struct to be processed into some attack process struct
// this means that we need animations to indicate what happens after they are done?
// also means we need a waiting array where attacks to be processed are put into (and their index remembered somehow)
// after animation, the attack from the waiting is swapbacked and put into the attack processing array
// this would be applicable to many actions - when we change speeds we wait on the animation so we put a waiting to change speed array
// then we finish the animation, we have to put the waiting to change speed to the speed change processing?
// OR do we just have the struct for the instruction be attached to the animation???
// that way, we dont have to maintain these arrays and pass the instruction directly to processing?
// it could be as easy as an index/flag in the animation tho... like 2 bytes to just say where (which waiting array) the action is waiting, and what index it is


// do we have pending action arrays?
// as well as processing action arrays?
// - so we can store commands in the pending action array like attack damage after weapon animation, 
// BUT DO WE NEED THIS?? its a fair question but in the end we want to try and design like this
// either LATs or ECS with structs

// So we still need input as the strating point; 
// then AI as this applies to all units
// this will produce this partAnimationStateData struct which has the target index (facing and anim) and the length and the AnimationTick
// could we separate the tick? as it is the only dynamic one? 
// everything else is immutable actually, depending on the state
// so tick should be separated?
// what we could instead store is start offset - this defines what the animation starts in 
// we could also define like a tick - for example a reverse is just the walk animation in reverse
// this basically functions like the animation data in the inspector
// so we create these animation states from the files BUT we should be able to create some scriptable objects that represent them in the editor
// scriptable objects OR just json files as these are also easily editable
// we need : idle, shutdown, idleToWalk, walk, idleToReverse, reverse, walkToRun, run, runToWalk, jump, hobble, 3 fall backs, 3 fall forwards, roll over, getup;
// 19 TOTAL states per mech
// i think all of the mcbitmap sheets can be saved in just a binary
// then we can also save the animation data in json together FOR now

// on load, we still check if we got a chached version of this file
// we check the mcBitmap and the animation data
// (use a fixed file structure)
