using UnityEngine;

/// <summary>
/// This can be used directly to index animation arrays
/// if the animation only has 9 faces, we get the nearest even facing then mirror on the rest of the 8 faces
/// if the animation only has 17 faces, we mirror after N = 16
/// if the animation has 32, we just use this 32
/// </summary>
public enum Facing : byte
{
	S = 0,
	S_SE_0 = 1,
	S_SE_1 = 2,
	S_SE_2 = 3,
	SE = 4,
	SE_E_0 = 5,
	SE_E_1 = 6,
	SE_E_2 = 7,
	E = 8,
	E_NE_0 = 9,
	E_NE_1 = 10,
	E_NE_2 = 11,
	NE = 12,
	NE_N_0 = 13,
	NE_N_1 = 14,
	NE_N_2 = 15,
	N = 16,
	N_NW_0 = 17,
	N_NW_1 = 18,
	N_NW_2 = 19,
	NW = 20,
	NW_W_0 = 21,
	NW_W_1 = 22,
	NW_W_2 = 23,
	W = 24,
	W_SW_1 = 25,
	W_SW_2 = 26,
	W_SW_3 = 27,
	SW = 28,
	SW_S_1 = 29,
	SW_S_2 = 30,
	SW_S_3 = 31
}

/**
 *	N = 0,
	N_NE_0 = 1,
	N_NE_1 = 2,
	N_NE_2 = 3,
	NE = 4,
	NE_E_0 = 5,
	NE_E_1 = 6,
	NE_E_2 = 7,
	E = 8,
	E_SE_0 = 9,
	E_SE_1 = 10,
	E_SE_2 = 11,
	SE = 12,
	SE_S_0 = 13,
	SE_S_1 = 14,
	SE_S_2 = 15,
	S = 16,
	S_SW_0 = 17,
	S_SW_1 = 18,
	S_SW_2 = 19,
	SW = 20,
	SW_W_0 = 21,
	SW_W_1 = 22,
	SW_W_2 = 23,
	W = 24,
	W_NW_1 = 25,
	W_NW_2 = 26,
	W_NW_3 = 27,
	NW = 28,
	NW_N_1 = 29,
	NW_N_2 = 30,
	NW_N_3 = 31
 */