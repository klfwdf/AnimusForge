using System.Diagnostics;

namespace System.Collections.Generic;

internal sealed class DictionaryKeyCollectionDebugView<TKey, TValue>
{
	private readonly ICollection<TKey> _collection;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public TKey[] Items
	{
		get
		{
			TKey[] array = new TKey[_collection.Count];
			_collection.CopyTo(array, 0);
			return array;
		}
	}

	public DictionaryKeyCollectionDebugView(ICollection<TKey> collection)
	{
		_collection = collection ?? throw new ArgumentNullException("collection");
	}
}
