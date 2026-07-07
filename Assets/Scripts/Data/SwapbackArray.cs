using UnityEngine;

public struct SwapbackArray<T>
{
	private T[] array;

	public int Count { get; private set; }

	public SwapbackArray(T type, int length)
	{
		array = new T[length];
		Count = length;
	}

	public SwapbackArray(T[] p_array)
	{
		array = p_array;
		Count = array.Length;
	}

	public void Swapback(int index)
	{
		array[Count - 1] = array[index];
		Count--;

	}

	public bool Add(T element)
	{
		if (Count + 1 >= array.Length)
		{
			return false;
		}

		array[Count++] = element;
		return true;
	}
}