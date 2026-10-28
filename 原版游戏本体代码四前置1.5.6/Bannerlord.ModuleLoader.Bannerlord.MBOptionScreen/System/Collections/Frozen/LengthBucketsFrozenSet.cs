using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class LengthBucketsFrozenSet : FrozenSetInternalBase<string, LengthBucketsFrozenSet.GSW>
{
	internal struct GSW : IGenericSpecializedWrapper
	{
		private LengthBucketsFrozenSet _set;

		public int Count => _set.Count;

		public IEqualityComparer<string> Comparer => _set.Comparer;

		public void Store(FrozenSet<string> set)
		{
			_set = (LengthBucketsFrozenSet)set;
		}

		public int FindItemIndex(string item)
		{
			return _set.FindItemIndex(item);
		}

		public Enumerator GetEnumerator()
		{
			return _set.GetEnumerator();
		}
	}

	private readonly int[] _lengthBuckets;

	private readonly int _minLength;

	private readonly string[] _items;

	private readonly bool _ignoreCase;

	private protected override string[] ItemsCore => _items;

	private protected override int CountCore => _items.Length;

	private LengthBucketsFrozenSet(string[] items, int[] lengthBuckets, int minLength, IEqualityComparer<string> comparer)
		: base(comparer)
	{
		_items = items;
		_lengthBuckets = lengthBuckets;
		_minLength = minLength;
		_ignoreCase = comparer == StringComparer.OrdinalIgnoreCase;
	}

	internal static LengthBucketsFrozenSet? CreateLengthBucketsFrozenSetIfAppropriate(string[] items, IEqualityComparer<string> comparer, int minLength, int maxLength)
	{
		int[] array = LengthBuckets.CreateLengthBucketsArrayIfAppropriate(items, comparer, minLength, maxLength);
		if (array == null)
		{
			return null;
		}
		return new LengthBucketsFrozenSet(items, array, minLength, comparer);
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenSet<string>.Enumerator(_items);
	}

	private protected override int FindItemIndex(string? item)
	{
		if (item != null)
		{
			int i = (item.Length - _minLength) * 5;
			int num = i + 5;
			int[] lengthBuckets = _lengthBuckets;
			if (i >= 0 && num <= lengthBuckets.Length)
			{
				string[] items = _items;
				if (!_ignoreCase)
				{
					for (; i < num; i++)
					{
						int num2 = lengthBuckets[i];
						if ((uint)num2 >= (uint)items.Length)
						{
							break;
						}
						if (item == items[num2])
						{
							return num2;
						}
					}
				}
				else
				{
					for (; i < num; i++)
					{
						int num3 = lengthBuckets[i];
						if ((uint)num3 >= (uint)items.Length)
						{
							break;
						}
						if (StringComparer.OrdinalIgnoreCase.Equals(item, items[num3]))
						{
							return num3;
						}
					}
				}
			}
		}
		return -1;
	}
}
