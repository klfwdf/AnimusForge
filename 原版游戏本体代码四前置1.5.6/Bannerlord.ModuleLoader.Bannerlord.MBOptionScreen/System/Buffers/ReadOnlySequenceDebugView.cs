using System.Diagnostics;

namespace System.Buffers;

internal sealed class ReadOnlySequenceDebugView<T>
{
	[DebuggerDisplay("Count: {Segments.Length}", Name = "Segments")]
	public struct ReadOnlySequenceDebugViewSegments
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public ReadOnlyMemory<T>[] Segments { get; set; }
	}

	private readonly T[] _array;

	private readonly ReadOnlySequenceDebugViewSegments _segments;

	public ReadOnlySequenceDebugViewSegments BufferSegments => _segments;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items => _array;

	public ReadOnlySequenceDebugView(ReadOnlySequence<T> sequence)
	{
		_array = BuffersExtensions.ToArray(in sequence);
		int num = 0;
		foreach (ReadOnlyMemory<T> item in sequence)
		{
			num++;
		}
		ReadOnlyMemory<T>[] array = new ReadOnlyMemory<T>[num];
		int num2 = 0;
		foreach (ReadOnlyMemory<T> item2 in sequence)
		{
			array[num2] = item2;
			num2++;
		}
		_segments = new ReadOnlySequenceDebugViewSegments
		{
			Segments = array
		};
	}
}
