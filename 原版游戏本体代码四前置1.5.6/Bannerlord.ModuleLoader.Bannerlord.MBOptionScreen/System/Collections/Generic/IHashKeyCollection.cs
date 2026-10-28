namespace System.Collections.Generic;

internal interface IHashKeyCollection<in TKey>
{
	IEqualityComparer<TKey> KeyComparer { get; }
}
