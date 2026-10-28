using System.Buffers;
using System.Collections.Generic;

namespace System.Collections.Frozen;

internal abstract class ItemsFrozenSet<T, TThisWrapper> : FrozenSetInternalBase<T, TThisWrapper> where TThisWrapper : struct, FrozenSetInternalBase<T, TThisWrapper>.IGenericSpecializedWrapper
{
	private protected readonly FrozenHashTable _hashTable;

	private protected readonly T[] _items;

	private protected sealed override T[] ItemsCore => _items;

	private protected sealed override int CountCore => _hashTable.Count;

	protected ItemsFrozenSet(HashSet<T> source, bool keysAreHashCodes = false)
		: base(source.Comparer)
	{
		T[] array = new T[source.Count];
		source.CopyTo(array);
		_items = new T[array.Length];
		int[] array2 = ArrayPool<int>.Shared.Rent(array.Length);
		Span<int> hashCodes = array2.AsSpan(0, array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			ref int reference = ref hashCodes[i];
			T val = array[i];
			reference = ((val != null) ? base.Comparer.GetHashCode(val) : 0);
		}
		_hashTable = FrozenHashTable.Create(hashCodes, keysAreHashCodes);
		for (int j = 0; j < hashCodes.Length; j++)
		{
			int num = hashCodes[j];
			_items[num] = array[j];
		}
		ArrayPool<int>.Shared.Return(array2);
	}

	private protected sealed override Enumerator GetEnumeratorCore()
	{
		return new FrozenSet<T>.Enumerator(_items);
	}
}
