using System.Collections.Generic;
public struct PartPositionsData
{
	private string partName;
	public string PartName { get { return partName; } }
	public IReadOnlyList<PositionData_2D_Byte> partPositions;

	public PartPositionsData(string p_partName, sbyte[] positionsX, sbyte[] positionsY)
	{
		partName = p_partName;
		var longerArray = positionsX.Length >= positionsY.Length ? positionsX : positionsY;
		var partPositionsPrivate = new PositionData_2D_Byte[longerArray.Length];
		sbyte x, y;

		for (int i = 0; i < longerArray.Length; i++)
		{
			if (i >= positionsX.Length) x = 0;
			else x = positionsX[i];
			if (i >= positionsY.Length) y = 0;
			else y = positionsY[i];

			partPositionsPrivate[i].x = x;
			partPositionsPrivate[i].y = y;
		}

		partPositions = (PositionData_2D_Byte[])partPositionsPrivate.Clone();
	}

	public PartPositionsData(PartAnimationDBSet dbSet)
	{
		partName = dbSet.partName;
		partPositions = (PositionData_2D_Byte[])dbSet.partPositions.Clone();
	}
}
