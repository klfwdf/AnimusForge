namespace System.Reflection.Internal;

internal sealed class ExternalMemoryBlock : System.Reflection.Internal.AbstractMemoryBlock
{
	private readonly object _memoryOwner;

	private unsafe byte* _buffer;

	private int _size;

	public unsafe override byte* Pointer => _buffer;

	public override int Size => _size;

	public unsafe ExternalMemoryBlock(object memoryOwner, byte* buffer, int size)
	{
		_memoryOwner = memoryOwner;
		_buffer = buffer;
		_size = size;
	}

	public unsafe override void Dispose()
	{
		_buffer = null;
		_size = 0;
	}
}
