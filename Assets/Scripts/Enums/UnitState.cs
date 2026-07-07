using System;
[Flags] // might not need to be flags?
public enum UnitState
{
	IDLE = 0, // can take commands and transition to move or jump or fall or shutdown and can change facing
	MOVING = 1 << 0, // can transition to run or idle, jump and fall, and change direction
	RUNNING = 1 << 1, // can transition to walk or idle (if stopped), jump and fall, change direction
	JUMPING = 1 << 2, // cant take commands until jump over
	CANT_MOVE = 1 << 3 // cant take command but has to transition to idle
}