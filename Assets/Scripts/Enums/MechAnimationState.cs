using UnityEngine;

public enum MechAnimationState : byte
{
	SHUT_DOWN = 0,
	BEGIN_WALK = 1,
	WALK = 2,
	STOP_WALK = 3,
	BEGIN_RUN = 4,
	RUN = 5,
	RUN_TO_WALK = 6,
	HOBBLE = 7,
	FALL_BACKWARDS_0 = 8,
	FALL_FORWARDS_0 = 9,
	FALL_BACKWARDS_1 = 10,
	FALL_FORWARDS_1 = 11,
	FALL_BACKWARDS_2 = 12,
	FALL_FORWARDS_2 = 13,
	JUMP = 14,
	ROLL_OVER = 15,
	GET_UP = 16,
	REVERSE = 17,		// have to create
	BEGIN_REVERSE = 18, // have to create
	STOP_REVERSE = 19, // have to create
	POWER_UP = 20, // have to create
	SHUTDOWN_IDLE = 21,
	IDLE = 22,
	SINGLE_0 = 23,
	SINGLE_1 = 24
}
