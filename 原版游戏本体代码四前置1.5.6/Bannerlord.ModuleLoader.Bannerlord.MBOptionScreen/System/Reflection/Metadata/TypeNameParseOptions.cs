namespace System.Reflection.Metadata;

internal sealed class TypeNameParseOptions
{
	private int _maxNodes = 20;

	public int MaxNodes
	{
		get
		{
			return _maxNodes;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_maxNodes = value;
		}
	}
}
