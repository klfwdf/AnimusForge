using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

public abstract class NativeObject
{
	private class NativeObjectKeeper
	{
		internal int TimerToReleaseStrongRef;

		internal GCHandle gcHandle;
	}

	private const int NativeObjectFirstReferencesTickCount = 10;

	private static List<EngineClassTypeDefinition> _typeDefinitions;

	private static List<ConstructorInfo> _constructors;

	private static List<NativeObjectKeeper> _nativeObjectKeepReferences;

	private static volatile int _numberOfAliveNativeObjects;

	private bool _manualInvalidated;

	public UIntPtr Pointer { get; private set; }

	internal void Construct(UIntPtr pointer)
	{
		Pointer = pointer;
		LibraryApplicationInterface.IManaged.IncreaseReferenceCount(Pointer);
		lock (_nativeObjectKeepReferences)
		{
			NativeObjectKeeper nativeObjectKeeper = new NativeObjectKeeper();
			nativeObjectKeeper.TimerToReleaseStrongRef = 10;
			nativeObjectKeeper.gcHandle = GCHandle.Alloc(this);
			_nativeObjectKeepReferences.Add(nativeObjectKeeper);
		}
	}

	~NativeObject()
	{
		if (!_manualInvalidated)
		{
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(Pointer);
		}
	}

	public void ManualInvalidate()
	{
		if (!_manualInvalidated)
		{
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(Pointer);
			_manualInvalidated = true;
		}
	}

	static NativeObject()
	{
		int classTypeDefinitionCount = LibraryApplicationInterface.IManaged.GetClassTypeDefinitionCount();
		_typeDefinitions = new List<EngineClassTypeDefinition>();
		_constructors = new List<ConstructorInfo>();
		for (int i = 0; i < classTypeDefinitionCount; i++)
		{
			EngineClassTypeDefinition engineClassTypeDefinition = default(EngineClassTypeDefinition);
			LibraryApplicationInterface.IManaged.GetClassTypeDefinition(i, ref engineClassTypeDefinition);
			_typeDefinitions.Add(engineClassTypeDefinition);
			_constructors.Add(null);
		}
		List<Type> list = new List<Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			try
			{
				if (!DoesNativeObjectDefinedAssembly(assembly))
				{
					continue;
				}
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.GetCustomAttributesSafe(typeof(EngineClass), inherit: false).Length == 1)
					{
						list.Add(type);
					}
				}
			}
			catch (Exception)
			{
			}
		}
		foreach (Type item in list)
		{
			EngineClass engineClass = (EngineClass)item.GetCustomAttributesSafe(typeof(EngineClass), inherit: false)[0];
			if (!item.IsAbstract)
			{
				ConstructorInfo constructor = item.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(UIntPtr) }, null);
				int typeDefinitionId = GetTypeDefinitionId(engineClass.EngineType);
				if (typeDefinitionId != -1)
				{
					_constructors[typeDefinitionId] = constructor;
				}
			}
		}
		_nativeObjectKeepReferences = new List<NativeObjectKeeper>();
	}

	internal static void HandleNativeObjects()
	{
		lock (_nativeObjectKeepReferences)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < _nativeObjectKeepReferences.Count; i++)
			{
				NativeObjectKeeper nativeObjectKeeper = _nativeObjectKeepReferences[i];
				nativeObjectKeeper.TimerToReleaseStrongRef--;
				if (nativeObjectKeeper.TimerToReleaseStrongRef == 0)
				{
					nativeObjectKeeper.gcHandle.Free();
					list.Add(i);
				}
			}
			for (int num = list.Count - 1; num >= 0; num--)
			{
				int index = list[num];
				_nativeObjectKeepReferences[index] = _nativeObjectKeepReferences[_nativeObjectKeepReferences.Count - 1];
				_nativeObjectKeepReferences.RemoveAt(_nativeObjectKeepReferences.Count - 1);
			}
		}
	}

	[LibraryCallback(null, false)]
	internal static int GetAliveNativeObjectCount()
	{
		return _numberOfAliveNativeObjects;
	}

	[LibraryCallback(null, false)]
	internal static string GetAliveNativeObjectNames()
	{
		return "";
	}

	private static int GetTypeDefinitionId(string typeName)
	{
		foreach (EngineClassTypeDefinition typeDefinition in _typeDefinitions)
		{
			if (typeDefinition.TypeName == typeName)
			{
				return typeDefinition.TypeId;
			}
		}
		return -1;
	}

	private static bool DoesNativeObjectDefinedAssembly(Assembly assembly)
	{
		if (typeof(NativeObject).Assembly.FullName == assembly.FullName)
		{
			return true;
		}
		AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
		string fullName = typeof(NativeObject).Assembly.FullName;
		AssemblyName[] array = referencedAssemblies;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].FullName == fullName)
			{
				return true;
			}
		}
		return false;
	}

	[Obsolete]
	protected void AddUnmanagedMemoryPressure(int size)
	{
	}

	internal static NativeObject CreateNativeObjectWrapper(NativeObjectPointer nativeObjectPointer)
	{
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			ConstructorInfo constructorInfo = _constructors[nativeObjectPointer.TypeId];
			if (constructorInfo != null)
			{
				return (NativeObject)constructorInfo.Invoke(new object[1] { nativeObjectPointer.Pointer });
			}
		}
		return null;
	}

	internal static T CreateNativeObjectWrapper<T>(NativeObjectPointer nativeObjectPointer) where T : NativeObject
	{
		return (T)CreateNativeObjectWrapper(nativeObjectPointer);
	}

	public override int GetHashCode()
	{
		return Pointer.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return ((NativeObject)obj).Pointer == Pointer;
	}

	public static bool operator ==(NativeObject a, NativeObject b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		return a.Equals(b);
	}

	public static bool operator !=(NativeObject a, NativeObject b)
	{
		return !(a == b);
	}
}
