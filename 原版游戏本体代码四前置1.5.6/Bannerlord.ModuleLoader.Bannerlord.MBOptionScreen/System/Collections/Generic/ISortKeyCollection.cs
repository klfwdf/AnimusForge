namespace System.Collections.Generic;

internal interface ISortKeyCollection<in TKey>
{
	IComparer<TKey> KeyComparer { get; }
}
