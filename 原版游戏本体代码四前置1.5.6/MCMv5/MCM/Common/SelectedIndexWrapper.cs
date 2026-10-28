using System;
using System.Collections.Concurrent;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Common;

public readonly ref struct SelectedIndexWrapper
{
	private delegate int GetSelectedIndexDelegate(object instance);

	private delegate void SetSelectedIndexDelegate(object instance, int value);

	private static readonly ConcurrentDictionary<Type, GetSelectedIndexDelegate?> _getSelectedIndexCache = new ConcurrentDictionary<Type, GetSelectedIndexDelegate>();

	private static readonly ConcurrentDictionary<Type, SetSelectedIndexDelegate?> _setSelectedIndexCache = new ConcurrentDictionary<Type, SetSelectedIndexDelegate>();

	private readonly GetSelectedIndexDelegate? _getSelectedIndex;

	private readonly SetSelectedIndexDelegate? _setSelectedIndex;

	private readonly object? _object;

	public int SelectedIndex
	{
		get
		{
			return _getSelectedIndex?.Invoke(_object) ?? (-1);
		}
		set
		{
			_setSelectedIndex?.Invoke(_object, value);
		}
	}

	public SelectedIndexWrapper(object? @object)
	{
		_object = @object;
		Type type = @object?.GetType();
		_getSelectedIndex = (((object)type != null) ? _getSelectedIndexCache.GetOrAdd(type, (Type t) => AccessTools2.GetPropertyGetterDelegate<GetSelectedIndexDelegate>(t, "SelectedIndex", logErrorInTrace: false)) : null);
		_setSelectedIndex = (((object)type != null) ? _setSelectedIndexCache.GetOrAdd(type, (Type t) => AccessTools2.GetPropertySetterDelegate<SetSelectedIndexDelegate>(t, "SelectedIndex", logErrorInTrace: false)) : null);
	}
}
