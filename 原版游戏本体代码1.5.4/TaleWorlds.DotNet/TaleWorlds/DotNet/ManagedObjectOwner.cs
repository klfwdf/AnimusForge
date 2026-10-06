using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

internal class ManagedObjectOwner
{
	private const int PooledManagedObjectOwnerCount = 8192;

	private static readonly List<ManagedObjectOwner> _pool;

	private static readonly List<WeakReference> _managedObjectOwnerWeakReferences;

	private static readonly Dictionary<int, WeakReference> _managedObjectOwners;

	private static readonly HashSet<ManagedObjectOwner> _managedObjectOwnerReferences;

	private static int _lastId;

	private static readonly List<ManagedObjectOwner> _lastframedeletedManagedObjects;

	private static int _numberOfAliveManagedObjects;

	private static readonly List<ManagedObjectOwner> _lastframedeletedManagedObjectBuffer;

	private Type _typeInfo;

	private int _nativeId;

	private UIntPtr _ptr;

	private readonly WeakReference _managedObject;

	private readonly WeakReference _managedObjectLongReference;

	internal static int NumberOfAliveManagedObjects => _numberOfAliveManagedObjects;

	internal int NativeId => _nativeId;

	internal UIntPtr Pointer
	{
		get
		{
			return _ptr;
		}
		set
		{
			if (value != UIntPtr.Zero)
			{
				LibraryApplicationInterface.IManaged.IncreaseReferenceCount(value);
			}
			_ptr = value;
		}
	}

	static ManagedObjectOwner()
	{
		_lastId = 10;
		_numberOfAliveManagedObjects = 0;
		_managedObjectOwners = new Dictionary<int, WeakReference>();
		_lastframedeletedManagedObjects = new List<ManagedObjectOwner>();
		_managedObjectOwnerReferences = new HashSet<ManagedObjectOwner>();
		_lastframedeletedManagedObjectBuffer = new List<ManagedObjectOwner>(1024);
		_pool = new List<ManagedObjectOwner>(8192);
		_managedObjectOwnerWeakReferences = new List<WeakReference>(8192);
		for (int i = 0; i < 8192; i++)
		{
			ManagedObjectOwner item = new ManagedObjectOwner();
			_pool.Add(item);
			WeakReference item2 = new WeakReference(null);
			_managedObjectOwnerWeakReferences.Add(item2);
		}
	}

	internal static void GarbageCollect()
	{
		lock (_managedObjectOwnerReferences)
		{
			_lastframedeletedManagedObjectBuffer.AddRange(_lastframedeletedManagedObjects);
			_lastframedeletedManagedObjects.Clear();
			foreach (ManagedObjectOwner item in _lastframedeletedManagedObjectBuffer)
			{
				if (item._ptr != UIntPtr.Zero)
				{
					LibraryApplicationInterface.IManaged.ReleaseManagedObject(item._ptr);
					item._ptr = UIntPtr.Zero;
				}
				_numberOfAliveManagedObjects--;
				WeakReference weakReference = _managedObjectOwners[item.NativeId];
				_managedObjectOwners.Remove(item.NativeId);
				weakReference.Target = null;
				_managedObjectOwnerWeakReferences.Add(weakReference);
			}
		}
		foreach (ManagedObjectOwner item2 in _lastframedeletedManagedObjectBuffer)
		{
			item2.Destruct();
			_pool.Add(item2);
		}
		_lastframedeletedManagedObjectBuffer.Clear();
	}

	internal static void LogFinalize()
	{
		Debug.Print("Checking if any managed object still lives...");
		int num = 0;
		lock (_managedObjectOwnerReferences)
		{
			foreach (KeyValuePair<int, WeakReference> managedObjectOwner in _managedObjectOwners)
			{
				if (managedObjectOwner.Value.Target != null)
				{
					Debug.Print("An object with type of " + managedObjectOwner.Value.Target.GetType().Name + " still lives");
					num++;
				}
			}
		}
		if (num == 0)
		{
			Debug.Print("There are no living managed objects.");
		}
		else
		{
			Debug.Print("There are " + num + " living managed objects.");
		}
	}

	internal static void PreFinalizeManagedObjects()
	{
		GarbageCollect();
	}

	internal static ManagedObject GetManagedObjectWithId(int id)
	{
		if (id == 0)
		{
			return null;
		}
		lock (_managedObjectOwnerReferences)
		{
			if (_managedObjectOwners[id].Target is ManagedObjectOwner managedObjectOwner)
			{
				ManagedObject managedObject = managedObjectOwner.TryGetManagedObject();
				ManagedObject.ManagedObjectFetched(managedObject);
				return managedObject;
			}
		}
		return null;
	}

	internal static void ManagedObjectGarbageCollected(ManagedObjectOwner owner, ManagedObject managedObject)
	{
		lock (_managedObjectOwnerReferences)
		{
			if (owner != null && owner._managedObjectLongReference.Target as ManagedObject == managedObject)
			{
				_lastframedeletedManagedObjects.Add(owner);
				_managedObjectOwnerReferences.Remove(owner);
			}
		}
	}

	internal static ManagedObjectOwner CreateManagedObjectOwner(UIntPtr ptr, ManagedObject managedObject)
	{
		ManagedObjectOwner managedObjectOwner = null;
		if (_pool.Count > 0)
		{
			managedObjectOwner = _pool[_pool.Count - 1];
			_pool.RemoveAt(_pool.Count - 1);
		}
		else
		{
			managedObjectOwner = new ManagedObjectOwner();
		}
		managedObjectOwner.Construct(ptr, managedObject);
		lock (_managedObjectOwnerReferences)
		{
			_numberOfAliveManagedObjects++;
			_managedObjectOwnerReferences.Add(managedObjectOwner);
			return managedObjectOwner;
		}
	}

	internal static string GetAliveManagedObjectNames()
	{
		string text = "";
		lock (_managedObjectOwnerReferences)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (WeakReference value in _managedObjectOwners.Values)
			{
				ManagedObjectOwner managedObjectOwner = value.Target as ManagedObjectOwner;
				if (!dictionary.ContainsKey(managedObjectOwner._typeInfo.Name))
				{
					dictionary.Add(managedObjectOwner._typeInfo.Name, 1);
				}
				else
				{
					dictionary[managedObjectOwner._typeInfo.Name] = dictionary[managedObjectOwner._typeInfo.Name] + 1;
				}
			}
			foreach (string key in dictionary.Keys)
			{
				text = text + key + "," + dictionary[key] + "-";
			}
			return text;
		}
	}

	internal static string GetAliveManagedObjectCreationCallstacks(string name)
	{
		return "";
	}

	private ManagedObjectOwner()
	{
		_ptr = UIntPtr.Zero;
		_managedObject = new WeakReference(null, trackResurrection: false);
		_managedObjectLongReference = new WeakReference(null, trackResurrection: true);
	}

	private void Construct(UIntPtr ptr, ManagedObject managedObject)
	{
		_typeInfo = managedObject.GetType();
		_managedObject.Target = managedObject;
		_managedObjectLongReference.Target = managedObject;
		Pointer = ptr;
		lock (_managedObjectOwnerReferences)
		{
			_nativeId = _lastId;
			_lastId++;
			WeakReference weakReference = null;
			if (_managedObjectOwnerWeakReferences.Count > 0)
			{
				weakReference = _managedObjectOwnerWeakReferences[_managedObjectOwnerWeakReferences.Count - 1];
				_managedObjectOwnerWeakReferences.RemoveAt(_managedObjectOwnerWeakReferences.Count - 1);
				weakReference.Target = this;
			}
			else
			{
				weakReference = new WeakReference(this);
			}
			_managedObjectOwners.Add(NativeId, weakReference);
		}
	}

	private void Destruct()
	{
		_managedObject.Target = null;
		_managedObjectLongReference.Target = null;
		_typeInfo = null;
		_ptr = UIntPtr.Zero;
		_nativeId = 0;
	}

	~ManagedObjectOwner()
	{
		lock (_managedObjectOwnerReferences)
		{
		}
	}

	private ManagedObject TryGetManagedObject()
	{
		ManagedObject managedObject = null;
		lock (_managedObjectOwnerReferences)
		{
			managedObject = _managedObject.Target as ManagedObject;
			if (managedObject == null)
			{
				managedObject = (ManagedObject)_typeInfo.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(UIntPtr),
					typeof(bool)
				}, null).Invoke(new object[2] { _ptr, false });
				managedObject.SetOwnerManagedObject(this);
				_managedObject.Target = managedObject;
				_managedObjectLongReference.Target = managedObject;
				if (!_managedObjectOwnerReferences.Contains(this))
				{
					_managedObjectOwnerReferences.Add(this);
				}
				_lastframedeletedManagedObjects.Remove(this);
			}
		}
		return managedObject;
	}
}
