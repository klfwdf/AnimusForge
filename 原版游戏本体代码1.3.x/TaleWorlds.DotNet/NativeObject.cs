using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200002B RID: 43
	public abstract class NativeObject
	{
		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000111 RID: 273 RVA: 0x00005157 File Offset: 0x00003357
		// (set) Token: 0x06000112 RID: 274 RVA: 0x0000515F File Offset: 0x0000335F
		public UIntPtr Pointer { get; private set; }

		// Token: 0x06000114 RID: 276 RVA: 0x00005170 File Offset: 0x00003370
		internal void Construct(UIntPtr pointer)
		{
			this.Pointer = pointer;
			LibraryApplicationInterface.IManaged.IncreaseReferenceCount(this.Pointer);
			List<NativeObject.NativeObjectKeeper> nativeObjectKeepReferences = NativeObject._nativeObjectKeepReferences;
			lock (nativeObjectKeepReferences)
			{
				NativeObject.NativeObjectKeeper nativeObjectKeeper = new NativeObject.NativeObjectKeeper();
				nativeObjectKeeper.TimerToReleaseStrongRef = 10;
				nativeObjectKeeper.gcHandle = GCHandle.Alloc(this);
				NativeObject._nativeObjectKeepReferences.Add(nativeObjectKeeper);
			}
		}

		// Token: 0x06000115 RID: 277 RVA: 0x000051E8 File Offset: 0x000033E8
		~NativeObject()
		{
			if (!this._manualInvalidated)
			{
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(this.Pointer);
			}
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00005228 File Offset: 0x00003428
		public void ManualInvalidate()
		{
			if (!this._manualInvalidated)
			{
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(this.Pointer);
				this._manualInvalidated = true;
			}
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000524C File Offset: 0x0000344C
		static NativeObject()
		{
			int classTypeDefinitionCount = LibraryApplicationInterface.IManaged.GetClassTypeDefinitionCount();
			NativeObject._typeDefinitions = new List<EngineClassTypeDefinition>();
			NativeObject._constructors = new List<ConstructorInfo>();
			for (int i = 0; i < classTypeDefinitionCount; i++)
			{
				EngineClassTypeDefinition item = default(EngineClassTypeDefinition);
				LibraryApplicationInterface.IManaged.GetClassTypeDefinition(i, ref item);
				NativeObject._typeDefinitions.Add(item);
				NativeObject._constructors.Add(null);
			}
			List<Type> list = new List<Type>();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					if (NativeObject.DoesNativeObjectDefinedAssembly(assembly))
					{
						foreach (Type type in assembly.GetTypes())
						{
							if (type.GetCustomAttributesSafe(typeof(EngineClass), false).Length == 1)
							{
								list.Add(type);
							}
						}
					}
				}
				catch (Exception)
				{
				}
			}
			foreach (Type type2 in list)
			{
				EngineClass engineClass = (EngineClass)type2.GetCustomAttributesSafe(typeof(EngineClass), false)[0];
				if (!type2.IsAbstract)
				{
					ConstructorInfo constructor = type2.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(UIntPtr) }, null);
					int typeDefinitionId = NativeObject.GetTypeDefinitionId(engineClass.EngineType);
					if (typeDefinitionId != -1)
					{
						NativeObject._constructors[typeDefinitionId] = constructor;
					}
				}
			}
			NativeObject._nativeObjectKeepReferences = new List<NativeObject.NativeObjectKeeper>();
		}

		// Token: 0x06000118 RID: 280 RVA: 0x000053E4 File Offset: 0x000035E4
		internal static void HandleNativeObjects()
		{
			List<NativeObject.NativeObjectKeeper> nativeObjectKeepReferences = NativeObject._nativeObjectKeepReferences;
			lock (nativeObjectKeepReferences)
			{
				List<int> list = new List<int>();
				for (int i = 0; i < NativeObject._nativeObjectKeepReferences.Count; i++)
				{
					NativeObject.NativeObjectKeeper nativeObjectKeeper = NativeObject._nativeObjectKeepReferences[i];
					nativeObjectKeeper.TimerToReleaseStrongRef--;
					if (nativeObjectKeeper.TimerToReleaseStrongRef == 0)
					{
						nativeObjectKeeper.gcHandle.Free();
						list.Add(i);
					}
				}
				for (int j = list.Count - 1; j >= 0; j--)
				{
					int index = list[j];
					NativeObject._nativeObjectKeepReferences[index] = NativeObject._nativeObjectKeepReferences[NativeObject._nativeObjectKeepReferences.Count - 1];
					NativeObject._nativeObjectKeepReferences.RemoveAt(NativeObject._nativeObjectKeepReferences.Count - 1);
				}
			}
		}

		// Token: 0x06000119 RID: 281 RVA: 0x000054CC File Offset: 0x000036CC
		[LibraryCallback(null, false)]
		internal static int GetAliveNativeObjectCount()
		{
			return NativeObject._numberOfAliveNativeObjects;
		}

		// Token: 0x0600011A RID: 282 RVA: 0x000054D5 File Offset: 0x000036D5
		[LibraryCallback(null, false)]
		internal static string GetAliveNativeObjectNames()
		{
			return "";
		}

		// Token: 0x0600011B RID: 283 RVA: 0x000054DC File Offset: 0x000036DC
		private static int GetTypeDefinitionId(string typeName)
		{
			foreach (EngineClassTypeDefinition engineClassTypeDefinition in NativeObject._typeDefinitions)
			{
				if (engineClassTypeDefinition.TypeName == typeName)
				{
					return engineClassTypeDefinition.TypeId;
				}
			}
			return -1;
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00005544 File Offset: 0x00003744
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

		// Token: 0x0600011D RID: 285 RVA: 0x000055B1 File Offset: 0x000037B1
		[Obsolete]
		protected void AddUnmanagedMemoryPressure(int size)
		{
		}

		// Token: 0x0600011E RID: 286 RVA: 0x000055B4 File Offset: 0x000037B4
		internal static NativeObject CreateNativeObjectWrapper(NativeObjectPointer nativeObjectPointer)
		{
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				ConstructorInfo constructorInfo = NativeObject._constructors[nativeObjectPointer.TypeId];
				if (constructorInfo != null)
				{
					return (NativeObject)constructorInfo.Invoke(new object[] { nativeObjectPointer.Pointer });
				}
			}
			return null;
		}

		// Token: 0x0600011F RID: 287 RVA: 0x0000560E File Offset: 0x0000380E
		internal static T CreateNativeObjectWrapper<T>(NativeObjectPointer nativeObjectPointer) where T : NativeObject
		{
			return (T)((object)NativeObject.CreateNativeObjectWrapper(nativeObjectPointer));
		}

		// Token: 0x06000120 RID: 288 RVA: 0x0000561C File Offset: 0x0000381C
		public override int GetHashCode()
		{
			return this.Pointer.GetHashCode();
		}

		// Token: 0x06000121 RID: 289 RVA: 0x00005637 File Offset: 0x00003837
		public override bool Equals(object obj)
		{
			return obj != null && !(obj.GetType() != base.GetType()) && ((NativeObject)obj).Pointer == this.Pointer;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00005669 File Offset: 0x00003869
		public static bool operator ==(NativeObject a, NativeObject b)
		{
			return a == b || (a != null && b != null && a.Equals(b));
		}

		// Token: 0x06000123 RID: 291 RVA: 0x00005680 File Offset: 0x00003880
		public static bool operator !=(NativeObject a, NativeObject b)
		{
			return !(a == b);
		}

		// Token: 0x04000070 RID: 112
		private const int NativeObjectFirstReferencesTickCount = 10;

		// Token: 0x04000071 RID: 113
		private static List<EngineClassTypeDefinition> _typeDefinitions;

		// Token: 0x04000072 RID: 114
		private static List<ConstructorInfo> _constructors;

		// Token: 0x04000073 RID: 115
		private static List<NativeObject.NativeObjectKeeper> _nativeObjectKeepReferences;

		// Token: 0x04000074 RID: 116
		private static volatile int _numberOfAliveNativeObjects;

		// Token: 0x04000075 RID: 117
		private bool _manualInvalidated;

		// Token: 0x02000045 RID: 69
		private class NativeObjectKeeper
		{
			// Token: 0x040000C4 RID: 196
			internal int TimerToReleaseStrongRef;

			// Token: 0x040000C5 RID: 197
			internal GCHandle gcHandle;
		}
	}
}
