using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaleWorlds.DotNet;

public abstract class ManagedObject
{
	private class ManagedObjectKeeper
	{
		internal int TimerToReleaseStrongRef;

		internal GCHandle gcHandle;
	}

	private const int ManagedObjectFirstReferencesTickCount = 200;

	private static Dictionary<int, ManagedObjectKeeper> _managedObjectKeepReferences;

	private static int _totalCreatedObjectCount;

	private ManagedObjectOwner _managedObjectOwner;

	private int forcedMemory;

	internal ManagedObjectOwner ManagedObjectOwner => _managedObjectOwner;

	internal UIntPtr Pointer
	{
		get
		{
			return _managedObjectOwner.Pointer;
		}
		set
		{
			_managedObjectOwner.Pointer = value;
		}
	}

	internal static void FinalizeManagedObjects()
	{
		lock (_managedObjectKeepReferences)
		{
			_managedObjectKeepReferences.Clear();
		}
	}

	protected void AddUnmanagedMemoryPressure(int size)
	{
		GC.AddMemoryPressure(size);
		forcedMemory = size;
	}

	static ManagedObject()
	{
		_managedObjectKeepReferences = new Dictionary<int, ManagedObjectKeeper>();
	}

	protected ManagedObject(UIntPtr ptr, bool createManagedObjectOwner)
	{
		if (createManagedObjectOwner)
		{
			SetOwnerManagedObject(ManagedObjectOwner.CreateManagedObjectOwner(ptr, this));
		}
		Initialize();
	}

	internal void SetOwnerManagedObject(ManagedObjectOwner owner)
	{
		_managedObjectOwner = owner;
	}

	private void Initialize()
	{
		ManagedObjectFetched(this);
	}

	~ManagedObject()
	{
		if (forcedMemory > 0)
		{
			GC.RemoveMemoryPressure(forcedMemory);
		}
		ManagedObjectOwner.ManagedObjectGarbageCollected(_managedObjectOwner, this);
		_managedObjectOwner = null;
	}

	internal static void HandleManagedObjects()
	{
		lock (_managedObjectKeepReferences)
		{
			List<int> list = new List<int>();
			foreach (KeyValuePair<int, ManagedObjectKeeper> managedObjectKeepReference in _managedObjectKeepReferences)
			{
				managedObjectKeepReference.Value.TimerToReleaseStrongRef--;
				if (managedObjectKeepReference.Value.TimerToReleaseStrongRef == 0)
				{
					managedObjectKeepReference.Value.gcHandle.Free();
					list.Add(managedObjectKeepReference.Key);
				}
			}
			foreach (int item in list)
			{
				_managedObjectKeepReferences.Remove(item);
			}
		}
	}

	internal static void ManagedObjectFetched(ManagedObject managedObject)
	{
		lock (_managedObjectKeepReferences)
		{
			if (!Managed.Closing)
			{
				_totalCreatedObjectCount++;
				ManagedObjectKeeper managedObjectKeeper = new ManagedObjectKeeper();
				managedObjectKeeper.gcHandle = GCHandle.Alloc(managedObject);
				managedObjectKeeper.TimerToReleaseStrongRef = 200;
				_managedObjectKeepReferences.Add(_totalCreatedObjectCount, managedObjectKeeper);
			}
		}
	}

	[LibraryCallback(null, false)]
	internal static int GetAliveManagedObjectCount()
	{
		return ManagedObjectOwner.NumberOfAliveManagedObjects;
	}

	[LibraryCallback(null, false)]
	internal static string GetAliveManagedObjectNames()
	{
		return ManagedObjectOwner.GetAliveManagedObjectNames();
	}

	[LibraryCallback(null, false)]
	internal static string GetCreationCallstack(string name)
	{
		return ManagedObjectOwner.GetAliveManagedObjectCreationCallstacks(name);
	}

	public int GetManagedId()
	{
		return _managedObjectOwner.NativeId;
	}

	[LibraryCallback(null, false)]
	internal string GetClassOfObject()
	{
		return GetType().Name;
	}

	public override int GetHashCode()
	{
		return _managedObjectOwner.NativeId;
	}
}
