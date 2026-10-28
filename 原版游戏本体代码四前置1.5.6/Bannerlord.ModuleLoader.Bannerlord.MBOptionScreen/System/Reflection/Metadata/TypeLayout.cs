namespace System.Reflection.Metadata;

internal readonly struct TypeLayout(int size, int packingSize)
{
	private readonly int _size = size;

	private readonly int _packingSize = packingSize;

	public int Size => _size;

	public int PackingSize => _packingSize;

	public bool IsDefault
	{
		get
		{
			if (_size == 0)
			{
				return _packingSize == 0;
			}
			return false;
		}
	}
}
