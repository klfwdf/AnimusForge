using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

public class DotNetObject
{
	private struct DotNetObjectReferenceCounter
	{
		internal int ReferenceCount;

		internal long CreationFrame;

		internal DotNetObject DotNetObject;
	}

	private class DotNetObjectKeeper
	{
		internal DotNetObject DotNetObject;

		internal int TimerToReleaseStrongRef;

		internal GCHandle gcHandle;
	}

	private static readonly object Locker;

	private const int DotnetObjectFirstReferencesTickCount = 200;

	private static long _frameNo;

	private static Dictionary<int, DotNetObjectKeeper> DotnetKeepReferences;

	private static readonly Dictionary<int, DotNetObjectReferenceCounter> DotnetObjectReferences;

	private static int _totalCreatedObjectCount;

	private readonly int _objectId;

	private static int _numberOfAliveDotNetObjects;

	internal static int NumberOfAliveDotNetObjects => _numberOfAliveDotNetObjects;

	static DotNetObject()
	{
		Locker = new object();
		DotnetObjectReferences = new Dictionary<int, DotNetObjectReferenceCounter>();
		DotnetKeepReferences = new Dictionary<int, DotNetObjectKeeper>();
	}

	protected DotNetObject()
	{
		lock (Locker)
		{
			_totalCreatedObjectCount++;
			_objectId = _totalCreatedObjectCount;
			DotNetObjectReferenceCounter value = new DotNetObjectReferenceCounter
			{
				DotNetObject = this,
				ReferenceCount = 0,
				CreationFrame = _frameNo
			};
			DotnetObjectReferences.Add(_objectId, value);
			DotNetObjectKeeper value2 = new DotNetObjectKeeper
			{
				DotNetObject = this,
				TimerToReleaseStrongRef = 200,
				gcHandle = GCHandle.Alloc(this, GCHandleType.Normal)
			};
			DotnetKeepReferences.Add(_objectId, value2);
			_numberOfAliveDotNetObjects++;
		}
	}

	~DotNetObject()
	{
		lock (Locker)
		{
			_numberOfAliveDotNetObjects--;
		}
	}

	[LibraryCallback(null, false)]
	internal static int GetAliveDotNetObjectCount()
	{
		return _numberOfAliveDotNetObjects;
	}

	[LibraryCallback(null, false)]
	internal static void IncreaseReferenceCount(int dotnetObjectId)
	{
		lock (Locker)
		{
			if (DotnetObjectReferences.ContainsKey(dotnetObjectId))
			{
				DotNetObjectReferenceCounter value = DotnetObjectReferences[dotnetObjectId];
				value.ReferenceCount++;
				DotnetObjectReferences[dotnetObjectId] = value;
			}
		}
	}

	[LibraryCallback(null, false)]
	internal static void DecreaseReferenceCount(int dotnetObjectId)
	{
		lock (Locker)
		{
			DotNetObjectReferenceCounter value = DotnetObjectReferences[dotnetObjectId];
			value.ReferenceCount--;
			if (value.ReferenceCount == 0)
			{
				DotnetObjectReferences.Remove(dotnetObjectId);
				if (DotnetKeepReferences.TryGetValue(dotnetObjectId, out var value2))
				{
					value2.gcHandle.Free();
					DotnetKeepReferences.Remove(dotnetObjectId);
				}
			}
			else
			{
				DotnetObjectReferences[dotnetObjectId] = value;
			}
		}
	}

	internal static DotNetObject GetManagedObjectWithId(int dotnetObjectId)
	{
		lock (Locker)
		{
			if (DotnetObjectReferences.TryGetValue(dotnetObjectId, out var value))
			{
				return value.DotNetObject;
			}
			if (dotnetObjectId == 0)
			{
				return null;
			}
			return new DotNetObject();
		}
	}

	internal int GetManagedId()
	{
		return _objectId;
	}

	[LibraryCallback(null, false)]
	internal static string GetAliveDotNetObjectNames()
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "GetAliveDotNetObjectNames");
		lock (Locker)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (DotNetObjectReferenceCounter value in DotnetObjectReferences.Values)
			{
				Type type = value.DotNetObject.GetType();
				if (!dictionary.ContainsKey(type.Name))
				{
					dictionary.Add(type.Name, 1);
				}
				else
				{
					dictionary[type.Name] = dictionary[type.Name] + 1;
				}
			}
			foreach (string key in dictionary.Keys)
			{
				mBStringBuilder.Append(key + "," + dictionary[key] + "-");
			}
		}
		return mBStringBuilder.ToStringAndRelease();
	}

	internal static void HandleDotNetObjects()
	{
		lock (Locker)
		{
			_frameNo++;
			List<int> list = new List<int>();
			foreach (KeyValuePair<int, DotNetObjectKeeper> dotnetKeepReference in DotnetKeepReferences)
			{
				dotnetKeepReference.Value.TimerToReleaseStrongRef--;
				if (dotnetKeepReference.Value.TimerToReleaseStrongRef == 0)
				{
					dotnetKeepReference.Value.gcHandle.Free();
					list.Add(dotnetKeepReference.Key);
				}
			}
			foreach (int item in list)
			{
				DotnetKeepReferences.Remove(item);
			}
		}
	}
}
