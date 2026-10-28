using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal sealed class LengthBucketsFrozenDictionary<TValue> : FrozenDictionary<string, TValue>
{
	private readonly int[] _lengthBuckets;

	private readonly int _minLength;

	private readonly string[] _keys;

	private readonly TValue[] _values;

	private readonly bool _ignoreCase;

	private protected override string[] KeysCore => _keys;

	private protected override TValue[] ValuesCore => _values;

	private protected override int CountCore => _keys.Length;

	private LengthBucketsFrozenDictionary(string[] keys, TValue[] values, int[] lengthBuckets, int minLength, IEqualityComparer<string> comparer)
		: base(comparer)
	{
		_keys = keys;
		_values = values;
		_lengthBuckets = lengthBuckets;
		_minLength = minLength;
		_ignoreCase = comparer == StringComparer.OrdinalIgnoreCase;
	}

	internal static LengthBucketsFrozenDictionary<TValue>? CreateLengthBucketsFrozenDictionaryIfAppropriate(string[] keys, TValue[] values, IEqualityComparer<string> comparer, int minLength, int maxLength)
	{
		int[] array = LengthBuckets.CreateLengthBucketsArrayIfAppropriate(keys, comparer, minLength, maxLength);
		if (array == null)
		{
			return null;
		}
		return new LengthBucketsFrozenDictionary<TValue>(keys, values, array, minLength, comparer);
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenDictionary<string, TValue>.Enumerator(_keys, _values);
	}

	private protected override ref readonly TValue GetValueRefOrNullRefCore(string key)
	{
		int i = (key.Length - _minLength) * 5;
		int num = i + 5;
		int[] lengthBuckets = _lengthBuckets;
		if (i >= 0 && num <= lengthBuckets.Length)
		{
			string[] keys = _keys;
			TValue[] values = _values;
			if (!_ignoreCase)
			{
				for (; i < num; i++)
				{
					int num2 = lengthBuckets[i];
					if ((uint)num2 >= (uint)keys.Length)
					{
						break;
					}
					if (key == keys[num2])
					{
						return ref values[num2];
					}
				}
			}
			else
			{
				for (; i < num; i++)
				{
					int num3 = lengthBuckets[i];
					if ((uint)num3 >= (uint)keys.Length)
					{
						break;
					}
					if (StringComparer.OrdinalIgnoreCase.Equals(key, keys[num3]))
					{
						return ref values[num3];
					}
				}
			}
		}
		return ref Unsafe.NullRef<TValue>();
	}
}
