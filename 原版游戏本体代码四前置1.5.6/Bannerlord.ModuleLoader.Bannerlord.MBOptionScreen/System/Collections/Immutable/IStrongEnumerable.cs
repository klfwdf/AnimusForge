namespace System.Collections.Immutable;

internal interface IStrongEnumerable<out T, TEnumerator> where TEnumerator : struct, IStrongEnumerator<T>
{
	TEnumerator GetEnumerator();
}
