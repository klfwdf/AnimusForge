using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Load;

internal class LoadCallbackInitializator
{
	private ObjectHeaderLoadData[] _objectHeaderLoadDatas;

	private int _objectCount;

	private LoadData _loadData;

	private Dictionary<int, ObjectLoadData> _objectLoadDatas;

	public LoadCallbackInitializator(LoadData loadData, ObjectHeaderLoadData[] objectHeaderLoadDatas, int objectCount)
	{
		_loadData = loadData;
		_objectHeaderLoadDatas = objectHeaderLoadDatas;
		_objectCount = objectCount;
		_objectLoadDatas = new Dictionary<int, ObjectLoadData>();
	}

	public void InitializeObjects()
	{
		using (new PerformanceTestBlock("LoadContext::Callbacks"))
		{
			for (int i = 0; i < _objectCount; i++)
			{
				ObjectHeaderLoadData objectHeaderLoadData = _objectHeaderLoadDatas[i];
				if (objectHeaderLoadData.Target == null)
				{
					continue;
				}
				IEnumerable<MethodInfo> enumerable = objectHeaderLoadData.TypeDefinition?.InitializationCallbacks;
				if (enumerable == null)
				{
					continue;
				}
				foreach (MethodInfo item in enumerable)
				{
					ParameterInfo[] parameters = item.GetParameters();
					if (parameters.Length > 1 && parameters[1].ParameterType == typeof(ObjectLoadData))
					{
						ObjectLoadData objectLoadData = GetObjectLoadData(objectHeaderLoadData, i);
						item.Invoke(objectHeaderLoadData.Target, new object[2] { _loadData.MetaData, objectLoadData });
					}
					else if (parameters.Length == 1)
					{
						item.Invoke(objectHeaderLoadData.Target, new object[1] { _loadData.MetaData });
					}
					else
					{
						item.Invoke(objectHeaderLoadData.Target, null);
					}
				}
			}
		}
		GC.Collect();
	}

	public void AfterInitializeObjects()
	{
		using (new PerformanceTestBlock("LoadContext::AfterCallbacks"))
		{
			for (int i = 0; i < _objectCount; i++)
			{
				ObjectHeaderLoadData objectHeaderLoadData = _objectHeaderLoadDatas[i];
				if (objectHeaderLoadData.Target == null)
				{
					continue;
				}
				IEnumerable<MethodInfo> enumerable = objectHeaderLoadData.TypeDefinition?.LateInitializationCallbacks;
				if (enumerable == null)
				{
					continue;
				}
				foreach (MethodInfo item in enumerable)
				{
					ParameterInfo[] parameters = item.GetParameters();
					if (parameters.Length > 1 && parameters[1].ParameterType == typeof(ObjectLoadData))
					{
						ObjectLoadData objectLoadData = GetObjectLoadData(objectHeaderLoadData, i);
						item.Invoke(objectHeaderLoadData.Target, new object[2] { _loadData.MetaData, objectLoadData });
					}
					else if (parameters.Length == 1)
					{
						item.Invoke(objectHeaderLoadData.Target, new object[1] { _loadData.MetaData });
					}
					else
					{
						item.Invoke(objectHeaderLoadData.Target, null);
					}
				}
			}
		}
		_objectLoadDatas.Clear();
		GC.Collect();
	}

	private ObjectLoadData GetObjectLoadData(ObjectHeaderLoadData objectHeaderLoadData, int i)
	{
		if (!_objectLoadDatas.TryGetValue(i, out var value))
		{
			value = LoadContext.CreateLoadData(_loadData, i, objectHeaderLoadData);
			_objectLoadDatas[i] = value;
		}
		return value;
	}
}
