namespace System.Reflection.Metadata;

internal readonly struct ExceptionRegion
{
	private readonly ExceptionRegionKind _kind;

	private readonly int _tryOffset;

	private readonly int _tryLength;

	private readonly int _handlerOffset;

	private readonly int _handlerLength;

	private readonly int _classTokenOrFilterOffset;

	public ExceptionRegionKind Kind => _kind;

	public int TryOffset => _tryOffset;

	public int TryLength => _tryLength;

	public int HandlerOffset => _handlerOffset;

	public int HandlerLength => _handlerLength;

	public int FilterOffset
	{
		get
		{
			if (Kind != ExceptionRegionKind.Filter)
			{
				return -1;
			}
			return _classTokenOrFilterOffset;
		}
	}

	public EntityHandle CatchType
	{
		get
		{
			if (Kind != ExceptionRegionKind.Catch)
			{
				return default(EntityHandle);
			}
			return new EntityHandle((uint)_classTokenOrFilterOffset);
		}
	}

	internal ExceptionRegion(ExceptionRegionKind kind, int tryOffset, int tryLength, int handlerOffset, int handlerLength, int classTokenOrFilterOffset)
	{
		_kind = kind;
		_tryOffset = tryOffset;
		_tryLength = tryLength;
		_handlerOffset = handlerOffset;
		_handlerLength = handlerLength;
		_classTokenOrFilterOffset = classTokenOrFilterOffset;
	}
}
