/// <summary>
/// This holds data like a vector2 but in pixel amounts (bytes to keep the mem footprint low)
/// This is designed to be written into unity json so no properties included?
/// might move this to a struct with readonly properties?? or at least properties
/// </summary>

[System.Serializable]
public struct PositionData_2D_Byte
{
	public sbyte x;
	public sbyte y;

}