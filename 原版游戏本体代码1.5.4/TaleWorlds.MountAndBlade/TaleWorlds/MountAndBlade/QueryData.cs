using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class QueryData<T> : IQueryData
{
	private T _cachedValue;

	private float _expireTime;

	private readonly float _lifetime;

	private readonly Func<T> _valueFunc;

	private IQueryData[] _syncGroup;

	public T Value
	{
		get
		{
			float currentTime = Mission.Current.CurrentTime;
			if (currentTime >= _expireTime)
			{
				if (_syncGroup != null)
				{
					IQueryData[] syncGroup = _syncGroup;
					for (int i = 0; i < syncGroup.Length; i++)
					{
						syncGroup[i].Evaluate(currentTime);
					}
				}
				Evaluate(currentTime);
			}
			return _cachedValue;
		}
	}

	public QueryData(Func<T> valueFunc, float lifetime)
	{
		_cachedValue = default(T);
		_expireTime = 0f;
		_lifetime = lifetime;
		_valueFunc = valueFunc;
		_syncGroup = null;
	}

	public QueryData(Func<T> valueFunc, float lifetime, T defaultCachedValue)
	{
		_cachedValue = defaultCachedValue;
		_expireTime = 0f;
		_lifetime = lifetime;
		_valueFunc = valueFunc;
		_syncGroup = null;
	}

	public void Evaluate(float currentTime)
	{
		SetValue(_valueFunc(), currentTime);
	}

	public void SetValue(T value, float currentTime)
	{
		_cachedValue = value;
		_expireTime = currentTime + _lifetime;
	}

	public T GetCachedValue()
	{
		return _cachedValue;
	}

	public T GetCachedValueUnlessTooOld()
	{
		return _cachedValue;
	}

	public T GetCachedValueWithMaxAge(float age)
	{
		if (Mission.Current.CurrentTime > _expireTime - _lifetime + TaleWorlds.Library.MathF.Min(_lifetime, age))
		{
			Expire();
			return Value;
		}
		return _cachedValue;
	}

	public void Expire()
	{
		_expireTime = 0f;
	}

	public static void SetupSyncGroup(params IQueryData[] groupItems)
	{
		for (int i = 0; i < groupItems.Length; i++)
		{
			groupItems[i].SetSyncGroup(groupItems);
		}
	}

	public void SetSyncGroup(IQueryData[] syncGroup)
	{
		_syncGroup = syncGroup;
	}
}
