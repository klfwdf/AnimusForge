namespace System.Collections.Immutable;

internal interface IStrongEnumerator<T>
{
	T Current { get; }

	bool MoveNext();
}
