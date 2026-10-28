namespace System.Buffers;

internal interface IMemoryOwner<T> : IDisposable
{
	Memory<T> Memory { get; }
}
