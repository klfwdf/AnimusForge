using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TaleWorlds.Library;

public class TimedDictionaryCache<TKey, TValue>
{
	private readonly Dictionary<TKey, (long Timestamp, TValue Value)> _dictionary;

	private readonly Stopwatch _stopwatch;

	private readonly long _validMilliseconds;

	public TValue this[TKey key]
	{
		get
		{
			RemoveIfExpired(key);
			return _dictionary[key].Value;
		}
		set
		{
			_dictionary[key] = (_stopwatch.ElapsedMilliseconds, value);
		}
	}

	public TimedDictionaryCache(long validMilliseconds)
	{
		_dictionary = new Dictionary<TKey, (long, TValue)>();
		_stopwatch = new Stopwatch();
		_stopwatch.Start();
		_validMilliseconds = validMilliseconds;
	}

	public TimedDictionaryCache(TimeSpan validTimeSpan)
		: this((long)validTimeSpan.TotalMilliseconds)
	{
	}

	private bool IsItemExpired(TKey key)
	{
		return _stopwatch.ElapsedMilliseconds - _dictionary[key].Timestamp >= _validMilliseconds;
	}

	private bool RemoveIfExpired(TKey key)
	{
		if (IsItemExpired(key))
		{
			_dictionary.Remove(key);
			return true;
		}
		return false;
	}

	public void PruneExpiredItems()
	{
		List<TKey> list = new List<TKey>();
		foreach (KeyValuePair<TKey, (long, TValue)> item in _dictionary)
		{
			if (IsItemExpired(item.Key))
			{
				list.Add(item.Key);
			}
		}
		foreach (TKey item2 in list)
		{
			_dictionary.Remove(item2);
		}
	}

	public void Clear()
	{
		_dictionary.Clear();
	}

	public bool ContainsKey(TKey key)
	{
		if (_dictionary.ContainsKey(key))
		{
			return !RemoveIfExpired(key);
		}
		return false;
	}

	public bool Remove(TKey key)
	{
		RemoveIfExpired(key);
		return _dictionary.Remove(key);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		if (ContainsKey(key))
		{
			value = _dictionary[key].Value;
			return true;
		}
		value = default(TValue);
		return false;
	}

	public MBReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary()
	{
		PruneExpiredItems();
		Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
		foreach (KeyValuePair<TKey, (long, TValue)> item in _dictionary)
		{
			dictionary[item.Key] = item.Value.Item2;
		}
		return dictionary.GetReadOnlyDictionary();
	}
}
